using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ConfigCenterWindow : EditorWindow
{
    private const string WindowTitle = "配置中心";
    private static readonly string[] ScanFolders =
    {
        "Assets/GameAssetConfig",
        "Assets/Config",
    };

    private static readonly GUIContent[] ScopeLabels =
    {
        new GUIContent("全部"),
        new GUIContent("GameAssetConfig"),
        new GUIContent("Config"),
    };

    private static readonly string[] CriticalGameAssetConfigs =
    {
        "EntityCampConfig",
        "GameSetting",
        "RollBackGameConfig",
        "SceneAssets",
    };

    private readonly List<ConfigEntry> _entries = new List<ConfigEntry>();
    private readonly List<string> _missingRuntimeTables = new List<string>();
    private readonly List<string> _missingCriticalConfigs = new List<string>();

    private Vector2 _leftScroll;
    private Vector2 _rightScroll;
    private string _searchText = string.Empty;
    private int _scopeIndex;
    private ConfigEntry _selectedEntry;
    private Editor _selectedEditor;

    [MenuItem("Tools/配置中心")]
    public static void OpenWindow()
    {
        ConfigCenterWindow window = GetWindow<ConfigCenterWindow>();
        window.titleContent = new GUIContent(WindowTitle);
        window.minSize = new Vector2(1100, 620);
        window.Show();
        window.RefreshConfigs();
    }

    private void OnEnable()
    {
        titleContent = new GUIContent(WindowTitle);
        RefreshConfigs();
    }

    private void OnDisable()
    {
        ReleaseEditor();
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawSummaryBar();

        EditorGUILayout.BeginHorizontal();
        DrawLeftPanel();
        DrawRightPanel();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(52)))
        {
            RefreshConfigs();
        }

        if (GUILayout.Button("保存全部", EditorStyles.toolbarButton, GUILayout.Width(68)))
        {
            SaveAllAssets();
        }

        GUILayout.Space(10);
        GUILayout.Label("范围", GUILayout.Width(32));
        _scopeIndex = EditorGUILayout.Popup(_scopeIndex, ScopeLabels, EditorStyles.toolbarPopup, GUILayout.Width(150));

        GUILayout.Space(10);
        GUILayout.Label("搜索", GUILayout.Width(32));
        _searchText = GUILayout.TextField(_searchText, EditorStyles.toolbarSearchField, GUILayout.MinWidth(220));

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("定位 GameAssetConfig", EditorStyles.toolbarButton, GUILayout.Width(150)))
        {
            PingFolder(ScanFolders[0]);
        }

        if (GUILayout.Button("定位 Config", EditorStyles.toolbarButton, GUILayout.Width(110)))
        {
            PingFolder(ScanFolders[1]);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSummaryBar()
    {
        int visibleCount = 0;
        for (int i = 0; i < _entries.Count; i++)
        {
            if (ShouldShow(_entries[i]))
            {
                visibleCount++;
            }
        }

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"配置总数: {_entries.Count}", GUILayout.Width(100));
        EditorGUILayout.LabelField($"当前显示: {visibleCount}", GUILayout.Width(100));
        EditorGUILayout.LabelField($"数据表缺失: {_missingRuntimeTables.Count}", GUILayout.Width(120));
        EditorGUILayout.LabelField($"关键配置缺失: {_missingCriticalConfigs.Count}", GUILayout.Width(140));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        if (_missingRuntimeTables.Count > 0)
        {
            EditorGUILayout.HelpBox($"运行时数据表缺失: {string.Join(", ", _missingRuntimeTables)}", MessageType.Warning);
        }

        if (_missingCriticalConfigs.Count > 0)
        {
            EditorGUILayout.HelpBox($"关键配置缺失: {string.Join(", ", _missingCriticalConfigs)}", MessageType.Warning);
        }
    }

    private void DrawLeftPanel()
    {
        float width = Mathf.Clamp(position.width * 0.36f, 320f, 440f);

        EditorGUILayout.BeginVertical(GUILayout.Width(width));
        EditorGUILayout.LabelField("配置文件", EditorStyles.boldLabel);

        _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll, GUI.skin.box);
        for (int i = 0; i < _entries.Count; i++)
        {
            ConfigEntry entry = _entries[i];
            if (!ShouldShow(entry))
            {
                continue;
            }

            DrawEntryItem(entry);
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();
    }

    private void DrawEntryItem(ConfigEntry entry)
    {
        bool selected = entry == _selectedEntry;
        GUIStyle boxStyle = selected ? EditorStyles.helpBox : GUI.skin.box;

        EditorGUILayout.BeginVertical(boxStyle);
        if (GUILayout.Button(entry.DisplayName, EditorStyles.boldLabel))
        {
            SelectEntry(entry);
        }

        EditorGUILayout.LabelField(entry.TypeName, EditorStyles.miniLabel);
        EditorGUILayout.LabelField(entry.AssetPath, EditorStyles.miniLabel);

        if (entry.IsAssetsTable)
        {
            EditorGUILayout.LabelField($"条目数: {entry.RecordCount}", EditorStyles.miniLabel);
        }

        if (!string.IsNullOrEmpty(entry.Warning))
        {
            EditorGUILayout.HelpBox(entry.Warning, MessageType.Warning);
        }

        EditorGUILayout.EndVertical();
        GUILayout.Space(4);
    }

    private void DrawRightPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

        if (_selectedEntry == null || _selectedEntry.Asset == null)
        {
            EditorGUILayout.HelpBox("从左侧选择一个配置文件。", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUILayout.LabelField(_selectedEntry.DisplayName, EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("定位", EditorStyles.toolbarButton, GUILayout.Width(52)))
        {
            Selection.activeObject = _selectedEntry.Asset;
            EditorGUIUtility.PingObject(_selectedEntry.Asset);
        }

        if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(52)))
        {
            EditorUtility.SetDirty(_selectedEntry.Asset);
            AssetDatabase.SaveAssets();
        }

        EditorGUILayout.EndHorizontal();

        DrawSelectedSummary();

        _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);
        if (_selectedEditor != null)
        {
            EditorGUI.BeginChangeCheck();
            _selectedEditor.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_selectedEntry.Asset);
                RefreshEntryMetadata(_selectedEntry);
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();
    }

    private void DrawSelectedSummary()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("路径", _selectedEntry.AssetPath);
        EditorGUILayout.LabelField("类型", _selectedEntry.TypeName);
        EditorGUILayout.LabelField("来源", _selectedEntry.ScopeName);

        if (_selectedEntry.IsAssetsTable)
        {
            EditorGUILayout.LabelField("条目数", _selectedEntry.RecordCount.ToString());
            EditorGUILayout.LabelField("数据类型", _selectedEntry.DataTypeName);
        }

        if (!string.IsNullOrEmpty(_selectedEntry.Warning))
        {
            EditorGUILayout.HelpBox(_selectedEntry.Warning, MessageType.Warning);
        }

        EditorGUILayout.EndVertical();
    }

    private bool ShouldShow(ConfigEntry entry)
    {
        if (_scopeIndex > 0 && entry.ScopeIndex != _scopeIndex)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_searchText))
        {
            return true;
        }

        string search = _searchText.Trim();
        return entry.DisplayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
               entry.AssetPath.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
               entry.TypeName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
               entry.ScopeName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void RefreshConfigs()
    {
        _entries.Clear();
        _missingRuntimeTables.Clear();
        _missingCriticalConfigs.Clear();

        AddScriptableObjects(ScanFolders[0], 1);
        AddScriptableObjects(ScanFolders[1], 2);

        ValidateRuntimeTables();
        ValidateCriticalConfigs();
        _entries.Sort(CompareEntries);

        if (_entries.Count == 0)
        {
            SelectEntry(null);
            Repaint();
            return;
        }

        if (_selectedEntry != null)
        {
            ConfigEntry matched = FindEntryByPath(_selectedEntry.AssetPath);
            SelectEntry(matched ?? _entries[0]);
        }
        else
        {
            SelectEntry(_entries[0]);
        }

        Repaint();
    }

    private void AddScriptableObjects(string folder, int scopeIndex)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { folder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (asset == null || asset is not IAssetsConfig)
            {
                continue;
            }

            ConfigEntry entry = new ConfigEntry
            {
                Asset = asset,
                AssetPath = path,
                DisplayName = asset.name,
                TypeName = asset.GetType().Name,
                ScopeIndex = scopeIndex,
                ScopeName = ScopeLabels[scopeIndex].text,
            };

            RefreshEntryMetadata(entry);
            _entries.Add(entry);
        }
    }

    private void RefreshEntryMetadata(ConfigEntry entry)
    {
        entry.Warning = string.Empty;
        entry.RecordCount = -1;
        entry.IsAssetsTable = true;

        IAssetsConfig assetsConfig = (IAssetsConfig)entry.Asset;
        entry.DataTypeName = assetsConfig.GetDataTableType().Name;

        List<EntityAssetsConfig> configs = assetsConfig.GetAllDataTable();
        entry.RecordCount = configs == null ? 0 : configs.Count;

        if (configs == null)
        {
            entry.Warning = "数据列表为空。";
            return;
        }

        HashSet<int> ids = new HashSet<int>();
        List<int> duplicateIds = new List<int>();
        for (int i = 0; i < configs.Count; i++)
        {
            EntityAssetsConfig config = configs[i];
            if (config == null)
            {
                entry.Warning = AppendWarning(entry.Warning, $"第 {i + 1} 条数据为空。");
                continue;
            }

            if (!ids.Add(config.assetsId) && !duplicateIds.Contains(config.assetsId))
            {
                duplicateIds.Add(config.assetsId);
            }
        }

        if (duplicateIds.Count > 0)
        {
            entry.Warning = AppendWarning(entry.Warning, $"重复 ID: {string.Join(", ", duplicateIds)}");
        }
    }

    private void ValidateRuntimeTables()
    {
        if (DataTableHelper.DataTableNames == null)
        {
            return;
        }

        for (int i = 0; i < DataTableHelper.DataTableNames.Length; i++)
        {
            string tableName = DataTableHelper.DataTableNames[i];
            string path = AssetsPathHelper.GameAssetsConfigHelper(tableName);
            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) == null)
            {
                _missingRuntimeTables.Add(tableName);
            }
        }
    }

    private void ValidateCriticalConfigs()
    {
        for (int i = 0; i < CriticalGameAssetConfigs.Length; i++)
        {
            string configName = CriticalGameAssetConfigs[i];
            string path = AssetsPathHelper.GameAssetsConfigHelper(configName);
            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) == null)
            {
                _missingCriticalConfigs.Add(configName);
            }
        }
    }

    private void SelectEntry(ConfigEntry entry)
    {
        _selectedEntry = entry;
        ReleaseEditor();

        if (_selectedEntry == null || _selectedEntry.Asset == null)
        {
            return;
        }

        _selectedEditor = Editor.CreateEditor(_selectedEntry.Asset);
        Selection.activeObject = _selectedEntry.Asset;
        EditorGUIUtility.PingObject(_selectedEntry.Asset);
    }



    private void ReleaseEditor()
    {
        if (_selectedEditor == null)
        {
            return;
        }

        DestroyImmediate(_selectedEditor);
        _selectedEditor = null;
    }

    private void SaveAllAssets()
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Asset == null)
            {
                continue;
            }

            EditorUtility.SetDirty(_entries[i].Asset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private ConfigEntry FindEntryByPath(string assetPath)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            if (string.Equals(_entries[i].AssetPath, assetPath, StringComparison.OrdinalIgnoreCase))
            {
                return _entries[i];
            }
        }

        return null;
    }

    private static void PingFolder(string folder)
    {
        UnityEngine.Object folderObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folder);
        if (folderObject == null)
        {
            return;
        }

        Selection.activeObject = folderObject;
        EditorGUIUtility.PingObject(folderObject);
    }

    private static int CompareEntries(ConfigEntry left, ConfigEntry right)
    {
        int scopeCompare = left.ScopeIndex.CompareTo(right.ScopeIndex);
        if (scopeCompare != 0)
        {
            return scopeCompare;
        }

        int nameCompare = string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
        if (nameCompare != 0)
        {
            return nameCompare;
        }

        return string.Compare(left.AssetPath, right.AssetPath, StringComparison.OrdinalIgnoreCase);
    }

    private static string AppendWarning(string current, string next)
    {
        if (string.IsNullOrEmpty(current))
        {
            return next;
        }

        return current + "\n" + next;
    }















    private enum ConfigScope
    {
        All = 0,
        GameAssetConfig = 1,
        Config = 2,
    }

    private sealed class ConfigEntry
    {
        public ScriptableObject Asset;
        public string AssetPath;
        public string DisplayName;
        public string TypeName;
        public string ScopeName;
        public string DataTypeName;
        public int ScopeIndex;
        public int RecordCount;
        public bool IsAssetsTable;
        public string Warning;
    }
}
