using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;

namespace Rogue.Editor.HotUpdate.Pipeline
{
    /// <summary>
    /// 解析 Addressables content state（addressables_content_state.bin）路径。
    /// </summary>
    internal static class HotUpdateContentStatePathUtility
    {
        private static readonly string[] FallbackSearchRoots =
        {
            "Assets/AddressableAssetsData",
            "Assets/Scripts/Libraries/AddressableAssetsData",
            "Assets/Config/AddressableAssetsData",
            "Library/com.unity.addressables",
        };

        /// <summary>
        /// 返回 Addressables 配置下的 content state 预期路径（不要求文件已存在）。
        /// </summary>
        public static string GetConfiguredContentStatePath()
        {
            if (AddressableAssetSettingsDefaultObject.Settings == null)
            {
                return string.Empty;
            }

            return ContentUpdateScript.GetContentStateDataPath(browse: false);
        }

        /// <summary>
        /// 解析可用于 Content Update 的 content state 绝对路径。
        /// </summary>
        public static bool TryResolve(out string contentStatePath)
        {
            HotUpdateManifestData manifest = HotUpdateManifest.Load();
            if (IsExistingFile(manifest.ContentStatePath))
            {
                contentStatePath = NormalizePath(manifest.ContentStatePath);
                return true;
            }

            string configuredPath = GetConfiguredContentStatePath();
            if (IsExistingFile(configuredPath))
            {
                contentStatePath = NormalizePath(configuredPath);
                return true;
            }

            string newest = FindNewestContentStateFile();
            if (!string.IsNullOrEmpty(newest))
            {
                contentStatePath = newest;
                return true;
            }

            contentStatePath = null;
            return false;
        }

        /// <summary>
        /// 全量构建后解析 content state，优先使用 Addressables 配置路径。
        /// </summary>
        public static string ResolveAfterFullBuild()
        {
            string configuredPath = GetConfiguredContentStatePath();
            if (IsExistingFile(configuredPath))
            {
                return NormalizePath(configuredPath);
            }

            if (HotUpdateManifest.TryResolveContentStatePath(out string resolved))
            {
                return resolved;
            }

            return configuredPath;
        }

        private static string FindNewestContentStateFile()
        {
            string projectRoot = Directory.GetCurrentDirectory();
            List<string> candidates = new List<string>();

            for (int i = 0; i < FallbackSearchRoots.Length; i++)
            {
                string searchRoot = Path.Combine(projectRoot, FallbackSearchRoots[i].Replace('/', Path.DirectorySeparatorChar));
                if (!Directory.Exists(searchRoot))
                {
                    continue;
                }

                candidates.AddRange(Directory.GetFiles(
                    searchRoot,
                    "addressables_content_state.bin",
                    SearchOption.AllDirectories));
            }

            return candidates
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .Select(NormalizePath)
                .FirstOrDefault();
        }

        private static bool IsExistingFile(string path)
        {
            return !string.IsNullOrEmpty(path) && File.Exists(path);
        }

        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(path).Replace('\\', '/');
        }
    }
}
