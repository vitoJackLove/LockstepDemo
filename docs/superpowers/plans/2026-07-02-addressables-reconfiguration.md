# Addressables 重配置实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 Addressables 从单一 `Runtime Assets` 大包重构为 8 个按加载阶段划分的 Group，支持本地首包 + 远程热更、运行时 CDN 配置、可开关的 Catalog 检查，且不改动 `AssetsPathHelper` 与业务加载路径。

**Architecture:** Editor 侧由 `RuntimeAddressablesConfigurator` 按路径规则同步资产到多 Group 并设置 Local/Remote Schema；运行时由 `AddressablesBootstrapComponent` 在 `DataTable` 之前注入 `AddressablesRemoteBaseUrl`、初始化 Addressables、可选检查 Catalog；`SceneComponent` 在 Addressables 模式下走 `Addressables.LoadSceneAsync`。

**Tech Stack:** Unity 2023.2、Addressables、`Cysharp.Threading.Tasks`、`ResourceComponent`、`AddressablesRuntimeProperties`

**Spec:** [`docs/superpowers/specs/2026-07-02-addressables-reconfiguration-design.md`](../specs/2026-07-02-addressables-reconfiguration-design.md)

## Global Constraints

- Address 必须保持 `Assets/...` 全路径，与 `AssetsPathHelper` 一致
- 不修改 `EntityViewSystem` 的相对 `assetsPath` 约定
- 不引入 `Resources.Load`
- `Remote-DLC` 组本阶段仅预留空组，不实现 DLC UI
- 服务器 URL 下发仅预留 `IAddressablesRemoteUrlProvider`，不修改帧同步协议
- Catalog 检查失败或超时不阻断启动，回退 Local catalog
- Agent 模式（`ClientAgentGameEntryMode.IsRunning`）跳过 Catalog 检查
- Addressables 配置权威目录：`Assets/Scripts/Libraries/AddressableAssetsData/`
- 新增公共类型需中文 XML 注释

---

## File Map

| 文件 | 职责 |
|------|------|
| `Assets/Scripts/RunTime/GameEntry/AddressablesContentSettings.cs` | ScriptableObject：远程 URL、Catalog 开关、超时 |
| `Assets/Scripts/RunTime/GameEntry/IAddressablesRemoteUrlProvider.cs` | 服务器 URL 预留接口 |
| `Assets/Scripts/RunTime/GameEntry/Component/AddressablesBootstrapComponent.cs` | 初始化、注入 URL、Catalog 更新 |
| `Assets/Scripts/RunTime/GameEntry/Component/SceneComponent.cs` | Addressables 场景加载 |
| `Assets/Scripts/RunTime/GameEntry/GameEntry.Component.cs` | 插入 bootstrap 初始化顺序 |
| `Assets/Scripts/Editor/Addressables/RuntimeAddressablesConfigurator.cs` | 多 Group 同步、Schema、构建菜单 |
| `Assets/Scripts/Editor/Addressables/AddressablesGroupResolver.cs` | Editor 纯逻辑：路径 → Group（可单测） |
| `Assets/Scripts/Test/EditMode/AddressablesGroupResolverTests.cs` | EditMode 测试 |
| `Assets/Scripts/Libraries/AddressableAssetsData/**` | Group/Settings/Profile/Labels |

---

### Task 1: 路径 → Group 解析器（Editor 可测）

**Files:**
- Create: `Assets/Scripts/Editor/Addressables/AddressablesGroupResolver.cs`
- Create: `Assets/Scripts/Test/EditMode/AddressablesGroupResolverTests.cs`

**Interfaces:**
- Produces: `AddressablesGroupResolver.ResolveGroupName(string assetPath) -> string?`
- Produces: `AddressablesGroupResolver.GroupNames` 常量集合

- [ ] **Step 1: 写 EditMode 失败测试**

```csharp
// Assets/Scripts/Test/EditMode/AddressablesGroupResolverTests.cs
using NUnit.Framework;

public class AddressablesGroupResolverTests
{
    [Test]
    public void ResolveGroupName_ConfigAsset_ReturnsConfig()
    {
        Assert.AreEqual("Config",
            AddressablesGroupResolver.ResolveGroupName("Assets/GameAssetConfig/HeroAssets.asset"));
    }

    [Test]
    public void ResolveGroupName_LauncherScene_ReturnsSceneCore()
    {
        Assert.AreEqual("Scene-Core",
            AddressablesGroupResolver.ResolveGroupName("Assets/Scene/Launcher.unity"));
    }

    [Test]
    public void ResolveGroupName_BattleScene_ReturnsSceneBattle()
    {
        Assert.AreEqual("Scene-Battle",
            AddressablesGroupResolver.ResolveGroupName("Assets/Scene/RougeBattle.unity"));
    }

    [Test]
    public void ResolveGroupName_HeroPrefab_ReturnsBattleEntity()
    {
        Assert.AreEqual("Battle-Entity",
            AddressablesGroupResolver.ResolveGroupName("Assets/Prefabs/Battle/Hero/1104/1104View.prefab"));
    }

    [Test]
    public void ResolveGroupName_SkillPrefab_ReturnsBattleContent()
    {
        Assert.AreEqual("Battle-Content",
            AddressablesGroupResolver.ResolveGroupName("Assets/Prefabs/Battle/Skill/1104/110401.prefab"));
    }

    [Test]
    public void ResolveGroupName_RoguelikeMap_ReturnsMap()
    {
        Assert.AreEqual("Map",
            AddressablesGroupResolver.ResolveGroupName("Assets/Prefabs/RoguelikeMap/Wall.prefab"));
    }

    [Test]
    public void ResolveGroupName_UIItem_ReturnsUI()
    {
        Assert.AreEqual("UI",
            AddressablesGroupResolver.ResolveGroupName("Assets/Prefabs/UI/Item/HeroInfo.prefab"));
    }

    [Test]
    public void ResolveGroupName_ArtFolder_ReturnsNull()
    {
        Assert.IsNull(AddressablesGroupResolver.ResolveGroupName("Assets/Art/foo.png"));
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: Unity Test Runner → EditMode → `AddressablesGroupResolverTests`
Expected: FAIL — `AddressablesGroupResolver` 不存在

- [ ] **Step 3: 实现解析器**

```csharp
// Assets/Scripts/Editor/Addressables/AddressablesGroupResolver.cs
using System;

public static class AddressablesGroupResolver
{
    public const string Config = "Config";
    public const string UI = "UI";
    public const string SceneCore = "Scene-Core";
    public const string BattleEntity = "Battle-Entity";
    public const string BattleContent = "Battle-Content";
    public const string Map = "Map";
    public const string SceneBattle = "Scene-Battle";
    public const string RemoteDlc = "Remote-DLC";

    public static string ResolveGroupName(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return null;
        }

        assetPath = assetPath.Replace("\\", "/");

        if (assetPath.StartsWith("Assets/GameAssetConfig/", StringComparison.Ordinal)
            || assetPath.StartsWith("Assets/Config/", StringComparison.Ordinal))
        {
            return Config;
        }

        if (assetPath.StartsWith("Assets/Prefabs/UI/", StringComparison.Ordinal))
        {
            return UI;
        }

        if (string.Equals(assetPath, "Assets/Scene/Launcher.unity", StringComparison.Ordinal))
        {
            return SceneCore;
        }

        if (assetPath.StartsWith("Assets/Scene/", StringComparison.Ordinal))
        {
            return SceneBattle;
        }

        if (assetPath.Contains("/Battle/Hero/", StringComparison.Ordinal)
            || assetPath.Contains("/Battle/Monster/", StringComparison.Ordinal))
        {
            return BattleEntity;
        }

        if (assetPath.StartsWith("Assets/Prefabs/Battle/", StringComparison.Ordinal))
        {
            return BattleContent;
        }

        if (assetPath.StartsWith("Assets/Prefabs/Map/", StringComparison.Ordinal)
            || assetPath.StartsWith("Assets/Prefabs/RoguelikeMap/", StringComparison.Ordinal)
            || assetPath.StartsWith("Assets/Prefabs/NavMesh/", StringComparison.Ordinal))
        {
            return Map;
        }

        return null;
    }
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: Unity Test Runner → EditMode → `AddressablesGroupResolverTests`
Expected: 全部 PASS

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Editor/Addressables/AddressablesGroupResolver.cs Assets/Scripts/Test/EditMode/AddressablesGroupResolverTests.cs
git commit -m "feat(addressables): add group resolver with EditMode tests"
```

---

### Task 2: 运行时配置类型

**Files:**
- Create: `Assets/Scripts/RunTime/GameEntry/AddressablesContentSettings.cs`
- Create: `Assets/Scripts/RunTime/GameEntry/IAddressablesRemoteUrlProvider.cs`

**Interfaces:**
- Produces: `AddressablesContentSettings` 字段（见下）
- Produces: `IAddressablesRemoteUrlProvider.TryGetRemoteBaseUrl(out string baseUrl) -> bool`

- [ ] **Step 1: 创建 ScriptableObject 设置**

```csharp
// Assets/Scripts/RunTime/GameEntry/AddressablesContentSettings.cs
using UnityEngine;

/// <summary>
/// Addressables 内容分发与 Catalog 检查配置。
/// </summary>
[CreateAssetMenu(fileName = "AddressablesContentSettings", menuName = "Game/Addressables Content Settings")]
public class AddressablesContentSettings : ScriptableObject
{
    [Tooltip("默认远程资源根 URL，不含 BuildTarget。例如 https://cdn.example.com/addressables")]
    public string DefaultRemoteBaseUrl = string.Empty;

    [Tooltip("启动时是否检查 Catalog 更新")]
    public bool EnableCatalogUpdateOnStartup = true;

    [Tooltip("为 true 时仅使用本地首包资源，不检查远程 Catalog")]
    public bool ForceLocalOnly = false;

    [Tooltip("Catalog 检查/更新超时（秒），超时后回退本地")]
    public float CatalogUpdateTimeoutSeconds = 10f;
}
```

- [ ] **Step 2: 创建 URL 提供者接口**

```csharp
// Assets/Scripts/RunTime/GameEntry/IAddressablesRemoteUrlProvider.cs
/// <summary>
/// 由服务器或会话层提供 Addressables 远程根 URL。
/// </summary>
public interface IAddressablesRemoteUrlProvider
{
    bool TryGetRemoteBaseUrl(out string baseUrl);
}
```

- [ ] **Step 3: 在 `Assets/GameAssetConfig/` 创建默认资产**

Unity Editor: `Assets → Create → Game → Addressables Content Settings`
保存为 `Assets/GameAssetConfig/AddressablesContentSettings.asset`

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/RunTime/GameEntry/AddressablesContentSettings.cs Assets/Scripts/RunTime/GameEntry/IAddressablesRemoteUrlProvider.cs Assets/GameAssetConfig/AddressablesContentSettings.asset Assets/GameAssetConfig/AddressablesContentSettings.asset.meta
git commit -m "feat(addressables): add runtime content settings and remote URL provider interface"
```

---

### Task 3: Addressables Bootstrap 组件

**Files:**
- Create: `Assets/Scripts/RunTime/GameEntry/Component/AddressablesBootstrapComponent.cs`
- Modify: `Assets/Scripts/RunTime/GameEntry/GameEntry.Component.cs`

**Interfaces:**
- Consumes: `AddressablesContentSettings`, `IAddressablesRemoteUrlProvider`, `ResourceComponent.GameResourceMode`, `ClientAgentGameEntryMode.IsRunning`
- Produces: `AddressablesBootstrapComponent.InitializeAsync() -> UniTask`

- [ ] **Step 1: 实现 Bootstrap**

```csharp
// Assets/Scripts/RunTime/GameEntry/Component/AddressablesBootstrapComponent.cs
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Addressables 初始化、远程 URL 注入与 Catalog 更新。
/// </summary>
public class AddressablesBootstrapComponent : RunTimeComponent
{
    private const string RemoteBaseUrlPropertyName = "AddressablesRemoteBaseUrl";
    private const string DevRemoteUrlPrefsKey = "Addressables.RemoteBaseUrlOverride";

    [SerializeField] private AddressablesContentSettings _settings;

    public override void Init()
    {
        base.Init();
    }

    public async UniTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        ResourceComponent resourceComponent = GameEntryRunTime.GetComponent<ResourceComponent>();
        if (resourceComponent.GameResourceMode != ResourceComponent.ResourceMode.Addressables)
        {
            return;
        }

        ApplyRemoteBaseUrl();
        await Addressables.InitializeAsync().ToUniTask(cancellationToken: cancellationToken);

        if (!ShouldCheckCatalog())
        {
            return;
        }

        await TryUpdateCatalogsAsync(cancellationToken);
    }

    public async UniTask TryUpdateCatalogsAsync(CancellationToken cancellationToken = default)
    {
        float timeoutSeconds = _settings == null ? 10f : _settings.CatalogUpdateTimeoutSeconds;
        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            AsyncOperationHandle<List<string>> checkHandle = Addressables.CheckForCatalogUpdates(false);
            await checkHandle.ToUniTask(cancellationToken: timeoutCts.Token);

            if (checkHandle.Status != AsyncOperationStatus.Succeeded || checkHandle.Result == null || checkHandle.Result.Count == 0)
            {
                Addressables.Release(checkHandle);
                return;
            }

            AsyncOperationHandle<List<AsyncOperationHandle>> updateHandle =
                Addressables.UpdateCatalogs(checkHandle.Result, false);
            await updateHandle.ToUniTask(cancellationToken: timeoutCts.Token);
            Addressables.Release(updateHandle);
            Addressables.Release(checkHandle);
            GameLog.Info(GameLogChannel.Resource, "Addressables catalog updated.");
        }
        catch (OperationCanceledException)
        {
            GameLog.Warning(GameLogChannel.Resource, "Addressables catalog update timed out. Continue with local catalog.");
        }
        catch (Exception ex)
        {
            GameLog.Warning(GameLogChannel.Resource, $"Addressables catalog update failed. Continue with local catalog. {ex.Message}");
        }
    }

    private void ApplyRemoteBaseUrl()
    {
        string remoteBaseUrl = ResolveRemoteBaseUrl();
        if (string.IsNullOrEmpty(remoteBaseUrl))
        {
            return;
        }

        AddressablesRuntimeProperties.SetPropertyValue(
            RemoteBaseUrlPropertyName,
            remoteBaseUrl.TrimEnd('/'));
    }

    private string ResolveRemoteBaseUrl()
    {
        if (_settings != null && _settings.ForceLocalOnly)
        {
            return string.Empty;
        }

        if (GameEntry.FrameSyncTransport is IAddressablesRemoteUrlProvider provider
            && provider.TryGetRemoteBaseUrl(out string serverUrl)
            && !string.IsNullOrEmpty(serverUrl))
        {
            return serverUrl;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        string overrideUrl = PlayerPrefs.GetString(DevRemoteUrlPrefsKey, string.Empty);
        if (!string.IsNullOrEmpty(overrideUrl))
        {
            return overrideUrl;
        }
#endif

        return _settings == null ? string.Empty : _settings.DefaultRemoteBaseUrl;
    }

    private bool ShouldCheckCatalog()
    {
        if (_settings == null || !_settings.EnableCatalogUpdateOnStartup || _settings.ForceLocalOnly)
        {
            return false;
        }

        if (ClientAgentGameEntryMode.IsRunning)
        {
            return false;
        }

        return !string.IsNullOrEmpty(ResolveRemoteBaseUrl());
    }
}
```

- [ ] **Step 2: 挂到 GameEntryRunTime**

在 `GameEntryRunTime` 所在 GameObject 添加 `AddressablesBootstrapComponent`，Inspector 绑定 `AddressablesContentSettings.asset`。

- [ ] **Step 3: 修改 `GameEntry.Component.cs` 初始化顺序**

在 `Resource.Init()` 之后、`await DataTable.InitAsync()` 之前插入：

```csharp
AddressablesBootstrapComponent addressablesBootstrap =
    GameEntryRunTime.GetComponent<AddressablesBootstrapComponent>();
if (addressablesBootstrap != null)
{
    await addressablesBootstrap.InitializeAsync();
}
```

- [ ] **Step 4: 编译验证**

Run: `dotnet build Assembly-CSharp.csproj -nologo -v:minimal`
Expected: 0 Error

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/RunTime/GameEntry/Component/AddressablesBootstrapComponent.cs Assets/Scripts/RunTime/GameEntry/GameEntry.Component.cs
git commit -m "feat(addressables): add bootstrap with runtime CDN and catalog update"
```

---

### Task 4: SceneComponent Addressables 场景加载

**Files:**
- Modify: `Assets/Scripts/RunTime/GameEntry/Component/SceneComponent.cs`

**Interfaces:**
- Consumes: `ResourceComponent.GameResourceMode`, `AssetsPathHelper.LoadScenePathHelper`
- Produces: `SceneComponent.AsyncLoadScene(string sceneAddress, LoadSceneMode) -> AsyncOperationHandle<SceneInstance>`

- [ ] **Step 1: 改造 SceneComponent**

```csharp
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class SceneComponent : RunTimeComponent
{
    public Camera battleCamera;

    public override void Init()
    {
        base.Init();
        if (battleCamera == null)
        {
            GameLog.Error(GameLogChannel.Battle, "SceneComponent init = error : battleCamera == null...");
        }
    }

    public AsyncOperation AsyncLoadScene(string sceneName, LoadSceneMode loadSceneMode)
    {
        ResourceComponent resourceComponent = GameEntryRunTime.GetComponent<ResourceComponent>();
#if UNITY_EDITOR
        if (resourceComponent.GameResourceMode == ResourceComponent.ResourceMode.Editor)
        {
            return SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
        }
#endif
        AsyncOperationHandle<SceneInstance> handle =
            Addressables.LoadSceneAsync(sceneName, loadSceneMode, false);
        return handle.IsValid() ? handle.AsyncOperation : null;
    }

    public void LoadScene(string sceneName, LoadSceneMode loadSceneMode)
    {
        ResourceComponent resourceComponent = GameEntryRunTime.GetComponent<ResourceComponent>();
#if UNITY_EDITOR
        if (resourceComponent.GameResourceMode == ResourceComponent.ResourceMode.Editor)
        {
            SceneManager.LoadScene(sceneName, loadSceneMode);
            return;
        }
#endif
        Addressables.LoadSceneAsync(sceneName, loadSceneMode).WaitForCompletion();
    }
}
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build Assembly-CSharp.csproj -nologo -v:minimal`
Expected: 0 Error

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/RunTime/GameEntry/Component/SceneComponent.cs
git commit -m "fix(addressables): load scenes through Addressables in player builds"
```

---

### Task 5: 重构 RuntimeAddressablesConfigurator（多 Group 同步）

**Files:**
- Modify: `Assets/Scripts/Editor/Addressables/RuntimeAddressablesConfigurator.cs`

**Interfaces:**
- Consumes: `AddressablesGroupResolver.ResolveGroupName`
- Produces: 同步 8 个 Group；删除 `Runtime Assets`/`Entity` stale entries；设置 Labels

- [ ] **Step 1: 定义 Group 元数据表**

在 `RuntimeAddressablesConfigurator` 内新增：

```csharp
private static readonly (string GroupName, string Label, bool UseRemoteLoadPath, int BundleMode)[] GroupDefinitions =
{
    (AddressablesGroupResolver.Config, "config", false, 1),          // Pack Together
    (AddressablesGroupResolver.UI, "ui", false, 1),
    (AddressablesGroupResolver.SceneCore, "scene-core", false, 0), // Pack Separately
    (AddressablesGroupResolver.BattleEntity, "battle-entity", true, 0),
    (AddressablesGroupResolver.BattleContent, "battle-content", true, 0),
    (AddressablesGroupResolver.Map, "map", true, 1),
    (AddressablesGroupResolver.SceneBattle, "scene-battle", true, 0),
    (AddressablesGroupResolver.RemoteDlc, "remote-dlc", true, 0),
};
```

`BundleMode`: `0 = PackSeparately`, `1 = PackTogether`（对应 `BundledAssetGroupSchema.BundleMode`）

- [ ] **Step 2: 替换 `EnumerateRuntimeAssetPaths`**

扫描范围改为：

```csharp
// Config
EnumerateFiles("Assets/GameAssetConfig", "*.asset", SearchOption.AllDirectories)
EnumerateFiles("Assets/Config", "*.asset", SearchOption.AllDirectories)
// UI
EnumerateFiles("Assets/Prefabs/UI", "*.prefab", SearchOption.AllDirectories)
// Battle（整体扫描，由 Resolver 细分 Entity/Content）
EnumerateFiles("Assets/Prefabs/Battle", "*.prefab", SearchOption.AllDirectories)
// Map
EnumerateFiles("Assets/Prefabs/Map", "*.prefab", SearchOption.AllDirectories)
EnumerateFiles("Assets/Prefabs/RoguelikeMap", "*.prefab", SearchOption.AllDirectories)
EnumerateFiles("Assets/Prefabs/NavMesh", "*.prefab", SearchOption.AllDirectories)
// Scene
EnumerateFiles("Assets/Scene", "*.unity", SearchOption.AllDirectories)
```

- [ ] **Step 3: 重写 `SyncRuntimeAssets` 主循环**

```csharp
Dictionary<string, HashSet<string>> validGuidsByGroup = new Dictionary<string, HashSet<string>>();

foreach (string assetPath in EnumerateRuntimeAssetPaths())
{
    string groupName = AddressablesGroupResolver.ResolveGroupName(assetPath);
    if (groupName == null)
    {
        continue;
    }

    string guid = AssetDatabase.AssetPathToGUID(assetPath);
    if (string.IsNullOrEmpty(guid))
    {
        continue;
    }

    AddressableAssetGroup group = GetOrCreateGroup(settings, groupName);
    AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
    if (entry == null)
    {
        continue;
    }

    if (entry.address != assetPath)
    {
        entry.SetAddress(assetPath, false);
    }

    ApplyLabel(entry, groupName);
    validGuidsByGroup.GetOrCreate(groupName).Add(guid);
}

RemoveStaleEntries(settings, validGuidsByGroup);
RemoveObsoleteGroups(settings); // Runtime Assets, Entity
settings.DefaultGroup = settings.FindGroup(AddressablesGroupResolver.Config);
```

- [ ] **Step 4: 实现 `GetOrCreateGroup` + Schema**

- Local Load Path Profile 变量 id: `1661947375ba50441933341008504858`
- Remote Load Path Profile 变量 id: `5013b71119f00994fa1818496b64f222`（需在 Task 6 创建自定义变量后替换为 `AddressablesRemoteBaseUrl` 对应 id）
- `Scene-Core` 的 `ContentUpdateGroupSchema` 设为 Static；其余 Can Change
- `Remote-DLC` 组 `IncludeInBuild = false`（仅远程）

- [ ] **Step 5: 新增菜单**

```csharp
[MenuItem("Tools/Addressables/Build Local Content")]
public static void BuildLocalContent() { SyncRuntimeAssets(); ... BuildPlayerContent ... }

[MenuItem("Tools/Addressables/Build Remote Update")]
public static void BuildRemoteUpdate()
{
    SyncRuntimeAssets();
    // 弹出选择 previous content state 的 Utility 窗口，调用 ContentUpdateScript.BuildContentUpdate
}
```

- [ ] **Step 6: Unity 菜单验证**

Run: `Tools/Addressables/Sync Runtime Assets`
Expected: 控制台输出各 Group entry 数量；`Runtime Assets` 组无 entry

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Editor/Addressables/RuntimeAddressablesConfigurator.cs
git commit -m "feat(addressables): sync assets into multi-group layout"
```

---

### Task 6: AddressableAssetSettings Profile 与 Group 资产

**Files:**
- Modify: `Assets/Scripts/Libraries/AddressableAssetsData/AddressableAssetSettings.asset`
- Create/Modify: `Assets/Scripts/Libraries/AddressableAssetsData/AssetGroups/*.asset`
- Delete: `Runtime Assets.asset`, `Entity.asset` 及其 Schemas（Sync 后）
- Cleanup: `Assets/AddressableAssetsData/` 重复项

**Interfaces:**
- Produces: Profile 自定义变量 `AddressablesRemoteBaseUrl`
- Produces: `BuildRemoteCatalog = 1`

- [ ] **Step 1: Unity Editor 配置 Profile**

Addressables Groups 窗口 → Profile → 新增变量：

| 名称 | 值 |
|------|-----|
| `AddressablesRemoteBaseUrl` | `http://127.0.0.1`（开发占位） |

修改 `Remote.LoadPath` 为：`{AddressablesRemoteBaseUrl}/[BuildTarget]`

- [ ] **Step 2: 启用 Remote Catalog**

`AddressableAssetSettings`：
- `Build Remote Catalog` = true
- `Bundle Local Catalog` = false

- [ ] **Step 3: 执行 Sync 生成 8 个 Group**

Run: `Tools/Addressables/Sync Runtime Assets`

- [ ] **Step 4: 删除废弃组**

确认 `Entity`、`Runtime Assets` 组已被脚本移除；手动检查 `.asset` 文件无残留引用。

- [ ] **Step 5: 清理重复目录**

若 `Assets/AddressableAssetsData/` 仅含 `ProfileDataSourceSettings` 或旧 `content_state`，迁移必要文件到 Libraries 路径后删除重复目录。

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Libraries/AddressableAssetsData/
git commit -m "chore(addressables): apply multi-group settings and remote catalog profile"
```

---

### Task 7: 构建与分析验证

**Files:** 无代码变更，验证构建管线

- [ ] **Step 1: 执行 Local Build**

Run: `Tools/Addressables/Build Local Content`
Expected: `Addressables build succeeded`

- [ ] **Step 2: Analyze**

Addressables Groups → Analyze → `Check Duplicate Bundle Dependencies`
Expected: 无 Critical 未解析依赖

- [ ] **Step 3: Editor Play 验证（Addressables 模式）**

1. `ResourceComponent.GameResourceMode = Addressables`
2. Play → 启动窗加载 → 单机进战斗
3. Console 无 Addressables load failed

- [ ] **Step 4: Agent 模式验证**

启用 `ClientAgentGameEntryMode` Play
Expected: 无 Catalog 检查日志；战斗流程正常

- [ ] **Step 5: 记录验证结果到 spec 状态**

更新 `docs/superpowers/specs/2026-07-02-addressables-reconfiguration-design.md` 状态为「已实施」

---

## Spec Coverage Checklist

| Spec 章节 | Task |
|-----------|------|
| §3 Group 设计 | Task 1, 5, 6 |
| §4 本地/远程双通道 | Task 5, 6 |
| §5 CDN 可配置 | Task 2, 3 |
| §6 Catalog 策略 | Task 3 |
| §7 场景加载 | Task 4 |
| §8 Editor 工具 | Task 5, 6 |
| §9 启动时序 | Task 3 |
| §10 构建流程 | Task 6, 7 |
| §11 测试计划 | Task 1, 7 |
| Remote-DLC 预留 | Task 5, 6（空组） |
| 服务器 URL 预留 | Task 2 |

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-07-02-addressables-reconfiguration.md`.

**两种执行方式：**

1. **Subagent-Driven（推荐）** — 每个 Task 派发独立 subagent，Task 间做代码审查，迭代快
2. **Inline Execution** — 本会话按 Task 顺序直接实施，关键节点暂停给你确认

你想用哪种方式开始实施？
