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
