using System;
using System.IO;
using Rogue.Editor.HotUpdate.Internal;
using Rogue.Editor.HotUpdate.Pipeline;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;

namespace Rogue.Editor.HotUpdate.Pipeline.Steps
{
    /// <summary>
    /// 执行 Addressables 全量构建（BuildPlayerContent）。
    /// </summary>
    public static class AddressablesFullBuildStep
    {
        /// <summary>
        /// 执行全量 Addressables 构建并返回 ServerData 输出目录。
        /// </summary>
        /// <param name="context">热更构建上下文。</param>
        /// <returns>ServerData/{BuildTarget} 绝对路径。</returns>
        public static string Execute(HotUpdateBuildContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

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
}
