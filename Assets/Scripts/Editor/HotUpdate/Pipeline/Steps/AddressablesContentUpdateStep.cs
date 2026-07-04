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
    /// 基于 content state 执行 Addressables Content Update 构建。
    /// </summary>
    public static class AddressablesContentUpdateStep
    {
        /// <summary>
        /// 执行 Content Update 并返回 ServerData 输出目录。
        /// </summary>
        /// <param name="context">热更构建上下文。</param>
        /// <param name="scope">构建范围（代码或资源）。</param>
        /// <returns>ServerData/{BuildTarget} 绝对路径。</returns>
        public static string Execute(HotUpdateBuildContext context, ContentUpdateScope scope)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (!HotUpdateManifest.TryResolveContentStatePath(out string contentStatePath))
            {
                throw new InvalidOperationException("未找到 content state，请先执行首包构建。");
            }

            context.LogLine($"[Addressables] Content Update，state={contentStatePath}，scope={scope}");
            AddressablesBuildLayoutGuard.PrepareForAddressablesBuild(false);

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                throw new InvalidOperationException("AddressableAssetSettings 不存在。");
            }

            AddressablesRemoteCatalogConfigurator.EnsureRemoteCatalogEnabled(settings);

            if (!AddressablesRemoteCatalogConfigurator.TryValidateContentStateForUpdate(
                    settings,
                    contentStatePath,
                    out string validationError))
            {
                throw new InvalidOperationException(validationError);
            }

            AddressablesPlayerBuildResult result = ContentUpdateScript.BuildContentUpdate(settings, contentStatePath);
            if (result == null)
            {
                throw new InvalidOperationException(
                    "Addressables Content Update 失败。请确认上次首包已启用 Build Remote Catalog 并重新执行首包构建。");
            }

            if (!string.IsNullOrEmpty(result.Error))
            {
                throw new InvalidOperationException($"Addressables Content Update 失败: {result.Error}");
            }

            context.LogLine($"[Addressables] Content Update 成功，Locations: {result.LocationCount}");

            return Path.Combine(Directory.GetCurrentDirectory(), "ServerData", context.BuildTarget.ToString());
        }
    }
}
