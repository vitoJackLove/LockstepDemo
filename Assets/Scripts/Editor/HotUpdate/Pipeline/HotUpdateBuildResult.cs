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
