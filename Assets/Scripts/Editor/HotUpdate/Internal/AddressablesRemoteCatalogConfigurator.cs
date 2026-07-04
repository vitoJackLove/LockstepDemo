using UnityEditor;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;

namespace Rogue.Editor.HotUpdate.Internal
{
    /// <summary>
    /// 确保 Addressables 远程 Catalog 配置满足 Content Update 要求。
    /// </summary>
    internal static class AddressablesRemoteCatalogConfigurator
    {
        /// <summary>
        /// 启用 Build Remote Catalog，并将 Catalog 路径绑定到 Remote Profile 变量。
        /// </summary>
        public static void EnsureRemoteCatalogEnabled(AddressableAssetSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            bool changed = false;

            if (!settings.BuildRemoteCatalog)
            {
                settings.BuildRemoteCatalog = true;
                changed = true;
            }

            ProfileValueReference buildPath = settings.RemoteCatalogBuildPath;
            buildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);

            ProfileValueReference loadPath = settings.RemoteCatalogLoadPath;
            loadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);

            EditorUtility.SetDirty(settings);
            if (changed)
            {
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// 校验 content state 是否来自启用 Remote Catalog 的首包构建。
        /// </summary>
        public static bool TryValidateContentStateForUpdate(
            AddressableAssetSettings settings,
            string contentStatePath,
            out string errorMessage)
        {
            errorMessage = null;

            if (settings == null)
            {
                errorMessage = "AddressableAssetSettings 不存在。";
                return false;
            }

            if (!settings.BuildRemoteCatalog)
            {
                errorMessage =
                    "当前 Addressables 设置未启用 Build Remote Catalog。Pipeline 将自动修复，但需重新执行首包构建。";
                return false;
            }

            AddressablesContentState cacheData = ContentUpdateScript.LoadContentState(contentStatePath);
            if (cacheData == null)
            {
                errorMessage = $"无法读取 content state: {contentStatePath}";
                return false;
            }

            if (string.IsNullOrEmpty(cacheData.remoteCatalogLoadPath))
            {
                errorMessage =
                    "上次首包构建未启用 Remote Catalog（content state 中 remoteCatalogLoadPath 为空）。" +
                    "请重新执行「构建首包」后再打包资源/代码热更。";
                return false;
            }

            string currentLoadPath = settings.RemoteCatalogLoadPath.GetValue(settings);
            if (cacheData.remoteCatalogLoadPath != currentLoadPath)
            {
                errorMessage =
                    $"Remote Catalog Load Path 与首包不一致。\n" +
                    $"  首包: {cacheData.remoteCatalogLoadPath}\n" +
                    $"  当前: {currentLoadPath}\n" +
                    "请保持 CDN 根 URL 与 Profile 一致，或重新执行首包构建。";
                return false;
            }

            return true;
        }
    }
}
