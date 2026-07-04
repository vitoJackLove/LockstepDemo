using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Rogue.Editor.HotUpdate.Dev
{
    /// <summary>
    /// 本地计算机 Addressables 热更测试环境配置与 HTTP 静态服务。
    /// </summary>
    public static class LocalDevEnvironment
    {
        public const string DevRemoteUrlPrefsKey = "Addressables.RemoteBaseUrlOverride";
        private const string ProfileVariableName = "AddressablesRemoteBaseUrl";

        private static Process _localHttpServerProcess;

        /// <summary>
        /// 本地 HTTP 静态服务是否正在运行。
        /// </summary>
        public static bool IsLocalServerRunning =>
            _localHttpServerProcess != null && !_localHttpServerProcess.HasExited;

        /// <summary>
        /// 一键配置本地热更测试环境（ContentSettings、Profile、PlayerPrefs）。
        /// </summary>
        /// <param name="settings">Addressables 内容配置资产。</param>
        public static void ConfigureLocalEnvironment(AddressablesContentSettings settings)
        {
            if (settings == null)
            {
                Debug.LogError("[HotUpdate] 未找到 AddressablesContentSettings 资产。");
                return;
            }

            Undo.RecordObject(settings, "Configure Local Hot Update Environment");

            int port = settings.LocalDevHttpPort > 0
                ? settings.LocalDevHttpPort
                : AddressablesContentSettings.DefaultLocalDevHttpPort;
            string baseUrl = BuildBaseUrl(port);

            settings.DefaultRemoteBaseUrl = baseUrl;
            settings.LocalDevRemoteBaseUrl = baseUrl;
            settings.LocalDevHttpPort = port;
            settings.UseLocalDevRemoteInEditor = true;
            settings.ForceLocalOnly = false;
            settings.EnableCatalogUpdateOnStartup = true;
            settings.EnableHotUpdateCatalogCheck = true;

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            SyncAddressablesProfileRemoteBaseUrl(baseUrl);
            PlayerPrefs.SetString(DevRemoteUrlPrefsKey, baseUrl);
            PlayerPrefs.Save();

            string buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString();
            string serverDataPath = GetServerDataRootPath();
            Debug.Log(
                "[HotUpdate] 本地热更环境已配置。\n" +
                $"  远程根 URL：{baseUrl}\n" +
                $"  运行时加载前缀：{baseUrl}/{buildTarget}\n" +
                $"  ServerData：{serverDataPath}\n" +
                "  下一步：在「热更发布中心」构建内容后启动本地 HTTP 服务，再 Play 或打 Development 包验证。");
        }

        /// <summary>
        /// 启动本地 HTTP 静态服务，托管 ServerData 目录。
        /// </summary>
        /// <param name="settings">Addressables 内容配置资产（用于读取端口）。</param>
        public static void StartLocalHttpServer(AddressablesContentSettings settings)
        {
            if (IsLocalServerRunning)
            {
                Debug.LogWarning("[HotUpdate] 本地 HTTP 服务已在运行。");
                return;
            }

            string serverDataRoot = GetServerDataRootPath();
            if (!Directory.Exists(serverDataRoot))
            {
                Debug.LogError(
                    $"[HotUpdate] ServerData 目录不存在：{serverDataRoot}\n" +
                    "请先在「热更发布中心」执行首包或热更构建。");
                return;
            }

            int port = settings == null || settings.LocalDevHttpPort <= 0
                ? AddressablesContentSettings.DefaultLocalDevHttpPort
                : settings.LocalDevHttpPort;

            if (!TryStartPythonHttpServer(serverDataRoot, port))
            {
                Debug.LogError(
                    "[HotUpdate] 无法启动本地 HTTP 服务。请确认已安装 Python，" +
                    $"或手动在项目根目录执行：python -m http.server {port} --directory ServerData");
                return;
            }

            string baseUrl = BuildBaseUrl(port);
            Debug.Log(
                $"[HotUpdate] 本地 HTTP 服务已启动：{baseUrl}\n" +
                $"  服务目录：{serverDataRoot}\n" +
                $"  示例 Catalog：{baseUrl}/{EditorUserBuildSettings.activeBuildTarget}/catalog_*.json");
        }

        /// <summary>
        /// 停止本地 HTTP 静态服务。
        /// </summary>
        public static void StopLocalHttpServer()
        {
            if (_localHttpServerProcess == null)
            {
                return;
            }

            try
            {
                if (!_localHttpServerProcess.HasExited)
                {
                    _localHttpServerProcess.Kill();
                    _localHttpServerProcess.WaitForExit(2000);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HotUpdate] 停止本地 HTTP 服务时出错：{ex.Message}");
            }
            finally
            {
                _localHttpServerProcess.Dispose();
                _localHttpServerProcess = null;
                Debug.Log("[HotUpdate] 本地 HTTP 服务已停止。");
            }
        }

        /// <summary>
        /// 清除 PlayerPrefs 中的远程 URL 覆盖。
        /// </summary>
        public static void ClearRemoteUrlOverride()
        {
            PlayerPrefs.DeleteKey(DevRemoteUrlPrefsKey);
            PlayerPrefs.Save();
            Debug.Log("[HotUpdate] 已清除 PlayerPrefs 远程 URL 覆盖。");
        }

        /// <summary>
        /// 获取 ServerData 根目录绝对路径。
        /// </summary>
        public static string GetServerDataRootPath()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "ServerData").Replace('\\', '/');
        }

        /// <summary>
        /// 加载或创建 AddressablesContentSettings 资产。
        /// </summary>
        public static AddressablesContentSettings LoadOrCreateSettingsAsset()
        {
            AddressablesContentSettings settings = AssetDatabase.LoadAssetAtPath<AddressablesContentSettings>(
                AddressablesContentSettings.DefaultAssetPath);
            if (settings != null)
            {
                return settings;
            }

            settings = ScriptableObject.CreateInstance<AddressablesContentSettings>();
            string folder = Path.GetDirectoryName(AddressablesContentSettings.DefaultAssetPath);
            if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
            {
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }

            AssetDatabase.CreateAsset(settings, AddressablesContentSettings.DefaultAssetPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        private static void SyncAddressablesProfileRemoteBaseUrl(string baseUrl)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning("[HotUpdate] 未找到 AddressableAssetSettings，跳过 Profile 同步。");
                return;
            }

            settings.profileSettings.SetValue(
                settings.activeProfileId,
                ProfileVariableName,
                baseUrl.TrimEnd('/'));
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        private static string BuildBaseUrl(int port)
        {
            return $"http://127.0.0.1:{port}";
        }

        private static bool TryStartPythonHttpServer(string serverDataRoot, int port)
        {
            string[] pythonCommands = { "python", "py", "python3" };
            for (int i = 0; i < pythonCommands.Length; i++)
            {
                if (TryStartPythonHttpServer(pythonCommands[i], serverDataRoot, port))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryStartPythonHttpServer(string pythonCommand, string serverDataRoot, int port)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = pythonCommand,
                    Arguments = $"-m http.server {port}",
                    WorkingDirectory = serverDataRoot,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                Process process = Process.Start(startInfo);
                if (process == null)
                {
                    return false;
                }

                process.EnableRaisingEvents = true;
                process.Exited += (_, __) =>
                {
                    if (_localHttpServerProcess == process)
                    {
                        _localHttpServerProcess = null;
                    }
                };

                _localHttpServerProcess = process;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
