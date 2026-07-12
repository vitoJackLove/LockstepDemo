using System.Collections.Generic;
using Rogue;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Play Mode 场景碰撞体调试窗口：实时调整地板、空气墙及额外碰撞体的位置与尺寸，并支持动态添加临时碰撞核。
/// </summary>
public sealed class SceneEnvironmentDebugWindow : OdinEditorWindow
{
    private const string MenuPath = "Tools/调试/场景碰撞体调试";

    [MenuItem(MenuPath)]
    public static void OpenWindow()
    {
        SceneEnvironmentDebugWindow window = GetWindow<SceneEnvironmentDebugWindow>("场景碰撞体调试");
        window.minSize = new Vector2(420f, 620f);
        window.Show();
    }

    [Title("运行状态")]
    [ShowInInspector, ReadOnly, LabelText("状态")]
    private string _statusMessage = "请先进入 Play Mode";

    [ShowInInspector, ReadOnly, LabelText("场景名")]
    private string _sceneName = "-";

    [ShowInInspector, ReadOnly, LabelText("地板顶面 Y")]
    private float _floorTopY;

    [ShowInInspector, ReadOnly, LabelText("碰撞体数量")]
    private int _pieceCount;

    [Title("竞技场模板（批量调整地板 + 四面墙）")]
    [InlineProperty, HideLabel]
    [SerializeField]
    private SceneArenaTemplate _arenaTemplate = new SceneArenaTemplate();

    [Title("动态添加碰撞核")]
    [LabelText("形状")]
    [SerializeField]
    private PhysicsShapeType _addShape = PhysicsShapeType.Box;

    [LabelText("碰撞层")]
    [SerializeField]
    private FPCollisionLayer _addLayer = FPCollisionLayer.Wall;

    [LabelText("Trigger")]
    [SerializeField]
    private bool _addIsTrigger;

    [LabelText("世界位置")]
    [SerializeField]
    private Vector3 _addPosition = new Vector3(0f, 1f, 0f);

    [LabelText("欧拉角")]
    [SerializeField]
    private Vector3 _addEuler;

    [LabelText("盒体半尺寸")]
    [ShowIf(nameof(IsAddShapeBox))]
    [SerializeField]
    private Vector3 _addHalfExtents = Vector3.one * 0.5f;

    [LabelText("球体半径")]
    [ShowIf(nameof(IsAddShapeSphere))]
    [SerializeField]
    private float _addRadius = 0.5f;

    [LabelText("胶囊半径")]
    [ShowIf(nameof(IsAddShapeCapsule))]
    [SerializeField]
    private float _addCapsuleRadius = 0.4f;

    [LabelText("胶囊高度")]
    [ShowIf(nameof(IsAddShapeCapsule))]
    [SerializeField]
    private float _addCapsuleHeight = 1.8f;

    [Title("动态添加移动平台")]
    [LabelText("世界位置")]
    [SerializeField]
    private Vector3 _platformPosition = new Vector3(0f, 1f, 0f);

    [LabelText("盒体半尺寸")]
    [SerializeField]
    private Vector3 _platformHalfExtents = new Vector3(2f, 0.15f, 1f);

    [LabelText("运动方向")]
    [SerializeField]
    private Vector3 _platformMoveDirection = Vector3.right;

    [LabelText("运动速度")]
    [SerializeField]
    private float _platformMoveSpeed = 2f;

    [LabelText("往复距离")]
    [SerializeField]
    private float _platformTravelDistance = 5f;

    [Title("单个碰撞体")]
    [ValueDropdown(nameof(GetPieceDropdown))]
    [LabelText("选中碰撞体")]
    [OnValueChanged(nameof(OnSelectedPieceChanged))]
    [SerializeField]
    private int _selectedPieceIndex;

    [ShowInInspector, ReadOnly, LabelText("形状")]
    private PhysicsShapeType _pieceShape = PhysicsShapeType.Box;

    [LabelText("碰撞层")]
    [SerializeField]
    private FPCollisionLayer _pieceLayer = FPCollisionLayer.Wall;

    [LabelText("Trigger")]
    [SerializeField]
    private bool _pieceIsTrigger;

    [LabelText("世界位置")]
    [SerializeField]
    private Vector3 _piecePosition;

    [LabelText("欧拉角")]
    [SerializeField]
    private Vector3 _pieceEuler;

    [LabelText("盒体半尺寸")]
    [ShowIf(nameof(IsPieceShapeBox))]
    [SerializeField]
    private Vector3 _pieceHalfExtents = Vector3.one * 0.5f;

    [LabelText("球体半径")]
    [ShowIf(nameof(IsPieceShapeSphere))]
    [SerializeField]
    private float _pieceRadius = 0.5f;

    [LabelText("胶囊半径")]
    [ShowIf(nameof(IsPieceShapeCapsule))]
    [SerializeField]
    private float _pieceCapsuleRadius = 0.4f;

    [LabelText("胶囊高度")]
    [ShowIf(nameof(IsPieceShapeCapsule))]
    [SerializeField]
    private float _pieceCapsuleHeight = 1.8f;

    [ShowInInspector, ReadOnly, LabelText("完整尺寸")]
    [ShowIf(nameof(IsPieceShapeBox))]
    private Vector3 PieceFullSize => _pieceHalfExtents * 2f;

    [ShowInInspector, ReadOnly, LabelText("类型")]
    private string _selectedPieceType = "-";

    [LabelText("运动方向")]
    [ShowIf(nameof(IsSelectedDebugPlatform))]
    [SerializeField]
    private Vector3 _pieceMoveDirection = Vector3.right;

    [LabelText("运动速度")]
    [ShowIf(nameof(IsSelectedDebugPlatform))]
    [SerializeField]
    private float _pieceMoveSpeed = 2f;

    [LabelText("往复距离")]
    [ShowIf(nameof(IsSelectedDebugPlatform))]
    [SerializeField]
    private float _pieceTravelDistance = 5f;

    private bool _selectedIsDebugPlatform;

    private readonly List<string> _pieceLabels = new List<string>();

    private bool IsAddShapeBox() => _addShape == PhysicsShapeType.Box;
    private bool IsAddShapeSphere() => _addShape == PhysicsShapeType.Sphere;
    private bool IsAddShapeCapsule() => _addShape == PhysicsShapeType.Capsule;
    private bool IsPieceShapeBox() => _pieceShape == PhysicsShapeType.Box;
    private bool IsPieceShapeSphere() => _pieceShape == PhysicsShapeType.Sphere;
    private bool IsPieceShapeCapsule() => _pieceShape == PhysicsShapeType.Capsule;
    private bool IsSelectedDebugPlatform() => _selectedIsDebugPlatform;

    protected override void OnEnable()
    {
        base.OnEnable();
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += OnEditorUpdate;
        RefreshFromWorld(forceLoadTemplate: true);
    }

    protected override void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
        base.OnDisable();
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
        {
            RefreshFromWorld(forceLoadTemplate: state == PlayModeStateChange.EnteredPlayMode);
        }
    }

    private void OnEditorUpdate()
    {
        if (!EditorApplication.isPlaying)
        {
            return;
        }

        Repaint();
    }

    [Button("刷新", ButtonSizes.Medium)]
    private void RefreshButton()
    {
        RefreshFromWorld(forceLoadTemplate: false);
    }

    [Button("添加碰撞体", ButtonSizes.Large), GUIColor(0.45f, 0.85f, 0.55f)]
    private async void AddDebugPieceButton()
    {
        if (!TryGetSceneEnvironmentSystem(out SceneEnvironmentSystem sceneEnvironment, out string message, requirePieces: false))
        {
            _statusMessage = message;
            return;
        }

        SceneColliderShapeParams shapeParams = BuildAddShapeParams();
        int newIndex = await sceneEnvironment.AddDebugPieceAsync(shapeParams);
        if (newIndex < 0)
        {
            _statusMessage = "添加失败：无法创建碰撞实体。";
            return;
        }

        RefreshFromWorld(forceLoadTemplate: false);
        _selectedPieceIndex = newIndex;
        ReadSelectedPieceFromWorld(sceneEnvironment);
        _statusMessage = $"已添加临时碰撞体：{GetSelectedPieceLabel(sceneEnvironment)}";
    }

    [Button("添加移动平台", ButtonSizes.Large), GUIColor(0.55f, 0.85f, 0.75f)]
    private async void AddDebugPlatformButton()
    {
        if (!TryGetSceneEnvironmentSystem(out SceneEnvironmentSystem sceneEnvironment, out string message, requirePieces: false))
        {
            _statusMessage = message;
            return;
        }

        int newIndex = await sceneEnvironment.AddDebugPlatformAsync(BuildPlatformCreateData());
        if (newIndex < 0)
        {
            _statusMessage = "添加失败：无法创建移动平台实体。";
            return;
        }

        RefreshFromWorld(forceLoadTemplate: false);
        _selectedPieceIndex = newIndex;
        ReadSelectedPieceFromWorld(sceneEnvironment);
        _statusMessage = $"已添加移动平台：{GetSelectedPieceLabel(sceneEnvironment)}";
    }

    [Button("应用竞技场模板", ButtonSizes.Large), GUIColor(0.45f, 0.85f, 0.55f)]
    private void ApplyArenaTemplateButton()
    {
        if (!TryGetSceneEnvironmentSystem(out SceneEnvironmentSystem sceneEnvironment, out string message))
        {
            _statusMessage = message;
            return;
        }

        if (!sceneEnvironment.ApplyArenaTemplate(_arenaTemplate))
        {
            _statusMessage = "应用失败：未找到竞技场碰撞体，请确认场景已生成 arena_* 块。";
            return;
        }

        _floorTopY = (float)sceneEnvironment.FloorTopY;
        _statusMessage = "竞技场模板已应用到场景碰撞体。";
        ReadSelectedPieceFromWorld(sceneEnvironment);
    }

    [Button("从场景读取选中碰撞体", ButtonSizes.Medium)]
    private void ReadSelectedPieceButton()
    {
        if (!TryGetSceneEnvironmentSystem(out SceneEnvironmentSystem sceneEnvironment, out string message))
        {
            _statusMessage = message;
            return;
        }

        ReadSelectedPieceFromWorld(sceneEnvironment);
        _statusMessage = "已读取选中碰撞体当前参数。";
    }

    [Button("应用选中碰撞体", ButtonSizes.Large), GUIColor(0.55f, 0.75f, 1f)]
    private void ApplySelectedPieceButton()
    {
        if (!TryGetSceneEnvironmentSystem(out SceneEnvironmentSystem sceneEnvironment, out string message))
        {
            _statusMessage = message;
            return;
        }

        if (!sceneEnvironment.ApplyPiece(_selectedPieceIndex, BuildEditShapeParams()))
        {
            _statusMessage = "应用失败：索引无效或实体缺少物理组件。";
            return;
        }

        if (_selectedIsDebugPlatform)
        {
            sceneEnvironment.ApplyDebugPlatformMotion(_selectedPieceIndex, BuildEditPlatformMotionData());
        }

        _statusMessage = $"已应用碰撞体：{GetSelectedPieceLabel(sceneEnvironment)}";
    }

    [Button("从配置加载竞技场模板", ButtonSizes.Medium)]
    private void LoadArenaTemplateFromConfigButton()
    {
        if (!TryGetCurrentWorld(out BaseWorld world, out string message))
        {
            _statusMessage = message;
            return;
        }

        if (!TryLoadArenaTemplateFromConfig(world.SceneName, out SceneArenaTemplate template, out string loadMessage))
        {
            _statusMessage = loadMessage;
            return;
        }

        _arenaTemplate = template;
        _statusMessage = "已从 SceneAssets 配置加载竞技场模板。";
    }

    private void RefreshFromWorld(bool forceLoadTemplate)
    {
        if (!EditorApplication.isPlaying)
        {
            _statusMessage = "请先进入 Play Mode";
            _sceneName = "-";
            _floorTopY = 0f;
            _pieceCount = 0;
            _pieceLabels.Clear();
            return;
        }

        if (!TryGetCurrentWorld(out BaseWorld world, out string worldMessage))
        {
            _statusMessage = worldMessage;
            return;
        }

        SceneEnvironmentSystem sceneEnvironment = world.GetSystem<SceneEnvironmentSystem>();
        if (sceneEnvironment == null)
        {
            _statusMessage = "当前世界未注册 SceneEnvironmentSystem";
            return;
        }

        _sceneName = world.SceneName;
        _floorTopY = (float)sceneEnvironment.FloorTopY;
        IReadOnlyList<SceneEnvironmentPieceHandle> pieces = sceneEnvironment.Pieces;
        _pieceCount = pieces.Count;

        _pieceLabels.Clear();
        for (int i = 0; i < pieces.Count; i++)
        {
            _pieceLabels.Add($"{i}: {pieces[i].Key}");
        }

        if (_selectedPieceIndex >= pieces.Count)
        {
            _selectedPieceIndex = Mathf.Max(0, pieces.Count - 1);
        }

        if (forceLoadTemplate)
        {
            TryLoadArenaTemplateFromConfig(world.SceneName, out SceneArenaTemplate template, out _);
            if (template != null)
            {
                _arenaTemplate = template;
            }
        }

        if (pieces.Count > 0)
        {
            ReadSelectedPieceFromWorld(sceneEnvironment);
        }

        _statusMessage = $"已连接运行世界，共 {_pieceCount} 个碰撞体。";
    }

    private void ReadSelectedPieceFromWorld(SceneEnvironmentSystem sceneEnvironment)
    {
        IReadOnlyList<SceneEnvironmentPieceHandle> pieces = sceneEnvironment.Pieces;
        if (pieces.Count == 0)
        {
            return;
        }

        int index = Mathf.Clamp(_selectedPieceIndex, 0, pieces.Count - 1);
        _selectedPieceIndex = index;
        _selectedIsDebugPlatform = pieces[index].IsDebugPlatform;
        _selectedPieceType = _selectedIsDebugPlatform ? "移动平台" : "静态碰撞体";

        PhysicsBodyComponent physics = pieces[index].Entity?.GetComponent<PhysicsBodyComponent>();
        if (physics != null && physics.TryReadStaticShape(out SceneColliderShapeParams shapeParams))
        {
            _pieceShape = shapeParams.shape;
            _pieceLayer = shapeParams.layer;
            _pieceIsTrigger = shapeParams.isTrigger;
            _piecePosition = shapeParams.position;
            _pieceEuler = shapeParams.eulerAngles;
            _pieceHalfExtents = shapeParams.halfExtents;
            _pieceRadius = shapeParams.radius;
            _pieceCapsuleRadius = shapeParams.capsuleRadius;
            _pieceCapsuleHeight = shapeParams.capsuleHeight;
        }

        if (_selectedIsDebugPlatform)
        {
            SceneDebugPlatformMotionComponent motion =
                pieces[index].Entity?.GetComponent<SceneDebugPlatformMotionComponent>();
            if (motion != null && motion.TryReadMotionData(out SceneDebugPlatformMotionData motionData))
            {
                _pieceMoveDirection = motionData.moveDirection;
                _pieceMoveSpeed = motionData.moveSpeed;
                _pieceTravelDistance = motionData.travelDistance;
            }
        }
    }

    private void OnSelectedPieceChanged()
    {
        if (!TryGetSceneEnvironmentSystem(out SceneEnvironmentSystem sceneEnvironment, out _, requirePieces: false))
        {
            return;
        }

        ReadSelectedPieceFromWorld(sceneEnvironment);
    }

    private SceneColliderShapeParams BuildAddShapeParams()
    {
        SceneColliderShapeParams shapeParams = SceneColliderShapeParams.CreateDefault(_addShape);
        shapeParams.position = _addPosition;
        shapeParams.eulerAngles = _addEuler;
        shapeParams.layer = _addLayer;
        shapeParams.isTrigger = _addIsTrigger;
        shapeParams.halfExtents = _addHalfExtents;
        shapeParams.radius = _addRadius;
        shapeParams.capsuleRadius = _addCapsuleRadius;
        shapeParams.capsuleHeight = _addCapsuleHeight;
        return shapeParams;
    }

    private SceneDebugPlatformCreateData BuildPlatformCreateData()
    {
        SceneColliderShapeParams shapeParams = SceneColliderShapeParams.CreateDefault(PhysicsShapeType.Box);
        shapeParams.position = _platformPosition;
        shapeParams.halfExtents = _platformHalfExtents;
        shapeParams.layer = FPCollisionLayer.KinematicPlatform;

        return new SceneDebugPlatformCreateData
        {
            shapeParams = shapeParams,
            moveDirection = _platformMoveDirection,
            moveSpeed = _platformMoveSpeed,
            travelDistance = _platformTravelDistance,
        };
    }

    private SceneDebugPlatformMotionData BuildEditPlatformMotionData()
    {
        return new SceneDebugPlatformMotionData
        {
            moveDirection = _pieceMoveDirection,
            moveSpeed = _pieceMoveSpeed,
            travelDistance = _pieceTravelDistance,
        };
    }

    private SceneColliderShapeParams BuildEditShapeParams()
    {
        return new SceneColliderShapeParams
        {
            shape = _pieceShape,
            position = _piecePosition,
            eulerAngles = _pieceEuler,
            layer = _pieceLayer,
            isTrigger = _pieceIsTrigger,
            halfExtents = _pieceHalfExtents,
            radius = _pieceRadius,
            capsuleRadius = _pieceCapsuleRadius,
            capsuleHeight = _pieceCapsuleHeight,
            directionAxis = 1,
        };
    }

    private IEnumerable<ValueDropdownItem<int>> GetPieceDropdown()
    {
        if (_pieceLabels.Count == 0)
        {
            yield return new ValueDropdownItem<int>("(无碰撞体)", 0);
            yield break;
        }

        for (int i = 0; i < _pieceLabels.Count; i++)
        {
            yield return new ValueDropdownItem<int>(_pieceLabels[i], i);
        }
    }

    private string GetSelectedPieceLabel(SceneEnvironmentSystem sceneEnvironment)
    {
        IReadOnlyList<SceneEnvironmentPieceHandle> pieces = sceneEnvironment.Pieces;
        if (_selectedPieceIndex < 0 || _selectedPieceIndex >= pieces.Count)
        {
            return "(无效)";
        }

        return pieces[_selectedPieceIndex].Key;
    }

    private static bool TryGetCurrentWorld(out BaseWorld world, out string unavailableMessage)
    {
        world = null;
        unavailableMessage = null;

        if (!EditorApplication.isPlaying)
        {
            unavailableMessage = "请先进入 Play Mode";
            return false;
        }

        WorldSystem worldSystem = WorldSystem.Instance;
        world = worldSystem?.CurrentRunWorld;
        if (world == null)
        {
            unavailableMessage = "Play Mode 中暂无运行世界";
            return false;
        }

        return true;
    }

    private static bool TryGetSceneEnvironmentSystem(
        out SceneEnvironmentSystem sceneEnvironment,
        out string message,
        bool requirePieces = true)
    {
        sceneEnvironment = null;
        if (!TryGetCurrentWorld(out BaseWorld world, out message))
        {
            return false;
        }

        sceneEnvironment = world.GetSystem<SceneEnvironmentSystem>();
        if (sceneEnvironment == null)
        {
            message = "当前世界未注册 SceneEnvironmentSystem";
            return false;
        }

        if (requirePieces && sceneEnvironment.Pieces.Count == 0)
        {
            message = "当前场景尚未生成碰撞体";
            return false;
        }

        message = null;
        return true;
    }

    private static bool TryLoadArenaTemplateFromConfig(
        string sceneName,
        out SceneArenaTemplate template,
        out string message)
    {
        template = null;
        message = null;

        if (string.IsNullOrEmpty(sceneName))
        {
            message = "场景名为空，无法加载 SceneAssets 配置";
            return false;
        }

        List<SceneAssetsConfig> configs = GameEntry.DataTable.GetAllDataTable<SceneAssetsConfig>();
        if (configs == null || configs.Count == 0)
        {
            message = "SceneAssets 配置表为空";
            return false;
        }

        for (int i = 0; i < configs.Count; i++)
        {
            SceneAssetsConfig config = configs[i];
            if (config?.arenaTemplate == null)
            {
                continue;
            }

            if (!string.Equals(config.sceneName, sceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            template = CopyArenaTemplate(config.arenaTemplate);
            return true;
        }

        message = $"未找到 sceneName={sceneName} 的 SceneAssets 配置";
        return false;
    }

    private static SceneArenaTemplate CopyArenaTemplate(SceneArenaTemplate source)
    {
        return new SceneArenaTemplate
        {
            enabled = source.enabled,
            center = source.center,
            width = source.width,
            depth = source.depth,
            wallHeight = source.wallHeight,
            floorThickness = source.floorThickness,
            wallThickness = source.wallThickness,
            floorViewPrefabPath = source.floorViewPrefabPath,
            wallViewPrefabPath = source.wallViewPrefabPath,
        };
    }
}
