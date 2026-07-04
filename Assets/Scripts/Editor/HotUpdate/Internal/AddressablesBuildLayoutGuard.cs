using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;

namespace Rogue.Editor.HotUpdate.Internal
{
    /// <summary>
    /// 在 Addressables 构建前禁用 Build Layout 报告，避免构建后自动打开报告窗口。
    /// </summary>
    [InitializeOnLoad]
    public static class AddressablesBuildLayoutGuard
    {
        private static Func<AddressableAssetSettings, AddressablesPlayerBuildResult> _previousBuildOverride;

        static AddressablesBuildLayoutGuard()
        {
            InstallPlayerBuildOverride();
            PrepareForAddressablesBuild(false);
        }

        /// <summary>
        /// 禁用 Build Layout 报告并清理历史报告路径。
        /// </summary>
        /// <param name="logResult">是否在 Console 输出状态日志。</param>
        public static void PrepareForAddressablesBuild(bool logResult)
        {
            bool changed = false;

            if (ProjectConfigData.GenerateBuildLayout)
            {
                ProjectConfigData.GenerateBuildLayout = false;
                changed = true;
            }

            if (DisableAutoOpenAddressablesReport())
            {
                changed = true;
            }

            if (ProjectConfigData.BuildReportFilePaths.Count > 0)
            {
                ProjectConfigData.ClearBuildReportFilePaths();
                changed = true;
            }

            if (logResult)
            {
                string state = changed ? "disabled and cleared" : "already disabled";
                GameLog.Info(GameLogChannel.Resource, $"Addressables Build Layout report is {state}.");
            }
        }

        private static void InstallPlayerBuildOverride()
        {
            if (AddressablesPlayerBuildProcessor.BuildAddressablesOverride == BuildPlayerContentWithoutBuildLayout)
            {
                return;
            }

            _previousBuildOverride = AddressablesPlayerBuildProcessor.BuildAddressablesOverride;
            AddressablesPlayerBuildProcessor.BuildAddressablesOverride = BuildPlayerContentWithoutBuildLayout;
        }

        private static AddressablesPlayerBuildResult BuildPlayerContentWithoutBuildLayout(AddressableAssetSettings settings)
        {
            PrepareForAddressablesBuild(false);

            if (_previousBuildOverride != null && _previousBuildOverride != BuildPlayerContentWithoutBuildLayout)
            {
                return _previousBuildOverride(settings);
            }

            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            return result;
        }

        private static bool DisableAutoOpenAddressablesReport()
        {
            PropertyInfo property = typeof(ProjectConfigData).GetProperty(
                "AutoOpenAddressablesReport",
                BindingFlags.Static | BindingFlags.NonPublic);

            if (property == null || !property.CanRead || !property.CanWrite)
            {
                return false;
            }

            object value = property.GetValue(null);
            if (value is bool enabled && enabled)
            {
                property.SetValue(null, false);
                return true;
            }

            return false;
        }
    }
}
