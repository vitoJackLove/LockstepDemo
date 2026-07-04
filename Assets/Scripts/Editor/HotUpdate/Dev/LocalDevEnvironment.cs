using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
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
        private const string LocalHttpServerPidFileRelativePath = "Library/HotUpdate/local_http_server.pid";
        private const double ServerStatusCacheSeconds = 1.0;
        private const int HttpServerStartupTimeoutMs = 8000;
        private const int HttpServerStartupPollIntervalMs = 200;

        private static Process _localHttpServerProcess;
        private static int _localHttpServerPort;
        private static bool _serverStatusCacheValid;
        private static double _serverStatusCacheTime;
        private static bool _cachedServerRunning;
        private static int _cachedServerPort;
        private static int _cachedServerProcessId;

        /// <summary>
        /// 本地 HTTP 服务状态快照，供 Editor UI 每帧单次查询。
        /// </summary>
        public readonly struct LocalHttpServerStatus
        {
            public LocalHttpServerStatus(bool isRunning, int activePort, int configuredPort)
            {
                IsRunning = isRunning;
                ActivePort = activePort;
                ConfiguredPort = configuredPort;
            }

            public bool IsRunning { get; }
            public int ActivePort { get; }
            public int ConfiguredPort { get; }
        }

        /// <summary>
        /// 获取本地 HTTP 服务状态（带缓存，避免 Editor UI 每帧重复 netstat/HTTP 探测）。
        /// </summary>
        public static LocalHttpServerStatus GetLocalHttpServerStatus(AddressablesContentSettings settings)
        {
            int configuredPort = GetConfiguredPort(settings);
            if (TryResolveRunningServerCached(settings, out int port, out _))
            {
                return new LocalHttpServerStatus(true, port, configuredPort);
            }

            return new LocalHttpServerStatus(false, configuredPort, configuredPort);
        }

        /// <summary>
        /// 本地 HTTP 静态服务是否正在运行（含域重载后由 PID 文件或端口探测恢复）。
        /// </summary>
        public static bool IsLocalServerRunning(AddressablesContentSettings settings)
        {
            return TryResolveRunningServerCached(settings, out _, out _);
        }

        /// <summary>
        /// 当前配置端口上是否有可停止的热更 HTTP 服务。
        /// </summary>
        public static bool CanStopLocalHttpServer(AddressablesContentSettings settings)
        {
            return GetLocalHttpServerStatus(settings).IsRunning;
        }

        /// <summary>
        /// 获取当前热更 HTTP 服务监听端口；未运行则返回配置端口。
        /// </summary>
        public static int GetActiveOrConfiguredPort(AddressablesContentSettings settings)
        {
            return GetLocalHttpServerStatus(settings).ActivePort;
        }

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
            if (IsLocalServerRunning(settings))
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

            int port = GetConfiguredPort(settings);
            if (!IsPortAvailable(port))
            {
                if (TryVerifyHttpServer(serverDataRoot, port, out _))
                {
                    if (TryResolveListenerProcessId(port, out int existingListenerProcessId))
                    {
                        WritePidFile(existingListenerProcessId, port);
                        InvalidateServerStatusCache();
                        Debug.Log(
                            $"[HotUpdate] 端口 {port} 上已有可用的 Python 静态服务，已接管。\n" +
                            $"  访问根 URL：{BuildBaseUrl(port)}");
                        return;
                    }
                }

                if (TryAdoptExistingPythonHttpServer(port))
                {
                    InvalidateServerStatusCache();
                    Debug.Log(
                        $"[HotUpdate] 端口 {port} 上已有 Python 静态服务在运行，已接管现有服务。\n" +
                        $"  访问根 URL：{BuildBaseUrl(port)}");
                    return;
                }

                TryKillAllListenersOnPort(port);
                Thread.Sleep(200);
            }

            if (!IsPortAvailable(port))
            {
                Debug.LogError(
                    $"[HotUpdate] 端口 {port} 已被占用，无法启动本地 HTTP 静态服务。\n" +
                    DescribePortOccupier(port) +
                    $"\n  若为本工具遗留进程，可先点「停止本地 HTTP 服务」或菜单 Tools/发布/停止本地 HTTP 服务\n" +
                    $"  也可手动执行：py -3 -m http.server {port} --bind 127.0.0.1 --directory \"{serverDataRoot}\"");
                return;
            }

            if (!TryStartPythonHttpServer(serverDataRoot, port))
            {
                Debug.LogError(
                    "[HotUpdate] 无法启动本地 HTTP 服务。请确认已安装 Python，" +
                    $"或手动在项目根目录执行：py -3 -m http.server {port} --bind 127.0.0.1 --directory ServerData");
                return;
            }

            if (!WaitForHttpServerReady(serverDataRoot, port))
            {
                StopLocalHttpServer(settings);
                Debug.LogError(
                    $"[HotUpdate] 本地 HTTP 服务启动后验证失败：无法在 {HttpServerStartupTimeoutMs}ms 内访问 Catalog 样例文件。\n" +
                    "  常见原因：8765 端口被多个 Python/其他服务占用，或 py 启动器子进程未就绪。\n" +
                    "  请先点「停止本地 HTTP 服务」，再手动执行：\n" +
                    $"  py -3 -m http.server {port} --bind 127.0.0.1 --directory ServerData");
                return;
            }

            if (!TryVerifyHttpServer(serverDataRoot, port, out string verifyError))
            {
                StopLocalHttpServer(settings);
                Debug.LogError(
                    $"[HotUpdate] 本地 HTTP 服务启动后验证失败：{verifyError}\n" +
                    "  若 Python 进程已退出，请确认未使用 WindowsApps 占位符 python.exe；" +
                    $"可手动执行：py -3 -m http.server {port} --bind 127.0.0.1 --directory ServerData");
                return;
            }

            string baseUrl = BuildBaseUrl(port);
            if (!TryResolveListenerProcessId(port, out int listenerProcessId))
            {
                StopLocalHttpServer(settings);
                Debug.LogError("[HotUpdate] 无法定位本地 HTTP 服务进程，已回滚启动。");
                return;
            }

            WritePidFile(listenerProcessId, port);
            InvalidateServerStatusCache();
            Debug.Log(
                $"[HotUpdate] 本地 HTTP 服务已启动：{baseUrl}\n" +
                $"  进程 ID：{listenerProcessId}\n" +
                $"  服务目录：{serverDataRoot}\n" +
                $"  示例 Catalog：{baseUrl}/{EditorUserBuildSettings.activeBuildTarget}/catalog_*.json");
        }

        private static void WritePidFile(int processId, int port)
        {
            string pidFilePath = GetPidFilePath();
            string folder = Path.GetDirectoryName(pidFilePath);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            File.WriteAllText(pidFilePath, $"{processId}{Environment.NewLine}{port}");
        }

        /// <summary>
        /// 停止本地 HTTP 静态服务（菜单入口）。
        /// </summary>
        [MenuItem("Tools/发布/停止本地 HTTP 服务")]
        public static void StopLocalHttpServerMenuItem()
        {
            AddressablesContentSettings settings = LoadOrCreateSettingsAsset();
            StopLocalHttpServer(settings);
        }

        [MenuItem("Tools/发布/停止本地 HTTP 服务", true)]
        public static bool StopLocalHttpServerMenuItemValidate()
        {
            AddressablesContentSettings settings = LoadOrCreateSettingsAsset();
            return CanStopLocalHttpServer(settings);
        }

        /// <summary>
        /// 停止本地 HTTP 静态服务。
        /// </summary>
        /// <param name="settings">用于日志展示配置端口（可选）。</param>
        /// <returns>是否成功停止或确认本工具管理的服务未在运行。</returns>
        public static bool StopLocalHttpServer(AddressablesContentSettings settings = null)
        {
            if (!TryResolveRunningServer(settings, out int port, out int processId))
            {
                ClearPidFile();
                Debug.Log("[HotUpdate] 本地 HTTP 服务未在运行。");
                return true;
            }

            int configuredPort = GetConfiguredPort(settings);

            try
            {
                KillProcessTree(processId);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HotUpdate] 停止本地 HTTP 服务时出错：{ex.Message}");
                return false;
            }
            finally
            {
                if (_localHttpServerProcess != null)
                {
                    try
                    {
                        if (!_localHttpServerProcess.HasExited)
                        {
                            _localHttpServerProcess.Kill();
                        }
                    }
                    catch
                    {
                        // 已通过 taskkill 处理实际监听进程。
                    }

                    _localHttpServerProcess.Dispose();
                    _localHttpServerProcess = null;
                }

                _localHttpServerPort = 0;
                ClearPidFile();
                InvalidateServerStatusCache();
            }

            Debug.Log($"[HotUpdate] 本地 HTTP 服务已停止（端口 {(port > 0 ? port : configuredPort)}）。");
            return true;
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
            return Path.Combine(GetProjectRootPath(), "ServerData").Replace('\\', '/');
        }

        private static string GetProjectRootPath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace('\\', '/');
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
            string directoryArgument = $"\"{serverDataRoot.Replace("\"", "\\\"")}\"";
            string serverArguments = $"-m http.server {port} --bind 127.0.0.1 --directory {directoryArgument}";

            foreach (string executable in EnumerateRealPythonExecutables())
            {
                if (TryLaunchPythonHttpServer(executable, serverArguments, port))
                {
                    return true;
                }
            }

            return TryLaunchPythonHttpServer("py", $"-3 {serverArguments}", port);
        }

        private static IEnumerable<string> EnumerateRealPythonExecutables()
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string executable in EnumeratePythonExecutablesFromLauncher())
            {
                if (seen.Add(executable))
                {
                    yield return executable;
                }
            }

            foreach (string executable in EnumeratePythonExecutablesFromPath())
            {
                if (seen.Add(executable))
                {
                    yield return executable;
                }
            }
        }

        private static IEnumerable<string> EnumeratePythonExecutablesFromLauncher()
        {
            List<string> executables = new List<string>();
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "py",
                    Arguments = "-0p",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using Process process = Process.Start(startInfo);
                if (process == null)
                {
                    return executables;
                }

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(3000);
                if (process.ExitCode != 0 || string.IsNullOrEmpty(output))
                {
                    return executables;
                }

                string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    int pathStart = line.LastIndexOf(' ');
                    if (pathStart < 0 || pathStart >= line.Length - 1)
                    {
                        continue;
                    }

                    string candidate = line.Substring(pathStart + 1).Trim();
                    if (IsUsablePythonExecutable(candidate))
                    {
                        executables.Add(candidate);
                    }
                }
            }
            catch
            {
                // ignored
            }

            return executables;
        }

        private static IEnumerable<string> EnumeratePythonExecutablesFromPath()
        {
            List<string> executables = new List<string>();
            string[] commands = { "python", "python3" };
            for (int i = 0; i < commands.Length; i++)
            {
                foreach (string candidate in EnumerateWhereMatches(commands[i]))
                {
                    if (IsUsablePythonExecutable(candidate))
                    {
                        executables.Add(candidate);
                    }
                }
            }

            return executables;
        }

        private static IEnumerable<string> EnumerateWhereMatches(string command)
        {
            List<string> matches = new List<string>();
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "where.exe",
                    Arguments = command,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using Process process = Process.Start(startInfo);
                if (process == null)
                {
                    return matches;
                }

                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit(3000);
                if (process.ExitCode != 0 || string.IsNullOrEmpty(output))
                {
                    return matches;
                }

                string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < lines.Length; i++)
                {
                    string candidate = lines[i].Trim();
                    if (!string.IsNullOrEmpty(candidate))
                    {
                        matches.Add(candidate);
                    }
                }
            }
            catch
            {
                // ignored
            }

            return matches;
        }

        private static bool IsUsablePythonExecutable(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                return false;
            }

            if (executablePath.IndexOf(@"\WindowsApps\", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            return File.Exists(executablePath);
        }

        private static bool TryLaunchPythonHttpServer(string pythonCommand, string arguments, int port)
        {
            Process process = null;
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = pythonCommand,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                };

                process = Process.Start(startInfo);
                if (process == null)
                {
                    return false;
                }

                if (!WaitForPythonProcessReady(process))
                {
                    TryDisposeProcess(process);
                    return false;
                }

                if (!TryResolveListenerProcessId(port, out _))
                {
                    TryDisposeProcess(process);
                    return false;
                }

                process.EnableRaisingEvents = true;
                process.Exited += (_, __) =>
                {
                    if (_localHttpServerProcess == process)
                    {
                        _localHttpServerProcess = null;
                        _localHttpServerPort = 0;
                    }

                    ClearPidFile();
                    InvalidateServerStatusCache();
                };

                _localHttpServerProcess = process;
                _localHttpServerPort = port;
                InvalidateServerStatusCache();
                return true;
            }
            catch
            {
                TryDisposeProcess(process);
                return false;
            }
        }

        private static bool WaitForHttpServerReady(string serverDataRoot, int port)
        {
            int elapsedMs = 0;
            while (elapsedMs < HttpServerStartupTimeoutMs)
            {
                if (TryVerifyHttpServer(serverDataRoot, port, out _))
                {
                    return true;
                }

                Thread.Sleep(HttpServerStartupPollIntervalMs);
                elapsedMs += HttpServerStartupPollIntervalMs;
            }

            return false;
        }

        private static void TryKillAllListenersOnPort(int port)
        {
            List<int> listenerProcessIds = GetAllListenerProcessIds(port);
            for (int i = 0; i < listenerProcessIds.Count; i++)
            {
                try
                {
                    KillProcessTree(listenerProcessIds[i]);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        $"[HotUpdate] 清理端口 {port} 占用进程 {listenerProcessIds[i]} 失败：{ex.Message}");
                }
            }
        }

        private static List<int> GetAllListenerProcessIds(int port)
        {
            List<int> processIds = new List<int>();
            if (port <= 0)
            {
                return processIds;
            }

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "netstat.exe",
                    Arguments = "-ano",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using Process netstatProcess = Process.Start(startInfo);
                if (netstatProcess == null)
                {
                    return processIds;
                }

                string output = netstatProcess.StandardOutput.ReadToEnd();
                netstatProcess.WaitForExit(5000);

                string portSuffix = $":{port}";
                HashSet<int> seen = new HashSet<int>();
                string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (line.IndexOf("LISTENING", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 4)
                    {
                        continue;
                    }

                    string localAddress = parts[1];
                    if (!localAddress.EndsWith(portSuffix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (int.TryParse(parts[parts.Length - 1], out int processId)
                        && processId > 0
                        && seen.Add(processId))
                    {
                        processIds.Add(processId);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HotUpdate] 枚举端口 {port} 监听进程失败：{ex.Message}");
            }

            return processIds;
        }

        private static bool WaitForPythonProcessReady(Process process)
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                process.Refresh();
                if (process.HasExited)
                {
                    return false;
                }

                Thread.Sleep(50);
            }

            process.Refresh();
            return !process.HasExited;
        }

        private static void TryDisposeProcess(Process process)
        {
            if (process == null)
            {
                return;
            }

            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch
            {
                // ignored
            }
            finally
            {
                process.Dispose();
            }
        }

        private static bool IsPortAvailable(int port)
        {
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            finally
            {
                listener?.Stop();
            }
        }

        private static bool TryAdoptExistingPythonHttpServer(int port)
        {
            if (!IsPythonStaticServer(port) || !TryResolveListenerProcessId(port, out int listenerProcessId))
            {
                return false;
            }

            WritePidFile(listenerProcessId, port);
            return true;
        }

        private static string DescribePortOccupier(int port)
        {
            if (!TryResolveListenerProcessId(port, out int processId))
            {
                return $"  占用详情：端口 {port} 有监听，但未能解析进程（可能刚释放或权限不足）。";
            }

            string processName = TryGetProcessName(processId);
            return $"  占用详情：端口 {port} → PID {processId}" +
                   (string.IsNullOrEmpty(processName) ? string.Empty : $" ({processName})");
        }

        private static string TryGetProcessName(int processId)
        {
            try
            {
                using Process process = Process.GetProcessById(processId);
                return process.ProcessName;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool TryVerifyHttpServer(string serverDataRoot, int port, out string error)
        {
            error = string.Empty;
            string buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString();
            string catalogFolder = Path.Combine(serverDataRoot, buildTarget);
            if (!Directory.Exists(catalogFolder))
            {
                error = $"ServerData 下不存在平台目录：{catalogFolder}";
                return false;
            }

            string[] hashFiles = Directory.GetFiles(catalogFolder, "catalog_*.hash");
            if (hashFiles.Length == 0)
            {
                error = $"未找到 catalog_*.hash：{catalogFolder}";
                return false;
            }

            Array.Sort(
                hashFiles,
                (left, right) => File.GetLastWriteTimeUtc(right).CompareTo(File.GetLastWriteTimeUtc(left)));

            string sampleFileName = Path.GetFileName(hashFiles[0]);
            string sampleUrl = $"{BuildBaseUrl(port)}/{buildTarget}/{sampleFileName}";

            for (int attempt = 0; attempt < 20; attempt++)
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(sampleUrl);
                    request.Method = "GET";
                    request.Timeout = 1500;
                    using HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300)
                    {
                        string serverHeader = response.Headers["Server"] ?? string.Empty;
                        if (serverHeader.IndexOf("uvicorn", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            error = $"端口 {port} 实际由 uvicorn 响应（可能是 Unity MCP），不是静态文件服务。";
                            return false;
                        }

                        if (serverHeader.IndexOf("SimpleHTTP", StringComparison.OrdinalIgnoreCase) < 0
                            && serverHeader.IndexOf("Python", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            error = $"端口 {port} 上的服务不是 Python 静态文件服务（Server: {serverHeader}）。";
                            return false;
                        }

                        return true;
                    }

                    error = $"HTTP {(int)response.StatusCode}：{sampleUrl}";
                    return false;
                }
                catch (WebException ex) when (ex.Response is HttpWebResponse failedResponse)
                {
                    error = $"HTTP {(int)failedResponse.StatusCode}：{sampleUrl}";
                    if ((int)failedResponse.StatusCode == 404)
                    {
                        string serverHeader = failedResponse.Headers["Server"] ?? string.Empty;
                        if (serverHeader.IndexOf("uvicorn", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            error =
                                $"端口 {port} 被 Unity MCP（uvicorn）占用，无法提供 Catalog 静态文件。" +
                                "请改用 8765 等端口并重新配置本地环境。";
                        }
                    }

                    return false;
                }
                catch
                {
                    Thread.Sleep(150);
                }
            }

            error = $"无法在限定时间内访问：{sampleUrl}";
            return false;
        }

        private static string GetPidFilePath()
        {
            return Path.Combine(GetProjectRootPath(), LocalHttpServerPidFileRelativePath).Replace('\\', '/');
        }

        private static bool TryReadPidFile(out int processId, out int port)
        {
            processId = 0;
            port = 0;
            string pidFilePath = GetPidFilePath();
            if (!File.Exists(pidFilePath))
            {
                return false;
            }

            try
            {
                string[] lines = File.ReadAllLines(pidFilePath);
                if (lines.Length == 0 || !int.TryParse(lines[0], out processId))
                {
                    return false;
                }

                if (lines.Length > 1)
                {
                    int.TryParse(lines[1], out port);
                }

                return processId > 0;
            }
            catch
            {
                return false;
            }
        }

        private static void ClearPidFile()
        {
            string pidFilePath = GetPidFilePath();
            if (File.Exists(pidFilePath))
            {
                File.Delete(pidFilePath);
            }
        }

        private static bool TryResolveRunningServerCached(
            AddressablesContentSettings settings,
            out int port,
            out int processId)
        {
            double now = EditorApplication.timeSinceStartup;
            if (_serverStatusCacheValid && now - _serverStatusCacheTime < ServerStatusCacheSeconds)
            {
                port = _cachedServerPort;
                processId = _cachedServerProcessId;
                return _cachedServerRunning;
            }

            bool isRunning = TryResolveRunningServer(settings, out port, out processId);
            _serverStatusCacheValid = true;
            _serverStatusCacheTime = now;
            _cachedServerRunning = isRunning;
            _cachedServerPort = port;
            _cachedServerProcessId = processId;
            return isRunning;
        }

        private static void InvalidateServerStatusCache()
        {
            _serverStatusCacheValid = false;
            _cachedServerRunning = false;
            _cachedServerPort = 0;
            _cachedServerProcessId = 0;
        }

        private static int GetConfiguredPort(AddressablesContentSettings settings)
        {
            return settings == null || settings.LocalDevHttpPort <= 0
                ? AddressablesContentSettings.DefaultLocalDevHttpPort
                : settings.LocalDevHttpPort;
        }

        private static bool TryResolveRunningServer(
            AddressablesContentSettings settings,
            out int port,
            out int processId)
        {
            port = 0;
            processId = 0;

            if (TryGetManagedLocalHttpServer(out port, out processId))
            {
                return true;
            }

            if (TryReadPidFile(out int fileProcessId, out int filePort)
                && filePort > 0
                && IsProcessAlive(fileProcessId)
                && IsProcessListeningOnPort(fileProcessId, filePort)
                && IsPythonStaticServer(filePort))
            {
                port = filePort;
                processId = fileProcessId;
                return true;
            }

            int configuredPort = GetConfiguredPort(settings);

            int[] candidatePorts = filePort > 0
                ? new[] { filePort, configuredPort }
                : new[] { configuredPort };

            for (int i = 0; i < candidatePorts.Length; i++)
            {
                int candidatePort = candidatePorts[i];
                if (candidatePort <= 0)
                {
                    continue;
                }

                if (!TryResolveListenerProcessId(candidatePort, out int listenerProcessId)
                    || !IsPythonStaticServer(candidatePort))
                {
                    continue;
                }

                port = candidatePort;
                processId = listenerProcessId;
                WritePidFile(listenerProcessId, candidatePort);
                return true;
            }

            if (filePort > 0 || fileProcessId > 0)
            {
                ClearPidFile();
            }

            return false;
        }

        private static bool TryGetManagedLocalHttpServer(out int port, out int processId)
        {
            port = 0;
            processId = 0;
            if (_localHttpServerProcess == null)
            {
                return false;
            }

            try
            {
                _localHttpServerProcess.Refresh();
                if (_localHttpServerProcess.HasExited)
                {
                    _localHttpServerProcess = null;
                    _localHttpServerPort = 0;
                    return false;
                }

                port = _localHttpServerPort;
                processId = _localHttpServerProcess.Id;
                return port > 0;
            }
            catch
            {
                _localHttpServerProcess = null;
                _localHttpServerPort = 0;
                return false;
            }
        }

        private static bool TryResolveListenerProcessId(int port, out int processId)
        {
            processId = 0;
            if (port <= 0)
            {
                return false;
            }

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "netstat.exe",
                    Arguments = "-ano",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using Process netstatProcess = Process.Start(startInfo);
                if (netstatProcess == null)
                {
                    return false;
                }

                string output = netstatProcess.StandardOutput.ReadToEnd();
                netstatProcess.WaitForExit(5000);

                string portSuffix = $":{port}";
                string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (line.IndexOf("LISTENING", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 4)
                    {
                        continue;
                    }

                    string localAddress = parts[1];
                    if (!localAddress.EndsWith(portSuffix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (int.TryParse(parts[parts.Length - 1], out processId))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HotUpdate] 解析端口 {port} 监听进程失败：{ex.Message}");
            }

            return false;
        }

        private static bool IsProcessAlive(int processId)
        {
            if (processId <= 0)
            {
                return false;
            }

            try
            {
                using Process process = Process.GetProcessById(processId);
                return !process.HasExited;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsProcessListeningOnPort(int processId, int port)
        {
            if (!TryResolveListenerProcessId(port, out int listenerProcessId))
            {
                return false;
            }

            return listenerProcessId == processId;
        }

        private static bool IsPythonStaticServer(int port)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"{BuildBaseUrl(port)}/");
                request.Method = "GET";
                request.Timeout = 1000;
                using HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                string serverHeader = response.Headers["Server"] ?? string.Empty;
                return serverHeader.IndexOf("SimpleHTTP", StringComparison.OrdinalIgnoreCase) >= 0
                    || serverHeader.IndexOf("Python", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }

        private static void KillProcessTree(int processId)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "taskkill.exe",
                Arguments = $"/PID {processId} /T /F",
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using Process taskkillProcess = Process.Start(startInfo);
            if (taskkillProcess == null)
            {
                throw new InvalidOperationException("无法启动 taskkill。");
            }

            taskkillProcess.WaitForExit(5000);
            if (taskkillProcess.ExitCode != 0)
            {
                throw new InvalidOperationException($"taskkill 退出码 {taskkillProcess.ExitCode}");
            }
        }
    }
}
