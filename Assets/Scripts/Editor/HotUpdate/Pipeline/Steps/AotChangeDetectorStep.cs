using System.Collections.Generic;
using System.IO;
using System.Linq;
using HybridCLR.Editor;
using Rogue.Editor.HotUpdate.Pipeline;
using UnityEngine;

namespace Rogue.Editor.HotUpdate.Pipeline.Steps
{
    /// <summary>
    /// 检测 AOT 元数据是否相对上次首包构建发生变更。
    /// </summary>
    public static class AotChangeDetectorStep
    {
        private const string HotUpdateAotAssetDir = "Assets/HotUpdate/Code/AOT";

        /// <summary>
        /// 若 AOT 元数据 hash 或程序集列表与 manifest 记录不一致则返回 true。
        /// </summary>
        public static bool HasAotChanges()
        {
            HotUpdateManifestData manifest = HotUpdateManifest.Load();
            if (manifest.AotMetadataHashes == null || manifest.AotMetadataHashes.Length == 0)
            {
                return false;
            }

            Dictionary<string, string> recordedHashes = manifest.AotMetadataHashes
                .Where(entry => !string.IsNullOrEmpty(entry?.FileName))
                .ToDictionary(entry => entry.FileName, entry => entry.Hash);

            int currentAssemblyCount = SettingsUtil.AOTAssemblyNames.Count;
            if (recordedHashes.Count != currentAssemblyCount)
            {
                return true;
            }

            string aotDir = GetAbsoluteAssetPath(HotUpdateAotAssetDir);
            if (!Directory.Exists(aotDir))
            {
                return recordedHashes.Count > 0;
            }

            string[] currentFiles = Directory.GetFiles(aotDir, "*.dll.bytes");
            if (currentFiles.Length != recordedHashes.Count)
            {
                return true;
            }

            for (int i = 0; i < currentFiles.Length; i++)
            {
                string fileName = Path.GetFileName(currentFiles[i]);
                string currentHash = HotUpdateManifest.ComputeFileSha256(currentFiles[i]);
                if (!recordedHashes.TryGetValue(fileName, out string recordedHash)
                    || !string.Equals(recordedHash, currentHash, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            foreach (string recordedFileName in recordedHashes.Keys)
            {
                if (!File.Exists(Path.Combine(aotDir, recordedFileName)))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetAbsoluteAssetPath(string assetPath)
        {
            return Path.Combine(
                Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath,
                assetPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        }
    }
}
