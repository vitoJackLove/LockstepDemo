# Task 1: Pipeline 基础类型

**Files:**
- Create: `Assets/Scripts/Editor/HotUpdate/Pipeline/HotUpdateBuildMode.cs`
- Create: `Assets/Scripts/Editor/HotUpdate/Pipeline/HotUpdateBuildContext.cs`
- Create: `Assets/Scripts/Editor/HotUpdate/Pipeline/HotUpdateBuildResult.cs`

**Interfaces:**
- Produces: `HotUpdateBuildMode` enum `{ FullPackage, CodePatch, ResourcePatch }`
- Produces: `HotUpdateBuildContext` 构造与属性
- Produces: `HotUpdateBuildResult` 成功/失败结果

Namespace: `Rogue.Editor.HotUpdate.Pipeline`
新增公共类型需中文 XML 注释。

## Step 1: 创建 HotUpdateBuildMode.cs

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

## Step 2: 创建 HotUpdateBuildContext.cs

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

## Step 3: 创建 HotUpdateBuildResult.cs

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

## Step 4: 编译验证

Run: `dotnet build Assets/Scripts/Editor/Game.Editor.csproj -nologo -v:minimal` (or find correct Editor csproj path in repo)
Expected: Build succeeded

## Step 5: Commit

```bash
git add Assets/Scripts/Editor/HotUpdate/Pipeline/HotUpdateBuildMode.cs \
        Assets/Scripts/Editor/HotUpdate/Pipeline/HotUpdateBuildContext.cs \
        Assets/Scripts/Editor/HotUpdate/Pipeline/HotUpdateBuildResult.cs
git commit -m "feat(editor): add hot update build pipeline core types"
```
