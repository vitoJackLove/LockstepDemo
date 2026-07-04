using System;
using System.Collections.Generic;
using System.IO;
using HybridCLR.Editor;
using Rogue.Editor.HotUpdate.Internal;
using Rogue.Editor.HotUpdate.Pipeline;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Rogue.Editor.HotUpdate.Pipeline.Steps
{
    /// <summary>
    /// 构建前环境校验，Error 级失败将抛出异常。
    /// </summary>
    public static class PreflightCheckStep
    {
        private const string HotUpdateRuntimeDllAssetPath = "Assets/HotUpdate/Code/Game.Runtime.dll.bytes";
        private const string RemoteBaseUrlVariableName = "AddressablesRemoteBaseUrl";

        /// <summary>
        /// 按构建模式执行 Preflight 检查。
        /// </summary>
        /// <param name="context">热更构建上下文。</param>
        public static void Execute(HotUpdateBuildContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            context.LogLine("[Preflight] 开始构建前检查...");

            List<string> warnings = new List<string>();
            List<string> errors = new List<string>();

            CheckAddressableSettings(errors);
            CheckRemoteBaseUrlProfileVariable(warnings);

            switch (context.Mode)
            {
                case HotUpdateBuildMode.FullPackage:
                    CheckHybridClrSettings(errors);
                    CheckHotUpdateDllOutputDirectory(context, errors);
                    CheckAotStripDirectory(context, errors);
                    CheckHotUpdateRuntimeDll(warnings);
                    break;

                case HotUpdateBuildMode.CodePatch:
                    CheckHybridClrSettings(errors);
                    CheckHotUpdateDllOutputDirectory(context, errors);
                    CheckContentState(errors);
                    CheckHotUpdateRuntimeDll(warnings);
                    break;

                case HotUpdateBuildMode.ResourcePatch:
                    CheckContentState(errors);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(context.Mode), context.Mode, "未知构建模式。");
            }

            for (int i = 0; i < warnings.Count; i++)
            {
                context.LogLine($"[Preflight] Warning: {warnings[i]}");
            }

            for (int i = 0; i < errors.Count; i++)
            {
                context.LogLine($"[Preflight] Error: {errors[i]}");
            }

            if (errors.Count > 0)
            {
                throw new InvalidOperationException($"构建前检查失败（{errors.Count} 项）: {errors[0]}");
            }

            context.LogLine("[Preflight] 检查通过。");
        }

        private static void CheckAddressableSettings(List<string> errors)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                errors.Add("AddressableAssetSettings 不存在，请初始化 Addressables。");
            }
        }

        private static void CheckRemoteBaseUrlProfileVariable(List<string> warnings)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                return;
            }

            string value = settings.profileSettings.GetValueByName(settings.activeProfileId, RemoteBaseUrlVariableName);
            if (string.IsNullOrEmpty(value))
            {
                warnings.Add($"Addressables Profile 变量 '{RemoteBaseUrlVariableName}' 未配置。");
            }
        }

        private static void CheckHybridClrSettings(List<string> errors)
        {
            try
            {
                IReadOnlyList<string> hotUpdateAssemblies = SettingsUtil.HotUpdateAssemblyFilesExcludePreserved;
                if (hotUpdateAssemblies == null || hotUpdateAssemblies.Count == 0)
                {
                    errors.Add("HybridCLR 热更程序集列表为空，请检查 HybridCLR Settings。");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"HybridCLR Settings 不可用: {ex.Message}");
            }
        }

        private static void CheckHotUpdateDllOutputDirectory(HotUpdateBuildContext context, List<string> errors)
        {
            string hotUpdateSourceDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(context.BuildTarget);
            if (!Directory.Exists(hotUpdateSourceDir))
            {
                errors.Add($"HybridCLR 热更 DLL 输出目录不存在: {hotUpdateSourceDir}");
            }
        }

        private static void CheckAotStripDirectory(HotUpdateBuildContext context, List<string> errors)
        {
            string aotSourceDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(context.BuildTarget);
            if (!Directory.Exists(aotSourceDir))
            {
                errors.Add($"HybridCLR AOT strip 目录不存在: {aotSourceDir}。请先执行 Generate/AotDlls 或完成一次 IL2CPP 打包。");
            }
        }

        private static void CheckContentState(List<string> errors)
        {
            if (!HotUpdateManifest.TryResolveContentStatePath(out string path))
            {
                string configured = HotUpdateContentStatePathUtility.GetConfiguredContentStatePath();
                errors.Add(
                    "未找到 Addressables content state，请先执行首包构建。" +
                    (string.IsNullOrEmpty(configured)
                        ? string.Empty
                        : $" 预期路径: {configured}"));
                return;
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressablesRemoteCatalogConfigurator.EnsureRemoteCatalogEnabled(settings);

            if (!AddressablesRemoteCatalogConfigurator.TryValidateContentStateForUpdate(settings, path, out string error))
            {
                errors.Add(error);
            }
        }

        private static void CheckHotUpdateRuntimeDll(List<string> warnings)
        {
            if (!File.Exists(GetAbsoluteAssetPath(HotUpdateRuntimeDllAssetPath)))
            {
                warnings.Add($"热更运行时 DLL 资产不存在: {HotUpdateRuntimeDllAssetPath}");
            }
        }

        private static string GetAbsoluteAssetPath(string assetPath)
        {
            return Path.Combine(
                Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath,
                assetPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        }
    }
}
