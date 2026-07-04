# 热更代码迁入 Addressables 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 HybridCLR 热更 DLL 从 `StreamingAssets/GameHotUpdate` 迁移到 Addressables，支持首包 Local 离线启动与远程 OTA 更新 `Game.Runtime.dll`（重启生效），且 Agent/CI 在无 CDN 环境下可跑。

**Architecture:** 启动前新增 `PreBootstrapAddressablesAsync()`（幂等 Initialize + 条件 Catalog 更新）；Player 侧用 `AddressablesAotMetadataLoader` / `AddressablesHotUpdateAssemblyLoader` 通过 Label/Address 加载 `TextAsset.bytes`；Editor 仍复用 AppDomain 内 `Game.Runtime`；AOT 元数据进 Static Local Group，主热更 DLL 进 Can Change Remote Group（首包 IncludeInBuild）。

**Tech Stack:** Unity 2023.2、HybridCLR、Addressables、UniTask、GameFramework / Game.Runtime 程序集拆分

**Spec:** [`docs/superpowers/specs/2026-07-03-hotupdate-addressables-design.md`](../specs/2026-07-03-hotupdate-addressables-design.md)

## Global Constraints

- Address 格式必须为 `Assets/...` 全路径
- 热更 DLL 加载不得经过 `ResourceComponent`（直接 `Addressables.*`）
- PreBootstrap 阶段不得依赖 `GameEntry.FrameSyncTransport` 解析 CDN URL
- 代码 OTA 需重启 App 后生效（HybridCLR 同会话不重载热更程序集）
- Catalog 更新失败/超时不阻断启动，回退 Local catalog
- `ClientAgentGameEntryMode.IsRunning` 跳过 Catalog 检查
- `Application.isBatchMode` 且 `SkipCatalogCheckInBatchMode=true` 时跳过 Catalog 检查
- `ForceLocalOnly=true` 时跳过 Catalog 检查
- 热更 DLL 不走 `AssetsPathHelper` 业务契约
- 新增公共类型需中文 XML 注释
- 废弃 `StreamingAssets/GameHotUpdate`（迁移验证通过后删除）

---

## File Map

| 文件 | 职责 |
|------|------|
| `Assets/HotUpdate/Code/AOT/` | AOT 元数据 `.dll.bytes` 资产目录 |
| `Assets/HotUpdate/Code/Game.Runtime.dll.bytes` | 热更主程序集资产 |
| `Assets/Scripts/RunTime/Helper/AddressablesGroupResolver.cs` | 路径 → Group（含 HotUpdate 规则） |
| `Assets/Scripts/GameFramework/AddressablesContentSettings.cs` | 扩展 Catalog/CI 开关 |
| `Assets/Scripts/GameFramework/Component/AddressablesBootstrapComponent.cs` | 幂等 Init + Catalog 策略 |
| `Assets/Scripts/GameFramework/Bootstrap/HotUpdate/GameHotUpdateEntryOptions.cs` | Address/Label 配置 |
| `Assets/Scripts/GameFramework/Bootstrap/HotUpdate/GameHotUpdateEntryComponent.cs` | 场景序列化字段 |
| `Assets/Scripts/GameFramework/Bootstrap/HotUpdate/AddressablesAotMetadataLoader.cs` | **新增** Player AOT 加载 |
| `Assets/Scripts/GameFramework/Bootstrap/HotUpdate/AddressablesHotUpdateAssemblyLoader.cs` | **新增** Player 热更 DLL 加载 |
| `Assets/Scripts/GameFramework/Bootstrap/HotUpdate/GameHotUpdateEntryFactory.cs` | Player 切换新 Loader |
| `Assets/Scripts/GameFramework/GameEntry.cs` | PreBootstrap 插入点 |
| `Assets/Scripts/GameFramework/GameEntry.Component.cs` | PreBootstrap 实现 + 幂等 Init |
| `Assets/Scripts/Editor/Addressables/RuntimeAddressablesConfigurator.cs` | 新 Group + 扫描路径 |
| `Assets/Scripts/Editor/HybridCLR/HybridCLRDllCopyTool.cs` | 拷贝目标改为 HotUpdate/Code |
| `Assets/Scene/Launcher.unity` | 更新 GameHotUpdateEntryComponent 字段 |
| `Assets/Scripts/Test/EditMode/HotUpdateAddressablesResolverTests.cs` | **新增** Group 路径测试 |

**删除（Task 8 完成后）：**

- `HybridClrAotMetadataLoader.cs`
- `HybridClrHotUpdateAssemblyLoader.cs`
- `GameHotUpdatePathUtility.cs`（若无 Editor 引用）

---

### Task 1: HotUpdate 资产目录与 Group 解析规则

**Files:**
- Create: `Assets/HotUpdate/Code/AOT/.gitkeep`（或首次拷贝后由工具生成）
- Modify: `Assets/Scripts/RunTime/Helper/AddressablesGroupResolver.cs`
- Create: `Assets/Scripts/Test/EditMode/HotUpdateAddressablesResolverTests.cs`

**Interfaces:**
- Produces: `AddressablesGroupResolver.HotUpdateCodeLocal = "HotUpdate-Code-Local"`
- Produces: `AddressablesGroupResolver.HotUpdateCodeRemote = "HotUpdate-Code-Remote"`
- Produces: `ResolveGroupName()` 识别 `Assets/HotUpdate/Code/AOT/` 与 `Game.Runtime.dll.bytes`

- [ ] **Step 1: 写失败测试**

```csharp
// Assets/Scripts/Test/EditMode/HotUpdateAddressablesResolverTests.cs
using NUnit.Framework;

public class HotUpdateAddressablesResolverTests
{
    [Test]
    public void ResolveGroupName_AotMetadata_ReturnsHotUpdateCodeLocal()
    {
        Assert.AreEqual(
            AddressablesGroupResolver.HotUpdateCodeLocal,
            AddressablesGroupResolver.ResolveGroupName(
                "Assets/HotUpdate/Code/AOT/mscorlib.dll.bytes"));
    }

    [Test]
    public void ResolveGroupName_RuntimeDll_ReturnsHotUpdateCodeRemote()
    {
        Assert.AreEqual(
            AddressablesGroupResolver.HotUpdateCodeRemote,
            AddressablesGroupResolver.ResolveGroupName(
                "Assets/HotUpdate/Code/Game.Runtime.dll.bytes"));
    }

    [Test]
    public void ResolveGroupName_Manifest_ReturnsHotUpdateCodeRemote()
    {
        Assert.AreEqual(
            AddressablesGroupResolver.HotUpdateCodeRemote,
            AddressablesGroupResolver.ResolveGroupName(
                "Assets/HotUpdate/Code/manifest.json"));
    }
}
```

- [ ] **Step 2: 运行测试确认 FAIL**

Run: Unity EditMode `HotUpdateAddressablesResolverTests`  
Expected: FAIL（常量或路径规则不存在）

- [ ] **Step 3: 实现 Resolver 扩展**

```csharp
// 在 AddressablesGroupResolver 中追加：
public const string HotUpdateCodeLocal = "HotUpdate-Code-Local";
public const string HotUpdateCodeRemote = "HotUpdate-Code-Remote";

// ResolveGroupName 开头（Config 之前）插入：
if (assetPath.StartsWith("Assets/HotUpdate/Code/AOT/", StringComparison.Ordinal))
{
    return HotUpdateCodeLocal;
}

if (assetPath.StartsWith("Assets/HotUpdate/Code/", StringComparison.Ordinal))
{
    return HotUpdateCodeRemote;
}
```

- [ ] **Step 4: 运行测试确认 PASS**

- [ ] **Step 5: 创建目录**

```bash
mkdir -p Assets/HotUpdate/Code/AOT
```

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/RunTime/Helper/AddressablesGroupResolver.cs \
        Assets/Scripts/Test/EditMode/HotUpdateAddressablesResolverTests.cs \
        Assets/HotUpdate/Code/
git commit -m "feat(hotupdate): add Addressables group resolver rules for hot-update code"
```

---

### Task 2: AddressablesContentSettings 扩展

**Files:**
- Modify: `Assets/Scripts/GameFramework/AddressablesContentSettings.cs`
- Modify: `Assets/GameAssetConfig/AddressablesContentSettings.asset`（新增字段默认值）

**Interfaces:**
- Produces: `bool EnableHotUpdateCatalogCheck { get; }` 默认 `true`
- Produces: `bool SkipCatalogCheckInBatchMode { get; }` 默认 `true`

- [ ] **Step 1: 扩展 ScriptableObject**

```csharp
[Tooltip("PreBootstrap 阶段是否检查热更/资源 Catalog 更新")]
public bool EnableHotUpdateCatalogCheck = true;

[Tooltip("batchmode（CI）下跳过 Catalog 检查")]
public bool SkipCatalogCheckInBatchMode = true;
```

- [ ] **Step 2: 在 Launcher 场景引用的 asset 中保存默认值**（Unity Inspector 确认序列化）

- [ ] **Step 3: Commit**

```bash
git commit -m "feat(addressables): add hot-update catalog check settings"
```

---

### Task 3: AddressablesBootstrapComponent 幂等与 Catalog 策略

**Files:**
- Modify: `Assets/Scripts/GameFramework/Component/AddressablesBootstrapComponent.cs`

**Interfaces:**
- Produces: `UniTask EnsureInitializedAsync(CancellationToken ct = default)` — 幂等 `InitializeAsync`
- Produces: `UniTask PreBootstrapAsync(CancellationToken ct = default)` — Init + 条件 Catalog
- Produces: `bool ShouldSkipCatalogCheck()` — Agent / batchmode / ForceLocalOnly / 无 URL

- [ ] **Step 1: 添加初始化状态字段**

```csharp
private static bool _addressablesInitialized;
```

- [ ] **Step 2: 实现 EnsureInitializedAsync**

```csharp
public async UniTask EnsureInitializedAsync(CancellationToken cancellationToken = default)
{
    ResourceComponent resourceComponent = GameEntryRunTime.GetComponent<ResourceComponent>();
    if (resourceComponent == null
        || resourceComponent.GameResourceMode != ResourceComponent.ResourceMode.Addressables)
    {
        return;
    }

    if (_addressablesInitialized)
    {
        return;
    }

    ApplyRemoteBaseUrl();
    await Addressables.InitializeAsync().ToUniTask(cancellationToken: cancellationToken);
    _addressablesInitialized = true;
}
```

- [ ] **Step 3: 实现 ShouldSkipCatalogCheck**

```csharp
private bool ShouldSkipCatalogCheck()
{
    if (_settings != null && _settings.ForceLocalOnly)
    {
        return true;
    }

    if (ClientAgentGameEntryMode.IsRunning)
    {
        return true;
    }

    if (_settings != null
        && _settings.SkipCatalogCheckInBatchMode
        && Application.isBatchMode)
    {
        return true;
    }

    if (_settings != null && !_settings.EnableHotUpdateCatalogCheck)
    {
        return true;
    }

    if (_settings != null && !_settings.EnableCatalogUpdateOnStartup)
    {
        return true;
    }

    return string.IsNullOrEmpty(ResolveRemoteBaseUrl());
}
```

- [ ] **Step 4: 实现 PreBootstrapAsync**

```csharp
public async UniTask PreBootstrapAsync(CancellationToken cancellationToken = default)
{
    await EnsureInitializedAsync(cancellationToken);

    if (ShouldSkipCatalogCheck())
    {
        GameLog.Info(GameLogChannel.Bootstrap, "PreBootstrap 跳过 Catalog 更新。");
        return;
    }

    await TryUpdateCatalogsAsync(cancellationToken);
}
```

- [ ] **Step 5: 重构 InitializeAsync 复用 EnsureInitializedAsync**

```csharp
public async UniTask InitializeAsync(CancellationToken cancellationToken = default)
{
    await EnsureInitializedAsync(cancellationToken);

    if (ShouldCheckCatalog()) // 保留原有业务资源 Catalog 逻辑，或统一调用 ShouldSkipCatalogCheck
    {
        await TryUpdateCatalogsAsync(cancellationToken);
    }
}
```

将 `ShouldCheckCatalog()` 内部改为调用 `ShouldSkipCatalogCheck()` 取反，避免重复条件。

- [ ] **Step 6: Commit**

```bash
git commit -m "feat(addressables): add PreBootstrap init and offline catalog skip policy"
```

---

### Task 4: GameHotUpdateEntryOptions 与 Component 配置

**Files:**
- Modify: `Assets/Scripts/GameFramework/Bootstrap/HotUpdate/GameHotUpdateEntryOptions.cs`
- Modify: `Assets/Scripts/GameFramework/Bootstrap/HotUpdate/GameHotUpdateEntryComponent.cs`

**Interfaces:**
- Produces: `GameHotUpdateEntryOptions.HotUpdateAssemblyAddress` 默认 `Assets/HotUpdate/Code/Game.Runtime.dll.bytes`
- Produces: `GameHotUpdateEntryOptions.AotMetadataLabel` 默认 `hotupdate-aot`
- Produces: `GameHotUpdateEntryComponent.CreateOptions()` 填充新字段

- [ ] **Step 1: 扩展 Options（保留 HotUpdateAssemblyName 供 Editor）**

```csharp
public GameHotUpdateEntryOptions(
    string hotUpdateAssemblyName,
    string hotUpdateAssemblyAddress,
    string aotMetadataLabel,
    bool autoDiscoverAotAssemblies,
    string[] aotAssemblyNames)
{
    ...
    HotUpdateAssemblyName = hotUpdateAssemblyName;
    HotUpdateAssemblyAddress = hotUpdateAssemblyAddress;
    AotMetadataLabel = aotMetadataLabel;
    ...
}

public string HotUpdateAssemblyAddress { get; }
public string AotMetadataLabel { get; }
```

- [ ] **Step 2: 更新 Component 序列化字段**

```csharp
[SerializeField] private string hotUpdateAssemblyAddress =
    "Assets/HotUpdate/Code/Game.Runtime.dll.bytes";

[SerializeField] private string aotMetadataLabel = "hotupdate-aot";

private GameHotUpdateEntryOptions CreateOptions()
{
    return new GameHotUpdateEntryOptions(
        hotUpdateAssemblyName,
        hotUpdateAssemblyAddress,
        aotMetadataLabel,
        autoDiscoverAotAssemblies,
        aotAssemblyNames);
}
```

移除 `streamingAssetsFolderName` 字段（或 `[Obsolete]` 后删除）。

- [ ] **Step 3: Commit**

```bash
git commit -m "feat(hotupdate): add Addressables address and label options"
```

---

### Task 5: Addressables 热更 Loader 实现

**Files:**
- Create: `Assets/Scripts/GameFramework/Bootstrap/HotUpdate/AddressablesAotMetadataLoader.cs`
- Create: `Assets/Scripts/GameFramework/Bootstrap/HotUpdate/AddressablesHotUpdateAssemblyLoader.cs`

**Interfaces:**
- Consumes: `GameHotUpdateEntryOptions.AotMetadataLabel`, `HotUpdateAssemblyAddress`
- Consumes: `HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly`
- Produces: `AddressablesAotMetadataLoader.LoadAsync() -> UniTask<int>`
- Produces: `AddressablesHotUpdateAssemblyLoader.LoadAsync() -> UniTask<Assembly>`

- [ ] **Step 1: 实现 AddressablesHotUpdateAssemblyLoader**

```csharp
using System;
using System.IO;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Rogue.Bootstrap.HotUpdate
{
    public sealed class AddressablesHotUpdateAssemblyLoader : IGameHotUpdateAssemblyLoader
    {
        public async UniTask<Assembly> LoadAsync(GameHotUpdateEntryOptions options)
        {
            AsyncOperationHandle<TextAsset> handle =
                Addressables.LoadAssetAsync<TextAsset>(options.HotUpdateAssemblyAddress);

            try
            {
                await handle.ToUniTask();
                if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
                {
                    throw new FileNotFoundException(
                        $"Addressables 加载热更程序集失败: {options.HotUpdateAssemblyAddress}, status={handle.Status}");
                }

                byte[] dllBytes = handle.Result.bytes;
                Assembly loadedAssembly = Assembly.Load(dllBytes);
                GameLog.Info(GameLogChannel.Bootstrap, $"已加载热更新程序集: {loadedAssembly.FullName}");
                return loadedAssembly;
            }
            finally
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
        }
    }
}
```

- [ ] **Step 2: 实现 AddressablesAotMetadataLoader**

```csharp
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using HybridCLR;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Rogue.Bootstrap.HotUpdate
{
    public sealed class AddressablesAotMetadataLoader : IGameAotMetadataLoader
    {
        public async UniTask<int> LoadAsync(GameHotUpdateEntryOptions options)
        {
            AsyncOperationHandle<IList<IResourceLocation>> locHandle =
                Addressables.LoadResourceLocationsAsync(
                    options.AotMetadataLabel,
                    typeof(TextAsset));

            await locHandle.ToUniTask();
            if (locHandle.Status != AsyncOperationStatus.Succeeded || locHandle.Result == null)
            {
                if (locHandle.IsValid()) Addressables.Release(locHandle);
                throw new System.IO.FileNotFoundException(
                    $"未找到 AOT 元数据 Label: {options.AotMetadataLabel}");
            }

            int loadedCount = 0;
            IList<IResourceLocation> locations = locHandle.Result;
            Addressables.Release(locHandle);

            for (int i = 0; i < locations.Count; i++)
            {
                AsyncOperationHandle<TextAsset> handle =
                    Addressables.LoadAssetAsync<TextAsset>(locations[i]);

                try
                {
                    await handle.ToUniTask();
                    if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
                    {
                        GameLog.Warn(GameLogChannel.Bootstrap,
                            $"跳过 AOT 元数据: {locations[i].PrimaryKey}, status={handle.Status}");
                        continue;
                    }

                    LoadImageErrorCode errorCode = RuntimeApi.LoadMetadataForAOTAssembly(
                        handle.Result.bytes,
                        HomologousImageMode.SuperSet);

                    GameLog.Info(GameLogChannel.Bootstrap,
                        $"补充 AOT 元数据: {locations[i].PrimaryKey} => {errorCode}");

                    if (errorCode == LoadImageErrorCode.OK)
                    {
                        loadedCount++;
                    }
                }
                finally
                {
                    if (handle.IsValid()) Addressables.Release(handle);
                }
            }

            return loadedCount;
        }
    }
}
```

- [ ] **Step 3: 编译验证**

Run: `dotnet build Assembly-CSharp.csproj -nologo -v:minimal`  
Expected: 0 Error（需 Task 6 切换 Factory 后可完整通过）

- [ ] **Step 4: Commit**

```bash
git commit -m "feat(hotupdate): add Addressables-based HybridCLR loaders"
```

---

### Task 6: Factory 切换与旧 Loader 移除

**Files:**
- Modify: `Assets/Scripts/GameFramework/Bootstrap/HotUpdate/GameHotUpdateEntryFactory.cs`
- Delete: `HybridClrAotMetadataLoader.cs`, `HybridClrHotUpdateAssemblyLoader.cs`, `GameHotUpdatePathUtility.cs`

- [ ] **Step 1: 更新 Factory**

```csharp
#else
            return new GameHotUpdateEntry(
                new AddressablesAotMetadataLoader(),
                new AddressablesHotUpdateAssemblyLoader(),
                usedEditorAssembly: false);
#endif
```

- [ ] **Step 2: 删除旧文件及 .meta**

- [ ] **Step 3: 编译**

Run: `dotnet build Roguelike_Master.sln -nologo`  
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git commit -m "refactor(hotupdate): switch player loaders to Addressables"
```

---

### Task 7: GameEntry 启动顺序调整

**Files:**
- Modify: `Assets/Scripts/GameFramework/GameEntry.cs`
- Modify: `Assets/Scripts/GameFramework/GameEntry.Component.cs`

**Interfaces:**
- Consumes: `AddressablesBootstrapComponent.PreBootstrapAsync()`
- Produces: `GameEntry.PreBootstrapAddressablesAsync()` 私有方法

- [ ] **Step 1: GameEntry.Init 插入 PreBootstrap**

```csharp
private async Cysharp.Threading.Tasks.UniTask Init()
{
    await PreBootstrapAddressablesAsync();
    await LoadHotUpdateAssembliesAsync();
    await InitOptionalComponent();
    HotUpdateBootstrap?.RegisterUiGroups();
    InitService();
}
```

- [ ] **Step 2: GameEntry.Component 实现 PreBootstrapAddressablesAsync**

```csharp
private async Cysharp.Threading.Tasks.UniTask PreBootstrapAddressablesAsync()
{
    AddressablesBootstrapComponent bootstrap =
        GameEntryRunTime.GetComponent<AddressablesBootstrapComponent>();
    if (bootstrap == null)
    {
        GameLog.Warn(GameLogChannel.Bootstrap,
            "未找到 AddressablesBootstrapComponent，PreBootstrap 跳过。");
        return;
    }

    await bootstrap.PreBootstrapAsync();
}
```

- [ ] **Step 3: InitOptionalComponent 中 InitializeAsync 保持不变**（内部已幂等）

- [ ] **Step 4: Commit**

```bash
git commit -m "feat(entry): run Addressables PreBootstrap before hot-update load"
```

---

### Task 8: Editor Addressables 配置器与 HybridCLR 拷贝工具

**Files:**
- Modify: `Assets/Scripts/Editor/Addressables/RuntimeAddressablesConfigurator.cs`
- Modify: `Assets/Scripts/Editor/HybridCLR/HybridCLRDllCopyTool.cs`

**Interfaces:**
- Produces: GroupDefinitions 含 `HotUpdate-Code-Local` / `HotUpdate-Code-Remote`
- Produces: Labels `hotupdate-aot` / `hotupdate-runtime`
- Produces: HybridCLR 拷贝到 `Assets/HotUpdate/Code/`

- [ ] **Step 1: GroupDefinitions 追加**

```csharp
(AddressablesGroupResolver.HotUpdateCodeLocal, "hotupdate-aot", false, true, true,
    BundledAssetGroupSchema.BundlePackingMode.PackTogether),
(AddressablesGroupResolver.HotUpdateCodeRemote, "hotupdate-runtime", true, false, true,
    BundledAssetGroupSchema.BundlePackingMode.PackTogether),
```

说明：Local 组 StaticContent=true；Remote 组 Can Change（StaticContent=false），IncludeInBuild=true 保留首包基线。

- [ ] **Step 2: EnumerateRuntimeAssetPaths 追加**

```csharp
foreach (string path in EnumerateFiles("Assets/HotUpdate/Code", "*.bytes", SearchOption.AllDirectories))
{
    yield return path;
}
foreach (string path in EnumerateFiles("Assets/HotUpdate/Code", "manifest.json", SearchOption.TopDirectoryOnly))
{
    yield return path;
}
```

- [ ] **Step 3: ApplyLabel 为 hotupdate-runtime 组内 AOT 路径除外** — 已在 Resolver 分 Group，各自 Label 自动应用。

- [ ] **Step 4: HybridCLRDllCopyTool 改拷贝目标**

```csharp
private const string HotUpdateAotDir = "Assets/HotUpdate/Code/AOT";
private const string HotUpdateRuntimeDllPath = "Assets/HotUpdate/Code/Game.Runtime.dll.bytes";

// 热更 dll -> HotUpdateRuntimeDllPath
// AOT dll -> HotUpdateAotDir/{name}.dll.bytes
// 菜单文案改为「同步 DLL 到 Addressables 热更目录」
// RunCopy 末尾可选：RuntimeAddressablesConfigurator.SyncRuntimeAssets();
```

- [ ] **Step 5: 菜单项重命名**

```
Tools/HybridCLR/正式入口/同步 DLL 到 Addressables 热更目录
Tools/HybridCLR/正式入口/编译并同步 DLL 到 Addressables 热更目录
```

- [ ] **Step 6: Commit**

```bash
git commit -m "feat(editor): sync hot-update dlls to Addressables asset paths"
```

---

### Task 9: 场景配置与 StreamingAssets 清理

**Files:**
- Modify: `Assets/Scene/Launcher.unity`
- Delete: `Assets/StreamingAssets/GameHotUpdate/`（若存在）

- [ ] **Step 1: 更新 Launcher 中 GameHotUpdateEntryComponent**

移除 `streamingAssetsFolderName`，确保：

```yaml
hotUpdateAssemblyAddress: Assets/HotUpdate/Code/Game.Runtime.dll.bytes
aotMetadataLabel: hotupdate-aot
```

- [ ] **Step 2: 确认 ResourceComponent.GameResourceMode: 0**（Addressables）

- [ ] **Step 3: 执行一次完整 Editor 流程**

1. Tools/HybridCLR/正式入口/编译并同步 DLL 到 Addressables 热更目录  
2. Tools/Addressables/同步运行时资源  
3. Tools/Addressables/构建运行时内容  

- [ ] **Step 4: 删除 `Assets/StreamingAssets/GameHotUpdate`**

- [ ] **Step 5: Commit**

```bash
git commit -m "chore(hotupdate): migrate launcher config and remove StreamingAssets dll folder"
```

---

### Task 10: 文档与 Spec 状态

**Files:**
- Modify: `docs/superpowers/specs/2026-07-03-hotupdate-addressables-design.md`（status → 已确认）
- Modify: `README.md` 热更流程段落（若存在 StreamingAssets 描述）

- [ ] **Step 1: 更新 spec 状态为「已确认」**

- [ ] **Step 2: README 热更步骤改为 Addressables 流程**

- [ ] **Step 3: Commit**

```bash
git commit -m "docs: update hot-update flow for Addressables distribution"
```

---

## 验证清单（Task 11 — 手工 / Agent）

- [ ] **Editor Play**：进入 Launcher，日志含「编辑器复用热更新程序集」，无 Addressables DLL 加载
- [ ] **Player 离线**：断网启动，PreBootstrap 跳过 Catalog，热更 DLL 从 Local 加载，进入 GameStartUp
- [ ] **Agent 模式**：`ClientAgentGameEntryRunner` 日志无 Catalog 网络错误，30s 内 `Enter game succeeded`
- [ ] **CI batchmode**：`-batchmode -runTests -testPlatform EditMode` 通过（含 HotUpdateAddressablesResolverTests）
- [ ] **远程 OTA（可选）**：构建 Content Update，上传 CDN，重启 Player 加载新 Game.Runtime

Run EditMode:

```bash
# Unity -batchmode -runTests -testPlatform EditMode -projectPath D:\UnityProject\lockstep
```

Run 编译:

```bash
dotnet build Roguelike_Master.sln -nologo
dotnet build Server/RogueGameServer/RogueGameServer.csproj -nologo
```

---

## Spec Coverage Self-Review

| Spec 章节 | 对应 Task |
|-----------|-----------|
| §2 启动顺序 | Task 7 |
| §3 Group 设计 | Task 1, 8 |
| §4 Loader | Task 5, 6 |
| §5 离线/Agent | Task 2, 3 |
| §6 Editor 工具 | Task 8 |
| §7 文件清单 | 全计划 |
| §8 manifest | Phase 2 可选，本计划未含（YAGNI） |
| §9 错误处理 | Task 5 Loader 内 |
| §10 测试 | Task 1 测试 + Task 11 清单 |
| §11 迁移 | Task 9 |

无 TBD / 占位符。

---
