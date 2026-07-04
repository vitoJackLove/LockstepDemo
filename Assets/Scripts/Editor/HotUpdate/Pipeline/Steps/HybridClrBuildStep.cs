using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using Rogue.Editor.HotUpdate.Pipeline;
using UnityEditor;
using UnityEngine;

namespace Rogue.Editor.HotUpdate.Pipeline.Steps
{
    /// <summary>
    /// 编译 HybridCLR 热更程序集，同步 DLL 到 Assets/HotUpdate/Code/ 并触发 Addressables 同步。
    /// </summary>
    public static class HybridClrBuildStep
    {
        private const string HotUpdateCodeRoot = "Assets/HotUpdate/Code";
        private const string HotUpdateAotDir = HotUpdateCodeRoot + "/AOT";
        private const string HotUpdateRuntimeDllAssetPath = HotUpdateCodeRoot + "/Game.Runtime.dll.bytes";
        private const string ManifestFileName = "manifest.json";

        /// <summary>
        /// 执行 HybridCLR 编译与 DLL 同步。
        /// </summary>
        /// <param name="context">热更构建上下文。</param>
        /// <param name="compileBeforeCopy">是否在拷贝前先执行 CompileDll。</param>
        public static void Execute(HotUpdateBuildContext context, bool compileBeforeCopy)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (compileBeforeCopy)
            {
                context.LogLine("[HybridCLR] CompileDll...");
                CompileDllCommand.CompileDll(context.BuildTarget, context.DevelopmentBuild);
            }

            context.LogLine("[HybridCLR] 同步 DLL 到 Assets/HotUpdate/Code/...");
            CopyResult result = CopyDlls(context, context.BuildTarget, compileBeforeCopy);
            AssetDatabase.Refresh();

            context.LogLine(
                $"[HybridCLR] 热更 DLL ({result.CopiedHotUpdateFiles.Count}): {string.Join(", ", result.CopiedHotUpdateFiles)}");
            context.LogLine(
                $"[HybridCLR] AOT 元数据 ({result.CopiedAotFiles.Count}): {string.Join(", ", result.CopiedAotFiles)}");

            AddressablesSyncStep.Execute(context);
        }

        private static CopyResult CopyDlls(
            HotUpdateBuildContext context,
            BuildTarget target,
            bool compiledBeforeCopy)
        {
            string hotUpdateSourceDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
            string aotSourceDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);

            if (!Directory.Exists(hotUpdateSourceDir))
            {
                throw new DirectoryNotFoundException(
                    $"未找到热更新 dll 目录: {hotUpdateSourceDir}。请先执行 HybridCLR/CompileDll/ActiveBuildTarget。");
            }

            if (!Directory.Exists(aotSourceDir))
            {
                throw new DirectoryNotFoundException(
                    $"未找到裁剪后的 AOT dll 目录: {aotSourceDir}。请先执行 HybridCLR/Generate/AotDlls，或完成一次 il2cpp 打包。");
            }

            Directory.CreateDirectory(GetAbsoluteAssetPath(HotUpdateAotDir));

            List<string> copiedHotUpdateFiles = CopyHotUpdateAssembly(
                context,
                SettingsUtil.HotUpdateAssemblyFilesExcludePreserved,
                hotUpdateSourceDir);

            List<string> copiedAotFiles = CopyAssemblyFiles(
                context,
                SettingsUtil.AOTAssemblyNames.Select(name => $"{name}.dll").ToList(),
                aotSourceDir,
                GetAbsoluteAssetPath(HotUpdateAotDir),
                required: false);

            WriteManifest(
                GetAbsoluteAssetPath(HotUpdateCodeRoot),
                target,
                compiledBeforeCopy,
                hotUpdateSourceDir,
                aotSourceDir,
                copiedHotUpdateFiles,
                copiedAotFiles);

            return new CopyResult(
                HotUpdateCodeRoot,
                hotUpdateSourceDir,
                aotSourceDir,
                copiedHotUpdateFiles,
                copiedAotFiles);
        }

        private static List<string> CopyHotUpdateAssembly(
            HotUpdateBuildContext context,
            IReadOnlyList<string> assemblyFileNames,
            string sourceDir)
        {
            List<string> copiedFiles = new List<string>();
            foreach (string assemblyFileName in assemblyFileNames)
            {
                if (string.IsNullOrWhiteSpace(assemblyFileName))
                {
                    continue;
                }

                string sourcePath = Path.Combine(sourceDir, assemblyFileName);
                if (!File.Exists(sourcePath))
                {
                    throw new FileNotFoundException($"未找到热更新程序集: {sourcePath}");
                }

                string destinationPath = GetAbsoluteAssetPath(HotUpdateRuntimeDllAssetPath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? GetAbsoluteAssetPath(HotUpdateCodeRoot));
                File.Copy(sourcePath, destinationPath, true);
                copiedFiles.Add(Path.GetFileName(HotUpdateRuntimeDllAssetPath));
                context.LogLine($"[HybridCLR] Copied {sourcePath} -> {destinationPath}");
            }

            return copiedFiles;
        }

        private static List<string> CopyAssemblyFiles(
            HotUpdateBuildContext context,
            IReadOnlyList<string> assemblyFileNames,
            string sourceDir,
            string destinationDir,
            bool required)
        {
            List<string> copiedFiles = new List<string>();

            foreach (string assemblyFileName in assemblyFileNames)
            {
                if (string.IsNullOrWhiteSpace(assemblyFileName))
                {
                    continue;
                }

                string sourcePath = Path.Combine(sourceDir, assemblyFileName);
                if (!File.Exists(sourcePath))
                {
                    string message = $"未找到程序集: {sourcePath}";
                    if (required)
                    {
                        throw new FileNotFoundException(message);
                    }

                    context.LogLine($"[HybridCLR] Warning: {message}");
                    continue;
                }

                string destinationFileName = $"{assemblyFileName}.bytes";
                string destinationPath = Path.Combine(destinationDir, destinationFileName);
                File.Copy(sourcePath, destinationPath, true);
                copiedFiles.Add(destinationFileName);
                context.LogLine($"[HybridCLR] Copied {sourcePath} -> {destinationPath}");
            }

            return copiedFiles;
        }

        private static void WriteManifest(
            string destinationDir,
            BuildTarget target,
            bool compiledBeforeCopy,
            string hotUpdateSourceDir,
            string aotSourceDir,
            IReadOnlyList<string> copiedHotUpdateFiles,
            IReadOnlyList<string> copiedAotFiles)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine($"  \"generatedAtUtc\": \"{DateTime.UtcNow:O}\",");
            builder.AppendLine($"  \"buildTarget\": \"{target}\",");
            builder.AppendLine($"  \"compiledBeforeCopy\": {compiledBeforeCopy.ToString().ToLowerInvariant()},");
            builder.AppendLine($"  \"hotUpdateSourceDir\": \"{EscapeJson(hotUpdateSourceDir)}\",");
            builder.AppendLine($"  \"aotSourceDir\": \"{EscapeJson(aotSourceDir)}\",");
            builder.AppendLine("  \"hotUpdateDlls\": [");
            AppendJsonArray(builder, copiedHotUpdateFiles);
            builder.AppendLine("  ],");
            builder.AppendLine("  \"aotMetadataDlls\": [");
            AppendJsonArray(builder, copiedAotFiles);
            builder.AppendLine("  ]");
            builder.AppendLine("}");

            File.WriteAllText(Path.Combine(destinationDir, ManifestFileName), builder.ToString(), Encoding.UTF8);
        }

        private static void AppendJsonArray(StringBuilder builder, IReadOnlyList<string> values)
        {
            for (int i = 0; i < values.Count; i++)
            {
                string suffix = i < values.Count - 1 ? "," : string.Empty;
                builder.AppendLine($"    \"{EscapeJson(values[i])}\"{suffix}");
            }
        }

        private static string EscapeJson(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string GetAbsoluteAssetPath(string assetPath)
        {
            return Path.Combine(
                Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath,
                assetPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        }

        private readonly struct CopyResult
        {
            public CopyResult(
                string destinationDir,
                string hotUpdateSourceDir,
                string aotSourceDir,
                IReadOnlyList<string> copiedHotUpdateFiles,
                IReadOnlyList<string> copiedAotFiles)
            {
                DestinationDir = destinationDir;
                HotUpdateSourceDir = hotUpdateSourceDir;
                AotSourceDir = aotSourceDir;
                CopiedHotUpdateFiles = copiedHotUpdateFiles;
                CopiedAotFiles = copiedAotFiles;
            }

            public string DestinationDir { get; }
            public string HotUpdateSourceDir { get; }
            public string AotSourceDir { get; }
            public IReadOnlyList<string> CopiedHotUpdateFiles { get; }
            public IReadOnlyList<string> CopiedAotFiles { get; }
        }
    }
}
