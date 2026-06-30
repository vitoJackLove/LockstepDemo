using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 快速创建与编辑子弹配置、View Prefab，并写入 BulletAssets 数据表。
/// </summary>
public class BulletQuickCreateWindow : EditorWindow
{
    private const string BulletAssetsPath = "Assets/GameAssetConfig/BulletAssets.asset";
    private const string DefaultVisualPrefabPath = "Assets/Prefabs/Battle/Bullet/Cube.prefab";

    private BulletAssets _bulletAssets;
    private readonly List<BulletAssetsConfig> _visibleConfigs = new List<BulletAssetsConfig>();
    private string _searchText = string.Empty;

    private int _selectedListIndex = -1;
    private bool _isEditingExisting;

    private int _bulletId = 3003;
    private string _bulletName = "新子弹";
    private GameObject _visualPrefab;
    private bool _syncAddressables = true;
    private bool _createViewPrefab = true;

    private float _attack = 10f;
    private int _lifeTime = -1;
    private int _attackNumber = 1;

    private PrimitiveEnum _colliderShape = PrimitiveEnum.BoxPrimitive;
    private Vector3 _colliderOffset = Vector3.zero;
    private float _colliderRadius = 0.5f;
    private float _colliderHeight = 1f;
    private Vector3 _boxSize = Vector3.one;
    private Color _colliderColor = new Color(0f, 0.5f, 1f, 1f);

    private Vector2 _leftScroll;
    private Vector2 _rightScroll;
    private PreviewRenderUtility _previewUtility;
    private GameObject _previewInstance;
    private GameObject _previewedVisual;
    private Bounds _previewBounds;
    private bool _hasPreviewBounds;
    private float _previewZoom = 1f;
    private Vector2 _previewEuler = new Vector2(20f, -35f);
    private readonly List<string> _lastReport = new List<string>();

    [MenuItem("Tools/子弹工厂/快速配置子弹")]
    public static void OpenWindow()
    {
        BulletQuickCreateWindow window = GetWindow<BulletQuickCreateWindow>();
        window.titleContent = new GUIContent("子弹快速配置");
        window.minSize = new Vector2(920f, 640f);
        window.Show();
    }

    private void OnEnable()
    {
        titleContent = new GUIContent("子弹快速配置");
        ReloadBulletAssets();
        EnsureDefaultVisual();
        SuggestNextBulletId();
    }

    private void OnDisable()
    {
        ReleaseModelPreview();
    }

    private void OnGUI()
    {
        EnsureDefaultVisual();

        EditorGUILayout.BeginHorizontal();
        DrawLeftPanel();
        DrawRightPanel();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawLeftPanel()
    {
        float width = Mathf.Clamp(position.width * 0.32f, 240f, 320f);
        EditorGUILayout.BeginVertical(GUILayout.Width(width));

        EditorGUILayout.LabelField("已有子弹", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("刷新", GUILayout.Width(52f)))
            {
                ReloadBulletAssets();
            }

            if (GUILayout.Button("新建", GUILayout.Width(52f)))
            {
                BeginNewBullet();
            }
        }

        _searchText = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField);

        _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll, GUI.skin.box);
        RefreshVisibleConfigs();
        for (int i = 0; i < _visibleConfigs.Count; i++)
        {
            BulletAssetsConfig config = _visibleConfigs[i];
            bool selected = _isEditingExisting && _selectedListIndex == i;
            using (new EditorGUILayout.VerticalScope(selected ? EditorStyles.helpBox : GUI.skin.box))
            {
                if (GUILayout.Button($"{config.assetsId} · {GetDisplayName(config)}", EditorStyles.boldLabel))
                {
                    LoadConfig(config, i);
                }

                EditorGUILayout.LabelField($"攻击 {config.attack} · 次数 {config.attackNumber} · 存活 {FormatLifeTime(config.lifeTime)}",
                    EditorStyles.miniLabel);
            }

            GUILayout.Space(3f);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawRightPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);

        string modeLabel = _isEditingExisting ? $"编辑子弹 #{_bulletId}" : "新建子弹";
        EditorGUILayout.LabelField(modeLabel, EditorStyles.largeLabel);
        DrawBasicSection();
        DrawStatsSection();
        DrawColliderSection();
        DrawPreviewSection();
        DrawActions();
        DrawReport();

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawBasicSection()
    {
        EditorGUILayout.LabelField("基础信息", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUI.BeginDisabledGroup(_isEditingExisting);
            _bulletId = EditorGUILayout.IntField("子弹 ID", _bulletId);
            EditorGUI.EndDisabledGroup();

            _bulletName = EditorGUILayout.TextField("子弹名字", _bulletName);

            EditorGUI.BeginChangeCheck();
            _visualPrefab = (GameObject)EditorGUILayout.ObjectField("视觉 Prefab", _visualPrefab, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck())
            {
                ReleaseModelPreview();
            }

            EditorGUILayout.HelpBox("未指定视觉 Prefab 时将使用默认 Cube 模板。技能时间轴里的「子弹Editor」字段可引用生成的 View Prefab。",
                MessageType.Info);

            DrawModelPreview();
            _createViewPrefab = EditorGUILayout.Toggle("生成 View Prefab", _createViewPrefab);
            _syncAddressables = EditorGUILayout.Toggle("保存后同步 Addressables", _syncAddressables);
        }
    }

    private void DrawStatsSection()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("战斗属性", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            _attack = EditorGUILayout.FloatField("攻击力", _attack);
            _lifeTime = EditorGUILayout.IntField("存活时间(帧)", _lifeTime);
            _attackNumber = EditorGUILayout.IntField("攻击次数", _attackNumber);
            EditorGUILayout.HelpBox("存活时间为 -1 表示不自动销毁。", MessageType.None);
        }
    }

    private void DrawColliderSection()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("受击盒", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            _colliderShape = (PrimitiveEnum)EditorGUILayout.EnumPopup("形状", _colliderShape);
            _colliderOffset = EditorGUILayout.Vector3Field("中心偏移", _colliderOffset);
            _colliderColor = EditorGUILayout.ColorField("调试颜色", _colliderColor);

            if (_colliderShape == PrimitiveEnum.CapsulePrimitive)
            {
                _colliderRadius = EditorGUILayout.FloatField("半径", _colliderRadius);
                _colliderHeight = EditorGUILayout.FloatField("高度", _colliderHeight);
            }
            else if (_colliderShape == PrimitiveEnum.SpherePrimitive)
            {
                _colliderRadius = EditorGUILayout.FloatField("半径", _colliderRadius);
            }
            else if (_colliderShape == PrimitiveEnum.BoxPrimitive)
            {
                _boxSize = EditorGUILayout.Vector3Field("盒子尺寸", _boxSize);
            }
        }
    }

    private void DrawPreviewSection()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("将写入", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            string id = _bulletId.ToString();
            EditorGUILayout.LabelField("配置表", BulletAssetsPath);
            EditorGUILayout.LabelField("assetsPath", $"Bullet/{id}/{id}View");
            if (_createViewPrefab)
            {
                EditorGUILayout.LabelField("View Prefab", $"Assets/Prefabs/Battle/Bullet/{id}/{id}View.prefab");
            }
        }
    }

    private void DrawActions()
    {
        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.HorizontalScope())
        {
            string buttonLabel = _isEditingExisting ? "保存修改" : "创建子弹配置";
            if (GUILayout.Button(buttonLabel, GUILayout.Height(34f)))
            {
                SaveBullet();
            }

            if (GUILayout.Button("重置表单", GUILayout.Width(90f), GUILayout.Height(34f)))
            {
                BeginNewBullet();
            }

            if (GUILayout.Button("清空日志", GUILayout.Width(90f), GUILayout.Height(34f)))
            {
                _lastReport.Clear();
            }
        }
    }

    private void DrawReport()
    {
        if (_lastReport.Count == 0)
        {
            return;
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("结果", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            for (int i = 0; i < _lastReport.Count; i++)
            {
                EditorGUILayout.LabelField(_lastReport[i], EditorStyles.wordWrappedLabel);
            }
        }
    }

    private void DrawModelPreview()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("预览", EditorStyles.miniBoldLabel, GUILayout.Width(40f));
            GUILayout.Label("缩放", GUILayout.Width(32f));
            _previewZoom = EditorGUILayout.Slider(_previewZoom, 0.35f, 3f);
            if (GUILayout.Button("重置", GUILayout.Width(52f)))
            {
                _previewZoom = 1f;
                _previewEuler = new Vector2(20f, -35f);
            }
        }

        Rect previewRect = GUILayoutUtility.GetRect(
            GUIContent.none,
            GUIStyle.none,
            GUILayout.Height(220f),
            GUILayout.ExpandWidth(true));

        GUI.Box(previewRect, GUIContent.none);
        HandlePreviewInput(previewRect);

        GameObject visual = GetVisualSource();
        if (visual == null)
        {
            DrawCenteredText(previewRect, "未找到可用视觉 Prefab");
            return;
        }

        EnsureModelPreview(visual);

        Rect contentRect = new Rect(
            previewRect.x + 4f,
            previewRect.y + 4f,
            previewRect.width - 8f,
            previewRect.height - 8f);

        if (_previewUtility != null && _previewInstance != null && _hasPreviewBounds)
        {
            RenderModelPreview(contentRect);
            if (contentRect.width > 1f && contentRect.height > 1f)
            {
                DrawColliderOverlay(contentRect);
                DrawPreviewTips(contentRect);
            }

            return;
        }

        Texture2D previewTexture = AssetPreview.GetAssetPreview(visual) ?? AssetPreview.GetMiniThumbnail(visual);
        if (previewTexture == null)
        {
            DrawCenteredText(previewRect, "当前资源没有可用预览");
            return;
        }

        GUI.DrawTexture(ScaleToFit(previewTexture, previewRect), previewTexture, ScaleMode.ScaleToFit, true);
    }

    private void HandlePreviewInput(Rect previewRect)
    {
        Event currentEvent = Event.current;
        if (!previewRect.Contains(currentEvent.mousePosition))
        {
            return;
        }

        if (currentEvent.type == EventType.ScrollWheel)
        {
            _previewZoom = Mathf.Clamp(_previewZoom - currentEvent.delta.y * 0.06f, 0.35f, 3f);
            currentEvent.Use();
            Repaint();
        }
        else if (currentEvent.type == EventType.MouseDrag && currentEvent.button == 0)
        {
            _previewEuler.x = Mathf.Clamp(_previewEuler.x - currentEvent.delta.y * 0.35f, -80f, 80f);
            _previewEuler.y += currentEvent.delta.x * 0.35f;
            currentEvent.Use();
            Repaint();
        }
    }

    private void EnsureModelPreview(GameObject visual)
    {
        if (_previewUtility != null && _previewInstance != null && _previewedVisual == visual)
        {
            return;
        }

        ReleaseModelPreview();
        if (visual == null)
        {
            return;
        }

        _previewUtility = new PreviewRenderUtility();
        _previewUtility.camera.nearClipPlane = 0.01f;
        _previewUtility.camera.farClipPlane = 500f;
        _previewUtility.camera.fieldOfView = 30f;
        _previewUtility.lights[0].intensity = 1.2f;
        _previewUtility.lights[0].transform.rotation = Quaternion.Euler(35f, 35f, 0f);
        _previewUtility.lights[1].intensity = 0.6f;

        _previewedVisual = visual;
        _previewInstance = Instantiate(visual);
        _previewInstance.hideFlags = HideFlags.HideAndDontSave;
        SetHideFlagsRecursive(_previewInstance, HideFlags.HideAndDontSave);
        _previewUtility.AddSingleGO(_previewInstance);
        _hasPreviewBounds = TryCalculateBounds(_previewInstance, out _previewBounds);
    }

    private void ReleaseModelPreview()
    {
        if (_previewUtility != null)
        {
            _previewUtility.Cleanup();
            _previewUtility = null;
        }

        if (_previewInstance != null)
        {
            DestroyImmediate(_previewInstance);
            _previewInstance = null;
        }

        _previewedVisual = null;
        _hasPreviewBounds = false;
    }

    private void RenderModelPreview(Rect rect)
    {
        if (Event.current.type != EventType.Repaint || rect.width <= 1f || rect.height <= 1f)
        {
            return;
        }

        Camera camera = _previewUtility.camera;
        Bounds bounds = _previewBounds;
        Vector3 center = bounds.center;
        float radius = Mathf.Max(bounds.extents.magnitude, 0.5f);
        Quaternion rotation = Quaternion.Euler(_previewEuler.x, _previewEuler.y, 0f);
        float distance = Mathf.Clamp(radius * 3.2f / _previewZoom, 0.5f, 200f);

        camera.transform.position = center + rotation * new Vector3(0f, 0f, -distance);
        camera.transform.rotation = rotation;
        camera.clearFlags = CameraClearFlags.Color;
        camera.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1f);

        _previewUtility.BeginPreview(rect, GUIStyle.none);
        camera.Render();
        Texture previewTexture = _previewUtility.EndPreview();
        GUI.DrawTexture(rect, previewTexture, ScaleMode.StretchToFill, false);
    }

    private void DrawColliderOverlay(Rect rect)
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        Handles.BeginGUI();
        Color previousColor = Handles.color;
        Handles.color = new Color(_colliderColor.r, _colliderColor.g, _colliderColor.b, 0.95f);

        Vector3 center = _colliderOffset;
        switch (_colliderShape)
        {
            case PrimitiveEnum.CapsulePrimitive:
                DrawCapsuleOverlay(rect, center, Mathf.Max(0.01f, _colliderRadius), Mathf.Max(_colliderHeight, _colliderRadius * 2f));
                break;
            case PrimitiveEnum.SpherePrimitive:
                DrawCircleOverlay(rect, center, Mathf.Max(0.01f, _colliderRadius));
                break;
            case PrimitiveEnum.BoxPrimitive:
                DrawBoxOverlay(rect, center, _boxSize);
                break;
        }

        Handles.color = previousColor;
        Handles.EndGUI();
    }

    private void DrawCircleOverlay(Rect rect, Vector3 center, float radius)
    {
        DrawCircleOverlay(rect, center, radius, Vector3.right, Vector3.up);
        DrawCircleOverlay(rect, center, radius, Vector3.right, Vector3.forward);
        DrawCircleOverlay(rect, center, radius, Vector3.forward, Vector3.up);
    }

    private void DrawCapsuleOverlay(Rect rect, Vector3 center, float radius, float height)
    {
        float halfLine = Mathf.Max(0f, height * 0.5f - radius);
        Vector3 top = center + Vector3.up * halfLine;
        Vector3 bottom = center - Vector3.up * halfLine;

        DrawCircleOverlay(rect, top, radius, Vector3.right, Vector3.forward);
        DrawCircleOverlay(rect, bottom, radius, Vector3.right, Vector3.forward);
        DrawArcOverlay(rect, top, radius, Vector3.right, Vector3.up, 0f, 180f);
        DrawArcOverlay(rect, bottom, radius, Vector3.right, Vector3.up, 180f, 360f);
        DrawArcOverlay(rect, top, radius, Vector3.forward, Vector3.up, 0f, 180f);
        DrawArcOverlay(rect, bottom, radius, Vector3.forward, Vector3.up, 180f, 360f);
        DrawLineOverlay(rect, top + Vector3.left * radius, bottom + Vector3.left * radius);
        DrawLineOverlay(rect, top + Vector3.right * radius, bottom + Vector3.right * radius);
        DrawLineOverlay(rect, top + Vector3.back * radius, bottom + Vector3.back * radius);
        DrawLineOverlay(rect, top + Vector3.forward * radius, bottom + Vector3.forward * radius);
    }

    private void DrawCircleOverlay(Rect rect, Vector3 center, float radius, Vector3 axisA, Vector3 axisB)
    {
        const int SegmentCount = 48;
        Vector3[] points = new Vector3[SegmentCount + 1];
        for (int i = 0; i <= SegmentCount; i++)
        {
            float angle = i / (float)SegmentCount * Mathf.PI * 2f;
            Vector3 worldPoint = center + axisA * (Mathf.Cos(angle) * radius) + axisB * (Mathf.Sin(angle) * radius);
            points[i] = WorldToPreviewPoint(rect, worldPoint);
        }

        Handles.DrawAAPolyLine(2.5f, points);
    }

    private void DrawArcOverlay(Rect rect, Vector3 center, float radius, Vector3 axisA, Vector3 axisB, float startDegrees, float endDegrees)
    {
        const int SegmentCount = 24;
        Vector3[] points = new Vector3[SegmentCount + 1];
        for (int i = 0; i <= SegmentCount; i++)
        {
            float angle = Mathf.Lerp(startDegrees, endDegrees, i / (float)SegmentCount) * Mathf.Deg2Rad;
            Vector3 worldPoint = center + axisA * (Mathf.Cos(angle) * radius) + axisB * (Mathf.Sin(angle) * radius);
            points[i] = WorldToPreviewPoint(rect, worldPoint);
        }

        Handles.DrawAAPolyLine(2.5f, points);
    }

    private void DrawBoxOverlay(Rect rect, Vector3 center, Vector3 size)
    {
        Vector3 half = new Vector3(
            Mathf.Max(0.01f, size.x) * 0.5f,
            Mathf.Max(0.01f, size.y) * 0.5f,
            Mathf.Max(0.01f, size.z) * 0.5f);
        Vector3[] corners =
        {
            center + new Vector3(-half.x, -half.y, -half.z),
            center + new Vector3(-half.x, -half.y, half.z),
            center + new Vector3(-half.x, half.y, -half.z),
            center + new Vector3(-half.x, half.y, half.z),
            center + new Vector3(half.x, -half.y, -half.z),
            center + new Vector3(half.x, -half.y, half.z),
            center + new Vector3(half.x, half.y, -half.z),
            center + new Vector3(half.x, half.y, half.z),
        };

        DrawLineOverlay(rect, corners[0], corners[1]);
        DrawLineOverlay(rect, corners[0], corners[2]);
        DrawLineOverlay(rect, corners[0], corners[4]);
        DrawLineOverlay(rect, corners[3], corners[1]);
        DrawLineOverlay(rect, corners[3], corners[2]);
        DrawLineOverlay(rect, corners[3], corners[7]);
        DrawLineOverlay(rect, corners[5], corners[1]);
        DrawLineOverlay(rect, corners[5], corners[4]);
        DrawLineOverlay(rect, corners[5], corners[7]);
        DrawLineOverlay(rect, corners[6], corners[2]);
        DrawLineOverlay(rect, corners[6], corners[4]);
        DrawLineOverlay(rect, corners[6], corners[7]);
    }

    private void DrawLineOverlay(Rect rect, Vector3 from, Vector3 to)
    {
        Handles.DrawAAPolyLine(2.5f, WorldToPreviewPoint(rect, from), WorldToPreviewPoint(rect, to));
    }

    private Vector3 WorldToPreviewPoint(Rect rect, Vector3 worldPoint)
    {
        Vector3 viewport = _previewUtility.camera.WorldToViewportPoint(worldPoint);
        return new Vector3(
            rect.x + viewport.x * rect.width,
            rect.y + (1f - viewport.y) * rect.height,
            0f);
    }

    private void DrawPreviewTips(Rect rect)
    {
        Rect labelRect = new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, 18f);
        GUI.Label(labelRect, "左键拖拽旋转，滚轮缩放；线框为当前受击盒", EditorStyles.whiteMiniLabel);
    }

    private static bool TryCalculateBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            bounds = new Bounds(root.transform.position, Vector3.one);
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 0; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return true;
    }

    private static void SetHideFlagsRecursive(GameObject root, HideFlags hideFlags)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            children[i].gameObject.hideFlags = hideFlags;
        }
    }

    private static void DrawCenteredText(Rect rect, string text)
    {
        GUI.Label(rect, text, new GUIStyle(EditorStyles.centeredGreyMiniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
        });
    }

    private static Rect ScaleToFit(Texture2D texture, Rect bounds)
    {
        float padding = 4f;
        Rect paddedBounds = new Rect(
            bounds.x + padding,
            bounds.y + padding,
            bounds.width - padding * 2f,
            bounds.height - padding * 2f);
        float textureAspect = texture.width / (float)texture.height;
        float boundsAspect = paddedBounds.width / paddedBounds.height;

        if (textureAspect > boundsAspect)
        {
            float height = paddedBounds.width / textureAspect;
            return new Rect(paddedBounds.x, paddedBounds.y + (paddedBounds.height - height) * 0.5f, paddedBounds.width, height);
        }

        float width = paddedBounds.height * textureAspect;
        return new Rect(paddedBounds.x + (paddedBounds.width - width) * 0.5f, paddedBounds.y, width, paddedBounds.height);
    }

    private void SaveBullet()
    {
        _lastReport.Clear();

        try
        {
            if (!ValidateInputs(out string error))
            {
                AddError(error);
                return;
            }

            BulletCreatePaths paths = BulletCreatePaths.Create(_bulletId);
            if (!_isEditingExisting && !ValidateCreateTargets(paths, out error))
            {
                AddError(error);
                return;
            }

            if (_createViewPrefab)
            {
                EnsureFolder(paths.BulletFolder);
                CreateViewPrefab(paths, GetVisualSource());
                AddInfo($"已创建 View Prefab：{paths.ViewPrefab}");
            }

            BulletAssetsConfig config = BuildConfig(paths);
            if (_isEditingExisting)
            {
                UpdateExistingConfig(config);
                AddInfo($"已更新 BulletAssets 配置：ID {_bulletId}");
            }
            else
            {
                _bulletAssets.bulletAssetsConfigList.Add(config);
                AddInfo($"已写入 BulletAssets 配置：ID {_bulletId}");
            }

            EditorUtility.SetDirty(_bulletAssets);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (_syncAddressables)
            {
                RuntimeAddressablesConfigurator.SyncRuntimeAssets();
                AddInfo("已同步 Runtime Addressables。");
            }

            ReloadBulletAssets();
            SelectConfigById(_bulletId);

            if (_createViewPrefab)
            {
                UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(paths.ViewPrefab);
                if (prefab != null)
                {
                    Selection.activeObject = prefab;
                    EditorGUIUtility.PingObject(prefab);
                }
            }

            AddInfo($"保存完成：子弹 {_bulletId} / {_bulletName}");
        }
        catch (Exception exception)
        {
            AddError(exception.ToString());
        }
    }

    private bool ValidateInputs(out string error)
    {
        if (_bulletId <= 0)
        {
            error = "子弹 ID 必须大于 0。";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_bulletName))
        {
            error = "子弹名字不能为空。";
            return false;
        }

        if (_attackNumber <= 0)
        {
            error = "攻击次数必须大于 0。";
            return false;
        }

        if (_bulletAssets == null)
        {
            error = $"找不到 BulletAssets 配置：{BulletAssetsPath}";
            return false;
        }

        if (GetVisualSource() == null)
        {
            error = "未找到可用视觉 Prefab，请指定视觉资源或保留默认 Cube 模板。";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private bool ValidateCreateTargets(BulletCreatePaths paths, out string error)
    {
        if (_bulletAssets.GetDataTable(_bulletId) != null)
        {
            error = $"BulletAssets 已存在子弹 ID：{_bulletId}";
            return false;
        }

        if (_createViewPrefab &&
            (AssetDatabase.LoadAssetAtPath<GameObject>(paths.ViewPrefab) != null || File.Exists(paths.ViewPrefab)))
        {
            error = $"目标 View Prefab 已存在：{paths.ViewPrefab}";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private BulletAssetsConfig BuildConfig(BulletCreatePaths paths)
    {
        return new BulletAssetsConfig
        {
            assetsId = _bulletId,
            assetsPath = _createViewPrefab ? $"Bullet/{_bulletId}/{_bulletId}View" : string.Empty,
            heroName = _bulletName,
            attack = _attack,
            lifeTime = _lifeTime,
            attackNumber = _attackNumber,
            colliderDataList = new List<HitColliderEditorSetting> { CreateColliderSetting() },
        };
    }

    private void UpdateExistingConfig(BulletAssetsConfig config)
    {
        List<BulletAssetsConfig> list = _bulletAssets.bulletAssetsConfigList;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].assetsId != _bulletId)
            {
                continue;
            }

            list[i] = config;
            return;
        }

        throw new InvalidOperationException($"未找到待更新的子弹配置：{_bulletId}");
    }

    private HitColliderEditorSetting CreateColliderSetting()
    {
        HitColliderEditorSetting setting = new HitColliderEditorSetting
        {
            key = $"Bullet{_bulletId}",
            primitiveEnum = _colliderShape,
            offset = _colliderOffset,
            eulerOffset = Vector3.zero,
            weight = 1,
            damageMagnification = 1f,
            isShow = true,
            color = _colliderColor,
        };

        if (_colliderShape == PrimitiveEnum.CapsulePrimitive)
        {
            setting.capRadius = _colliderRadius;
            setting.height = _colliderHeight;
        }
        else if (_colliderShape == PrimitiveEnum.SpherePrimitive)
        {
            setting.spRadius = _colliderRadius;
        }
        else if (_colliderShape == PrimitiveEnum.BoxPrimitive)
        {
            setting.x = _boxSize.x;
            setting.y = _boxSize.y;
            setting.z = _boxSize.z;
        }

        return setting;
    }

    private void CreateViewPrefab(BulletCreatePaths paths, GameObject visualSource)
    {
        GameObject root = new GameObject($"{_bulletId}View");
        try
        {
            root.AddComponent<BulletEntityView>();

            GameObject visualInstance = PrefabUtility.InstantiatePrefab(visualSource) as GameObject;
            if (visualInstance == null)
            {
                visualInstance = Instantiate(visualSource);
            }

            visualInstance.name = visualSource.name;
            visualInstance.transform.SetParent(root.transform, false);

            PrefabUtility.SaveAsPrefabAsset(root, paths.ViewPrefab);
        }
        finally
        {
            DestroyImmediate(root);
        }
    }

    private void ReloadBulletAssets()
    {
        _bulletAssets = AssetDatabase.LoadAssetAtPath<BulletAssets>(BulletAssetsPath);
        RefreshVisibleConfigs();
        Repaint();
    }

    private void RefreshVisibleConfigs()
    {
        _visibleConfigs.Clear();
        if (_bulletAssets?.bulletAssetsConfigList == null)
        {
            return;
        }

        string search = string.IsNullOrWhiteSpace(_searchText) ? null : _searchText.Trim();
        List<BulletAssetsConfig> source = _bulletAssets.bulletAssetsConfigList;
        for (int i = 0; i < source.Count; i++)
        {
            BulletAssetsConfig config = source[i];
            if (config == null)
            {
                continue;
            }

            if (search != null &&
                config.assetsId.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0 &&
                GetDisplayName(config).IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            _visibleConfigs.Add(config);
        }

        _visibleConfigs.Sort((left, right) => left.assetsId.CompareTo(right.assetsId));
    }

    private void BeginNewBullet()
    {
        _isEditingExisting = false;
        _selectedListIndex = -1;
        SuggestNextBulletId();
        _bulletName = "新子弹";
        _visualPrefab = null;
        _attack = 10f;
        _lifeTime = -1;
        _attackNumber = 1;
        _colliderShape = PrimitiveEnum.BoxPrimitive;
        _colliderOffset = Vector3.zero;
        _colliderRadius = 0.5f;
        _colliderHeight = 1f;
        _boxSize = Vector3.one;
        _colliderColor = new Color(0f, 0.5f, 1f, 1f);
        _createViewPrefab = true;
        ReleaseModelPreview();
        Repaint();
    }

    private void LoadConfig(BulletAssetsConfig config, int visibleIndex)
    {
        if (config == null)
        {
            return;
        }

        _isEditingExisting = true;
        _selectedListIndex = visibleIndex;
        _bulletId = config.assetsId;
        _bulletName = GetDisplayName(config);
        _attack = config.attack;
        _lifeTime = config.lifeTime;
        _attackNumber = config.attackNumber;

        if (config.colliderDataList != null && config.colliderDataList.Count > 0)
        {
            HitColliderEditorSetting collider = config.colliderDataList[0];
            _colliderShape = collider.primitiveEnum;
            _colliderOffset = collider.offset;
            _colliderColor = collider.color;
            _colliderRadius = collider.primitiveEnum == PrimitiveEnum.SpherePrimitive ? collider.spRadius : collider.capRadius;
            _colliderHeight = collider.height;
            _boxSize = new Vector3(collider.x, collider.y, collider.z);
        }

        if (!string.IsNullOrEmpty(config.assetsPath))
        {
            string prefabPath = AssetsPathHelper.EntityPathHelper(config.assetsPath);
            _visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }
        else
        {
            _visualPrefab = null;
        }

        _createViewPrefab = string.IsNullOrEmpty(config.assetsPath);
        ReleaseModelPreview();
        Repaint();
    }

    private void SelectConfigById(int bulletId)
    {
        RefreshVisibleConfigs();
        for (int i = 0; i < _visibleConfigs.Count; i++)
        {
            if (_visibleConfigs[i].assetsId != bulletId)
            {
                continue;
            }

            LoadConfig(_visibleConfigs[i], i);
            return;
        }
    }

    private void SuggestNextBulletId()
    {
        int maxId = 3000;
        if (_bulletAssets?.bulletAssetsConfigList != null)
        {
            for (int i = 0; i < _bulletAssets.bulletAssetsConfigList.Count; i++)
            {
                BulletAssetsConfig config = _bulletAssets.bulletAssetsConfigList[i];
                if (config != null)
                {
                    maxId = Math.Max(maxId, config.assetsId);
                }
            }
        }

        _bulletId = maxId + 1;
    }

    private void EnsureDefaultVisual()
    {
        if (_visualPrefab != null)
        {
            return;
        }

        _visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultVisualPrefabPath);
    }

    private GameObject GetVisualSource()
    {
        if (_visualPrefab != null)
        {
            return _visualPrefab;
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(DefaultVisualPrefabPath);
    }

    private static string GetDisplayName(BulletAssetsConfig config)
    {
        return string.IsNullOrWhiteSpace(config.heroName) ? $"Bullet {config.assetsId}" : config.heroName;
    }

    private static string FormatLifeTime(int lifeTime)
    {
        return lifeTime < 0 ? "永久" : lifeTime.ToString();
    }

    private static void EnsureFolder(string folder)
    {
        string normalized = folder.Replace("\\", "/");
        if (AssetDatabase.IsValidFolder(normalized))
        {
            return;
        }

        string[] parts = normalized.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private void AddInfo(string message)
    {
        _lastReport.Add(message);
        Debug.Log($"[BulletQuickCreate] {message}");
    }

    private void AddError(string message)
    {
        _lastReport.Add($"错误：{message}");
        Debug.LogError($"[BulletQuickCreate] {message}");
    }

    private sealed class BulletCreatePaths
    {
        public string BulletFolder;
        public string ViewPrefab;

        public static BulletCreatePaths Create(int bulletId)
        {
            string id = bulletId.ToString();
            return new BulletCreatePaths
            {
                BulletFolder = $"Assets/Prefabs/Battle/Bullet/{id}",
                ViewPrefab = $"Assets/Prefabs/Battle/Bullet/{id}/{id}View.prefab",
            };
        }
    }
}
