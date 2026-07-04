# 热更发布工具 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 用单一 EditorWindow「热更发布中心」替代分散的 HybridCLR/Addressables 构建 MenuItem，提供首包构建、代码热更打包、资源热更打包三个一键工具，底层共享 `HotUpdateBuildPipeline`。

**Architecture:** `HotUpdatePublishWindow` 仅负责 UI 与日志；`HotUpdateBuildPipeline` 按 `HotUpdateBuildMode` 编排 Step 序列；各 Step 从现有 `HybridCLRDllCopyTool`、`RuntimeAddressablesConfigurator`、`AddressablesLocalHotUpdateEnvironment` 迁移逻辑；`Build/HotUpdateManifest.json` 记录 content state 与 hash，消除手选 content state。

**Tech Stack:** Unity 2023.2.20f1、HybridCLR.Editor、Addressables Editor、C# EditorWindow、NUnit EditMode

**Spec:** [`docs/superpowers/specs/2026-07-04-hotupdate-publish-tools-design.md`](../specs/2026-07-04-hotupdate-publish-tools-design.md)

## Global Constraints

- 不修改 `GameFramework/Bootstrap/HotUpdate/*` 运行时加载逻辑
- 新增公共 Editor 类型使用 `Rogue.Editor.HotUpdate` 命名空间；新增公共类型需中文 XML 注释
- Address 格式保持 `Assets/...` 全路径
- content state **禁止** `EditorUtility.OpenFilePanel`；从 manifest 或 AddressableAssetsData 自动定位
- CodePatch 仅更新 `HotUpdate-Code-Remote` 组；ResourcePatch 排除 `HotUpdate-Code-*` 组
- 构建过程中禁止并发 Pipeline（单例运行锁）
- Phase 1 不实现 CDN 上传与 CLI；仅预留接口/目录
- 保留 `RuntimeAddressablesConfigurator.SyncRuntimeAssets()` 公开转发（BulletFactory 等仍调用）
- 菜单最终仅保留 `Tools/发布/热更发布中心`

---

## File Map

| 文件 | 职责 |
|------|------|
| `Assets/Scripts/Editor/HotUpdate/Pipeline/HotUpdateBuildMode.cs` | 构建模式枚举 |
| `Assets/Scripts/Editor/HotUpdate/Pipeline/HotUpdateBuildContext.cs` | 构建上下文 |
| `Assets/Scripts/Editor/HotUpdate/Pipeline/HotUpdateBuildResult.cs` | 构建结果 |
| `Assets/Scripts/Editor/HotUpdate/Pipeline/HotUpdateManifest.cs` | manifest 读写 + hash |
| `Assets/Scripts/Editor/HotUpdate/Pipeline/HotUpdateBuildPipeline.cs` | Step 编排 |
| `Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/*.cs` | 各 Step 实现 |
| `Assets/Scripts/Editor/HotUpdate/Internal/AddressablesBuildLayoutGuard.cs` | 从 Configurator 拆出 |
| `Assets/Scripts/Editor/HotUpdate/Dev/LocalDevEnvironment.cs` | 本地 HTTP / 环境配置 |
| `Assets/Scripts/Editor/HotUpdate/HotUpdatePublishWindow.cs` | 发布中心 UI |
| `Assets/Scripts/Editor/Addressables/RuntimeAddressablesConfigurator.cs` | **Modify** → 仅保留 `SyncRuntimeAssets()` 转发 |
| `Assets/Scripts/Editor/HybridCLR/HybridCLRDllCopyTool.cs` | **Delete** |
| `Assets/Scripts/Editor/Addressables/AddressablesLocalHotUpdateEnvironment.cs` | **Delete** |
| `Assets/Scripts/Editor/Addressables/AddressablesContentSettingsEditor.cs` | **Modify** 移除操作按钮 |
| `Assets/Scripts/Test/EditMode/HotUpdateManifestTests.cs` | manifest 单元测试 |
| `Build/HotUpdateManifest.json` | 运行时生成（gitignore 可选） |

---

### Task 1: Pipeline 基础类型

**Files:**
- Create: `Assets/Scripts/Editor/HotUpdate/Pipeline/HotUpdateBuildMode.cs`
- Create: `Assets/Scripts/Editor/HotUpdate/Pipeline/HotUpdateBuildContext.cs`
- Create: `Assets/Scripts/Editor/HotUpdate/Pipeline/HotUpdateBuildResult.cs`

**Interfaces:**
- Produces: `HotUpdateBuildMode` enum `{ FullPackage, CodePatch, ResourcePatch }`
- Produces: `HotUpdateBuildContext` 构造与属性
- Produces: `HotUpdateBuildResult` 成功/失败结果

- [ ] **Step 1: 创建 HotUpdateBuildMode.cs**

```csharp
namespace Rogue.Editor.HotUpdate.Pipeline
{
    /// <summary>
    /// 热更发布构建模式。
    /// </summary>
    public enum HotUpdateBuildMode
    {
        FullPackage = 0,
        CodePatch = 1,
        ResourcePatch = 2,
    }
}
```

- [ ] **Step 2: 创建 HotUpdateBuildContext.cs**

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;

namespace Rogue.Editor.HotUpdate.Pipeline
{
    /// <summary>
    /// 热更构建上下文，Window 与后续 CLI 共用。
    /// </summary>
    public sealed class HotUpdateBuildContext
    {
        public HotUpdateBuildContext(
            HotUpdateBuildMode mode,
            BuildTarget buildTarget,
            string version,
            string remoteBaseUrl,
            bool developmentBuild,
            IProgress<string> log = null,
            CancellationToken cancellationToken = default)
        {
            Mode = mode;
            BuildTarget = buildTarget;
            Version = version ?? throw new ArgumentNullException(nameof(version));
            RemoteBaseUrl = remoteBaseUrl ?? string.Empty;
            DevelopmentBuild = developmentBuild;
            Log = log ?? NoOpProgress.Instance;
            CancellationToken = cancellationToken;
        }

        public HotUpdateBuildMode Mode { get; }
        public BuildTarget BuildTarget { get; }
        public string Version { get; }
        public string RemoteBaseUrl { get; }
        public bool DevelopmentBuild { get; }
        public IReadOnlyList<string> ResourceGroupFilter { get; set; }
        public bool SkipPlayerBuild { get; set; }
        public bool ForceSkipAotChangeCheck { get; set; }
        public IProgress<string> Log { get; }
        public CancellationToken CancellationToken { get; }

        public void LogLine(string message) => Log.Report(message);

        private sealed class NoOpProgress : IProgress<string>
        {
            public static readonly NoOpProgress Instance = new NoOpProgress();
            public void Report(string value) { }
        }
    }
}
```

- [ ] **Step 3: 创建 HotUpdateBuildResult.cs**

```csharp
using System.Collections.Generic;

namespace Rogue.Editor.HotUpdate.Pipeline
{
    /// <summary>
    /// 热更构建结果。
    /// </summary>
    public sealed class HotUpdateBuildResult
    {
        public static HotUpdateBuildResult Succeeded(
            IReadOnlyList<string> logs,
            string serverDataPath = null,
            string playerOutputPath = null,
            string manifestPath = null)
        {
            return new HotUpdateBuildResult(true, null, logs, serverDataPath, playerOutputPath, manifestPath);
        }

        public static HotUpdateBuildResult Failed(string errorMessage, IReadOnlyList<string> logs)
        {
            return new HotUpdateBuildResult(false, errorMessage, logs, null, null, null);
        }

        private HotUpdateBuildResult(
            bool success,
            string errorMessage,
            IReadOnlyList<string> logs,
            string serverDataPath,
            string playerOutputPath,
            string manifestPath)
        {
            Success = success;
            ErrorMessage = errorMessage;
            Logs = logs ?? System.Array.Empty<string>();
            ServerDataPath = serverDataPath;
            PlayerOutputPath = playerOutputPath;
            ManifestPath = manifestPath;
        }

        public bool Success { get; }
        public string ErrorMessage { get; }
        public IReadOnlyList<string> Logs { get; }
        public string ServerDataPath { get; }
        public string PlayerOutputPath { get; }
        public string ManifestPath { get; }
    }
}
```

- [ ] **Step 4: 编译验证**

Run: `dotnet build Assets/Scripts/Editor/Game.Editor.csproj -nologo -v:minimal`
Expected: Build succeeded（若 csproj 名不同，改用 Unity 生成的 Editor 程序集 csproj）

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Editor/HotUpdate/Pipeline/HotUpdateBuildMode.cs \
        Assets/Scripts/Editor/HotUpdate/Pipeline/HotUpdateBuildContext.cs \
        Assets/Scripts/Editor/HotUpdate/Pipeline/HotUpdateBuildResult.cs
git commit -m "feat(editor): add hot update build pipeline core types"
```

---

### Task 2: HotUpdateManifest 读写与测试

**Files:**
- Create: `Assets/Scripts/Editor/HotUpdate/Pipeline/HotUpdateManifest.cs`
- Create: `Assets/Scripts/Test/EditMode/HotUpdateManifestTests.cs`

**Interfaces:**
- Produces: `HotUpdateManifest.Load()` / `Save()`
- Produces: `HotUpdateManifest.TryResolveContentStatePath(out string path)`
- Produces: `HotUpdateManifest.ComputeFileSha256(string absolutePath)`

- [ ] **Step 1: 写失败测试**

```csharp
// Assets/Scripts/Test/EditMode/HotUpdateManifestTests.cs
using System.IO;
using NUnit.Framework;
using Rogue.Editor.HotUpdate.Pipeline;
using UnityEngine;

public class HotUpdateManifestTests
{
    private string _tempDir;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Application.temporaryCachePath, "HotUpdateManifestTests");
        Directory.CreateDirectory(_tempDir);
        HotUpdateManifest.SetManifestPathForTests(Path.Combine(_tempDir, "HotUpdateManifest.json"));
    }

    [TearDown]
    public void TearDown()
    {
        HotUpdateManifest.ResetManifestPathForTests();
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Test]
    public void SaveAndLoad_RoundTripsContentStatePath()
    {
        var manifest = new HotUpdateManifestData
        {
            AppVersion = "1.0.0",
            ContentStatePath = "C:/fake/content_state.bin",
        };

        HotUpdateManifest.Save(manifest);
        HotUpdateManifestData loaded = HotUpdateManifest.Load();

        Assert.AreEqual("1.0.0", loaded.AppVersion);
        Assert.AreEqual("C:/fake/content_state.bin", loaded.ContentStatePath);
    }

    [Test]
    public void TryResolveContentStatePath_UsesManifestFirst()
    {
        HotUpdateManifest.Save(new HotUpdateManifestData
        {
            ContentStatePath = Path.Combine(_tempDir, "from_manifest.bin"),
        });
        File.WriteAllText(Path.Combine(_tempDir, "from_manifest.bin"), "x");

        Assert.IsTrue(HotUpdateManifest.TryResolveContentStatePath(out string path));
        Assert.AreEqual(Path.Combine(_tempDir, "from_manifest.bin"), path);
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: Unity Test Runner EditMode → `HotUpdateManifestTests`
Expected: FAIL — `HotUpdateManifest` 不存在

- [ ] **Step 3: 实现 HotUpdateManifest.cs**

使用 `[Serializable]` 数据类 + `JsonUtility`（项目无 Newtonsoft 依赖时优先 Unity 内置）。为支持 `patches` 数组与 `aotMetadataHashes` 字典，使用两个 serializable 辅助类：

```csharp
// Assets/Scripts/Editor/HotUpdate/Pipeline/HotUpdateManifest.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Rogue.Editor.HotUpdate.Pipeline
{
    [Serializable]
    public sealed class HotUpdateManifestData
    {
        public string AppVersion = "0.0.0";
        public string BuildTarget = string.Empty;
        public string LastFullBuildUtc = string.Empty;
        public string ContentStatePath = string.Empty;
        public string RemoteBaseUrl = string.Empty;
        public string GameRuntimeHash = string.Empty;
        public HotUpdateManifestHashEntry[] AotMetadataHashes = Array.Empty<HotUpdateManifestHashEntry>();
        public HotUpdateManifestPatchEntry[] Patches = Array.Empty<HotUpdateManifestPatchEntry>();
    }

    [Serializable]
    public sealed class HotUpdateManifestHashEntry
    {
        public string FileName;
        public string Hash;
    }

    [Serializable]
    public sealed class HotUpdateManifestPatchEntry
    {
        public string Type;
        public string Version;
        public string GeneratedAtUtc;
        public string PlayerOutputPath;
        public string ServerDataPath;
        public string GameRuntimeHash;
        public int BundleCount;
        public string ChangedGroupsCsv;
    }

    /// <summary>
    /// 读写 Build/HotUpdateManifest.json 并解析 content state。
    /// </summary>
    public static class HotUpdateManifest
    {
        private const string DefaultRelativePath = "Build/HotUpdateManifest.json";
        private static string _overridePathForTests;

        public static string ManifestPath =>
            _overridePathForTests ?? Path.Combine(Directory.GetCurrentDirectory(), DefaultRelativePath);

        internal static void SetManifestPathForTests(string path) => _overridePathForTests = path;
        internal static void ResetManifestPathForTests() => _overridePathForTests = null;

        public static HotUpdateManifestData Load()
        {
            if (!File.Exists(ManifestPath))
            {
                return new HotUpdateManifestData();
            }

            string json = File.ReadAllText(ManifestPath, Encoding.UTF8);
            return JsonUtility.FromJson<HotUpdateManifestData>(json) ?? new HotUpdateManifestData();
        }

        public static void Save(HotUpdateManifestData data)
        {
            string dir = Path.GetDirectoryName(ManifestPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(ManifestPath, json, Encoding.UTF8);
        }

        public static bool TryResolveContentStatePath(out string contentStatePath)
        {
            HotUpdateManifestData manifest = Load();
            if (!string.IsNullOrEmpty(manifest.ContentStatePath) && File.Exists(manifest.ContentStatePath))
            {
                contentStatePath = manifest.ContentStatePath;
                return true;
            }

            string searchRoot = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Assets/Scripts/Libraries/AddressableAssetsData");
            if (Directory.Exists(searchRoot))
            {
                string newest = Directory
                    .GetFiles(searchRoot, "addressables_content_state.bin", SearchOption.AllDirectories)
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();

                if (!string.IsNullOrEmpty(newest))
                {
                    contentStatePath = newest;
                    return true;
                }
            }

            contentStatePath = null;
            return false;
        }

        public static string ComputeFileSha256(string absolutePath)
        {
            using FileStream stream = File.OpenRead(absolutePath);
            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(stream);
            return "sha256:" + BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: Unity Test Runner EditMode → `HotUpdateManifestTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Editor/HotUpdate/Pipeline/HotUpdateManifest.cs \
        Assets/Scripts/Test/EditMode/HotUpdateManifestTests.cs
git commit -m "feat(editor): add HotUpdateManifest read/write and tests"
```

---

### Task 3: 拆分 AddressablesSyncStep 与 BuildLayoutGuard

**Files:**
- Create: `Assets/Scripts/Editor/HotUpdate/Internal/AddressablesBuildLayoutGuard.cs`
- Create: `Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/AddressablesSyncStep.cs`
- Modify: `Assets/Scripts/Editor/Addressables/RuntimeAddressablesConfigurator.cs`

**Interfaces:**
- Produces: `AddressablesSyncStep.Execute(HotUpdateBuildContext context)`
- Produces: `AddressablesBuildLayoutGuard.PrepareForAddressablesBuild(bool logResult)`
- Produces: `RuntimeAddressablesConfigurator.SyncRuntimeAssets()` → 转发到 `AddressablesSyncStep.ExecuteSyncOnly()`

- [ ] **Step 1: 将 AddressablesBuildLayoutGuard 移到新文件**

从 `RuntimeAddressablesConfigurator.cs` 剪切 `[InitializeOnLoad] AddressablesBuildLayoutGuard` 类（约 400–489 行）到：

`Assets/Scripts/Editor/HotUpdate/Internal/AddressablesBuildLayoutGuard.cs`

命名空间改为 `Rogue.Editor.HotUpdate.Internal`，**删除** `[MenuItem("Tools/Addressables/禁用构建布局报告")]`。

- [ ] **Step 2: 创建 AddressablesSyncStep.cs**

将 `RuntimeAddressablesConfigurator` 中除 `AddressablesBuildLayoutGuard` 外的全部逻辑移入 `AddressablesSyncStep`：

```csharp
namespace Rogue.Editor.HotUpdate.Pipeline.Steps
{
    /// <summary>
    /// 同步运行时资产到 Addressables 分组。
    /// </summary>
    public static class AddressablesSyncStep
    {
        public static void Execute(HotUpdateBuildContext context)
        {
            context.LogLine("[AddressablesSync] 开始同步运行时资源...");
            ExecuteSyncOnly();
            context.LogLine("[AddressablesSync] 同步完成。");
        }

        /// <summary>
        /// 供 BulletFactory 等遗留调用方使用的无上下文入口。
        /// </summary>
        public static void ExecuteSyncOnly()
        {
            // 原 RuntimeAddressablesConfigurator.SyncRuntimeAssets 方法体（GroupDefinitions、EnumerateRuntimeAssetPaths 等）
        }
    }
}
```

- [ ] **Step 3: 精简 RuntimeAddressablesConfigurator.cs 为转发壳**

```csharp
public static class RuntimeAddressablesConfigurator
{
    [System.Obsolete("请使用 Tools/发布/热更发布中心。此方法保留供内容工厂工具调用。")]
    public static void SyncRuntimeAssets()
    {
        Rogue.Editor.HotUpdate.Pipeline.Steps.AddressablesSyncStep.ExecuteSyncOnly();
    }

    // 删除 BuildLocalContent / BuildRuntimeContent / BuildRemoteUpdate MenuItem
    // 删除 AddressablesBuildLayoutGuard 类
}
```

- [ ] **Step 4: 编译验证**

Run: `dotnet build` 对应 Editor 程序集
Expected: Build succeeded；`BulletQuickCreateWindow` 等仍可编译

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Editor/HotUpdate/Internal/AddressablesBuildLayoutGuard.cs \
        Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/AddressablesSyncStep.cs \
        Assets/Scripts/Editor/Addressables/RuntimeAddressablesConfigurator.cs
git commit -m "refactor(editor): extract AddressablesSyncStep from configurator"
```

---

### Task 4: HybridClrBuildStep

**Files:**
- Create: `Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/HybridClrBuildStep.cs`
- Delete: `Assets/Scripts/Editor/HybridCLR/HybridCLRDllCopyTool.cs`（及 .meta）

**Interfaces:**
- Consumes: `HotUpdateBuildContext.BuildTarget`, `DevelopmentBuild`, `CancellationToken`
- Produces: `HybridClrBuildStep.Execute(HotUpdateBuildContext context, bool compileBeforeCopy)`
- Produces: 更新 `Assets/HotUpdate/Code/` 与 `manifest.json`（资产侧）

- [ ] **Step 1: 创建 HybridClrBuildStep.cs**

从 `HybridCLRDllCopyTool.cs` 迁移 `CopyDlls`、`CopyHotUpdateAssembly`、`CopyAssemblyFiles`、`WriteManifest`（资产目录 manifest.json，非 Build/HotUpdateManifest.json）逻辑。

```csharp
namespace Rogue.Editor.HotUpdate.Pipeline.Steps
{
    public static class HybridClrBuildStep
    {
        public static void Execute(HotUpdateBuildContext context, bool compileBeforeCopy)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (compileBeforeCopy)
            {
                context.LogLine("[HybridCLR] CompileDll...");
                CompileDllCommand.CompileDll(context.BuildTarget, context.DevelopmentBuild);
            }

            context.LogLine("[HybridCLR] 同步 DLL 到 Assets/HotUpdate/Code/...");
            // 原 CopyDlls 逻辑
            AddressablesSyncStep.Execute(context);
        }
    }
}
```

- [ ] **Step 2: 删除 HybridCLRDllCopyTool.cs**

确认无其他文件引用 `HybridCLRDllCopyTool`（grep 应为 0）。

- [ ] **Step 3: 手动验证**

在 Unity 中临时调用 `HybridClrBuildStep.Execute(context, compileBeforeCopy: true)`（可用 Editor 测试脚本或 Window 未完成前用 MenuItem 临时入口），确认：

- `Assets/HotUpdate/Code/Game.Runtime.dll.bytes` 更新
- `Assets/HotUpdate/Code/AOT/*.dll.bytes` 存在
- Console 无 Error

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/HybridClrBuildStep.cs
git rm Assets/Scripts/Editor/HybridCLR/HybridCLRDllCopyTool.cs
git commit -m "refactor(editor): migrate HybridCLR copy logic to HybridClrBuildStep"
```

---

### Task 5: Addressables 构建 Step

**Files:**
- Create: `Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/AddressablesFullBuildStep.cs`
- Create: `Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/AddressablesContentUpdateStep.cs`

**Interfaces:**
- Consumes: `HotUpdateManifest.TryResolveContentStatePath`
- Consumes: `AddressablesBuildLayoutGuard.PrepareForAddressablesBuild`
- Produces: `AddressablesFullBuildStep.Execute(HotUpdateBuildContext context)`
- Produces: `AddressablesContentUpdateStep.Execute(HotUpdateBuildContext context, ContentUpdateScope scope)`

```csharp
public enum ContentUpdateScope
{
    CodeOnly,
    ResourceOnly,
}
```

- [ ] **Step 1: 实现 AddressablesFullBuildStep**

```csharp
public static class AddressablesFullBuildStep
{
    public static string Execute(HotUpdateBuildContext context)
    {
        AddressablesBuildLayoutGuard.PrepareForAddressablesBuild(false);
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
        if (!string.IsNullOrEmpty(result.Error))
        {
            throw new InvalidOperationException($"Addressables 全量构建失败: {result.Error}");
        }

        context.LogLine($"[Addressables] 全量构建成功，Locations: {result.LocationCount}");
        return Path.Combine(Directory.GetCurrentDirectory(), "ServerData", context.BuildTarget.ToString());
    }
}
```

- [ ] **Step 2: 实现 AddressablesContentUpdateStep**

```csharp
public static class AddressablesContentUpdateStep
{
    public static string Execute(HotUpdateBuildContext context, ContentUpdateScope scope)
    {
        if (!HotUpdateManifest.TryResolveContentStatePath(out string contentStatePath))
        {
            throw new InvalidOperationException("未找到 content state，请先执行首包构建。");
        }

        context.LogLine($"[Addressables] Content Update，state={contentStatePath}，scope={scope}");
        AddressablesBuildLayoutGuard.PrepareForAddressablesBuild(false);

        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        ContentUpdateScript.BuildContentUpdate(settings, contentStatePath);

        // scope 过滤说明：Addressables Content Update 基于 state diff；
        // CodeOnly 前一步 Sync 应仅触达 HotUpdate-Code-Remote 资产变更；
        // ResourceOnly 时 Sync 正常扫描但构建产物自然排除 Static 的 HotUpdate-Code-Local。
        // 若需严格隔离，在 Execute 前由 Pipeline 设置 context 标志，
        // Sync 后临时移除/恢复非目标组 entry（Phase 1 可选增强）。

        return Path.Combine(Directory.GetCurrentDirectory(), "ServerData", context.BuildTarget.ToString());
    }
}
```

- [ ] **Step 3: 编译验证**

Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/AddressablesFullBuildStep.cs \
        Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/AddressablesContentUpdateStep.cs
git commit -m "feat(editor): add Addressables full build and content update steps"
```

---

### Task 6: PreflightCheckStep 与 AotChangeDetectorStep

**Files:**
- Create: `Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/PreflightCheckStep.cs`
- Create: `Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/AotChangeDetectorStep.cs`
- Create: `Assets/Scripts/Test/EditMode/AotChangeDetectorStepTests.cs`

**Interfaces:**
- Produces: `PreflightCheckStep.Execute(HotUpdateBuildContext context)` — 失败抛 `InvalidOperationException`
- Produces: `AotChangeDetectorStep.HasAotChanges()` → `bool`

- [ ] **Step 1: 写 AotChangeDetector 失败测试**

```csharp
[Test]
public void HasAotChanges_WhenNoManifest_ReturnsFalse()
{
    Assert.IsFalse(AotChangeDetectorStep.HasAotChanges());
}
```

- [ ] **Step 2: 实现 AotChangeDetectorStep**

比对 `Assets/HotUpdate/Code/AOT/*.dll.bytes` 的 SHA256 与 `HotUpdateManifest.Load().AotMetadataHashes`。

- [ ] **Step 3: 实现 PreflightCheckStep**

按 spec §5.4 表格实现各模式检查；Error 级失败抛异常并 `context.LogLine` 详情。

- [ ] **Step 4: 运行 EditMode 测试**

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/PreflightCheckStep.cs \
        Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/AotChangeDetectorStep.cs \
        Assets/Scripts/Test/EditMode/AotChangeDetectorStepTests.cs
git commit -m "feat(editor): add preflight and AOT change detection steps"
```

---

### Task 7: PlayerBuildStep、ManifestWriteStep、AssetChangeSummaryStep

**Files:**
- Create: `Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/PlayerBuildStep.cs`
- Create: `Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/ManifestWriteStep.cs`
- Create: `Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/AssetChangeSummaryStep.cs`

**Interfaces:**
- Produces: `PlayerBuildStep.Execute(HotUpdateBuildContext context)` → player 输出路径
- Produces: `ManifestWriteStep.Execute(HotUpdateBuildContext context, HotUpdateManifestPatchEntry patch, string contentStatePath)`
- Produces: `AssetChangeSummaryStep.Execute(HotUpdateBuildContext context)` → 变更摘要字符串

- [ ] **Step 1: 实现 PlayerBuildStep**

```csharp
public static class PlayerBuildStep
{
    public static string Execute(HotUpdateBuildContext context)
    {
        string outputDir = Path.Combine("Build", context.BuildTarget.ToString(), context.Version);
        Directory.CreateDirectory(outputDir);

        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray(),
            locationPathName = Path.Combine(outputDir, PlayerSettings.productName +
                (context.BuildTarget == BuildTarget.StandaloneWindows64 ? ".exe" : string.Empty)),
            target = context.BuildTarget,
            options = context.DevelopmentBuild ? BuildOptions.Development : BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException($"Player 构建失败: {report.summary.result}");
        }

        return options.locationPathName;
    }
}
```

- [ ] **Step 2: 实现 ManifestWriteStep**

Full build 时写入 `ContentStatePath`（搜索最新 `addressables_content_state.bin`）、`AotMetadataHashes`、`GameRuntimeHash`；每次 patch 追加 `Patches` 条目。

- [ ] **Step 3: 实现 AssetChangeSummaryStep**

ResourcePatch 前调用 `AddressablesSyncStep.ExecuteSyncOnly()` 后，对比各 Group entry 数量变化（简化：记录 Sync 日志中的 changedCount / removedCount，或读取 Addressables 设置 dirty 计数）。输出到 `context.LogLine`。

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/PlayerBuildStep.cs \
        Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/ManifestWriteStep.cs \
        Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/AssetChangeSummaryStep.cs
git commit -m "feat(editor): add player build, manifest write, and asset summary steps"
```

---

### Task 8: HotUpdateBuildPipeline 编排

**Files:**
- Create: `Assets/Scripts/Editor/HotUpdate/Pipeline/HotUpdateBuildPipeline.cs`

**Interfaces:**
- Produces:
  - `HotUpdateBuildPipeline.RunFullPackageAsync(HotUpdateBuildContext context)`
  - `HotUpdateBuildPipeline.RunCodePatchAsync(HotUpdateBuildContext context)`
  - `HotUpdateBuildPipeline.RunResourcePatchAsync(HotUpdateBuildContext context)`
- Internal: 静态 `_isRunning` 锁

- [ ] **Step 1: 实现 Pipeline**

```csharp
public static class HotUpdateBuildPipeline
{
    private static bool _isRunning;

    public static HotUpdateBuildResult RunFullPackage(HotUpdateBuildContext context)
    {
        return RunInternal(context, () =>
        {
            PreflightCheckStep.Execute(context);
            HybridClrBuildStep.Execute(context, compileBeforeCopy: true);
            string serverData = AddressablesFullBuildStep.Execute(context);
            string playerPath = context.SkipPlayerBuild ? null : PlayerBuildStep.Execute(context);
            string contentState = ResolveLatestContentStatePath();
            ManifestWriteStep.Execute(context, CreatePatch("full", context), contentState);
            return HotUpdateBuildResult.Succeeded(CollectLogs(context), serverData, playerPath, HotUpdateManifest.ManifestPath);
        });
    }

    public static HotUpdateBuildResult RunCodePatch(HotUpdateBuildContext context)
    {
        return RunInternal(context, () =>
        {
            PreflightCheckStep.Execute(context);
            if (!context.ForceSkipAotChangeCheck && AotChangeDetectorStep.HasAotChanges())
            {
                throw new InvalidOperationException("检测到 AOT 元数据变更，请使用首包构建。");
            }
            HybridClrBuildStep.Execute(context, compileBeforeCopy: true);
            string serverData = AddressablesContentUpdateStep.Execute(context, ContentUpdateScope.CodeOnly);
            ManifestWriteStep.Execute(context, CreatePatch("code", context), null);
            return HotUpdateBuildResult.Succeeded(CollectLogs(context), serverData, null, HotUpdateManifest.ManifestPath);
        });
    }

    public static HotUpdateBuildResult RunResourcePatch(HotUpdateBuildContext context)
    {
        return RunInternal(context, () =>
        {
            PreflightCheckStep.Execute(context);
            AssetChangeSummaryStep.Execute(context);
            AddressablesSyncStep.Execute(context);
            string serverData = AddressablesContentUpdateStep.Execute(context, ContentUpdateScope.ResourceOnly);
            ManifestWriteStep.Execute(context, CreatePatch("resource", context), null);
            return HotUpdateBuildResult.Succeeded(CollectLogs(context), serverData, null, HotUpdateManifest.ManifestPath);
        });
    }

    // RunInternal: 检查 _isRunning，try/finally 释放锁，catch 返回 Failed
}
```

- [ ] **Step 2: 编译验证**

Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Editor/HotUpdate/Pipeline/HotUpdateBuildPipeline.cs
git commit -m "feat(editor): add HotUpdateBuildPipeline orchestrator"
```

---

### Task 9: LocalDevEnvironment 迁移

**Files:**
- Create: `Assets/Scripts/Editor/HotUpdate/Dev/LocalDevEnvironment.cs`
- Delete: `Assets/Scripts/Editor/Addressables/AddressablesLocalHotUpdateEnvironment.cs`
- Modify: `Assets/Scripts/Editor/Addressables/AddressablesContentSettingsEditor.cs`

**Interfaces:**
- Produces: `LocalDevEnvironment.ConfigureLocalEnvironment(AddressablesContentSettings settings)`
- Produces: `LocalDevEnvironment.StartLocalHttpServer(...)` / `StopLocalHttpServer()`
- Produces: `LocalDevEnvironment.DevRemoteUrlPrefsKey`（常量，替代旧类名）

- [ ] **Step 1: 迁移 LocalDevEnvironment.cs**

从 `AddressablesLocalHotUpdateEnvironment.cs` 复制逻辑，命名空间 `Rogue.Editor.HotUpdate.Dev`，**删除全部 MenuItem**。

- [ ] **Step 2: 更新 AddressablesContentSettingsEditor**

- 将所有 `AddressablesLocalHotUpdateEnvironment` 引用改为 `LocalDevEnvironment`
- **删除** `DrawActionButtons()` 中的配置/HTTP/清除 PlayerPrefs 按钮
- 保留只读环境状态 HelpBox，底部加按钮：

```csharp
if (GUILayout.Button("打开发布中心"))
{
    HotUpdatePublishWindow.ShowWindow();
}
```

- [ ] **Step 3: 删除旧文件**

```bash
git rm Assets/Scripts/Editor/Addressables/AddressablesLocalHotUpdateEnvironment.cs
```

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Editor/HotUpdate/Dev/LocalDevEnvironment.cs \
        Assets/Scripts/Editor/Addressables/AddressablesContentSettingsEditor.cs
git commit -m "refactor(editor): move local dev environment to HotUpdate/Dev"
```

---

### Task 10: HotUpdatePublishWindow UI

**Files:**
- Create: `Assets/Scripts/Editor/HotUpdate/HotUpdatePublishWindow.cs`

**Interfaces:**
- Consumes: `HotUpdateBuildPipeline.RunFullPackage/RunCodePatch/RunResourcePatch`
- Consumes: `LocalDevEnvironment`
- Produces: `[MenuItem("Tools/发布/热更发布中心")]`

- [ ] **Step 1: 创建 Window 骨架**

```csharp
namespace Rogue.Editor.HotUpdate
{
    public sealed class HotUpdatePublishWindow : EditorWindow
    {
        private enum Tab { Publish, DevTest }

        [MenuItem("Tools/发布/热更发布中心")]
        public static void ShowWindow()
        {
            GetWindow<HotUpdatePublishWindow>("热更发布中心");
        }

        private Tab _tab = Tab.Publish;
        private Vector2 _logScroll;
        private readonly List<string> _logs = new List<string>();
        private bool _isBuilding;
        private BuildTarget _buildTarget;
        private string _version;
        private string _remoteBaseUrl;
        private bool _developmentBuild;
        private bool _skipPlayerBuild;
        private bool _forceSkipAotCheck;
        private bool _showAdvanced;

        private void OnEnable()
        {
            _buildTarget = EditorUserBuildSettings.activeBuildTarget;
            _version = PlayerSettings.bundleVersion;
            _developmentBuild = EditorUserBuildSettings.development;
            _remoteBaseUrl = LoadDefaultRemoteUrl();
        }

        private void OnGUI()
        {
            _tab = (Tab)GUILayout.Toolbar((int)_tab, new[] { "发布", "开发测试" });
            EditorGUILayout.Space(4);

            switch (_tab)
            {
                case Tab.Publish:
                    DrawPublishTab();
                    break;
                case Tab.DevTest:
                    DrawDevTestTab();
                    break;
            }

            DrawLogPanel();
        }
    }
}
```

- [ ] **Step 2: 实现发布 Tab**

- 公共控件：平台、版本、CDN URL、Development、构建前检查按钮
- 三个大按钮（`GUI.enabled = !_isBuilding`）：
  - 首包 → `RunBuild(HotUpdateBuildPipeline.RunFullPackage)`
  - 代码热更 → `RunBuild(HotUpdateBuildPipeline.RunCodePatch)`
  - 资源热更 → `RunBuild(HotUpdateBuildPipeline.RunResourcePatch)`
- 高级折叠：SkipPlayerBuild、ForceSkipAotCheck
- CodePatch 常显 HelpBox：「需重启 App 生效；AOT 变更请首包」

- [ ] **Step 3: 实现开发测试 Tab**

迁移本地环境配置、HTTP 启停、ForceLocalOnly 开关、环境状态只读面板。

- [ ] **Step 4: 实现异步构建与日志**

使用 `EditorApplication.update` 或 `async void` + `EditorUtility.DisplayProgressBar`：

```csharp
private void RunBuild(Func<HotUpdateBuildContext, HotUpdateBuildResult> pipelineFunc, HotUpdateBuildMode mode)
{
    if (_isBuilding) return;
    _isBuilding = true;
    _logs.Clear();

    var logCollector = new Progress<string>(msg =>
    {
        _logs.Add(msg);
        Repaint();
    });

    var context = new HotUpdateBuildContext(
        mode, _buildTarget, _version, _remoteBaseUrl, _developmentBuild, logCollector)
    {
        SkipPlayerBuild = _skipPlayerBuild,
        ForceSkipAotChangeCheck = _forceSkipAotCheck,
    };

    try
    {
        EditorUtility.DisplayProgressBar("热更发布", "构建中...", 0.5f);
        HotUpdateBuildResult result = pipelineFunc(context);
        if (result.Success)
        {
            _logs.Add($"✓ 构建成功: {result.ServerDataPath}");
        }
        else
        {
            _logs.Add($"✗ 构建失败: {result.ErrorMessage}");
        }
    }
    catch (Exception ex)
    {
        _logs.Add($"✗ 异常: {ex.Message}");
        Debug.LogException(ex);
    }
    finally
    {
        EditorUtility.ClearProgressBar();
        _isBuilding = false;
        Repaint();
    }
}
```

- [ ] **Step 5: 手动验收**

| 操作 | 预期 |
|------|------|
| MenuItem 打开 Window | 正常显示 |
| 构建前检查 | 日志输出检查结果 |
| 首包（Skip Player） | ServerData 生成，manifest 写入 |
| 重复点击构建 | 第二次被 `_isBuilding` 阻止 |

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Editor/HotUpdate/HotUpdatePublishWindow.cs
git commit -m "feat(editor): add HotUpdatePublishWindow with three build actions"
```

---

### Task 11: 清理旧 MenuItem 与文档

**Files:**
- Modify: `Assets/Scripts/Editor/Addressables/RuntimeAddressablesConfigurator.cs`（确认无残留 MenuItem）
- Modify: `Assets/Scripts/Editor/CLAUDE.md`（若存在 Addressables 工具说明）
- Modify: `.gitignore`（可选添加 `Build/` 产物，保留 manifest 策略见 spec）

- [ ] **Step 1: grep 确认旧 MenuItem 已清除**

Run:
```bash
rg "MenuItem.*Tools/Addressables" Assets/Scripts/Editor
rg "MenuItem.*Tools/HybridCLR/正式入口" Assets/Scripts/Editor
```
Expected: 无匹配（或仅 Obsolete 注释）

- [ ] **Step 2: 更新 Editor CLAUDE.md 工具说明**

将 Addressables/HybridCLR 分散菜单描述替换为「热更发布中心」单一入口。

- [ ] **Step 3: 运行 EditMode 全量测试**

Run: Unity Test Runner EditMode
Expected: `HotUpdateAddressablesResolverTests`、`HotUpdateManifestTests`、`AotChangeDetectorStepTests` 均 PASS

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Editor/CLAUDE.md .gitignore
git commit -m "docs(editor): document hot update publish center and remove old menus"
```

---

## Spec Coverage Self-Review

| Spec 章节 | 对应 Task |
|-----------|-----------|
| §4 EditorWindow 三工具 | Task 10 |
| §4.3 开发测试 Tab | Task 9 + 10 |
| §5 Pipeline + Steps | Task 1–8 |
| §6 HotUpdateManifest | Task 2 + 7 |
| §7 删除 MenuItem | Task 3–4 + 9 + 11 |
| §8 目录结构 | Task 1–10 |
| §9 Phase 2 扩展 | 未纳入 Phase 1（符合 spec） |
| §10 错误处理 | Task 6 + 8 + 10 |
| §11 测试计划 | Task 2 + 6 + 11 |

无 TBD / 占位符。

---

## Manual Test Checklist（Phase 1 完成标准）

- [ ] `Tools/发布/热更发布中心` 可打开，三按钮可用
- [ ] 首包构建（Skip Player）生成 `ServerData/{BuildTarget}/`
- [ ] `Build/HotUpdateManifest.json` 含 `contentStatePath`
- [ ] 代码热更不弹 content state 文件选择框
- [ ] AOT 变更后代码热更被阻断
- [ ] 资源热更生成 ServerData patch
- [ ] 开发测试 Tab 可启停本地 HTTP
- [ ] BulletFactory 等内容工厂调用 `SyncRuntimeAssets()` 仍正常
- [ ] 旧 `Tools/Addressables/*` 构建菜单不存在

---

**Plan complete and saved to `docs/superpowers/plans/2026-07-04-hotupdate-publish-tools.md`. Two execution options:**

**1. Subagent-Driven (recommended)** — 每个 Task 派发独立 subagent，Task 间做 review，迭代更快

**2. Inline Execution** — 在本会话按 Task 顺序直接实施，批次间设检查点

**Which approach?**
