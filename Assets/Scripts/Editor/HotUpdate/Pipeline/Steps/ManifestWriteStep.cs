using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rogue.Editor.HotUpdate.Pipeline;
using UnityEngine;

namespace Rogue.Editor.HotUpdate.Pipeline.Steps
{
    /// <summary>
    /// 写入 Build/HotUpdateManifest.json，记录 content state、hash 与补丁历史。
    /// </summary>
    public static class ManifestWriteStep
    {
        private const string HotUpdateRuntimeDllAssetPath = "Assets/HotUpdate/Code/Game.Runtime.dll.bytes";
        private const string HotUpdateAotAssetDir = "Assets/HotUpdate/Code/AOT";

        /// <summary>
        /// 更新 manifest 并追加补丁记录。
        /// </summary>
        /// <param name="context">热更构建上下文。</param>
        /// <param name="patch">本次补丁条目。</param>
        /// <param name="contentStatePath">首包构建后的 content state 绝对路径；补丁构建可传 null。</param>
        public static void Execute(
            HotUpdateBuildContext context,
            HotUpdateManifestPatchEntry patch,
            string contentStatePath)
        {
            HotUpdateManifestData manifest = HotUpdateManifest.Load();
            manifest.AppVersion = context.Version;
            manifest.BuildTarget = context.BuildTarget.ToString();
            manifest.RemoteBaseUrl = context.RemoteBaseUrl ?? string.Empty;

            if (!string.IsNullOrEmpty(contentStatePath))
            {
                manifest.ContentStatePath = contentStatePath;
                manifest.LastFullBuildUtc = DateTime.UtcNow.ToString("O");
                manifest.AotMetadataHashes = CollectAotMetadataHashes();
                manifest.GameRuntimeHash = ComputeGameRuntimeHash();
            }

            List<HotUpdateManifestPatchEntry> patches = manifest.Patches?.ToList() ?? new List<HotUpdateManifestPatchEntry>();
            patches.Add(patch);
            manifest.Patches = patches.ToArray();

            HotUpdateManifest.Save(manifest);
            context.LogLine($"[Manifest] 已写入 {HotUpdateManifest.ManifestPath}");
        }

        /// <summary>
        /// 创建补丁记录条目。
        /// </summary>
        /// <param name="patchType">补丁类型：full / code / resource。</param>
        /// <param name="context">热更构建上下文。</param>
        /// <param name="serverDataPath">ServerData 输出目录。</param>
        /// <param name="playerOutputPath">Player 产物路径（可选）。</param>
        /// <param name="changedGroupsCsv">变更分组 CSV（可选）。</param>
        /// <returns>补丁条目。</returns>
        public static HotUpdateManifestPatchEntry CreatePatchEntry(
            string patchType,
            HotUpdateBuildContext context,
            string serverDataPath,
            string playerOutputPath = null,
            string changedGroupsCsv = null)
        {
            return new HotUpdateManifestPatchEntry
            {
                Type = patchType,
                Version = ResolvePatchVersion(patchType, context.Version),
                GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
                PlayerOutputPath = playerOutputPath ?? string.Empty,
                ServerDataPath = serverDataPath ?? string.Empty,
                GameRuntimeHash = ComputeGameRuntimeHash(),
                BundleCount = CountBundles(serverDataPath),
                ChangedGroupsCsv = changedGroupsCsv ?? string.Empty,
            };
        }

        private static string ResolvePatchVersion(string patchType, string appVersion)
        {
            switch (patchType)
            {
                case "full":
                    return appVersion;
                case "code":
                    return $"{appVersion}-code";
                case "resource":
                    return $"{appVersion}-res";
                default:
                    return appVersion;
            }
        }

        private static HotUpdateManifestHashEntry[] CollectAotMetadataHashes()
        {
            string aotDir = GetAbsoluteAssetPath(HotUpdateAotAssetDir);
            if (!Directory.Exists(aotDir))
            {
                return Array.Empty<HotUpdateManifestHashEntry>();
            }

            return Directory.GetFiles(aotDir, "*.dll.bytes")
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(path => new HotUpdateManifestHashEntry
                {
                    FileName = Path.GetFileName(path),
                    Hash = HotUpdateManifest.ComputeFileSha256(path),
                })
                .ToArray();
        }

        private static string ComputeGameRuntimeHash()
        {
            string runtimeDllPath = GetAbsoluteAssetPath(HotUpdateRuntimeDllAssetPath);
            if (!File.Exists(runtimeDllPath))
            {
                return string.Empty;
            }

            return HotUpdateManifest.ComputeFileSha256(runtimeDllPath);
        }

        private static int CountBundles(string serverDataPath)
        {
            if (string.IsNullOrEmpty(serverDataPath) || !Directory.Exists(serverDataPath))
            {
                return 0;
            }

            return Directory.GetFiles(serverDataPath, "*", SearchOption.AllDirectories)
                .Count(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)
                    && !path.EndsWith(".hash", StringComparison.OrdinalIgnoreCase)
                    && !path.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
        }

        private static string GetAbsoluteAssetPath(string assetPath)
        {
            return Path.Combine(
                Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath,
                assetPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        }
    }
}
