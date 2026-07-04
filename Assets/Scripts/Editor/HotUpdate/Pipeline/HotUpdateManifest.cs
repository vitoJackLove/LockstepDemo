using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Rogue.Editor.HotUpdate.Pipeline
{
    /// <summary>
    /// 热更发布 manifest 数据。
    /// </summary>
    [Serializable]
    public sealed class HotUpdateManifestData
    {
        public string AppVersion = "0.0.0";
        public string BuildTarget = string.Empty;
        public string LastFullBuildUtc = string.Empty;
        public string ContentStatePath = string.Empty;
        public string RemoteBaseUrl = string.Empty;
        public string GameRuntimeHash = string.Empty;
        public HotUpdateManifestHashEntry[] AotMetadataHashes = Array.Empty<HotUpdateManifestHashEntry>();
        public HotUpdateManifestPatchEntry[] Patches = Array.Empty<HotUpdateManifestPatchEntry>();
    }

    /// <summary>
    /// AOT 元数据文件哈希条目。
    /// </summary>
    [Serializable]
    public sealed class HotUpdateManifestHashEntry
    {
        public string FileName;
        public string Hash;
    }

    /// <summary>
    /// 热更补丁记录条目。
    /// </summary>
    [Serializable]
    public sealed class HotUpdateManifestPatchEntry
    {
        public string Type;
        public string Version;
        public string GeneratedAtUtc;
        public string PlayerOutputPath;
        public string ServerDataPath;
        public string GameRuntimeHash;
        public int BundleCount;
        public string ChangedGroupsCsv;
    }

    /// <summary>
    /// 读写 Build/HotUpdateManifest.json 并解析 content state。
    /// </summary>
    public static class HotUpdateManifest
    {
        private const string DefaultRelativePath = "Build/HotUpdateManifest.json";
        private static string _overridePathForTests;

        public static string ManifestPath =>
            _overridePathForTests ?? Path.Combine(Directory.GetCurrentDirectory(), DefaultRelativePath);

        internal static void SetManifestPathForTests(string path) => _overridePathForTests = path;

        internal static void ResetManifestPathForTests() => _overridePathForTests = null;

        public static HotUpdateManifestData Load()
        {
            if (!File.Exists(ManifestPath))
            {
                return new HotUpdateManifestData();
            }

            string json = File.ReadAllText(ManifestPath, Encoding.UTF8);
            return JsonUtility.FromJson<HotUpdateManifestData>(json) ?? new HotUpdateManifestData();
        }

        public static void Save(HotUpdateManifestData data)
        {
            string dir = Path.GetDirectoryName(ManifestPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(ManifestPath, json, Encoding.UTF8);
        }

        public static bool TryResolveContentStatePath(out string contentStatePath)
        {
            return HotUpdateContentStatePathUtility.TryResolve(out contentStatePath);
        }

        public static string ComputeFileSha256(string absolutePath)
        {
            using FileStream stream = File.OpenRead(absolutePath);
            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(stream);
            return "sha256:" + BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
