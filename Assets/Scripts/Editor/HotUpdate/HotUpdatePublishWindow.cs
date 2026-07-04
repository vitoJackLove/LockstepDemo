using System;
using System.Collections.Generic;
using Rogue.Editor.HotUpdate.Dev;
using Rogue.Editor.HotUpdate.Pipeline;
using Rogue.Editor.HotUpdate.Pipeline.Steps;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Rogue.Editor.HotUpdate
{
    /// <summary>
    /// 热更发布中心 EditorWindow，提供首包、代码热更、资源热更构建与本地开发测试。
    /// </summary>
    public sealed class HotUpdatePublishWindow : EditorWindow
    {
        private enum Tab
        {
            Publish = 0,
            DevTest = 1,
            Addressables = 2,
        }

        private Tab _tab = Tab.Publish;
        private Vector2 _logScroll;
        private Vector2 _addressablesScroll;
        private readonly List<string> _logs = new List<string>();
        private bool _isBuilding;
        private BuildTarget _buildTarget;
        private string _version;
        private string _remoteBaseUrl;
        private bool _developmentBuild;
        private bool _skipPlayerBuild;
        private bool _forceSkipAotCheck;
        private bool _showAdvanced;
        private AddressablesContentSettings _contentSettings;

        [MenuItem("Tools/发布/热更发布中心")]
        public static void ShowWindow()
        {
            GetWindow<HotUpdatePublishWindow>("热更发布中心");
        }

        private void OnEnable()
        {
            _buildTarget = EditorUserBuildSettings.activeBuildTarget;
            _version = PlayerSettings.bundleVersion;
            _developmentBuild = EditorUserBuildSettings.development;
            _remoteBaseUrl = LoadDefaultRemoteUrl();
            _contentSettings = LocalDevEnvironment.LoadOrCreateSettingsAsset();
        }

        private void OnGUI()
        {
            _tab = (Tab)GUILayout.Toolbar((int)_tab, new[] { "发布", "开发测试", "Addressables 配置" });
            EditorGUILayout.Space(4);

            switch (_tab)
            {
                case Tab.Publish:
                    DrawPublishTab();
                    break;
                case Tab.DevTest:
                    DrawDevTestTab();
                    break;
                case Tab.Addressables:
                    DrawAddressablesTab();
                    break;
            }

            if (_tab != Tab.Addressables)
            {
                DrawLogPanel();
            }
        }

        private void DrawPublishTab()
        {
            EditorGUILayout.LabelField("构建参数", EditorStyles.boldLabel);
            _buildTarget = (BuildTarget)EditorGUILayout.EnumPopup("平台", _buildTarget);
            _version = EditorGUILayout.TextField("版本", _version);
            _remoteBaseUrl = EditorGUILayout.TextField("CDN 根 URL", _remoteBaseUrl);
            _developmentBuild = EditorGUILayout.Toggle("Development Build", _developmentBuild);

            EditorGUILayout.Space(4);

            using (new EditorGUI.DisabledScope(_isBuilding))
            {
                if (GUILayout.Button("构建前检查"))
                {
                    RunPreflightCheck(HotUpdateBuildMode.FullPackage);
                }
            }

            EditorGUILayout.HelpBox(
                "代码热更需用户重启 App 后生效；若修改了 GameFramework 或 AOT 元数据，请使用「首包构建」。",
                MessageType.Info);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("构建操作", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(_isBuilding))
            {
                if (GUILayout.Button("首包构建", GUILayout.Height(32)))
                {
                    RunBuild(HotUpdateBuildPipeline.RunFullPackage, HotUpdateBuildMode.FullPackage);
                }

                if (GUILayout.Button("代码热更打包", GUILayout.Height(32)))
                {
                    RunBuild(HotUpdateBuildPipeline.RunCodePatch, HotUpdateBuildMode.CodePatch);
                }

                if (GUILayout.Button("资源热更打包", GUILayout.Height(32)))
                {
                    RunBuild(HotUpdateBuildPipeline.RunResourcePatch, HotUpdateBuildMode.ResourcePatch);
                }
            }

            EditorGUILayout.Space(4);
            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "高级选项", true);
            if (_showAdvanced)
            {
                EditorGUI.indentLevel++;
                _skipPlayerBuild = EditorGUILayout.Toggle("跳过 Player 构建", _skipPlayerBuild);
                _forceSkipAotCheck = EditorGUILayout.Toggle("强制跳过 AOT 变更检测", _forceSkipAotCheck);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawAddressablesTab()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetSettingsPanelDrawer.Draw(settings, ref _addressablesScroll, _remoteBaseUrl);
        }

        private void DrawDevTestTab()
        {
            if (_contentSettings == null)
            {
                _contentSettings = LocalDevEnvironment.LoadOrCreateSettingsAsset();
            }

            LocalDevEnvironment.LocalHttpServerStatus serverStatus =
                LocalDevEnvironment.GetLocalHttpServerStatus(_contentSettings);

            EditorGUILayout.LabelField("本地环境", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            int port = EditorGUILayout.IntField("本地 HTTP 端口", _contentSettings.LocalDevHttpPort);
            bool forceLocalOnly = EditorGUILayout.Toggle("仅使用本地首包 (ForceLocalOnly)", _contentSettings.ForceLocalOnly);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_contentSettings, "Update Local Dev Settings");
                _contentSettings.LocalDevHttpPort = port;
                _contentSettings.ForceLocalOnly = forceLocalOnly;
                EditorUtility.SetDirty(_contentSettings);
            }

            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("一键配置本地环境"))
                {
                    LocalDevEnvironment.ConfigureLocalEnvironment(_contentSettings);
                    AssetDatabase.SaveAssets();
                    _remoteBaseUrl = LoadDefaultRemoteUrl();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(serverStatus.IsRunning))
                {
                    if (GUILayout.Button("启动本地 HTTP 服务", GUILayout.Height(28)))
                    {
                        LocalDevEnvironment.StartLocalHttpServer(_contentSettings);
                        Repaint();
                    }
                }

                using (new EditorGUI.DisabledScope(!serverStatus.IsRunning))
                {
                    if (GUILayout.Button("停止本地 HTTP 服务", GUILayout.Height(28)))
                    {
                        LocalDevEnvironment.StopLocalHttpServer(_contentSettings);
                        Repaint();
                    }
                }
            }

            if (GUILayout.Button("清除 PlayerPrefs 远程 URL 覆盖"))
            {
                LocalDevEnvironment.ClearRemoteUrlOverride();
            }

            DrawDevEnvironmentStatus(serverStatus);

            EditorGUILayout.HelpBox(
                "模拟 OTA 流程：\n" +
                "1. 在「发布」Tab 执行代码/资源热更或首包构建\n" +
                "2. 点击「一键配置本地环境」并「启动本地 HTTP 服务」\n" +
                "3. Editor Play 或 Development 包验证 Catalog 更新与热更加载",
                MessageType.None);
        }

        private void DrawDevEnvironmentStatus(LocalDevEnvironment.LocalHttpServerStatus serverStatus)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("环境状态", EditorStyles.boldLabel);

            string buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString();
            string serverDataPath = LocalDevEnvironment.GetServerDataRootPath();
            bool serverDataExists = System.IO.Directory.Exists(serverDataPath);
            string prefsOverride = PlayerPrefs.GetString(LocalDevEnvironment.DevRemoteUrlPrefsKey, string.Empty);

            EditorGUILayout.HelpBox(
                $"当前平台：{buildTarget}\n" +
                $"ServerData 路径：{serverDataPath}\n" +
                $"ServerData 已构建：{(serverDataExists ? "是" : "否（需先在发布 Tab 构建）")}\n" +
                $"本地 HTTP 服务：{(serverStatus.IsRunning ? $"运行中（端口 {serverStatus.ActivePort}）" : "未启动")}\n" +
                $"PlayerPrefs 覆盖：{(string.IsNullOrEmpty(prefsOverride) ? "无" : prefsOverride)}",
                serverDataExists ? MessageType.Info : MessageType.Warning);
        }

        private void DrawLogPanel()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("日志", EditorStyles.boldLabel);

            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.MinHeight(120), GUILayout.MaxHeight(240));
            if (_logs.Count == 0)
            {
                EditorGUILayout.LabelField("（暂无日志）", EditorStyles.miniLabel);
            }
            else
            {
                for (int i = 0; i < _logs.Count; i++)
                {
                    EditorGUILayout.LabelField(_logs[i], EditorStyles.wordWrappedLabel);
                }
            }

            EditorGUILayout.EndScrollView();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("清空日志"))
                {
                    _logs.Clear();
                }

                GUILayout.FlexibleSpace();
                if (_isBuilding)
                {
                    EditorGUILayout.LabelField("构建中...", EditorStyles.boldLabel);
                }
            }
        }

        private void RunPreflightCheck(HotUpdateBuildMode mode)
        {
            _logs.Clear();
            var logCollector = new Progress<string>(msg =>
            {
                _logs.Add(msg);
                Repaint();
            });

            var context = new HotUpdateBuildContext(
                mode,
                _buildTarget,
                _version,
                _remoteBaseUrl,
                _developmentBuild,
                logCollector);

            try
            {
                PreflightCheckStep.Execute(context);
                _logs.Add("✓ 构建前检查通过。");
            }
            catch (Exception ex)
            {
                _logs.Add($"✗ 检查失败: {ex.Message}");
                Debug.LogException(ex);
            }

            Repaint();
        }

        private void RunBuild(
            Func<HotUpdateBuildContext, HotUpdateBuildResult> pipelineFunc,
            HotUpdateBuildMode mode)
        {
            if (_isBuilding)
            {
                return;
            }

            _isBuilding = true;
            _logs.Clear();

            var logCollector = new Progress<string>(msg =>
            {
                _logs.Add(msg);
                Repaint();
            });

            var context = new HotUpdateBuildContext(
                mode,
                _buildTarget,
                _version,
                _remoteBaseUrl,
                _developmentBuild,
                logCollector)
            {
                SkipPlayerBuild = _skipPlayerBuild,
                ForceSkipAotChangeCheck = _forceSkipAotCheck,
            };

            try
            {
                EditorUtility.DisplayProgressBar("热更发布", "构建中...", 0.5f);
                HotUpdateBuildResult result = pipelineFunc(context);
                if (result.Success)
                {
                    _logs.Add($"✓ 构建成功: {result.ServerDataPath}");
                    if (!string.IsNullOrEmpty(result.PlayerOutputPath))
                    {
                        _logs.Add($"  Player: {result.PlayerOutputPath}");
                    }
                }
                else
                {
                    _logs.Add($"✗ 构建失败: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                _logs.Add($"✗ 异常: {ex.Message}");
                Debug.LogException(ex);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                _isBuilding = false;
                Repaint();
            }
        }

        private static string LoadDefaultRemoteUrl()
        {
            HotUpdateManifestData manifest = HotUpdateManifest.Load();
            if (!string.IsNullOrEmpty(manifest.RemoteBaseUrl))
            {
                return manifest.RemoteBaseUrl;
            }

            AddressablesContentSettings settings = AssetDatabase.LoadAssetAtPath<AddressablesContentSettings>(
                AddressablesContentSettings.DefaultAssetPath);
            if (settings != null && !string.IsNullOrEmpty(settings.DefaultRemoteBaseUrl))
            {
                return settings.DefaultRemoteBaseUrl;
            }

            return AddressablesContentSettings.DefaultLocalDevBaseUrl;
        }
    }
}
