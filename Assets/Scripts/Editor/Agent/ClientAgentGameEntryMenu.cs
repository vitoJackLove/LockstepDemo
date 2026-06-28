using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Agent 自动进入战斗的编辑器菜单入口，用于测试流程从 Launcher 进入运行世界。
/// </summary>
public static class ClientAgentGameEntryMenu
{
    /// <summary>
    /// 自动进入流程使用的启动场景路径。
    /// </summary>
    private const string LauncherScenePath = "Assets/Scene/Launcher.unity";

    /// <summary>
    /// 编辑器菜单路径。
    /// </summary>
    private const string MenuPath = "Tools/Agent/Run Client Enter Game Test";

    /// <summary>
    /// 批处理模式等待成功或失败信号的超时时间。
    /// </summary>
    private const double BatchTimeoutSeconds = 120d;

    /// <summary>
    /// 当前是否正在监听 Agent 进入游戏流程。
    /// </summary>
    private static bool _isBatchRunning;

    /// <summary>
    /// 监听流程开始时的编辑器时间。
    /// </summary>
    private static double _batchStartTime;

    /// <summary>
    /// 是否已经从 Unity 日志中观察到成功信号。
    /// </summary>
    private static bool _sawSuccess;

    /// <summary>
    /// 是否已经从 Unity 日志中观察到失败信号。
    /// </summary>
    private static bool _sawFailure;

    /// <summary>
    /// 监听完成后是否需要退出编辑器进程，批处理入口需要退出，普通菜单入口只停止监听。
    /// </summary>
    private static bool _exitEditorOnCompletion;

    /// <summary>
    /// 是否正在等待当前 Play Mode 退出后重新从 Launcher 启动 Agent 入场流程。
    /// </summary>
    private static bool _restartAfterPlayModeExit;

    /// <summary>
    /// Play Mode 重启后恢复执行时是否沿用批处理入口语义。
    /// </summary>
    private static bool _restartBatchMode;

    /// <summary>
    /// 从 Unity 菜单启动客户端进入战斗测试。
    /// </summary>
    [MenuItem(MenuPath, false, 2000)]
    public static void RunClientEnterGameTest()
    {
        StartClientEnterGameTest(false);
    }

    /// <summary>
    /// 从批处理脚本启动客户端进入战斗测试，并在成功或失败后退出编辑器进程。
    /// </summary>
    public static void RunClientEnterGameTestBatch()
    {
        StartClientEnterGameTest(true);
    }

    /// <summary>
    /// 执行进入战斗测试的通用启动流程。
    /// </summary>
    /// <param name="batchMode">是否由批处理入口调用。</param>
    private static void StartClientEnterGameTest(bool batchMode)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (EditorApplication.isPlaying)
            {
                if (TryReportWorldReady())
                {
                    if (batchMode)
                    {
                        EditorApplication.Exit(0);
                    }

                    return;
                }

                ScheduleRestartAfterPlayModeExit(batchMode);
                return;
            }

            GameLog.Warn(GameLogChannel.AgentTest, "[AgentClientEntry] Unity is already entering or running Play Mode.");
            if (batchMode)
            {
                EditorApplication.Exit(1);
            }

            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            GameLog.Warn(GameLogChannel.AgentTest, "[AgentClientEntry] Run cancelled because modified scenes were not saved.");
            if (batchMode)
            {
                EditorApplication.Exit(1);
            }

            return;
        }

        BeginMonitoring(batchMode);

        EditorSceneManager.OpenScene(LauncherScenePath);
        PlayerPrefs.SetInt(ClientAgentGameEntryMode.RunOnPlayPrefsKey, 1);
        PlayerPrefs.Save();

        GameLog.Info(GameLogChannel.AgentTest, "[AgentClientEntry] Starting Play Mode from Launcher scene.");
        EditorApplication.EnterPlaymode();
    }

    /// <summary>
    /// 当前编辑器已经处于 Play Mode 但没有运行世界时，安排退出后重新执行标准 Launcher 入场流程。
    /// </summary>
    /// <param name="batchMode">重启后是否按批处理入口处理退出码。</param>
    private static void ScheduleRestartAfterPlayModeExit(bool batchMode)
    {
        _restartAfterPlayModeExit = true;
        _restartBatchMode = batchMode;
        GameLog.Info(GameLogChannel.AgentTest, "[AgentClientEntry] Restarting Play Mode from Launcher scene because no runtime world is available.");
        EditorApplication.update -= RestartClientEnterGameTestAfterPlayModeExit;
        EditorApplication.update += RestartClientEnterGameTestAfterPlayModeExit;
        EditorApplication.ExitPlaymode();
    }

    /// <summary>
    /// 等待当前 Play Mode 完全退出后，重新调用客户端自动入场菜单的标准启动流程。
    /// </summary>
    private static void RestartClientEnterGameTestAfterPlayModeExit()
    {
        if (!_restartAfterPlayModeExit || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        bool batchMode = _restartBatchMode;
        _restartAfterPlayModeExit = false;
        _restartBatchMode = false;
        EditorApplication.update -= RestartClientEnterGameTestAfterPlayModeExit;
        StartClientEnterGameTest(batchMode);
    }

    /// <summary>
    /// 开始监听 Agent 进入战斗流程的日志和世界创建状态。
    /// </summary>
    /// <param name="exitEditorOnCompletion">监听结束后是否退出编辑器进程。</param>
    private static void BeginMonitoring(bool exitEditorOnCompletion)
    {
        _isBatchRunning = true;
        _batchStartTime = EditorApplication.timeSinceStartup;
        _sawSuccess = false;
        _sawFailure = false;
        _exitEditorOnCompletion = exitEditorOnCompletion;
        Application.logMessageReceived -= OnLogMessageReceived;
        Application.logMessageReceived += OnLogMessageReceived;
        EditorApplication.update -= MonitorBatchRun;
        EditorApplication.update += MonitorBatchRun;
    }

    /// <summary>
    /// 接收 Unity 日志并记录 Agent 成功或失败信号。
    /// </summary>
    /// <param name="condition">Unity 日志正文。</param>
    /// <param name="stackTrace">Unity 日志堆栈。</param>
    /// <param name="type">Unity 日志类型。</param>
    private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        if (!_isBatchRunning)
        {
            return;
        }

        if (condition.Contains(ClientAgentGameEntryRunner.SuccessLog))
        {
            _sawSuccess = true;
        }
        else if (condition.Contains(ClientAgentGameEntryRunner.FailureLog))
        {
            _sawFailure = true;
        }
    }

    /// <summary>
    /// 在编辑器 Update 中监控 Agent 流程，确保普通菜单和批处理入口都能产生可观测结论。
    /// </summary>
    private static void MonitorBatchRun()
    {
        if (!_isBatchRunning)
        {
            return;
        }

        if (_sawSuccess)
        {
            CompleteMonitoredRun(0);
            return;
        }

        if (_sawFailure)
        {
            CompleteMonitoredRun(1);
            return;
        }

        if (TryReportWorldReady())
        {
            CompleteMonitoredRun(0);
            return;
        }

        if (EditorApplication.timeSinceStartup - _batchStartTime > BatchTimeoutSeconds)
        {
            GameLog.Error(GameLogChannel.AgentTest, "[AgentClientEntry] Batch run timed out.");
            CompleteMonitoredRun(1);
        }
    }

    /// <summary>
    /// 当运行世界已经创建但 Runner 成功日志未被监听到时，补发标准成功信号供自动化测试识别。
    /// </summary>
    /// <returns>当前运行世界可用时返回 true。</returns>
    private static bool TryReportWorldReady()
    {
        if (!EditorApplication.isPlaying)
        {
            return false;
        }

        if (WorldSystem.Instance?.CurrentRunWorld == null)
        {
            return false;
        }

        GameLog.Info(GameLogChannel.AgentTest, ClientAgentGameEntryRunner.SuccessLog);
        return true;
    }

    /// <summary>
    /// 完成 Agent 监听流程，并按入口类型决定是否退出 Play Mode 和编辑器进程。
    /// </summary>
    /// <param name="exitCode">批处理入口使用的编辑器退出码。</param>
    private static void CompleteMonitoredRun(int exitCode)
    {
        _isBatchRunning = false;
        Application.logMessageReceived -= OnLogMessageReceived;
        EditorApplication.update -= MonitorBatchRun;

        if (!_exitEditorOnCompletion)
        {
            return;
        }

        if (EditorApplication.isPlaying)
        {
            EditorApplication.ExitPlaymode();
        }

        EditorApplication.Exit(exitCode);
    }
}
