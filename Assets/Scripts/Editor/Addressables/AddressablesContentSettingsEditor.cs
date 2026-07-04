using Rogue.Editor.HotUpdate;
using Rogue.Editor.HotUpdate.Dev;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AddressablesContentSettings))]
public class AddressablesContentSettingsEditor : Editor
{
    private static readonly GUIContent LabelDefaultRemoteBaseUrl =
        new GUIContent("默认远程资源根 URL", "不含 BuildTarget。运行时 Remote.LoadPath 为 {URL}/{BuildTarget}。");

    private static readonly GUIContent LabelEnableCatalogUpdateOnStartup =
        new GUIContent("启动时检查 Catalog 更新", "为 false 时跳过 CheckForCatalogUpdates。");

    private static readonly GUIContent LabelForceLocalOnly =
        new GUIContent("仅使用本地首包", "为 true 时不注入远程 URL，也不检查 Catalog。");

    private static readonly GUIContent LabelCatalogUpdateTimeoutSeconds =
        new GUIContent("Catalog 更新超时（秒）", "超时后继续使用本地 Catalog。");

    private static readonly GUIContent LabelEnableHotUpdateCatalogCheck =
        new GUIContent("PreBootstrap 热更 Catalog 检查", "PreBootstrap 阶段是否执行 Catalog 更新。");

    private static readonly GUIContent LabelSkipCatalogCheckInBatchMode =
        new GUIContent("BatchMode 跳过 Catalog", "CI / -batchmode 下跳过 Catalog 检查。");

    private static readonly GUIContent LabelLocalDevRemoteBaseUrl =
        new GUIContent("本地热更测试 URL", "Editor Play 时使用的远程根 URL，不含 BuildTarget。");

    private static readonly GUIContent LabelLocalDevHttpPort =
        new GUIContent("本地 HTTP 服务端口", "静态服务 ServerData 目录时使用的端口。");

    private static readonly GUIContent LabelUseLocalDevRemoteInEditor =
        new GUIContent("Editor Play 使用本地热更 URL", "Play 模式下优先使用上方本地热更 URL。");

    private SerializedProperty _defaultRemoteBaseUrl;
    private SerializedProperty _enableCatalogUpdateOnStartup;
    private SerializedProperty _forceLocalOnly;
    private SerializedProperty _catalogUpdateTimeoutSeconds;
    private SerializedProperty _enableHotUpdateCatalogCheck;
    private SerializedProperty _skipCatalogCheckInBatchMode;
    private SerializedProperty _localDevRemoteBaseUrl;
    private SerializedProperty _localDevHttpPort;
    private SerializedProperty _useLocalDevRemoteInEditor;

    private void OnEnable()
    {
        _defaultRemoteBaseUrl = serializedObject.FindProperty(nameof(AddressablesContentSettings.DefaultRemoteBaseUrl));
        _enableCatalogUpdateOnStartup =
            serializedObject.FindProperty(nameof(AddressablesContentSettings.EnableCatalogUpdateOnStartup));
        _forceLocalOnly = serializedObject.FindProperty(nameof(AddressablesContentSettings.ForceLocalOnly));
        _catalogUpdateTimeoutSeconds =
            serializedObject.FindProperty(nameof(AddressablesContentSettings.CatalogUpdateTimeoutSeconds));
        _enableHotUpdateCatalogCheck =
            serializedObject.FindProperty(nameof(AddressablesContentSettings.EnableHotUpdateCatalogCheck));
        _skipCatalogCheckInBatchMode =
            serializedObject.FindProperty(nameof(AddressablesContentSettings.SkipCatalogCheckInBatchMode));
        _localDevRemoteBaseUrl =
            serializedObject.FindProperty(nameof(AddressablesContentSettings.LocalDevRemoteBaseUrl));
        _localDevHttpPort = serializedObject.FindProperty(nameof(AddressablesContentSettings.LocalDevHttpPort));
        _useLocalDevRemoteInEditor =
            serializedObject.FindProperty(nameof(AddressablesContentSettings.UseLocalDevRemoteInEditor));
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("远程分发", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_defaultRemoteBaseUrl, LabelDefaultRemoteBaseUrl);
        EditorGUILayout.PropertyField(_enableCatalogUpdateOnStartup, LabelEnableCatalogUpdateOnStartup);
        EditorGUILayout.PropertyField(_forceLocalOnly, LabelForceLocalOnly);
        EditorGUILayout.PropertyField(_catalogUpdateTimeoutSeconds, LabelCatalogUpdateTimeoutSeconds);
        EditorGUILayout.PropertyField(_enableHotUpdateCatalogCheck, LabelEnableHotUpdateCatalogCheck);
        EditorGUILayout.PropertyField(_skipCatalogCheckInBatchMode, LabelSkipCatalogCheckInBatchMode);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("本地热更测试", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_localDevRemoteBaseUrl, LabelLocalDevRemoteBaseUrl);
        EditorGUILayout.PropertyField(_localDevHttpPort, LabelLocalDevHttpPort);
        EditorGUILayout.PropertyField(_useLocalDevRemoteInEditor, LabelUseLocalDevRemoteInEditor);

        DrawEnvironmentStatus();

        EditorGUILayout.Space(4f);
        if (GUILayout.Button("打开发布中心"))
        {
            HotUpdatePublishWindow.ShowWindow();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawEnvironmentStatus()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("环境状态", EditorStyles.boldLabel);

        string buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString();
        string serverDataPath = LocalDevEnvironment.GetServerDataRootPath();
        bool serverDataExists = System.IO.Directory.Exists(serverDataPath);
        bool serverRunning = LocalDevEnvironment.GetLocalHttpServerStatus((AddressablesContentSettings)target).IsRunning;
        string prefsOverride = PlayerPrefs.GetString(LocalDevEnvironment.DevRemoteUrlPrefsKey, string.Empty);

        EditorGUILayout.HelpBox(
            $"当前平台：{buildTarget}\n" +
            $"ServerData 路径：{serverDataPath}\n" +
            $"ServerData 已构建：{(serverDataExists ? "是" : "否（请在「热更发布中心」构建）")}\n" +
            $"本地 HTTP 服务：{(serverRunning ? "运行中" : "未启动")}\n" +
            $"PlayerPrefs 覆盖：{(string.IsNullOrEmpty(prefsOverride) ? "无" : prefsOverride)}",
            serverDataExists ? MessageType.Info : MessageType.Warning);
    }
}
