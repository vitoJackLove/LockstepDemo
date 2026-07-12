using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// 帧同步服务器编辑器工具，用于在 Unity 内启动、停止和编译独立 .NET 服务器。
/// </summary>
public sealed class FrameSyncServerEditorWindow : EditorWindow
{
    private const string MenuPath = "Tools/调试/帧同步服务器";
    private const string WindowTitle = "帧同步服务器";
    private const string DefaultClientAddress = "127.0.0.1";

    private const int DefaultPort = 8888;
    private const int DefaultMaxPlayers = 3;
    private const int SingleMaxPlayers = 1;

    private static readonly double StatusRefreshIntervalSeconds = 1d;

    private int _port = DefaultPort;
    private int _maxPlayers = DefaultMaxPlayers;
    private double _nextStatusRefreshTime;
    private string _statusMessage = "未检测";
    private MessageType _statusMessageType = MessageType.Info;
    private Vector2 _logScrollPosition;

    [MenuItem(MenuPath, false, 2100)]
    public static void OpenWindow()
    {
        FrameSyncServerEditorWindow window = GetWindow<FrameSyncServerEditorWindow>(false, WindowTitle, true);
        window.minSize = new Vector2(420f, 320f);
        window.Show();
    }

    private void OnEnable()
    {
        RefreshStatus(force: true);
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        if (EditorApplication.timeSinceStartup < _nextStatusRefreshTime)
        {
            return;
        }

        RefreshStatus(force: false);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("帧同步服务器", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            $"服务器监听 0.0.0.0:{_port}，Unity 客户端默认连接 {DefaultClientAddress}:{_port}。\n" +
            $"工作目录：{FrameSyncServerProcessUtility.ServerDirectory}",
            MessageType.Info);

        EditorGUI.BeginChangeCheck();
        _port = EditorGUILayout.IntField("端口", _port);
        _maxPlayers = EditorGUILayout.IntField("最大玩家数", _maxPlayers);
        if (EditorGUI.EndChangeCheck())
        {
            _port = Mathf.Max(1, _port);
            _maxPlayers = Mathf.Max(1, _maxPlayers);
            RefreshStatus(force: true);
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.HelpBox(_statusMessage, _statusMessageType);

        EditorGUILayout.Space(4f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("启动（多人）", GUILayout.Height(28f)))
            {
                StartServer(DefaultMaxPlayers);
            }

            if (GUILayout.Button("启动（单人）", GUILayout.Height(28f)))
            {
                StartServer(SingleMaxPlayers);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("编译服务器", GUILayout.Height(24f)))
            {
                BuildServer();
            }

            using (new EditorGUI.DisabledScope(!FrameSyncServerProcessUtility.IsPortListening(_port)))
            {
                if (GUILayout.Button("停止服务器", GUILayout.Height(24f)))
                {
                    StopServer();
                }
            }
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("最近操作", EditorStyles.boldLabel);
        using (EditorGUILayout.ScrollViewScope scroll = new EditorGUILayout.ScrollViewScope(_logScrollPosition))
        {
            _logScrollPosition = scroll.scrollPosition;
            foreach (string line in FrameSyncServerProcessUtility.RecentLogs)
            {
                EditorGUILayout.LabelField(line, EditorStyles.wordWrappedMiniLabel);
            }
        }
    }

    private void StartServer(int maxPlayers)
    {
        try
        {
            FrameSyncServerProcessUtility.StartServer(_port, maxPlayers);
            RefreshStatus(force: true);
        }
        catch (Exception ex)
        {
            _statusMessage = ex.Message;
            _statusMessageType = MessageType.Error;
            Debug.LogError($"[FrameSyncServer] {ex.Message}");
        }
    }

    private void BuildServer()
    {
        try
        {
            FrameSyncServerProcessUtility.BuildServer();
            RefreshStatus(force: true);
        }
        catch (Exception ex)
        {
            _statusMessage = ex.Message;
            _statusMessageType = MessageType.Error;
            Debug.LogError($"[FrameSyncServer] {ex.Message}");
        }
    }

    private void StopServer()
    {
        try
        {
            FrameSyncServerProcessUtility.StopServer(_port);
            RefreshStatus(force: true);
        }
        catch (Exception ex)
        {
            _statusMessage = ex.Message;
            _statusMessageType = MessageType.Error;
            Debug.LogError($"[FrameSyncServer] {ex.Message}");
        }
    }

    private void RefreshStatus(bool force)
    {
        if (!force && EditorApplication.timeSinceStartup < _nextStatusRefreshTime)
        {
            return;
        }

        _nextStatusRefreshTime = EditorApplication.timeSinceStartup + StatusRefreshIntervalSeconds;

        if (!FrameSyncServerProcessUtility.TryValidateEnvironment(out string validationError))
        {
            _statusMessage = validationError;
            _statusMessageType = MessageType.Warning;
            return;
        }

        if (FrameSyncServerProcessUtility.IsPortListening(_port))
        {
            List<int> processIds = FrameSyncServerProcessUtility.GetListenerProcessIds(_port);
            string pidText = processIds.Count > 0 ? string.Join(", ", processIds) : "未知";
            _statusMessage = $"运行中：端口 {_port} 已被监听（PID: {pidText}）";
            _statusMessageType = MessageType.Info;
            return;
        }

        _statusMessage = $"未运行：端口 {_port} 当前空闲，可直接启动。";
        _statusMessageType = MessageType.None;
    }
}

/// <summary>
/// 帧同步服务器进程管理工具。
/// </summary>
internal static class FrameSyncServerProcessUtility
{
    private const int MaxLogLines = 12;

    private static readonly List<string> _recentLogs = new List<string>(MaxLogLines);

    public static IReadOnlyList<string> RecentLogs => _recentLogs;

    public static string ServerDirectory =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty, "Server"));

    public static string ProjectFilePath =>
        Path.Combine(ServerDirectory, "RogueGameServer", "RogueGameServer.csproj");

    public static bool TryValidateEnvironment(out string errorMessage)
    {
        if (!Directory.Exists(ServerDirectory))
        {
            errorMessage = $"未找到 Server 目录：{ServerDirectory}";
            return false;
        }

        if (!File.Exists(ProjectFilePath))
        {
            errorMessage = $"未找到服务器项目：{ProjectFilePath}";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    public static void StartServer(int port, int maxPlayers)
    {
        if (!TryValidateEnvironment(out string validationError))
        {
            throw new InvalidOperationException(validationError);
        }

        if (IsPortListening(port))
        {
            throw new InvalidOperationException($"端口 {port} 已被占用，请先停止现有服务器。");
        }

        string arguments =
            $"/c start \"Frame Sync Server\" dotnet run --project RogueGameServer/RogueGameServer.csproj -- --port {port} --max-players {maxPlayers}";

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = arguments,
            WorkingDirectory = ServerDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using Process launcher = Process.Start(startInfo);
        if (launcher == null)
        {
            throw new InvalidOperationException("无法启动帧同步服务器进程。");
        }

        AppendLog($"已启动服务器：端口 {port}，最大玩家数 {maxPlayers}。");
        Debug.Log($"[FrameSyncServer] Started on port {port}, maxPlayers={maxPlayers}.");
    }

    public static void BuildServer()
    {
        if (!TryValidateEnvironment(out string validationError))
        {
            throw new InvalidOperationException(validationError);
        }

        int exitCode = RunBlockingProcess(
            "dotnet",
            "build RogueGameServer/RogueGameServer.csproj -nologo",
            ServerDirectory);

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"服务器编译失败，退出码 {exitCode}。");
        }

        AppendLog("服务器编译成功。");
        Debug.Log("[FrameSyncServer] Build succeeded.");
    }

    public static void StopServer(int port)
    {
        List<int> processIds = GetListenerProcessIds(port);
        if (processIds.Count == 0)
        {
            throw new InvalidOperationException($"端口 {port} 当前没有监听进程。");
        }

        for (int i = 0; i < processIds.Count; i++)
        {
            KillProcessTree(processIds[i]);
        }

        AppendLog($"已停止端口 {port} 上的服务器进程。");
        Debug.Log($"[FrameSyncServer] Stopped listener processes on port {port}.");
    }

    public static bool IsPortListening(int port)
    {
        return GetListenerProcessIds(port).Count > 0;
    }

    public static List<int> GetListenerProcessIds(int port)
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
            Debug.LogWarning($"[FrameSyncServer] 枚举端口 {port} 监听进程失败：{ex.Message}");
        }

        return processIds;
    }

    private static int RunBlockingProcess(string fileName, string arguments, string workingDirectory)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using Process process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException($"无法启动进程：{fileName} {arguments}");
        }

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!string.IsNullOrWhiteSpace(output))
        {
            Debug.Log($"[FrameSyncServer] {output.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            Debug.LogWarning($"[FrameSyncServer] {error.Trim()}");
        }

        return process.ExitCode;
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
            throw new InvalidOperationException($"taskkill 退出码 {taskkillProcess.ExitCode}。");
        }
    }

    private static void AppendLog(string message)
    {
        string line = $"{DateTime.Now:HH:mm:ss} {message}";
        _recentLogs.Insert(0, line);
        while (_recentLogs.Count > MaxLogLines)
        {
            _recentLogs.RemoveAt(_recentLogs.Count - 1);
        }
    }
}
