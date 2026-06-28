using System;
using System.Collections.Generic;
using System.IO;
using Animancer;
using TheKiwiCoder;
using UnityEditor;
using UnityEngine;

public class MonsterQuickCreateWindow : EditorWindow
{
    private const string MonsterAssetsPath = "Assets/GameAssetConfig/MonsterAssets.asset";
    private const string DefaultBlendTreeValueOnePath = "Assets/Prefabs/Battle/BlendTreePam/MoveSpeed.asset";
    private const string DefaultBlendTreeValueTwoPath = "Assets/Prefabs/Battle/BlendTreePam/Rotate.asset";

    private int _monsterId = 2002;
    private string _monsterName = "新怪物";
    private GameObject _modelOrPrefab;
    private Sprite _monsterIcon;
    private bool _syncAddressables = true;

    private int _hp = 100;
    private int _attackDistance = 2;
    private int _initActionNumber = 1;
    private int _speed = 5;
    private int _attack = 10;
    private int _defence = 1;
    private CampEnum _campEnum = CampEnum.MonsterCamp;

    private PrimitiveEnum _colliderShape = PrimitiveEnum.CapsulePrimitive;
    private Vector3 _colliderOffset = new Vector3(0f, 1f, 0f);
    private float _colliderRadius = 0.5f;
    private float _colliderHeight = 2f;
    private Vector3 _boxSize = Vector3.one;

    private BlendTreeType _blendTreeType = BlendTreeType.Mixer2D;
    private MixerTransition2D.MixerType _mixer2DType = MixerTransition2D.MixerType.Directional;
    private StringAsset _blendTreeValueOne;
    private StringAsset _blendTreeValueTwo;
    private float _blendTreeSmoothTime = 0.15f;

    private Vector2 _scrollPosition;
    private PreviewRenderUtility _previewUtility;
    private GameObject _previewInstance;
    private GameObject _previewedModelOrPrefab;
    private Bounds _previewBounds;
    private bool _hasPreviewBounds;
    private float _previewZoom = 1f;
    private Vector2 _previewEuler = new Vector2(20f, -35f);
    private readonly List<string> _lastReport = new List<string>();

    [MenuItem("Tools/怪物工厂/快速创建怪物基础数据")]
    public static void OpenWindow()
    {
        MonsterQuickCreateWindow window = GetWindow<MonsterQuickCreateWindow>();
        window.titleContent = new GUIContent("怪物基础数据创建");
        window.minSize = new Vector2(760f, 680f);
        window.Show();
    }

    private void OnGUI()
    {
        EnsureDefaultBlendTreeParameters();

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        DrawBasicSection();
        DrawStatsSection();
        DrawColliderSection();
        DrawBlendTreeSection();
        DrawPreviewSection();
        DrawActions();
        DrawReport();
        EditorGUILayout.EndScrollView();
    }

    private void OnDisable()
    {
        ReleaseModelPreview();
    }

    private void EnsureDefaultBlendTreeParameters()
    {
        if (_blendTreeValueOne == null)
        {
            _blendTreeValueOne = AssetDatabase.LoadAssetAtPath<StringAsset>(DefaultBlendTreeValueOnePath);
        }

        if (_blendTreeValueTwo == null)
        {
            _blendTreeValueTwo = AssetDatabase.LoadAssetAtPath<StringAsset>(DefaultBlendTreeValueTwoPath);
        }
    }

    private void DrawBasicSection()
    {
        EditorGUILayout.LabelField("基础信息", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            _monsterId = EditorGUILayout.IntField("怪物ID", _monsterId);
            _monsterName = EditorGUILayout.TextField("怪物名字", _monsterName);
            EditorGUI.BeginChangeCheck();
            _modelOrPrefab = (GameObject)EditorGUILayout.ObjectField("模型或Prefab", _modelOrPrefab, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck())
            {
                ReleaseModelPreview();
            }

            DrawModelPreview();
            _monsterIcon = (Sprite)EditorGUILayout.ObjectField("怪物头像", _monsterIcon, typeof(Sprite), false);
            _syncAddressables = EditorGUILayout.Toggle("生成后同步Addressables", _syncAddressables);
        }
    }

    private void DrawModelPreview()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("模型预览", EditorStyles.miniBoldLabel, GUILayout.Width(64f));
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
            GUILayout.Height(260f),
            GUILayout.ExpandWidth(true));

        GUI.Box(previewRect, GUIContent.none);
        HandlePreviewInput(previewRect);

        if (_modelOrPrefab == null)
        {
            DrawCenteredText(previewRect, "选择模型或Prefab后显示预览");
            return;
        }

        EnsureModelPreview();

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

        Texture2D previewTexture = AssetPreview.GetAssetPreview(_modelOrPrefab);
        if (previewTexture == null)
        {
            previewTexture = AssetPreview.GetMiniThumbnail(_modelOrPrefab);
        }

        if (previewTexture == null)
        {
            DrawCenteredText(previewRect, "当前资源没有可用预览");
            return;
        }

        Rect textureRect = ScaleToFit(previewTexture, previewRect);
        GUI.DrawTexture(textureRect, previewTexture, ScaleMode.ScaleToFit, true);
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

    private void EnsureModelPreview()
    {
        if (_previewUtility != null && _previewInstance != null && _previewedModelOrPrefab == _modelOrPrefab)
        {
            return;
        }

        ReleaseModelPreview();

        if (_modelOrPrefab == null)
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

        _previewedModelOrPrefab = _modelOrPrefab;
        _previewInstance = Instantiate(_modelOrPrefab);
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

        _previewedModelOrPrefab = null;
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
        Handles.color = new Color(1f, 0.15f, 0.1f, 0.95f);

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
        GUI.Label(labelRect, "左键拖拽旋转，滚轮或滑条缩放；红色线框为当前受击盒", EditorStyles.whiteMiniLabel);
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
        for (int i = 1; i < renderers.Length; i++)
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

    private void DrawStatsSection()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("默认属性", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            _hp = EditorGUILayout.IntField("血量", _hp);
            _attackDistance = EditorGUILayout.IntField("攻击距离", _attackDistance);
            _initActionNumber = EditorGUILayout.IntField("初始行动次数", _initActionNumber);
            _speed = EditorGUILayout.IntField("速度", _speed);
            _attack = EditorGUILayout.IntField("攻击力", _attack);
            _defence = EditorGUILayout.IntField("防御力", _defence);
            _campEnum = (CampEnum)EditorGUILayout.EnumPopup("阵营", _campEnum);
        }
    }

    private void DrawColliderSection()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("默认受击盒", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            _colliderShape = (PrimitiveEnum)EditorGUILayout.EnumPopup("形状", _colliderShape);
            _colliderOffset = EditorGUILayout.Vector3Field("中心偏移", _colliderOffset);

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

    private void DrawBlendTreeSection()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("动画混合树", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            _blendTreeType = (BlendTreeType)EditorGUILayout.EnumPopup("混合树类型", _blendTreeType);
            _blendTreeValueOne = (StringAsset)EditorGUILayout.ObjectField("变量1", _blendTreeValueOne, typeof(StringAsset), false);

            if (_blendTreeType == BlendTreeType.Mixer2D)
            {
                _blendTreeValueTwo = (StringAsset)EditorGUILayout.ObjectField("变量2", _blendTreeValueTwo, typeof(StringAsset), false);
                _mixer2DType = (MixerTransition2D.MixerType)EditorGUILayout.EnumPopup("2D混合类型", _mixer2DType);
                _blendTreeSmoothTime = Mathf.Max(0f, EditorGUILayout.FloatField("变量平滑过度", _blendTreeSmoothTime));
            }

            EditorGUILayout.HelpBox("生成空的 Animancer 混合树资产，动画片段可在生成后到该资产 Inspector 中配置。", MessageType.Info);
        }
    }

    private void DrawPreviewSection()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("将创建", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            string id = _monsterId.ToString();
            EditorGUILayout.LabelField("Monster", $"Assets/Prefabs/Battle/Monster/{id}/{id}View.prefab");
            EditorGUILayout.LabelField("Skill", $"Assets/Prefabs/Battle/Skill/{id}/{_monsterId * 100 + 1}.prefab");
            EditorGUILayout.LabelField("State", $"Assets/Prefabs/Battle/State/{id}/{id}Hit.prefab");
            EditorGUILayout.LabelField("Tree", $"Assets/Prefabs/Battle/Tree/{id}/{id}.prefab");
            EditorGUILayout.LabelField("BlendTree", $"Assets/Prefabs/Battle/Monster/{id}/BlendTree/{id}BlendTree.asset");
        }
    }

    private void DrawActions()
    {
        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("创建怪物基础数据", GUILayout.Height(34f)))
            {
                CreateMonster();
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

    private void CreateMonster()
    {
        _lastReport.Clear();

        try
        {
            if (!ValidateInputs(out string error))
            {
                AddError(error);
                return;
            }

            MonsterCreatePaths paths = MonsterCreatePaths.Create(_monsterId);
            if (!ValidateTargets(paths, out error))
            {
                AddError(error);
                return;
            }

            EnsureFolder(paths.MonsterFolder);
            EnsureFolder(paths.BlendTreeFolder);
            EnsureFolder(paths.SkillFolder);
            EnsureFolder(paths.StateFolder);
            EnsureFolder(paths.TreeFolder);

            SkillLineAsset[] skillAssets = CreateSkillAssets(paths);
            SkillLineAsset stateAsset = CreateStateAsset(paths);
            BehaviourTree treeAsset = CreateTreeAsset(paths);
            TransitionAssetBase blendTreeAsset = CreateBlendTreeAsset(paths);

            AddInfo("View prefab 将直接引用选择的模型或Prefab。");
            CreateViewPrefab(paths, _modelOrPrefab);
            CreateLogicPrefab(paths);
            CreateSkillPrefabs(paths, skillAssets);
            CreateStatePrefab(paths, stateAsset);
            CreateTreePrefab(paths, treeAsset);
            RegisterMonsterConfig(paths, blendTreeAsset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (_syncAddressables)
            {
                RuntimeAddressablesConfigurator.SyncRuntimeAssets();
                AddInfo("已同步 Runtime Addressables。");
            }

            AddInfo($"创建完成：怪物 {_monsterId} / {_monsterName}");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(paths.ViewPrefab);
            EditorGUIUtility.PingObject(Selection.activeObject);
        }
        catch (Exception exception)
        {
            AddError(exception.ToString());
        }
    }

    private bool ValidateInputs(out string error)
    {
        if (_monsterId <= 0)
        {
            error = "怪物ID必须大于0。";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_monsterName))
        {
            error = "怪物名字不能为空。";
            return false;
        }

        if (_modelOrPrefab == null)
        {
            error = "必须选择模型或Prefab。";
            return false;
        }

        if (AssetDatabase.LoadAssetAtPath<MonsterAssets>(MonsterAssetsPath) == null)
        {
            error = $"找不到 MonsterAssets 配置：{MonsterAssetsPath}";
            return false;
        }

        if (_blendTreeValueOne == null)
        {
            error = "动画混合树变量1不能为空。";
            return false;
        }

        if (_blendTreeType == BlendTreeType.Mixer2D && _blendTreeValueTwo == null)
        {
            error = "2D动画混合树变量2不能为空。";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private bool ValidateTargets(MonsterCreatePaths paths, out string error)
    {
        MonsterAssets monsterAssets = AssetDatabase.LoadAssetAtPath<MonsterAssets>(MonsterAssetsPath);
        if (monsterAssets.GetDataTable(_monsterId) != null)
        {
            error = $"MonsterAssets 已存在怪物ID：{_monsterId}";
            return false;
        }

        string[] folders =
        {
            paths.MonsterFolder,
            paths.SkillFolder,
            paths.StateFolder,
            paths.TreeFolder,
        };

        for (int i = 0; i < folders.Length; i++)
        {
            if (AssetDatabase.IsValidFolder(folders[i]) || Directory.Exists(folders[i]))
            {
                error = $"目标目录已存在，已阻止生成：{folders[i]}";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    private SkillLineAsset[] CreateSkillAssets(MonsterCreatePaths paths)
    {
        SkillLineAsset[] assets = new SkillLineAsset[2];
        for (int i = 0; i < assets.Length; i++)
        {
            int skillId = paths.SkillIds[i];
            string assetPath = $"{paths.SkillFolder}/{skillId}.asset";

            SkillLineAsset emptyAsset = ScriptableObject.CreateInstance<SkillLineAsset>();
            emptyAsset.duration = 60;
            emptyAsset.fps = 30;
            AssetDatabase.CreateAsset(emptyAsset, assetPath);

            assets[i] = AssetDatabase.LoadAssetAtPath<SkillLineAsset>(assetPath);
            assets[i].name = skillId.ToString();
            UnityEditor.EditorUtility.SetDirty(assets[i]);
            AddInfo($"已创建技能Timeline：{assetPath}");
        }

        return assets;
    }

    private SkillLineAsset CreateStateAsset(MonsterCreatePaths paths)
    {
        SkillLineAsset emptyAsset = ScriptableObject.CreateInstance<SkillLineAsset>();
        emptyAsset.duration = 30;
        emptyAsset.fps = 30;
        AssetDatabase.CreateAsset(emptyAsset, paths.StateAsset);

        SkillLineAsset stateAsset = AssetDatabase.LoadAssetAtPath<SkillLineAsset>(paths.StateAsset);
        stateAsset.name = $"{_monsterId}Hit";
        UnityEditor.EditorUtility.SetDirty(stateAsset);
        AddInfo($"已创建状态Timeline：{paths.StateAsset}");
        return stateAsset;
    }

    private BehaviourTree CreateTreeAsset(MonsterCreatePaths paths)
    {
        BehaviourTree emptyTree = ScriptableObject.CreateInstance<BehaviourTree>();
        emptyTree.name = _monsterId.ToString();
        AssetDatabase.CreateAsset(emptyTree, paths.TreeAsset);

        BehaviourTree treeAsset = AssetDatabase.LoadAssetAtPath<BehaviourTree>(paths.TreeAsset);
        treeAsset.name = _monsterId.ToString();
        UnityEditor.EditorUtility.SetDirty(treeAsset);
        AddInfo($"已创建行为树资产：{paths.TreeAsset}");
        return treeAsset;
    }

    private void CreateViewPrefab(MonsterCreatePaths paths, GameObject modelAsset)
    {
        GameObject root = new GameObject($"{_monsterId}View");
        try
        {
            Animator rootAnimator = root.AddComponent<Animator>();
            root.AddComponent<MonsterEntityView>();
            root.AddComponent<AnimancerComponent>();

            GameObject modelInstance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
            if (modelInstance == null)
            {
                modelInstance = Instantiate(modelAsset);
            }

            modelInstance.name = modelAsset.name;
            modelInstance.transform.SetParent(root.transform, false);

            Animator sourceAnimator = modelInstance.GetComponentInChildren<Animator>();
            if (sourceAnimator != null)
            {
                rootAnimator.avatar = sourceAnimator.avatar;
                rootAnimator.runtimeAnimatorController = sourceAnimator.runtimeAnimatorController;
                sourceAnimator.enabled = false;
            }

            PrefabUtility.SaveAsPrefabAsset(root, paths.ViewPrefab);
            AddInfo($"已创建View prefab：{paths.ViewPrefab}");
        }
        finally
        {
            DestroyImmediate(root);
        }
    }

    private void CreateLogicPrefab(MonsterCreatePaths paths)
    {
        GameObject root = new GameObject($"{_monsterId}Logic");
        try
        {
            root.AddComponent<MonsterEntityView>();
            PrefabUtility.SaveAsPrefabAsset(root, paths.LogicPrefab);
        }
        finally
        {
            DestroyImmediate(root);
        }

        AddInfo($"已创建Logic prefab：{paths.LogicPrefab}");
    }

    private void CreateSkillPrefabs(MonsterCreatePaths paths, SkillLineAsset[] skillAssets)
    {
        for (int i = 0; i < skillAssets.Length; i++)
        {
            int skillId = paths.SkillIds[i];
            string prefabPath = $"{paths.SkillFolder}/{skillId}.prefab";

            CreateSkillLauncherPrefab(prefabPath, skillId.ToString(), skillAssets[i]);

            AddInfo($"已创建技能prefab：{prefabPath}");
        }
    }

    private void CreateStatePrefab(MonsterCreatePaths paths, SkillLineAsset stateAsset)
    {
        CreateSkillLauncherPrefab(paths.StatePrefab, $"{_monsterId}Hit", stateAsset);

        AddInfo($"已创建状态prefab：{paths.StatePrefab}");
    }

    private void CreateTreePrefab(MonsterCreatePaths paths, BehaviourTree treeAsset)
    {
        GameObject root = new GameObject(_monsterId.ToString());
        try
        {
            BehaviourTreeInstance instance = root.AddComponent<BehaviourTreeInstance>();
            instance.behaviourTree = treeAsset;
            instance.validate = true;
            PrefabUtility.SaveAsPrefabAsset(root, paths.TreePrefab);
        }
        finally
        {
            DestroyImmediate(root);
        }

        AddInfo($"已创建行为树prefab：{paths.TreePrefab}");
    }

    private TransitionAssetBase CreateBlendTreeAsset(MonsterCreatePaths paths)
    {
        TransitionAsset asset = ScriptableObject.CreateInstance<TransitionAsset>();
        asset.name = $"{_monsterId}BlendTree";

        if (_blendTreeType == BlendTreeType.Mixer2D)
        {
            MixerTransition2D transition = new MixerTransition2D();
            transition.ParameterNameX = _blendTreeValueOne;
            transition.ParameterNameY = _blendTreeValueTwo;
            transition.Type = _mixer2DType;
            asset.Transition = transition;
        }
        else
        {
            LinearMixerTransition transition = new LinearMixerTransition();
            transition.ParameterName = _blendTreeValueOne;
            asset.Transition = transition;
        }

        AssetDatabase.CreateAsset(asset, paths.BlendTreeAsset);
        UnityEditor.EditorUtility.SetDirty(asset);
        AddInfo($"已创建动画混合树资产：{paths.BlendTreeAsset}");
        return asset;
    }

    private void RegisterMonsterConfig(MonsterCreatePaths paths, TransitionAssetBase blendTreeAsset)
    {
        MonsterAssets monsterAssets = AssetDatabase.LoadAssetAtPath<MonsterAssets>(MonsterAssetsPath);

        MonsterAssetsConfig config = new MonsterAssetsConfig
        {
            assetsId = _monsterId,
            assetsPath = $"Monster/{_monsterId}/{_monsterId}View",
            monsterIcon = _monsterIcon,
            monsterName = _monsterName,
            hp = _hp,
            attackDistance = _attackDistance,
            initActionNumber = _initActionNumber,
            speed = _speed,
            attack = _attack,
            defence = _defence,
            campEnum = _campEnum,
            colliderDataList = new List<HitColliderEditorSetting> { CreateColliderSetting() },
            monsterSkillList = new List<MonsterSkillConfig>(),
            stateList = new List<StateConfig>(),
            treeAssetsPath = $"Tree/{_monsterId}/{_monsterId}",
            blendTree = CreateBlendTreeConfig(blendTreeAsset),
        };

        for (int i = 0; i < paths.SkillIds.Length; i++)
        {
            config.monsterSkillList.Add(new MonsterSkillConfig
            {
                skillId = paths.SkillIds[i],
                skillAssetsPath = $"Skill/{_monsterId}/{paths.SkillIds[i]}",
            });
        }

        config.stateList.Add(new StateConfig
        {
            stateId = 2,
            assetsPath = $"State/{_monsterId}/{_monsterId}Hit",
        });

        config.stateList.Add(new StateConfig
        {
            stateId = 1,
            assetsPath = string.Empty,
        });

        monsterAssets.monsterAssetsConfigList.Add(config);
        UnityEditor.EditorUtility.SetDirty(monsterAssets);
        AddInfo($"已写入配置：{MonsterAssetsPath}");
    }

    private HitColliderEditorSetting CreateColliderSetting()
    {
        HitColliderEditorSetting setting = new HitColliderEditorSetting
        {
            key = "center",
            primitiveEnum = _colliderShape,
            offset = _colliderOffset,
            eulerOffset = Vector3.zero,
            weight = 1,
            damageMagnification = 1f,
            isShow = true,
            color = Color.red,
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

    private AnimatorBlendTree CreateBlendTreeConfig(TransitionAssetBase blendTreeAsset)
    {
        return new AnimatorBlendTree
        {
            blendTreeType = _blendTreeType,
            transitionAssetBase = blendTreeAsset,
            valueOne = _blendTreeValueOne,
            valueTwo = _blendTreeType == BlendTreeType.Mixer2D ? _blendTreeValueTwo : null,
            smoothTime = _blendTreeType == BlendTreeType.Mixer2D ? _blendTreeSmoothTime : 0f,
        };
    }

    private static void CreateSkillLauncherPrefab(string prefabPath, string rootName, SkillLineAsset asset)
    {
        GameObject root = new GameObject(rootName);
        try
        {
            SkillTimelineLauncher launcher = root.AddComponent<SkillTimelineLauncher>();
            launcher.graph = asset;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            DestroyImmediate(root);
        }
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
        Debug.Log($"[MonsterQuickCreate] {message}");
    }

    private void AddError(string message)
    {
        _lastReport.Add($"错误：{message}");
        Debug.LogError($"[MonsterQuickCreate] {message}");
    }

    private sealed class MonsterCreatePaths
    {
        public string MonsterFolder;
        public string SkillFolder;
        public string StateFolder;
        public string TreeFolder;
        public string BlendTreeFolder;
        public string ViewPrefab;
        public string LogicPrefab;
        public string BlendTreeAsset;
        public string StateAsset;
        public string StatePrefab;
        public string TreeAsset;
        public string TreePrefab;
        public int[] SkillIds;

        public static MonsterCreatePaths Create(int monsterId)
        {
            string id = monsterId.ToString();
            return new MonsterCreatePaths
            {
                MonsterFolder = $"Assets/Prefabs/Battle/Monster/{id}",
                SkillFolder = $"Assets/Prefabs/Battle/Skill/{id}",
                StateFolder = $"Assets/Prefabs/Battle/State/{id}",
                TreeFolder = $"Assets/Prefabs/Battle/Tree/{id}",
                BlendTreeFolder = $"Assets/Prefabs/Battle/Monster/{id}/BlendTree",
                ViewPrefab = $"Assets/Prefabs/Battle/Monster/{id}/{id}View.prefab",
                LogicPrefab = $"Assets/Prefabs/Battle/Monster/{id}/{id}Logic.prefab",
                BlendTreeAsset = $"Assets/Prefabs/Battle/Monster/{id}/BlendTree/{id}BlendTree.asset",
                StateAsset = $"Assets/Prefabs/Battle/State/{id}/{id}Hit.asset",
                StatePrefab = $"Assets/Prefabs/Battle/State/{id}/{id}Hit.prefab",
                TreeAsset = $"Assets/Prefabs/Battle/Tree/{id}/{id}.asset",
                TreePrefab = $"Assets/Prefabs/Battle/Tree/{id}/{id}.prefab",
                SkillIds = new[] { monsterId * 100 + 1, monsterId * 100 + 2 },
            };
        }
    }
}
