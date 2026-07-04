using System;
using System.IO;
using System.Linq;
using Rogue.Editor.HotUpdate.Pipeline;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Rogue.Editor.HotUpdate.Pipeline.Steps
{
    /// <summary>
    /// 构建 IL2CPP Player 可执行产物。
    /// </summary>
    public static class PlayerBuildStep
    {
        /// <summary>
        /// 执行 Player 构建并返回产物路径。
        /// </summary>
        /// <param name="context">热更构建上下文。</param>
        /// <returns>Player 可执行文件或包体路径。</returns>
        public static string Execute(HotUpdateBuildContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            string outputDir = Path.Combine("Build", context.BuildTarget.ToString(), context.Version);
            Directory.CreateDirectory(outputDir);

            string locationPathName = Path.Combine(
                outputDir,
                PlayerSettings.productName + GetPlayerExtension(context.BuildTarget));

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(scene => scene.path)
                    .ToArray(),
                locationPathName = locationPathName,
                target = context.BuildTarget,
                options = context.DevelopmentBuild ? BuildOptions.Development : BuildOptions.None,
            };

            context.LogLine($"[PlayerBuild] 开始构建 Player: {locationPathName}");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Player 构建失败: {report.summary.result}");
            }

            context.LogLine($"[PlayerBuild] 构建成功: {locationPathName}");
            return locationPathName;
        }

        private static string GetPlayerExtension(BuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return ".exe";
                default:
                    return string.Empty;
            }
        }
    }
}
