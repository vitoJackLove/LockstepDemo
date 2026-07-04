using System.Collections.Generic;
using System.IO;
using System.Text;
using Rogue.Editor.HotUpdate.Dev;
using Rogue.Editor.HotUpdate.Pipeline;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Rogue.Editor.HotUpdate
{
    /// <summary>
    /// 热更发布中心内的 AddressableAssetSettings 中文配置面板。
    /// 悬停提示包含当前解析值与 Pipeline 约束等实时数据。
    /// </summary>
    public static class AddressableAssetSettingsPanelDrawer
    {
        private const string RemoteBaseUrlVariableName = "AddressablesRemoteBaseUrl";
        private const string SettingsAssetPath = "Assets/Config/AddressableAssetsData/AddressableAssetSettings.asset";

        
        private static bool _showBuildOptions = true;
        private static bool _showProfilePaths = true;
        private static bool _showRemoteCatalog = true;
        private static bool _showGroups = true;
        private static bool _showLabels = true;

        private static SerializedObject _serializedSettings;
        private static SerializedProperty _buildRemoteCatalog;
        private static SerializedProperty _bundleLocalCatalog;
        private static SerializedProperty _disableCatalogUpdateOnStart;
        private static SerializedProperty _catalogRequestsTimeout;
        private static SerializedProperty _maxConcurrentWebRequests;
        private static SerializedProperty _contiguousBundles;
        private static SerializedProperty _buildAddressablesWithPlayerBuild;
        private static SerializedProperty _optimizeCatalogSize;
        private static SerializedProperty _nonRecursiveBuilding;

        /// <param name="settings">AddressableAssetSettings 实例。</param>
        /// <param name="scroll">滚动位置。</param>
        /// <param name="publishTabRemoteBaseUrl">发布 Tab 中的 CDN 根 URL，用于一键同步 Profile。</param>
        public static void Draw(
            AddressableAssetSettings settings,
            ref Vector2 scroll,
            string publishTabRemoteBaseUrl)
        {
            if (settings == null)
            {
                EditorGUILayout.HelpBox(
                    "未找到 AddressableAssetSettings。\n" +
                    "请通过 Window → Asset Management → Addressables → Groups 初始化 Addressables。",
                    MessageType.Error);
                return;
            }

            EnsureSerializedProperties(settings);
            _serializedSettings.Update();

            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawHeader(settings);
            EditorGUILayout.Space(6f);
            DrawEnvironmentSnapshot(settings);
            EditorGUILayout.Space(6f);
            DrawRemoteCatalogSection(settings);
            EditorGUILayout.Space(6f);
            DrawProfileSection(settings, publishTabRemoteBaseUrl);
            EditorGUILayout.Space(6f);
            DrawBuildOptionsSection(settings);
            EditorGUILayout.Space(6f);
            DrawGroupsSection(settings);
            EditorGUILayout.Space(6f);
            DrawLabelsSection(settings);

            EditorGUILayout.EndScrollView();

            if (_serializedSettings.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(settings);
            }

            DrawFooterActions(settings);
        }

        private static void EnsureSerializedProperties(AddressableAssetSettings settings)
        {
            if (_serializedSettings != null && _serializedSettings.targetObject == settings)
            {
                return;
            }

            _serializedSettings = new SerializedObject(settings);
            _buildRemoteCatalog = _serializedSettings.FindProperty("m_BuildRemoteCatalog");
            _bundleLocalCatalog = _serializedSettings.FindProperty("m_BundleLocalCatalog");
            _disableCatalogUpdateOnStart = _serializedSettings.FindProperty("m_DisableCatalogUpdateOnStart");
            _catalogRequestsTimeout = _serializedSettings.FindProperty("m_CatalogRequestsTimeout");
            _maxConcurrentWebRequests = _serializedSettings.FindProperty("m_maxConcurrentWebRequests");
            _contiguousBundles = _serializedSettings.FindProperty("m_ContiguousBundles");
            _buildAddressablesWithPlayerBuild = _serializedSettings.FindProperty("m_BuildAddressablesWithPlayerBuild");
            _optimizeCatalogSize = _serializedSettings.FindProperty("m_OptimizeCatalogSize");
            _nonRecursiveBuilding = _serializedSettings.FindProperty("m_NonRecursiveBuilding");
        }

        private static void DrawHeader(AddressableAssetSettings settings)
        {
            EditorGUILayout.LabelField("Addressables 全局配置", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("资产路径", SettingsAssetPath, EditorStyles.miniLabel);
            EditorGUILayout.LabelField(
                "当前 Profile",
                GetActiveProfileName(settings),
                EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("打开 Addressables Groups 窗口"))
                {
                    EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups");
                }

                if (GUILayout.Button("选中 Settings 资产"))
                {
                    Selection.activeObject = settings;
                    EditorGUIUtility.PingObject(settings);
                }
            }
        }

        private static void DrawEnvironmentSnapshot(AddressableAssetSettings settings)
        {
            EditorGUILayout.LabelField("环境快照（只读）", EditorStyles.boldLabel);

            string buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString();
            string remoteBaseUrl = GetProfileVariable(settings, RemoteBaseUrlVariableName);
            string remoteLoadPath = settings.RemoteCatalogLoadPath.GetValue(settings);
            string remoteBuildPath = settings.RemoteCatalogBuildPath.GetValue(settings);
            string serverDataPath = LocalDevEnvironment.GetServerDataRootPath();
            bool serverDataExists = Directory.Exists(serverDataPath);
            string contentStatePath = HotUpdateContentStatePathUtility.GetConfiguredContentStatePath();
            bool contentStateExists = !string.IsNullOrEmpty(contentStatePath) && File.Exists(contentStatePath);
            HotUpdateManifestData manifest = HotUpdateManifest.Load();
            int groupCount = settings.groups != null ? settings.groups.Count : 0;
            int labelCount = settings.GetLabels().Count;

            EditorGUILayout.HelpBox(
                $"当前平台：{buildTarget}\n" +
                $"Profile 变量 {RemoteBaseUrlVariableName}：{(string.IsNullOrEmpty(remoteBaseUrl) ? "（未配置）" : remoteBaseUrl)}\n" +
                $"Remote Catalog 构建路径：{remoteBuildPath}\n" +
                $"Remote Catalog 加载路径：{remoteLoadPath}\n" +
                $"ServerData：{serverDataPath}（{(serverDataExists ? "已存在" : "未构建")}）\n" +
                $"Content State：{(contentStateExists ? contentStatePath : "未找到 addressables_content_state.bin")}\n" +
                $"Manifest 上次远程 URL：{(string.IsNullOrEmpty(manifest.RemoteBaseUrl) ? "无" : manifest.RemoteBaseUrl)}\n" +
                $"资源组：{groupCount} 个，标签：{labelCount} 个",
                serverDataExists && settings.BuildRemoteCatalog ? MessageType.Info : MessageType.Warning);
        }

        private static void DrawRemoteCatalogSection(AddressableAssetSettings settings)
        {
            _showRemoteCatalog = EditorGUILayout.BeginFoldoutHeaderGroup(_showRemoteCatalog, "远程 Catalog");
            if (_showRemoteCatalog)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(
                    _buildRemoteCatalog,
                    new GUIContent(
                        "构建远程 Catalog",
                        BuildRemoteCatalogTooltip(settings)));

                EditorGUILayout.LabelField(
                    new GUIContent(
                        "Catalog 构建路径变量",
                        BuildCatalogPathTooltip(settings, isLoadPath: false)),
                    new GUIContent(settings.RemoteCatalogBuildPath.GetValue(settings)));

                EditorGUILayout.LabelField(
                    new GUIContent(
                        "Catalog 加载路径变量",
                        BuildCatalogPathTooltip(settings, isLoadPath: true)),
                    new GUIContent(settings.RemoteCatalogLoadPath.GetValue(settings)));

                EditorGUILayout.PropertyField(
                    _bundleLocalCatalog,
                    new GUIContent(
                        "将 Catalog 打入本地包",
                        BuildBundleLocalCatalogTooltip(settings)));

                EditorGUILayout.PropertyField(
                    _disableCatalogUpdateOnStart,
                    new GUIContent(
                        "启动时禁用 Catalog 更新",
                        BuildDisableCatalogUpdateTooltip(settings)));

                EditorGUILayout.PropertyField(
                    _catalogRequestsTimeout,
                    new GUIContent(
                        "Catalog 请求超时（秒，0=默认）",
                        BuildCatalogTimeoutTooltip(settings)));

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private static void DrawProfileSection(AddressableAssetSettings settings, string publishTabRemoteBaseUrl)
        {
            _showProfilePaths = EditorGUILayout.BeginFoldoutHeaderGroup(_showProfilePaths, "Profile 路径变量");
            if (_showProfilePaths)
            {
                EditorGUI.indentLevel++;

                string currentRemoteBaseUrl = GetProfileVariable(settings, RemoteBaseUrlVariableName);
                EditorGUILayout.LabelField(
                    new GUIContent(
                        "Remote.LoadPath 解析结果",
                        BuildProfilePathTooltip(settings, "Remote.LoadPath", isRemote: true)),
                    new GUIContent(ResolveProfilePath(settings, "Remote.LoadPath")));

                EditorGUILayout.LabelField(
                    new GUIContent(
                        "Remote.BuildPath 解析结果",
                        BuildProfilePathTooltip(settings, "Remote.BuildPath", isRemote: true)),
                    new GUIContent(ResolveProfilePath(settings, "Remote.BuildPath")));

                EditorGUILayout.LabelField(
                    new GUIContent(
                        "Local.LoadPath 解析结果",
                        BuildProfilePathTooltip(settings, "Local.LoadPath", isRemote: false)),
                    new GUIContent(ResolveProfilePath(settings, "Local.LoadPath")));

                EditorGUI.BeginChangeCheck();
                string editedRemoteBaseUrl = EditorGUILayout.TextField(
                    new GUIContent(
                        "CDN 根 URL（AddressablesRemoteBaseUrl）",
                        BuildRemoteBaseUrlTooltip(settings, publishTabRemoteBaseUrl)),
                    currentRemoteBaseUrl ?? string.Empty);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(settings, "Update AddressablesRemoteBaseUrl");
                    settings.profileSettings.SetValue(
                        settings.activeProfileId,
                        RemoteBaseUrlVariableName,
                        editedRemoteBaseUrl.TrimEnd('/'));
                    EditorUtility.SetDirty(settings);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(publishTabRemoteBaseUrl)))
                    {
                        if (GUILayout.Button("从发布 Tab 同步 CDN URL 到 Profile"))
                        {
                            Undo.RecordObject(settings, "Sync CDN URL To Profile");
                            settings.profileSettings.SetValue(
                                settings.activeProfileId,
                                RemoteBaseUrlVariableName,
                                publishTabRemoteBaseUrl.TrimEnd('/'));
                            EditorUtility.SetDirty(settings);
                        }
                    }

                    if (GUILayout.Button("同步本地开发 URL (127.0.0.1:8765)"))
                    {
                        Undo.RecordObject(settings, "Sync Local Dev URL To Profile");
                        settings.profileSettings.SetValue(
                            settings.activeProfileId,
                            RemoteBaseUrlVariableName,
                            AddressablesContentSettings.DefaultLocalDevBaseUrl);
                        EditorUtility.SetDirty(settings);
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private static void DrawBuildOptionsSection(AddressableAssetSettings settings)
        {
            _showBuildOptions = EditorGUILayout.BeginFoldoutHeaderGroup(_showBuildOptions, "构建与下载行为");
            if (_showBuildOptions)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(
                    _buildAddressablesWithPlayerBuild,
                    new GUIContent(
                        "随 Player 构建 Addressables",
                        BuildWithPlayerTooltip(settings)));

                EditorGUILayout.PropertyField(
                    _maxConcurrentWebRequests,
                    new GUIContent(
                        "最大并发 Web 请求数",
                        BuildMaxConcurrentTooltip(settings)));

                EditorGUILayout.PropertyField(
                    _contiguousBundles,
                    new GUIContent(
                        "Contiguous Bundles",
                        BuildContiguousBundlesTooltip(settings)));

                EditorGUILayout.PropertyField(
                    _optimizeCatalogSize,
                    new GUIContent(
                        "优化 Catalog 体积",
                        BuildOptimizeCatalogTooltip(settings)));

                EditorGUILayout.PropertyField(
                    _nonRecursiveBuilding,
                    new GUIContent(
                        "非递归构建依赖",
                        BuildNonRecursiveTooltip(settings)));

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private static void DrawGroupsSection(AddressableAssetSettings settings)
        {
            _showGroups = EditorGUILayout.BeginFoldoutHeaderGroup(
                _showGroups,
                $"资源分组概览（{settings.groups.Count} 组）");
            if (_showGroups)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.HelpBox(
                    "下列分组由 AddressablesSyncStep 按资产路径自动维护。\n" +
                    "IncludeInBuild=false 的分组仅远程分发，不打入 Player。",
                    MessageType.None);

                for (int i = 0; i < KnownGroupDefinitions.Length; i++)
                {
                    KnownGroupDefinition definition = KnownGroupDefinitions[i];
                    AddressableAssetGroup group = settings.FindGroup(definition.GroupName);
                    DrawGroupRow(settings, definition, group);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private static void DrawGroupRow(
            AddressableAssetSettings settings,
            KnownGroupDefinition definition,
            AddressableAssetGroup group)
        {
            int entryCount = group != null ? group.entries.Count : 0;
            string loadPathVariable = "—";
            string bundleMode = "—";
            bool includeInBuild = false;

            if (group != null)
            {
                BundledAssetGroupSchema bundledSchema = group.GetSchema<BundledAssetGroupSchema>();
                if (bundledSchema != null)
                {
                    loadPathVariable = bundledSchema.LoadPath.GetName(settings);
                    bundleMode = bundledSchema.BundleMode.ToString();
                    includeInBuild = bundledSchema.IncludeInBuild;
                }
            }

            string status = group == null ? "未创建" : $"{entryCount} 项";
            EditorGUILayout.LabelField(
                new GUIContent(
                    definition.GroupName,
                    BuildGroupTooltip(definition, group, loadPathVariable, bundleMode, includeInBuild)),
                new GUIContent(status));
        }

        private static void DrawLabelsSection(AddressableAssetSettings settings)
        {
            List<string> labels = settings.GetLabels();
            _showLabels = EditorGUILayout.BeginFoldoutHeaderGroup(_showLabels, $"标签（{labels.Count} 个）");
            if (_showLabels)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(
                    "标签由 AddressablesSyncStep.EnsureLabels 与分组定义同步。\n" +
                    "运行时可通过 Addressables.LoadAssetsAsync(label) 批量加载。",
                    MessageType.None);

                for (int i = 0; i < labels.Count; i++)
                {
                    string label = labels[i];
                    EditorGUILayout.LabelField(
                        new GUIContent(label, BuildLabelTooltip(label, settings)),
                        GetLabelUsageSummary(settings, label));
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private static void DrawFooterActions(AddressableAssetSettings settings)
        {
            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("保存配置"))
                {
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                }

                if (GUILayout.Button("执行资源同步（AddressablesSyncStep）"))
                {
                    Rogue.Editor.HotUpdate.Pipeline.Steps.AddressablesSyncStep.ExecuteSyncOnly();
                }
            }
        }

        private static string GetActiveProfileName(AddressableAssetSettings settings)
        {
            string profileId = settings.activeProfileId;
            return settings.profileSettings.GetProfileName(profileId);
        }

        private static string GetProfileVariable(AddressableAssetSettings settings, string variableName)
        {
            return settings.profileSettings.GetValueByName(settings.activeProfileId, variableName);
        }

        private static string ResolveProfilePath(AddressableAssetSettings settings, string entryName)
        {
            string template = settings.profileSettings.GetValueByName(settings.activeProfileId, entryName);
            if (string.IsNullOrEmpty(template))
            {
                return "（未配置）";
            }

            return settings.profileSettings.EvaluateString(settings.activeProfileId, template);
        }

        private static string GetLabelUsageSummary(AddressableAssetSettings settings, string label)
        {
            int count = 0;
            for (int i = 0; i < settings.groups.Count; i++)
            {
                AddressableAssetGroup group = settings.groups[i];
                foreach (AddressableAssetEntry entry in group.entries)
                {
                    if (entry.labels != null && entry.labels.Contains(label))
                    {
                        count++;
                    }
                }
            }

            return $"{count} 项资产";
        }

        private static string BuildRemoteCatalogTooltip(AddressableAssetSettings settings)
        {
            var sb = new StringBuilder(512);
            sb.AppendLine("开启后会在 ServerData/{BuildTarget} 输出 catalog.bin / catalog.hash，供 OTA 检查更新。");
            sb.AppendLine();
            sb.AppendLine("【当前值】");
            sb.AppendLine($"  BuildRemoteCatalog = {settings.BuildRemoteCatalog}");
            sb.AppendLine($"  RemoteCatalogBuildPath → {settings.RemoteCatalogBuildPath.GetValue(settings)}");
            sb.AppendLine($"  RemoteCatalogLoadPath → {settings.RemoteCatalogLoadPath.GetValue(settings)}");
            sb.AppendLine();
            sb.AppendLine("【项目约束】");
            sb.AppendLine("  • AddressablesFullBuildStep / ContentUpdateStep 构建前调用 EnsureRemoteCatalogEnabled");
            sb.AppendLine("  • PreflightCheckStep（代码/资源热更）校验 content state 中 remoteCatalogLoadPath 非空");
            sb.AppendLine("  • 若关闭此项，资源/代码热更 Preflight 将失败并提示重新首包构建");
            return sb.ToString().TrimEnd();
        }

        private static string BuildCatalogPathTooltip(AddressableAssetSettings settings, bool isLoadPath)
        {
            string name = isLoadPath ? "Remote Catalog 加载路径" : "Remote Catalog 构建路径";
            ProfileValueReference reference = isLoadPath
                ? settings.RemoteCatalogLoadPath
                : settings.RemoteCatalogBuildPath;
            string resolved = reference.GetValue(settings);

            var sb = new StringBuilder(384);
            sb.AppendLine($"{name}，绑定 Profile 变量后随 BuildTarget / CDN URL 自动展开。");
            sb.AppendLine();
            sb.AppendLine("【当前解析】");
            sb.AppendLine($"  {resolved}");
            sb.AppendLine();
            sb.AppendLine("【本项目默认】");
            sb.AppendLine(isLoadPath
                ? "  {AddressablesRemoteBaseUrl}/[BuildTarget] → 运行时 HTTP 拉取 Catalog"
                : "  ServerData/[BuildTarget] → 热更发布中心构建产物目录");
            if (isLoadPath && HotUpdateContentStatePathUtility.TryResolve(out string contentStatePath))
            {
                sb.AppendLine();
                sb.AppendLine("【Content State 记录的 Load Path】");
                sb.AppendLine($"  见 addressables_content_state.bin（{contentStatePath}）");
            }

            return sb.ToString().TrimEnd();
        }

        private static string BuildBundleLocalCatalogTooltip(AddressableAssetSettings settings)
        {
            return "为 true 时将 Catalog 复制进 StreamingAssets，离线首包可直接读取。\n\n" +
                   $"【当前值】m_BundleLocalCatalog = {_bundleLocalCatalog.boolValue}\n" +
                   "【本项目】BuildRemoteCatalog=1 且 BundleLocalCatalog=0：Catalog 仅远程分发，首包体积更小，依赖启动时 Catalog 更新。";
        }

        private static string BuildDisableCatalogUpdateTooltip(AddressableAssetSettings settings)
        {
            AddressablesContentSettings contentSettings = AssetDatabase.LoadAssetAtPath<AddressablesContentSettings>(
                AddressablesContentSettings.DefaultAssetPath);
            bool runtimeCheck = contentSettings == null || contentSettings.EnableCatalogUpdateOnStartup;

            return "Addressables 运行时初始化是否跳过 CheckForCatalogUpdates。\n\n" +
                   $"【Addressables 设置】DisableCatalogUpdateOnStart = {_disableCatalogUpdateOnStart.boolValue}\n" +
                   $"【ContentSettings】EnableCatalogUpdateOnStartup = {runtimeCheck}\n" +
                   "【说明】项目运行时 Catalog 检查由 AddressablesContentSettings 与 PreBootstrap 共同控制；此处为引擎层总开关。";
        }

        private static string BuildCatalogTimeoutTooltip(AddressableAssetSettings settings)
        {
            AddressablesContentSettings contentSettings = AssetDatabase.LoadAssetAtPath<AddressablesContentSettings>(
                AddressablesContentSettings.DefaultAssetPath);
            float projectTimeout = contentSettings != null
                ? contentSettings.CatalogUpdateTimeoutSeconds
                : 10f;

            return "Catalog HTTP 请求超时。0 表示使用 Addressables 默认超时。\n\n" +
                   $"【Addressables】m_CatalogRequestsTimeout = {_catalogRequestsTimeout.intValue}\n" +
                   $"【ContentSettings】CatalogUpdateTimeoutSeconds = {projectTimeout}s\n" +
                   "【行为】超时后 GameFramework 回退本地 Catalog，避免弱网阻塞启动。";
        }

        private static string BuildProfilePathTooltip(
            AddressableAssetSettings settings,
            string entryName,
            bool isRemote)
        {
            string resolved = ResolveProfilePath(settings, entryName);
            return $"{entryName} 在当前 Profile 下的解析结果。\n\n" +
                   $"【解析值】{resolved}\n" +
                   $"【Profile】{GetActiveProfileName(settings)}\n" +
                   (isRemote
                       ? "【用途】Battle-Entity / HotUpdate-Code-Remote 等远程分组 Bundle 与 Catalog 的 HTTP 前缀。"
                       : "【用途】Config / UI / HotUpdate-Code-Local 等首包分组的 StreamingAssets 路径。");
        }

        private static string BuildRemoteBaseUrlTooltip(
            AddressableAssetSettings settings,
            string publishTabRemoteBaseUrl)
        {
            string current = GetProfileVariable(settings, RemoteBaseUrlVariableName);
            string buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString();
            string fullLoadPrefix = string.IsNullOrEmpty(current)
                ? "（未配置）"
                : $"{current.TrimEnd('/')}/{buildTarget}";

            var sb = new StringBuilder(480);
            sb.AppendLine("Profile 变量，不含 BuildTarget。Remote.LoadPath 模板为 {AddressablesRemoteBaseUrl}/[BuildTarget]。");
            sb.AppendLine();
            sb.AppendLine("【当前 Profile 值】");
            sb.AppendLine($"  {current ?? "（空）"}");
            sb.AppendLine($"【完整运行时加载前缀】");
            sb.AppendLine($"  {fullLoadPrefix}");
            sb.AppendLine();
            sb.AppendLine("【发布 Tab CDN URL】");
            sb.AppendLine($"  {(string.IsNullOrEmpty(publishTabRemoteBaseUrl) ? "（空）" : publishTabRemoteBaseUrl)}");
            sb.AppendLine();
            sb.AppendLine("【关联代码】");
            sb.AppendLine("  LocalDevEnvironment.SyncAddressablesProfileRemoteBaseUrl");
            sb.AppendLine("  PreflightCheckStep.CheckRemoteBaseUrlProfileVariable");
            return sb.ToString().TrimEnd();
        }

        private static string BuildWithPlayerTooltip(AddressableAssetSettings settings)
        {
            return "Unity Build Player 时是否自动构建 Addressables。\n\n" +
                   $"【当前值】{_buildAddressablesWithPlayerBuild.boolValue}\n" +
                   "【本项目】热更发布中心通过 AddressablesFullBuildStep 独立构建；\n" +
                   "  首包 Pipeline 顺序：HybridCLR → Addressables 全量 → PlayerBuildStep（可选）。";
        }

        private static string BuildMaxConcurrentTooltip(AddressableAssetSettings settings)
        {
            return "ResourceManager 同时发起的 HTTP 下载上限。\n\n" +
                   $"【当前值】{_maxConcurrentWebRequests.intValue}\n" +
                   "【本项目 asset 默认】500（见 AddressableAssetSettings.asset）\n" +
                   "【建议】战斗场景大量并行 LoadAssetAsync 时可保持较高值；低端机可适当下调。";
        }

        private static string BuildContiguousBundlesTooltip(AddressableAssetSettings settings)
        {
            return "下载 Bundle 时在磁盘上保持连续布局，利于某些平台的 IO 性能。\n\n" +
                   $"【当前值】{_contiguousBundles.boolValue}\n" +
                   "【本项目 asset 默认】1（已启用）";
        }

        private static string BuildOptimizeCatalogTooltip(AddressableAssetSettings settings)
        {
            return "压缩 Catalog 内重复字符串，减小 catalog.bin 体积。\n\n" +
                   $"【当前值】{_optimizeCatalogSize.boolValue}\n" +
                   "【权衡】开启后 Catalog 解析 CPU 略增，CDN 流量减少。";
        }

        private static string BuildNonRecursiveTooltip(AddressableAssetSettings settings)
        {
            return "构建 Group 时是否递归包含依赖项。\n\n" +
                   $"【当前值】{_nonRecursiveBuilding.boolValue}\n" +
                   "【本项目 asset 默认】1（非递归）\n" +
                   "【说明】与 AddressablesSyncStep 按路径精确入组策略配合，避免重复打包依赖。";
        }

        private static string BuildGroupTooltip(
            KnownGroupDefinition definition,
            AddressableAssetGroup group,
            string loadPathVariable,
            string bundleMode,
            bool includeInBuild)
        {
            var sb = new StringBuilder(640);
            sb.AppendLine(definition.Description);
            sb.AppendLine();
            sb.AppendLine("【同步规则（AddressablesSyncStep）】");
            sb.AppendLine($"  标签：{definition.Label}");
            sb.AppendLine($"  路径匹配：{definition.PathHint}");
            sb.AppendLine($"  远程 LoadPath：{definition.UseRemoteLoadPath}");
            sb.AppendLine($"  IncludeInBuild：{definition.IncludeInBuild}");
            sb.AppendLine($"  BundleMode：{definition.BundleMode}");
            sb.AppendLine();
            sb.AppendLine("【当前资产状态】");
            if (group == null)
            {
                sb.AppendLine("  分组尚未创建，下次「资源同步」或热更构建时将自动创建。");
            }
            else
            {
                sb.AppendLine($"  条目数：{group.entries.Count}");
                sb.AppendLine($"  LoadPath 变量：{loadPathVariable}");
                sb.AppendLine($"  Schema BundleMode：{bundleMode}");
                sb.AppendLine($"  IncludeInBuild：{includeInBuild}");
            }

            if (!definition.IncludeInBuild)
            {
                sb.AppendLine();
                sb.AppendLine("【分发】IncludeInBuild=false → 不打入 Player，仅通过远程 Catalog 按需下载（如 Remote-DLC）。");
            }

            return sb.ToString().TrimEnd();
        }

        private static string BuildLabelTooltip(string label, AddressableAssetSettings settings)
        {
            int usageCount = 0;
            var groupsUsingLabel = new List<string>();
            for (int i = 0; i < settings.groups.Count; i++)
            {
                AddressableAssetGroup group = settings.groups[i];
                int groupCount = 0;
                foreach (AddressableAssetEntry entry in group.entries)
                {
                    if (entry.labels != null && entry.labels.Contains(label))
                    {
                        groupCount++;
                    }
                }

                if (groupCount > 0)
                {
                    usageCount += groupCount;
                    groupsUsingLabel.Add($"{group.Name}({groupCount})");
                }
            }

            return $"Addressables 标签，用于批量加载与内容更新筛选。\n\n" +
                   $"【当前引用】{usageCount} 项资产\n" +
                   $"【所在分组】{(groupsUsingLabel.Count > 0 ? string.Join(", ", groupsUsingLabel) : "无")}\n" +
                   "【来源】AddressablesSyncStep.EnsureLabels 按 GroupDefinitions 自动注册。";
        }

        private sealed class KnownGroupDefinition
        {
            public KnownGroupDefinition(
                string groupName,
                string label,
                string pathHint,
                bool useRemoteLoadPath,
                bool includeInBuild,
                string bundleMode,
                string description)
            {
                GroupName = groupName;
                Label = label;
                PathHint = pathHint;
                UseRemoteLoadPath = useRemoteLoadPath;
                IncludeInBuild = includeInBuild;
                BundleMode = bundleMode;
                Description = description;
            }

            public string GroupName { get; }
            public string Label { get; }
            public string PathHint { get; }
            public bool UseRemoteLoadPath { get; }
            public bool IncludeInBuild { get; }
            public string BundleMode { get; }
            public string Description { get; }
        }

        private static readonly KnownGroupDefinition[] KnownGroupDefinitions =
        {
            new KnownGroupDefinition(
                AddressablesGroupResolver.Config,
                "config",
                "Assets/GameAssetConfig/**, Assets/Config/**",
                true,
                true,
                "PackTogether",
                "ScriptableObject 配置表（含 HeroAssets 等），Remote 路径输出到 ServerData，支持配表资源热更。"),
            new KnownGroupDefinition(
                AddressablesGroupResolver.UI,
                "ui",
                "Assets/Prefabs/UI/**",
                false,
                true,
                "PackTogether",
                "UI Prefab，随首包 Local.LoadPath 加载。"),
            new KnownGroupDefinition(
                AddressablesGroupResolver.SceneCore,
                "scene-core",
                "Assets/Scene/Launcher.unity",
                false,
                true,
                "PackSeparately",
                "启动场景 Launcher，随首包 Local 路径加载。"),
            new KnownGroupDefinition(
                AddressablesGroupResolver.BattleEntity,
                "battle-entity",
                "Assets/Prefabs/Battle/Hero/**, Monster/**",
                true,
                true,
                "PackSeparately",
                "英雄/怪物 Prefab，远程热更，按角色分包 PackSeparately。"),
            new KnownGroupDefinition(
                AddressablesGroupResolver.BattleContent,
                "battle-content",
                "Assets/Prefabs/Battle/**（非 Hero/Monster）",
                true,
                true,
                "PackSeparately",
                "战斗特效、技能等非实体 Prefab，支持资源热更。"),
            new KnownGroupDefinition(
                AddressablesGroupResolver.Map,
                "map",
                "Assets/Prefabs/Map/**, RoguelikeMap/**, NavMesh/**",
                true,
                true,
                "PackTogether",
                "地图与 NavMesh Prefab，远程 PackTogether 减少 Bundle 数。"),
            new KnownGroupDefinition(
                AddressablesGroupResolver.SceneBattle,
                "scene-battle",
                "Assets/Scene/**（除 Launcher）",
                true,
                true,
                "PackSeparately",
                "战斗场景，远程分发，可按场景独立热更。"),
            new KnownGroupDefinition(
                AddressablesGroupResolver.RemoteDlc,
                "remote-dlc",
                "（手动分配，Sync 不自动扫描）",
                true,
                false,
                "PackSeparately",
                "可选 DLC 内容，IncludeInBuild=false，仅 Catalog 可见、按需下载。"),
            new KnownGroupDefinition(
                AddressablesGroupResolver.HotUpdateCodeLocal,
                "hotupdate-aot",
                "Assets/HotUpdate/Code/AOT/*.bytes",
                false,
                true,
                "PackTogether",
                "HybridCLR AOT 元数据，随首包 Local 路径加载。"),
            new KnownGroupDefinition(
                AddressablesGroupResolver.HotUpdateCodeRemote,
                "hotupdate-runtime",
                "Assets/HotUpdate/Code/*.bytes, manifest.json",
                true,
                true,
                "PackTogether",
                "Game.Runtime.dll.bytes 等热更程序集，代码热更 Content Update 目标分组。"),
        };
    }
}
