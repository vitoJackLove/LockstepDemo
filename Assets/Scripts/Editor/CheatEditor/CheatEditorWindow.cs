using System;
using System.Collections.Generic;
using System.Linq;
using Rogue;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Unity.Mathematics.FixedPoint;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 运行时金手指编辑器窗口，用于在 Play Mode 中浏览实体并修改选中实体属性。
/// </summary>
public class CheatEditorWindow : OdinEditorWindow
{
    /// <summary>
    /// 全部实体筛选项的显示文本。
    /// </summary>
    private const string AllFilterName = "全部";

    /// <summary>
    /// Agent 自动化读取作弊窗口快照时使用的日志前缀。
    /// </summary>
    private const string AgentSnapshotLogPrefix = "[CheatEditorAgent]";

    /// <summary>
    /// Agent 自动化验证 Buff 添加能力时使用的日志前缀。
    /// </summary>
    private const string AgentBuffSmokeLogPrefix = "[CheatEditorBuffSmoke]";

    /// <summary>
    /// Agent 自动化验证属性编辑能力时使用的日志前缀。
    /// </summary>
    private const string AgentPropertySmokeLogPrefix = "[CheatEditorPropertySmoke]";

    /// <summary>
    /// 编辑器创建子弹时写入 AgentTest 日志的稳定前缀。
    /// </summary>
    private const string AgentBulletCreateLogPrefix = "[CheatEditorBulletCreate]";

    /// <summary>
    /// Agent 自动化验证子弹创建能力时使用的日志前缀。
    /// </summary>
    private const string AgentBulletSmokeLogPrefix = "[CheatEditorBulletSmoke]";

    /// <summary>
    /// Agent 自动化验证完整金手指组合流程时使用的日志前缀。
    /// </summary>
    private const string AgentCombinedSmokeLogPrefix = "[CheatEditorCombinedSmoke]";

    /// <summary>
    /// Agent Buff 烟测等待运行世界创建的最长秒数。
    /// </summary>
    private const double AgentBuffSmokeTimeoutSeconds = 45d;

    /// <summary>
    /// Agent 属性烟测等待运行世界创建的最长秒数。
    /// </summary>
    private const double AgentPropertySmokeTimeoutSeconds = 45d;

    /// <summary>
    /// Agent 子弹烟测等待运行世界创建的最长秒数。
    /// </summary>
    private const double AgentBulletSmokeTimeoutSeconds = 45d;

    /// <summary>
    /// Agent 组合烟测等待运行世界创建的最长秒数。
    /// </summary>
    private const double AgentCombinedSmokeTimeoutSeconds = 60d;

    /// <summary>
    /// Agent Buff 烟测等待运行世界时的截止编辑器时间。
    /// </summary>
    private static double _agentBuffSmokeDeadlineTime;

    /// <summary>
    /// Agent 属性烟测等待运行世界时的截止编辑器时间。
    /// </summary>
    private static double _agentPropertySmokeDeadlineTime;

    /// <summary>
    /// Agent 子弹烟测等待运行世界时的截止编辑器时间。
    /// </summary>
    private static double _agentBulletSmokeDeadlineTime;

    /// <summary>
    /// Agent 组合烟测等待运行世界时的截止编辑器时间。
    /// </summary>
    private static double _agentCombinedSmokeDeadlineTime;

    /// <summary>
    /// Agent Buff 烟测是否正在等待 Play Mode 运行世界。
    /// </summary>
    private static bool _isAgentBuffSmokeWaiting;

    /// <summary>
    /// Agent 属性烟测是否正在等待 Play Mode 运行世界。
    /// </summary>
    private static bool _isAgentPropertySmokeWaiting;

    /// <summary>
    /// Agent 子弹烟测是否正在等待 Play Mode 运行世界。
    /// </summary>
    private static bool _isAgentBulletSmokeWaiting;

    /// <summary>
    /// Agent 组合烟测是否正在等待 Play Mode 运行世界。
    /// </summary>
    private static bool _isAgentCombinedSmokeWaiting;

    /// <summary>
    /// Agent 组合烟测当前运行的递增序号，用于生成可查询的 runId。
    /// </summary>
    private static int _agentCombinedSmokeSequence;

    /// <summary>
    /// Agent 组合烟测当前运行 ID，用于区分旧日志和本轮执行结果。
    /// </summary>
    private static string _agentCombinedSmokeRunId;

    /// <summary>
    /// 编辑器会话内子弹创建指纹的递增序号，用于降低同帧重复点击产生相同指纹的概率。
    /// </summary>
    private static int _bulletCheatCreateSequence;

    /// <summary>
    /// 编辑器会话内已创建子弹使用过的指纹集合，用于重复创建时主动避开已知冲突。
    /// </summary>
    private static readonly HashSet<int> _createdBulletFingerprints = new HashSet<int>();

    /// <summary>
    /// 当前世界的运行状态提示。
    /// </summary>
    [TitleGroup("公共状态 - 世界状态")]
    [ShowInInspector, ReadOnly, LabelText("状态")]
    private string _worldState = "未刷新";

    /// <summary>
    /// 当前运行世界的本地预测帧。
    /// </summary>
    [TitleGroup("公共状态 - 世界状态")]
    [ShowInInspector, ReadOnly, LabelText("LocalTick")]
    private uint _localTick;

    /// <summary>
    /// 当前运行世界的权威帧。
    /// </summary>
    [TitleGroup("公共状态 - 世界状态")]
    [ShowInInspector, ReadOnly, LabelText("LocalTick (GGPO)")]
    private uint _authorityTick;

    /// <summary>
    /// 当前世界中执行实体数量。
    /// </summary>
    [TitleGroup("公共状态 - 世界状态")]
    [ShowInInspector, ReadOnly, LabelText("实体数量")]
    private int _entityCount;

    /// <summary>
    /// 是否在窗口 Update 中自动刷新实体和属性快照。
    /// </summary>
    [TitleGroup("公共状态 - 刷新")]
    [ToggleLeft, LabelText("自动刷新")]
    public bool AutoRefresh = true;

    /// <summary>
    /// 最近一次编辑器操作的结果提示。
    /// </summary>
    [TitleGroup("公共状态 - 刷新")]
    [ShowInInspector, ReadOnly, LabelText("操作结果")]
    private string _operationMessage = "等待操作";

    /// <summary>
    /// 实体类型调试过滤器，全部代表不过滤，仅用于展开完整实体列表时辅助定位。
    /// </summary>
    [TabGroup("功能页签", "筛选实体")]
    [FoldoutGroup("功能页签/筛选实体/调试实体筛选", false)]
    [ValueDropdown(nameof(GetEntityTypeFilterOptions))]
    [LabelText("实体类型")]
    [OnValueChanged(nameof(RefreshData))]
    public string EntityTypeFilter = AllFilterName;

    /// <summary>
    /// 实体更新域调试过滤器，全部代表不过滤，仅用于展开完整实体列表时辅助定位。
    /// </summary>
    [TabGroup("功能页签", "筛选实体")]
    [FoldoutGroup("功能页签/筛选实体/调试实体筛选", false)]
    [ValueDropdown(nameof(GetEntityUpdateTypeFilterOptions))]
    [LabelText("更新域")]
    [OnValueChanged(nameof(RefreshData))]
    public string EntityUpdateTypeFilter = AllFilterName;

    /// <summary>
    /// 配置 ID 调试过滤器，0 表示不过滤，仅用于展开完整实体列表时辅助定位。
    /// </summary>
    [TabGroup("功能页签", "筛选实体")]
    [FoldoutGroup("功能页签/筛选实体/调试实体筛选", false)]
    [LabelText("配置ID")]
    [OnValueChanged(nameof(RefreshData))]
    public int ConfigIdFilter;

    /// <summary>
    /// 实体 ID 调试过滤器，0 表示不过滤，仅用于展开完整实体列表时辅助定位。
    /// </summary>
    [TabGroup("功能页签", "筛选实体")]
    [FoldoutGroup("功能页签/筛选实体/调试实体筛选", false)]
    [LabelText("实体ID")]
    [OnValueChanged(nameof(RefreshData))]
    public int EntityIdFilter;

    /// <summary>
    /// 调试实体列表是否只显示仍然存活的实体。
    /// </summary>
    [TabGroup("功能页签", "筛选实体")]
    [FoldoutGroup("功能页签/筛选实体/调试实体筛选", false)]
    [ToggleLeft, LabelText("仅存活实体")]
    [OnValueChanged(nameof(RefreshData))]
    public bool OnlySurvivalEntity = true;

    /// <summary>
    /// 是否允许调试时保留非本地预测实体选择，默认关闭以避免 Buff 和属性操作落到错误对象。
    /// </summary>
    [TabGroup("功能页签", "筛选实体")]
    [FoldoutGroup("功能页签/筛选实体/调试实体筛选", false)]
    [ToggleLeft, LabelText("允许调试选择非本地预测实体")]
    [OnValueChanged(nameof(RefreshData))]
    public bool AllowDebugEntitySelection;

    /// <summary>
    /// 当前运行世界中自动解析出的本地预测实体摘要，用于让用户确认默认操作对象。
    /// </summary>
    [TabGroup("功能页签", "筛选实体")]
    [TitleGroup("功能页签/筛选实体/本地预测实体")]
    [ShowInInspector, ReadOnly, MultiLineProperty(6), HideLabel]
    private string _localPredictedEntityInfo = "未刷新";

    /// <summary>
    /// 当前本地预测实体解析状态，明确提示 Play Mode、世界或实体不可用的原因。
    /// </summary>
    [TabGroup("功能页签", "筛选实体")]
    [TitleGroup("功能页签/筛选实体/本地预测实体")]
    [ShowInInspector, ReadOnly, LabelText("解析状态")]
    private string _localPredictedEntityState = "未刷新";

    /// <summary>
    /// 当前筛选后的调试实体显示列表，默认折叠为辅助入口。
    /// </summary>
    [TabGroup("功能页签", "筛选实体")]
    [FoldoutGroup("功能页签/筛选实体/调试实体列表", false)]
    [ShowInInspector, ReadOnly, ListDrawerSettings(HideAddButton = true, HideRemoveButton = true, DraggableItems = false, DefaultExpandedState = true)]
    [LabelText("实体")]
    [NonSerialized]
    private List<EntityDebugViewData> _filteredEntities = new List<EntityDebugViewData>();

    /// <summary>
    /// 当前选中的实体上下文，默认由本地预测实体自动填充，调试时可从完整列表切换。
    /// </summary>
    [TabGroup("功能页签", "筛选实体")]
    [TitleGroup("功能页签/筛选实体/本地预测实体")]
    [ValueDropdown(nameof(GetEntitySelectionOptions))]
    [LabelText("当前实体上下文")]
    [OnValueChanged(nameof(OnSelectedEntityChanged))]
    [NonSerialized]
    public EntityDebugViewData SelectedEntityView;

    /// <summary>
    /// 选中实体的基础信息。
    /// </summary>
    [TabGroup("功能页签", "筛选实体")]
    [TitleGroup("功能页签/筛选实体/实体信息")]
    [ShowInInspector, ReadOnly, MultiLineProperty(8), HideLabel]
    private string _selectedEntityInfo = "未选择实体";

    /// <summary>
    /// 当前选中实体无法继续操作时的具体原因。
    /// </summary>
    private string _selectedEntityUnavailableMessage;

    /// <summary>
    /// Buff 页签展示的当前选中实体依赖状态，用于在缺少实体时说明不可操作原因。
    /// </summary>
    [TabGroup("功能页签", "增加 Buff")]
    [ShowInInspector, ReadOnly, MultiLineProperty(3), LabelText("选中状态")]
    private string _buffOperationState = "未选择实体";

    /// <summary>
    /// 属性页签展示的当前选中实体依赖状态，用于解释属性列表为空或不可编辑的原因。
    /// </summary>
    [TabGroup("功能页签", "修改属性")]
    [ShowInInspector, ReadOnly, MultiLineProperty(3), LabelText("选中状态")]
    private string _propertyOperationState = "未选择实体";

    /// <summary>
    /// 子弹页签展示的当前选中实体依赖状态，用于提示父实体来源和选中上下文是否可用。
    /// </summary>
    [TabGroup("功能页签", "添加子弹")]
    [ShowInInspector, ReadOnly, MultiLineProperty(3), LabelText("选中状态")]
    private string _bulletOperationState = "未选择实体";

    /// <summary>
    /// 选中实体的属性调试行。
    /// </summary>
    [TabGroup("功能页签", "修改属性")]
    [TitleGroup("功能页签/修改属性/属性修改")]
    [ShowInInspector, ListDrawerSettings(HideAddButton = true, HideRemoveButton = true, DraggableItems = false, DefaultExpandedState = true)]
    [LabelText("属性")]
    [NonSerialized]
    private List<EntityPropertyCheatRow> _propertyRows = new List<EntityPropertyCheatRow>();

    /// <summary>
    /// 准备应用到当前选中实体的 Buff 配置 ID。
    /// </summary>
    [TabGroup("功能页签", "增加 Buff")]
    [TitleGroup("功能页签/增加 Buff/Buff操作")]
    [LabelText("Buff配置ID")]
    public int BuffConfigId;

    /// <summary>
    /// 准备通过金手指创建的子弹配置 ID。
    /// </summary>
    [TabGroup("功能页签", "添加子弹")]
    [TitleGroup("功能页签/添加子弹/子弹创建")]
    [LabelText("子弹配置ID")]
    public int BulletConfigId;

    /// <summary>
    /// 子弹创建时使用的世界坐标。
    /// </summary>
    [TabGroup("功能页签", "添加子弹")]
    [TitleGroup("功能页签/添加子弹/子弹创建")]
    [LabelText("生成位置")]
    public Vector3 BulletSpawnPosition;

    /// <summary>
    /// 子弹创建时使用的欧拉角，通常 Y 轴朝向决定直线运动方向。
    /// </summary>
    [TabGroup("功能页签", "添加子弹")]
    [TitleGroup("功能页签/添加子弹/子弹创建")]
    [LabelText("生成旋转")]
    public Vector3 BulletSpawnEulerAngles;

    /// <summary>
    /// 子弹父实体来源，决定创建上下文、阵营和更新域。
    /// </summary>
    [TabGroup("功能页签", "添加子弹")]
    [TitleGroup("功能页签/添加子弹/子弹创建")]
    [LabelText("父实体来源")]
    public BulletOwnerSource BulletParentSource = BulletOwnerSource.SelectedEntity;

    /// <summary>
    /// 是否给子弹附加 MovementData，使其按运行时位移组件执行运动。
    /// </summary>
    [TabGroup("功能页签", "添加子弹")]
    [TitleGroup("功能页签/添加子弹/子弹创建/运动参数")]
    [ToggleLeft, LabelText("启用运动")]
    public bool BulletUseMovementData;

    /// <summary>
    /// 运动子弹持续移动的帧数。
    /// </summary>
    [TabGroup("功能页签", "添加子弹")]
    [TitleGroup("功能页签/添加子弹/子弹创建/运动参数")]
    [LabelText("运动帧数")]
    public int BulletMovementTime = 30;

    /// <summary>
    /// 运动子弹每帧使用的位移速度。
    /// </summary>
    [TabGroup("功能页签", "添加子弹")]
    [TitleGroup("功能页签/添加子弹/子弹创建/运动参数")]
    [LabelText("运动速度")]
    public float BulletMovementSpeed = 1f;

    /// <summary>
    /// 运动子弹使用的位移类型。
    /// </summary>
    [TabGroup("功能页签", "添加子弹")]
    [TitleGroup("功能页签/添加子弹/子弹创建/运动参数")]
    [LabelText("运动类型")]
    public DisplacementEnum BulletMovementType = DisplacementEnum.Line;

    /// <summary>
    /// 打开金手指编辑器窗口。
    /// </summary>
    [MenuItem("Tools/调试/金手指工具")]
    public static void OpenWindow()
    {
        CheatEditorWindow window = GetWindow<CheatEditorWindow>();
        window.titleContent = new GUIContent("金手指工具");
        window.minSize = new Vector2(860, 640);
        window.RefreshData();
    }

    /// <summary>
    /// 输出金手指窗口当前快照，供 Agent 自动化验证实体和属性行。
    /// </summary>
    [MenuItem("Tools/Agent/Log Cheat Editor Snapshot", false, 2001)]
    public static void LogAgentSmokeSnapshot()
    {
        CheatEditorWindow window = GetWindow<CheatEditorWindow>();
        window.titleContent = new GUIContent("金手指工具");
        window.minSize = new Vector2(860, 640);
        window.RefreshData();
        window.LogCurrentSnapshotForAgent();
    }

    /// <summary>
    /// 执行 Buff 添加烟测，供 Agent 验证有效添加、叠层和无效 ID 提示。
    /// </summary>
    [MenuItem("Tools/Agent/Run Cheat Buff Smoke", false, 2002)]
    public static void RunAgentBuffSmoke()
    {
        CheatEditorWindow window = GetWindow<CheatEditorWindow>();
        window.titleContent = new GUIContent("金手指工具");
        window.minSize = new Vector2(860, 640);
        StartBuffSmokeWaitForAgent(window);
    }

    /// <summary>
    /// 执行属性编辑烟测，供 Agent 在 Play Mode 运行世界中验证属性行按钮和失败提示。
    /// </summary>
    [MenuItem("Tools/Agent/Run Cheat Property Smoke", false, 2003)]
    public static void RunAgentPropertySmoke()
    {
        CheatEditorWindow window = GetWindow<CheatEditorWindow>();
        window.titleContent = new GUIContent("金手指工具");
        window.minSize = new Vector2(860, 640);
        StartPropertySmokeWaitForAgent(window);
    }

    /// <summary>
    /// 执行子弹创建烟测，供 Agent 在 Play Mode 运行世界中验证有效创建、重复指纹、运动数据和无效 ID 提示。
    /// </summary>
    [MenuItem("Tools/Agent/Run Cheat Bullet Smoke", false, 2004)]
    public static void RunAgentBulletSmoke()
    {
        CheatEditorWindow window = GetWindow<CheatEditorWindow>();
        window.titleContent = new GUIContent("金手指工具");
        window.minSize = new Vector2(860, 640);
        StartBulletSmokeWaitForAgent(window);
    }

    /// <summary>
    /// 执行完整金手指组合烟测，供 Agent 一次性验证属性、Buff、子弹和失败分支。
    /// </summary>
    [MenuItem("Tools/Agent/Run Cheat Combined Smoke", false, 2005)]
    public static void RunAgentCombinedSmoke()
    {
        CheatEditorWindow window = GetWindow<CheatEditorWindow>();
        window.titleContent = new GUIContent("金手指工具");
        window.minSize = new Vector2(860, 640);
        StartCombinedSmokeWaitForAgent(window);
    }

    /// <summary>
    /// Unity 编辑器窗口启用时刷新一次显示数据。
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        RefreshData();
    }

    /// <summary>
    /// Unity 编辑器窗口轮询刷新入口，仅刷新显示数据。
    /// </summary>
    private void Update()
    {
        if (!AutoRefresh)
        {
            return;
        }

        RefreshData();
        Repaint();
    }

    /// <summary>
    /// 手动刷新当前世界、实体列表和选中实体属性。
    /// </summary>
    [TitleGroup("公共状态 - 刷新")]
    [Button("刷新", ButtonSizes.Medium)]
    public void RefreshData()
    {
        if (!TryGetCurrentWorld(out BaseWorld world, out string unavailableMessage))
        {
            ClearRuntimeState(unavailableMessage);
            return;
        }

        EntitySystem entitySystem = world.GetSystem<EntitySystem>();
        IReadOnlyList<BaseEntity> entities = entitySystem?.GetExecutingEntitiesForDebug() ?? Array.Empty<BaseEntity>();

        _worldState = $"运行中：{world.SceneName}";
        _localTick = world.LocalTick;
        _authorityTick = world.LocalTick;
        _entityCount = entities.Count;
        _filteredEntities = entities.Where(IsEntityMatched).Select(entity => new EntityDebugViewData(entity)).ToList();

        RefreshSelectedEntityReference(entitySystem);
        RefreshSelectedEntityInfoAndProperties();
    }

    /// <summary>
    /// 重新选中当前世界自动解析出的本地预测实体。
    /// </summary>
    [TabGroup("功能页签", "筛选实体")]
    [TitleGroup("功能页签/筛选实体/本地预测实体")]
    [Button("选中本地预测实体", ButtonSizes.Medium)]
    public void SelectActorLocalEntity()
    {
        if (!TryGetCurrentEntitySystem(out EntitySystem entitySystem, out string unavailableMessage))
        {
            SetOperationMessage(unavailableMessage, false);
            return;
        }

        SelectEntity(entitySystem.ActorLocalEntity, "未找到主角本地实体");
    }

    /// <summary>
    /// 快捷选中当前世界的权威主角实体。
    /// </summary>
    [TabGroup("功能页签", "筛选实体")]
    [FoldoutGroup("功能页签/筛选实体/调试实体选择", false)]
    [HorizontalGroup("功能页签/筛选实体/调试实体选择/Buttons")]
    [Button("主角权威", ButtonSizes.Medium)]
    public void SelectActorAuthorityEntity()
    {
        if (!AllowDebugEntitySelection)
        {
            SetOperationMessage("默认工作流仅允许选中本地预测实体；如需选择权威实体，请先展开调试实体筛选并启用调试选择。", false);
            return;
        }

        if (!TryGetCurrentEntitySystem(out EntitySystem entitySystem, out string unavailableMessage))
        {
            SetOperationMessage(unavailableMessage, false);
            return;
        }

        SelectEntity(entitySystem.ActorAuthorityEntity, "未找到主角权威实体");
    }

    /// <summary>
    /// 快捷选中当前世界的远端对手英雄。
    /// </summary>
    [TabGroup("功能页签", "筛选实体")]
    [FoldoutGroup("功能页签/筛选实体/调试实体选择", false)]
    [HorizontalGroup("功能页签/筛选实体/调试实体选择/Buttons")]
    [Button("远端对手", ButtonSizes.Medium)]
    public void SelectRemoteHeroEntity()
    {
        if (!AllowDebugEntitySelection)
        {
            SetOperationMessage("默认工作流仅允许选中本地预测实体；如需选择对手实体，请先展开调试实体筛选并启用调试选择。", false);
            return;
        }

        if (!TryGetCurrentEntitySystem(out EntitySystem entitySystem, out string unavailableMessage))
        {
            SetOperationMessage(unavailableMessage, false);
            return;
        }

        BaseEntity remoteHero = entitySystem?.HeroEntityList?.FirstOrDefault(entity =>
            entity != null && !entitySystem.IsActorEntity(entity));
        SelectEntity(remoteHero, "未找到远端对手英雄");
    }

    /// <summary>
    /// 将输入的 Buff 配置应用到当前选中实体。
    /// </summary>
    [TabGroup("功能页签", "增加 Buff")]
    [TitleGroup("功能页签/增加 Buff/Buff操作")]
    [Button("添加Buff", ButtonSizes.Medium)]
    public void ApplyBuffToSelectedEntity()
    {
        if (!TryGetSelectedEntityReadyForBuff(out CheatEntityOperationTargets targets, out string failureMessage))
        {
            RefreshData();
            SetOperationMessage($"添加Buff失败：{failureMessage}", false);
            return;
        }

        for (int i = 0; i < targets.Entities.Count; i++)
        {
            BaseEntity targetEntity = targets.Entities[i];
            // 每个目标实体独立生成指纹，避免联机 Local/Authority 或不同 Buff 配置发生碰撞
            int buffFingerprints = GenerateCheatBuffFingerprint(targetEntity, BuffConfigId);
            targetEntity.GetComponent<BuffComponent>().CreateBuffWithFingerprints(BuffConfigId, buffFingerprints);
        }

        string targetSummary = targets.BuildSummary();
        RefreshData();
        SetOperationMessage($"已向 {targetSummary} 添加 Buff：{BuffConfigId}，影响 {targets.Entities.Count} 个实体", false);
    }

    /// <summary>
    /// 为金手指 Buff 生成唯一指纹。
    /// 不使用运行时 FingerprintsGenerate，因其会将 Buff 配置 ID 截断到 3 位，导致 4001/4002 等配置碰撞。
    /// </summary>
    private static int GenerateCheatBuffFingerprint(BaseEntity targetEntity, int buffConfigId)
    {
        EntitySystem entitySystem = targetEntity.GetSystem<EntitySystem>();
        int stableEntityIdentity = entitySystem.GetStableEntityIdentity(targetEntity);
        uint frame = targetEntity.BaseWorld.LocalTick;

        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (int)frame;
            hash = hash * 31 + targetEntity.ConfigId;
            hash = hash * 31 + buffConfigId;
            hash = hash * 31 + stableEntityIdentity;
            hash = hash * 31 + (int)targetEntity.EntityUpdateType;
            hash = hash * 31 + targetEntity.EntityId;
            return hash;
        }
    }

    /// <summary>
    /// 按当前输入在运行世界中创建一个子弹实体。
    /// </summary>
    [TabGroup("功能页签", "添加子弹")]
    [TitleGroup("功能页签/添加子弹/子弹创建")]
    [Button("创建子弹", ButtonSizes.Medium)]
    public void CreateBulletInWorld()
    {
        if (!TryGetBulletCreationContext(
                out EntitySystem entitySystem,
                out BaseEntity parentEntity,
                out BulletAssetsConfig bulletConfig,
                out fp3 spawnPosition,
                out fp3 spawnEulerAngles,
                out string failureMessage))
        {
            RefreshData();
            SetOperationMessage($"创建子弹失败：{failureMessage}", false);
            return;
        }

        object customData = CreateBulletCustomData();
        EntityCreateData createData = EntityCreateData.Create(
            bulletConfig,
            spawnPosition,
            spawnEulerAngles,
            new fp3(1, 1, 1),
            parentEntity.IsNeedExecuteView,
            null,
            parentEntity,
            customData);
        int fingerprints = GenerateCheatBulletFingerprint(
            entitySystem,
            parentEntity,
            BulletConfigId,
            spawnPosition,
            spawnEulerAngles);

        try
        {
            BulletEntity bulletEntity = entitySystem.CreateDynamicEntity<BulletEntity>(
                createData,
                parentEntity.EntityUpdateType,
                fingerprints);

            if (bulletEntity == null)
            {
                SetOperationMessage($"创建子弹失败：运行时 CreateDynamicEntity 返回空，config={BulletConfigId}", true);
                return;
            }

            GameLog.Info(GameLogChannel.AgentTest,
                $"{AgentBulletCreateLogPrefix} success: bullet={bulletEntity.EntityId}, config={BulletConfigId}, parent={parentEntity.EntityType} #{parentEntity.EntityId}, updateType={parentEntity.EntityUpdateType}, fingerprints={fingerprints}, movement={BulletUseMovementData}");
            SetOperationMessage(
                $"已创建子弹 #{bulletEntity.EntityId}：Config {BulletConfigId}，父实体 {parentEntity.EntityType} #{parentEntity.EntityId}，更新域 {parentEntity.EntityUpdateType}，指纹 {fingerprints}",
                true);
        }
        catch (Exception exception)
        {
            GameLog.Error(GameLogChannel.AgentTest,
                $"{AgentBulletCreateLogPrefix} failed: config={BulletConfigId}, parent={parentEntity.EntityType} #{parentEntity.EntityId}, reason={exception.Message}");
            SetOperationMessage($"创建子弹失败：运行时创建异常（{exception.Message}）", true);
        }
    }

    /// <summary>
    /// 根据选中实体重新生成基础信息和属性行。
    /// </summary>
    private void OnSelectedEntityChanged()
    {
        RefreshSelectedEntityInfoAndProperties();
    }

    /// <summary>
    /// 读取并校验创建子弹所需的运行时上下文。
    /// </summary>
    /// <param name="entitySystem">当前运行世界的实体系统。</param>
    /// <param name="parentEntity">子弹初始化时使用的父实体。</param>
    /// <param name="bulletConfig">子弹配置数据。</param>
    /// <param name="spawnPosition">转换为定点数后的生成位置。</param>
    /// <param name="spawnEulerAngles">转换为定点数后的生成旋转。</param>
    /// <param name="failureMessage">上下文不可用时展示给用户的原因。</param>
    /// <returns>运行世界、父实体、配置和数值输入都可用于创建子弹时返回 true。</returns>
    private bool TryGetBulletCreationContext(
        out EntitySystem entitySystem,
        out BaseEntity parentEntity,
        out BulletAssetsConfig bulletConfig,
        out fp3 spawnPosition,
        out fp3 spawnEulerAngles,
        out string failureMessage)
    {
        parentEntity = null;
        bulletConfig = null;
        spawnPosition = fp3.zero;
        spawnEulerAngles = fp3.zero;

        if (!TryGetCurrentEntitySystem(out entitySystem, out failureMessage))
        {
            return false;
        }

        if (!TryGetBulletConfig(BulletConfigId, out bulletConfig, out failureMessage))
        {
            return false;
        }

        if (!TryGetBulletParentEntity(entitySystem, out parentEntity, out failureMessage))
        {
            return false;
        }

        if (parentEntity.EntityState != EntityState.Survival)
        {
            failureMessage = $"父实体不是存活状态：{parentEntity.EntityState}";
            return false;
        }

        if (parentEntity.GetComponent<BulletControlCompinent>() == null)
        {
            failureMessage = $"父实体缺少 BulletControlCompinent，无法完成 BulletEntity 初始化：{parentEntity.EntityType} #{parentEntity.EntityId}";
            return false;
        }

        if (!TryConvertVector3ToFp3(BulletSpawnPosition, "生成位置", out spawnPosition, out failureMessage) ||
            !TryConvertVector3ToFp3(BulletSpawnEulerAngles, "生成旋转", out spawnEulerAngles, out failureMessage))
        {
            return false;
        }

        if (BulletUseMovementData && BulletMovementTime <= 0)
        {
            failureMessage = "启用运动时运动帧数必须大于 0";
            return false;
        }

        if (BulletUseMovementData &&
            !TryConvertFiniteInput(BulletMovementSpeed, "运动速度", out _, out failureMessage))
        {
            return false;
        }

        failureMessage = null;
        return true;
    }

    /// <summary>
    /// 从运行时数据表读取子弹配置。
    /// </summary>
    /// <param name="bulletConfigId">用户输入的子弹配置 ID。</param>
    /// <param name="bulletConfig">读取到的子弹配置。</param>
    /// <param name="failureMessage">配置不可用时展示给用户的原因。</param>
    /// <returns>子弹配置存在且数据表可访问时返回 true。</returns>
    private static bool TryGetBulletConfig(int bulletConfigId, out BulletAssetsConfig bulletConfig, out string failureMessage)
    {
        bulletConfig = null;
        failureMessage = null;

        if (bulletConfigId <= 0)
        {
            failureMessage = "请输入大于 0 的子弹配置 ID";
            return false;
        }

        if (GameEntry.DataTable == null)
        {
            failureMessage = "运行时数据表尚不可用";
            return false;
        }

        bulletConfig = GameEntry.DataTable.GetDataTable<BulletAssetsConfig>(bulletConfigId);

        if (bulletConfig == null)
        {
            failureMessage = $"未找到子弹配置：{bulletConfigId}";
            return false;
        }

        return true;
    }

    /// <summary>
    /// 根据窗口输入选择子弹父实体。
    /// </summary>
    /// <param name="entitySystem">当前运行世界的实体系统。</param>
    /// <param name="parentEntity">找到的父实体。</param>
    /// <param name="failureMessage">无法找到父实体时展示给用户的原因。</param>
    /// <returns>父实体来源可解析且实体引用有效时返回 true。</returns>
    private bool TryGetBulletParentEntity(EntitySystem entitySystem, out BaseEntity parentEntity, out string failureMessage)
    {
        parentEntity = null;
        failureMessage = null;

        switch (BulletParentSource)
        {
            case BulletOwnerSource.SelectedEntity:
                if (!TryGetSelectedEntity(out parentEntity, out failureMessage))
                {
                    failureMessage = $"需要有效的选中实体作为父实体：{failureMessage}";
                    return false;
                }

                return true;
            case BulletOwnerSource.ActorLocal:
                parentEntity = entitySystem.ActorLocalEntity;
                failureMessage = parentEntity == null ? "未找到本地主角实体" : GetEntityReferenceFailureMessage(parentEntity, entitySystem);
                return failureMessage == null;
            case BulletOwnerSource.ActorAuthority:
                parentEntity = entitySystem.ActorAuthorityEntity;
                failureMessage = parentEntity == null ? "未找到权威主角实体" : GetEntityReferenceFailureMessage(parentEntity, entitySystem);
                return failureMessage == null;
            default:
                failureMessage = $"不支持的父实体来源：{BulletParentSource}";
                return false;
        }
    }

    /// <summary>
    /// 创建子弹可选的自定义数据，当前用于驱动运动子弹位移组件。
    /// </summary>
    /// <returns>启用运动时返回 MovementData；否则返回 null。</returns>
    private object CreateBulletCustomData()
    {
        if (!BulletUseMovementData)
        {
            return null;
        }

        return MovementData.Create(BulletMovementTime, (fp)BulletMovementSpeed, BulletMovementType);
    }

    /// <summary>
    /// 为编辑器创建的子弹生成调试指纹，并在当前编辑器会话内避开已知重复值。
    /// </summary>
    /// <param name="entitySystem">当前运行世界的实体系统。</param>
    /// <param name="parentEntity">创建子弹的父实体。</param>
    /// <param name="bulletConfigId">子弹配置 ID。</param>
    /// <param name="spawnPosition">子弹生成位置。</param>
    /// <param name="spawnEulerAngles">子弹生成旋转。</param>
    /// <returns>用于动态实体创建的指纹。</returns>
    private static int GenerateCheatBulletFingerprint(
        EntitySystem entitySystem,
        BaseEntity parentEntity,
        int bulletConfigId,
        fp3 spawnPosition,
        fp3 spawnEulerAngles)
    {
        uint frame = parentEntity.BaseWorld.LocalTick;
        int stableEntityIdentity = entitySystem.GetStableEntityIdentity(parentEntity);

        for (int i = 0; i < 64; i++)
        {
            int sequence = ++_bulletCheatCreateSequence;
            fp sequenceOffset = (fp)(sequence * 0.001f);
            fp3 fingerprintPosition = spawnPosition + new fp3(sequenceOffset, (fp)(i * 0.001f), 0);
            fp3 fingerprintEuler = spawnEulerAngles + new fp3(0, (fp)(sequence * 0.01f), (fp)(i * 0.01f));
            int fingerprints = FingerprintsGenerate.GenerateFingerprint(
                (int)(frame + (uint)sequence),
                stableEntityIdentity,
                bulletConfigId,
                fingerprintPosition,
                fingerprintEuler);

            if (_createdBulletFingerprints.Add(fingerprints))
            {
                return fingerprints;
            }
        }

        int fallbackFingerprints = FingerprintsGenerate.GenerateFingerprint(
            (int)frame,
            stableEntityIdentity,
            bulletConfigId,
            spawnPosition,
            spawnEulerAngles) ^ (++_bulletCheatCreateSequence << 8) ^ parentEntity.EntityId;
        _createdBulletFingerprints.Add(fallbackFingerprints);
        return fallbackFingerprints;
    }

    /// <summary>
    /// 将编辑器 Vector3 输入转换为运行时定点三维向量。
    /// </summary>
    /// <param name="inputValue">编辑器控件输入的三维向量。</param>
    /// <param name="displayName">展示给用户的输入名称。</param>
    /// <param name="fpValue">转换后的定点三维向量。</param>
    /// <param name="failureMessage">输入非法或转换失败时的原因。</param>
    /// <returns>三维向量每个分量都可转换为定点数时返回 true。</returns>
    private static bool TryConvertVector3ToFp3(Vector3 inputValue, string displayName, out fp3 fpValue, out string failureMessage)
    {
        fpValue = fp3.zero;

        if (!TryConvertFiniteInput(inputValue.x, $"{displayName}.x", out fp x, out failureMessage) ||
            !TryConvertFiniteInput(inputValue.y, $"{displayName}.y", out fp y, out failureMessage) ||
            !TryConvertFiniteInput(inputValue.z, $"{displayName}.z", out fp z, out failureMessage))
        {
            return false;
        }

        fpValue = new fp3(x, y, z);
        return true;
    }

    /// <summary>
    /// 将编辑器浮点输入转换为定点数。
    /// </summary>
    /// <param name="inputValue">编辑器控件输入的浮点值。</param>
    /// <param name="displayName">展示给用户的输入名称。</param>
    /// <param name="fpValue">转换后的定点数。</param>
    /// <param name="failureMessage">输入非法或转换失败时的原因。</param>
    /// <returns>输入为有限数且可转换为定点数时返回 true。</returns>
    private static bool TryConvertFiniteInput(float inputValue, string displayName, out fp fpValue, out string failureMessage)
    {
        fpValue = 0;

        if (float.IsNaN(inputValue) || float.IsInfinity(inputValue))
        {
            failureMessage = $"{displayName} 必须是有限数";
            return false;
        }

        try
        {
            fpValue = (fp)inputValue;
        }
        catch (Exception exception)
        {
            failureMessage = $"{displayName} 无法转换为定点数（{exception.Message}）";
            return false;
        }

        failureMessage = null;
        return true;
    }

    /// <summary>
    /// 将当前窗口刷新状态和快捷选择结果输出到 Console。
    /// </summary>
    private void LogCurrentSnapshotForAgent()
    {
        GameLog.Info(GameLogChannel.AgentTest, $"{AgentSnapshotLogPrefix} worldState={_worldState}, entityCount={_entityCount}, filteredCount={_filteredEntities.Count}, operation={_operationMessage}");

        if (!TryGetCurrentEntitySystem(out EntitySystem entitySystem, out string unavailableMessage))
        {
            GameLog.Warn(GameLogChannel.AgentTest, $"{AgentSnapshotLogPrefix} unavailable={unavailableMessage}");
            return;
        }

        LogSelectionSnapshotForAgent("ActorLocal", entitySystem.ActorLocalEntity);
        LogSelectionSnapshotForAgent("ActorAuthority", entitySystem.ActorAuthorityEntity);
        BaseEntity remoteHero = entitySystem.HeroEntityList?.FirstOrDefault(entity =>
            entity != null && !entitySystem.IsActorEntity(entity));
        LogSelectionSnapshotForAgent("RemoteHero", remoteHero);
    }

    /// <summary>
    /// 选中指定实体并输出实体摘要和属性行数量。
    /// </summary>
    /// <param name="selectionName">本次快捷选择的名称。</param>
    /// <param name="entity">准备选中的实体引用。</param>
    private void LogSelectionSnapshotForAgent(string selectionName, BaseEntity entity)
    {
        SelectEntity(entity, $"{selectionName} 不存在");
        TryGetSelectedEntity(out BaseEntity selectedEntity, out _);
        int propertyRowCount = _propertyRows?.Count ?? 0;
        string result = selectedEntity == null ? _operationMessage : $"{SelectedEntityView} propertyRows={propertyRowCount}";
        GameLog.Info(GameLogChannel.AgentTest, $"{AgentSnapshotLogPrefix} selection={selectionName}, result={result}");
    }

    /// <summary>
    /// 启动 Buff 烟测等待流程。
    /// </summary>
    /// <param name="window">负责执行烟测的金手指窗口。</param>
    private static void StartBuffSmokeWaitForAgent(CheatEditorWindow window)
    {
        if (_isAgentBuffSmokeWaiting)
        {
            EditorApplication.update -= TickBuffSmokeWaitForAgent;
        }

        _isAgentBuffSmokeWaiting = true;
        _agentBuffSmokeDeadlineTime = EditorApplication.timeSinceStartup + AgentBuffSmokeTimeoutSeconds;
        EditorApplication.update += TickBuffSmokeWaitForAgent;
        GameLog.Info(GameLogChannel.AgentTest, $"{AgentBuffSmokeLogPrefix} waiting for runtime world...");
        window.TryRunBuffSmokeForAgent(false);
    }

    /// <summary>
    /// 在编辑器 Update 中轮询 Play Mode 运行世界并执行 Buff 烟测。
    /// </summary>
    private static void TickBuffSmokeWaitForAgent()
    {
        CheatEditorWindow window = GetWindow<CheatEditorWindow>();

        if (window.TryRunBuffSmokeForAgent(true))
        {
            StopBuffSmokeWaitForAgent();
            return;
        }

        if (EditorApplication.timeSinceStartup >= _agentBuffSmokeDeadlineTime)
        {
            StopBuffSmokeWaitForAgent();
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentBuffSmokeLogPrefix} failed: 等待 Play Mode 运行世界超时");
        }
    }

    /// <summary>
    /// 停止 Agent Buff 烟测等待流程。
    /// </summary>
    private static void StopBuffSmokeWaitForAgent()
    {
        EditorApplication.update -= TickBuffSmokeWaitForAgent;
        _isAgentBuffSmokeWaiting = false;
    }

    /// <summary>
    /// 启动属性烟测等待流程。
    /// </summary>
    /// <param name="window">负责执行烟测的金手指窗口。</param>
    private static void StartPropertySmokeWaitForAgent(CheatEditorWindow window)
    {
        if (_isAgentPropertySmokeWaiting)
        {
            EditorApplication.update -= TickPropertySmokeWaitForAgent;
        }

        _isAgentPropertySmokeWaiting = true;
        _agentPropertySmokeDeadlineTime = EditorApplication.timeSinceStartup + AgentPropertySmokeTimeoutSeconds;
        EditorApplication.update += TickPropertySmokeWaitForAgent;
        GameLog.Info(GameLogChannel.AgentTest, $"{AgentPropertySmokeLogPrefix} waiting for runtime world...");
        window.TryRunPropertySmokeForAgent(false);
    }

    /// <summary>
    /// 在编辑器 Update 中轮询 Play Mode 运行世界并执行属性烟测。
    /// </summary>
    private static void TickPropertySmokeWaitForAgent()
    {
        CheatEditorWindow window = GetWindow<CheatEditorWindow>();

        if (window.TryRunPropertySmokeForAgent(true))
        {
            StopPropertySmokeWaitForAgent();
            return;
        }

        if (EditorApplication.timeSinceStartup >= _agentPropertySmokeDeadlineTime)
        {
            StopPropertySmokeWaitForAgent();
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentPropertySmokeLogPrefix} failed: 等待 Play Mode 运行世界超时");
        }
    }

    /// <summary>
    /// 停止 Agent 属性烟测等待流程。
    /// </summary>
    private static void StopPropertySmokeWaitForAgent()
    {
        EditorApplication.update -= TickPropertySmokeWaitForAgent;
        _isAgentPropertySmokeWaiting = false;
    }

    /// <summary>
    /// 启动子弹创建烟测等待流程。
    /// </summary>
    /// <param name="window">负责执行烟测的金手指窗口。</param>
    private static void StartBulletSmokeWaitForAgent(CheatEditorWindow window)
    {
        if (_isAgentBulletSmokeWaiting)
        {
            EditorApplication.update -= TickBulletSmokeWaitForAgent;
        }

        _isAgentBulletSmokeWaiting = true;
        _agentBulletSmokeDeadlineTime = EditorApplication.timeSinceStartup + AgentBulletSmokeTimeoutSeconds;
        EditorApplication.update += TickBulletSmokeWaitForAgent;
        GameLog.Info(GameLogChannel.AgentTest, $"{AgentBulletSmokeLogPrefix} waiting for runtime world...");
        window.TryRunBulletSmokeForAgent(false);
    }

    /// <summary>
    /// 在编辑器 Update 中轮询 Play Mode 运行世界并执行子弹创建烟测。
    /// </summary>
    private static void TickBulletSmokeWaitForAgent()
    {
        CheatEditorWindow window = GetWindow<CheatEditorWindow>();

        if (window.TryRunBulletSmokeForAgent(true))
        {
            StopBulletSmokeWaitForAgent();
            return;
        }

        if (EditorApplication.timeSinceStartup >= _agentBulletSmokeDeadlineTime)
        {
            StopBulletSmokeWaitForAgent();
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentBulletSmokeLogPrefix} failed: 等待 Play Mode 运行世界超时");
        }
    }

    /// <summary>
    /// 停止 Agent 子弹创建烟测等待流程。
    /// </summary>
    private static void StopBulletSmokeWaitForAgent()
    {
        EditorApplication.update -= TickBulletSmokeWaitForAgent;
        _isAgentBulletSmokeWaiting = false;
    }

    /// <summary>
    /// 启动 Agent 组合烟测等待流程，并生成本轮唯一 runId。
    /// </summary>
    /// <param name="window">负责执行组合烟测的金手指窗口。</param>
    private static void StartCombinedSmokeWaitForAgent(CheatEditorWindow window)
    {
        if (_isAgentCombinedSmokeWaiting)
        {
            EditorApplication.update -= TickCombinedSmokeWaitForAgent;
        }

        _isAgentCombinedSmokeWaiting = true;
        _agentCombinedSmokeRunId = GenerateAgentCombinedSmokeRunId();
        _agentCombinedSmokeDeadlineTime = EditorApplication.timeSinceStartup + AgentCombinedSmokeTimeoutSeconds;
        EditorApplication.update += TickCombinedSmokeWaitForAgent;
        ClearConsoleForAgentCombinedSmoke();
        GameLog.Info(GameLogChannel.AgentTest, $"{AgentCombinedSmokeLogPrefix} runId={_agentCombinedSmokeRunId} start: waiting for runtime world...");

        if (window.TryRunCombinedSmokeForAgent(false))
        {
            StopCombinedSmokeWaitForAgent();
        }
    }

    /// <summary>
    /// 在编辑器 Update 中轮询 Play Mode 运行世界并执行组合烟测。
    /// </summary>
    private static void TickCombinedSmokeWaitForAgent()
    {
        CheatEditorWindow window = GetWindow<CheatEditorWindow>();

        if (window.TryRunCombinedSmokeForAgent(true))
        {
            StopCombinedSmokeWaitForAgent();
            return;
        }

        if (EditorApplication.timeSinceStartup >= _agentCombinedSmokeDeadlineTime)
        {
            string runId = _agentCombinedSmokeRunId;
            StopCombinedSmokeWaitForAgent();
            GameLog.Warn(GameLogChannel.AgentTest, $"{AgentCombinedSmokeLogPrefix} runId={runId} pending-timeout: 等待 Play Mode 运行世界超时，请先通过 Tools/Agent/Run Client Enter Game Test 建立运行世界");
        }
    }

    /// <summary>
    /// 停止 Agent 组合烟测等待流程。
    /// </summary>
    private static void StopCombinedSmokeWaitForAgent()
    {
        EditorApplication.update -= TickCombinedSmokeWaitForAgent;
        _isAgentCombinedSmokeWaiting = false;
    }

    /// <summary>
    /// 生成组合烟测 runId，便于测试工程师从 Console 中过滤本轮结果。
    /// </summary>
    /// <returns>包含递增序号和编辑器时间戳的组合烟测运行 ID。</returns>
    private static string GenerateAgentCombinedSmokeRunId()
    {
        int sequence = ++_agentCombinedSmokeSequence;
        int timeStamp = Mathf.Abs((int)(EditorApplication.timeSinceStartup * 1000d));
        return $"{sequence}-{timeStamp}";
    }

    /// <summary>
    /// 清理 Unity Console 中的旧日志，避免组合烟测结果被历史错误淹没。
    /// </summary>
    private static void ClearConsoleForAgentCombinedSmoke()
    {
        Type logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");
        logEntriesType?.GetMethod("Clear")?.Invoke(null, null);
    }

    /// <summary>
    /// 通过现有三个 smoke 方法执行完整金手指组合验收路径。
    /// </summary>
    /// <param name="suppressWorldUnavailableLog">等待世界过程中是否暂时压制世界不可用日志。</param>
    /// <returns>组合烟测已完成或已确认失败时返回 true；仍需继续等待运行世界时返回 false。</returns>
    private bool TryRunCombinedSmokeForAgent(bool suppressWorldUnavailableLog)
    {
        string runId = _agentCombinedSmokeRunId ?? "unknown";

        if (!TryGetCurrentEntitySystem(out EntitySystem entitySystem, out string unavailableMessage))
        {
            if (!suppressWorldUnavailableLog)
            {
                GameLog.Info(GameLogChannel.AgentTest, $"{AgentCombinedSmokeLogPrefix} runId={runId} pending: {unavailableMessage}");
            }

            return false;
        }

        GameLog.Info(GameLogChannel.AgentTest, $"{AgentCombinedSmokeLogPrefix} runId={runId} step=property start");

        if (!TryRunPropertySmokeForAgent(true))
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentCombinedSmokeLogPrefix} runId={runId} failed: step=property, operation={_operationMessage}");
            return true;
        }

        GameLog.Info(GameLogChannel.AgentTest, $"{AgentCombinedSmokeLogPrefix} runId={runId} step=property success");
        GameLog.Info(GameLogChannel.AgentTest, $"{AgentCombinedSmokeLogPrefix} runId={runId} step=buff start");

        if (!TryRunBuffSmokeForAgent(true))
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentCombinedSmokeLogPrefix} runId={runId} failed: step=buff, operation={_operationMessage}");
            return true;
        }

        GameLog.Info(GameLogChannel.AgentTest, $"{AgentCombinedSmokeLogPrefix} runId={runId} step=buff success");
        GameLog.Info(GameLogChannel.AgentTest, $"{AgentCombinedSmokeLogPrefix} runId={runId} step=bullet start");

        if (!TryRunBulletSmokeForAgent(true))
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentCombinedSmokeLogPrefix} runId={runId} failed: step=bullet, operation={_operationMessage}");
            return true;
        }

        GameLog.Info(GameLogChannel.AgentTest, $"{AgentCombinedSmokeLogPrefix} runId={runId} step=bullet success");
        RefreshData();
        GameLog.Info(GameLogChannel.AgentTest,
            $"{AgentCombinedSmokeLogPrefix} runId={runId} success: CheatEditorPropertySmoke=success, CheatEditorBuffSmoke=success, CheatEditorBulletSmoke=success, worldState={_worldState}, entityCount={_entityCount}, operation={_operationMessage}");
        return true;
    }

    /// <summary>
    /// 通过当前窗口的公开按钮方法执行 Buff 添加验收路径。
    /// </summary>
    /// <param name="suppressWorldUnavailableLog">等待世界过程中是否暂时压制世界不可用日志。</param>
    /// <returns>烟测已经成功完成时返回 true。</returns>
    private bool TryRunBuffSmokeForAgent(bool suppressWorldUnavailableLog)
    {
        if (!TryGetCurrentEntitySystem(out EntitySystem entitySystem, out string unavailableMessage))
        {
            if (!suppressWorldUnavailableLog)
            {
                GameLog.Info(GameLogChannel.AgentTest, $"{AgentBuffSmokeLogPrefix} pending: {unavailableMessage}");
            }

            return false;
        }

        if (!TryFindBuffSmokeTarget(entitySystem, out BaseEntity targetEntity, out string targetFailureMessage))
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentBuffSmokeLogPrefix} failed: {targetFailureMessage}");
            return false;
        }

        if (!TryFindValidBuffConfigId(out int validBuffConfigId, out string configFailureMessage))
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentBuffSmokeLogPrefix} failed: {configFailureMessage}");
            return false;
        }

        SelectEntity(targetEntity, "未找到可添加 Buff 的实体");
        BuffConfigId = validBuffConfigId;
        int beforeCount = CountBuffEntities(entitySystem, targetEntity, validBuffConfigId);

        ApplyBuffToSelectedEntity();
        BuffEntity firstBuff = FindBuffEntity(entitySystem, targetEntity, validBuffConfigId);

        if (firstBuff == null)
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentBuffSmokeLogPrefix} failed: 添加有效 Buff 后未找到 BuffEntity，operation={_operationMessage}");
            return false;
        }

        int afterFirstCount = CountBuffEntities(entitySystem, targetEntity, validBuffConfigId);
        int layerAfterFirst = firstBuff.Layer;

        ApplyBuffToSelectedEntity();
        BuffEntity secondBuff = FindBuffEntity(entitySystem, targetEntity, validBuffConfigId);

        if (secondBuff == null)
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentBuffSmokeLogPrefix} failed: 重复添加后 BuffEntity 丢失，operation={_operationMessage}");
            return false;
        }

        int afterSecondCount = CountBuffEntities(entitySystem, targetEntity, validBuffConfigId);
        int layerAfterSecond = secondBuff.Layer;

        if (afterFirstCount != afterSecondCount || layerAfterSecond <= layerAfterFirst)
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentBuffSmokeLogPrefix} failed: 重复添加未走叠层，count {afterFirstCount}->{afterSecondCount}, layer {layerAfterFirst}->{layerAfterSecond}");
            return false;
        }

        BuffConfigId = -1;
        ApplyBuffToSelectedEntity();

        if (!_operationMessage.Contains("添加Buff失败"))
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentBuffSmokeLogPrefix} failed: 无效 Buff ID 未产生失败提示，operation={_operationMessage}");
            return false;
        }

        GameLog.Info(GameLogChannel.AgentTest, $"{AgentBuffSmokeLogPrefix} success: target={targetEntity.EntityType} #{targetEntity.EntityId}, buff={validBuffConfigId}, count {beforeCount}->{afterFirstCount}->{afterSecondCount}, layer {layerAfterFirst}->{layerAfterSecond}");
        return true;
    }

    /// <summary>
    /// 通过当前窗口的公开按钮方法执行子弹创建验收路径。
    /// </summary>
    /// <param name="suppressWorldUnavailableLog">等待世界过程中是否暂时压制世界不可用日志。</param>
    /// <returns>烟测已经成功完成时返回 true。</returns>
    private bool TryRunBulletSmokeForAgent(bool suppressWorldUnavailableLog)
    {
        if (!TryGetCurrentEntitySystem(out EntitySystem entitySystem, out string unavailableMessage))
        {
            if (!suppressWorldUnavailableLog)
            {
                GameLog.Info(GameLogChannel.AgentTest, $"{AgentBulletSmokeLogPrefix} pending: {unavailableMessage}");
            }

            return false;
        }

        if (!TryFindBulletSmokeTarget(entitySystem, out BaseEntity parentEntity, out string targetFailureMessage))
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentBulletSmokeLogPrefix} failed: {targetFailureMessage}");
            return false;
        }

        if (!TryFindValidBulletConfigId(out int validBulletConfigId, out string configFailureMessage))
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentBulletSmokeLogPrefix} failed: {configFailureMessage}");
            return false;
        }

        ResetFiltersForAgentBulletSmoke();
        SelectEntity(parentEntity, "未找到可创建子弹的父实体");
        BulletParentSource = BulletOwnerSource.SelectedEntity;
        BulletConfigId = validBulletConfigId;
        BulletSpawnPosition = Fp3ToVector3(parentEntity.transform.Position + parentEntity.transform.Forward);
        BulletSpawnEulerAngles = Fp3ToVector3(parentEntity.transform.EulerAngles);
        BulletUseMovementData = false;

        int beforeCount = CountBulletEntities(entitySystem, parentEntity);
        CreateBulletInWorld();
        BulletEntity firstBullet = FindLatestBulletEntity(entitySystem, parentEntity);

        if (firstBullet == null || !_operationMessage.Contains("已创建子弹"))
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentBulletSmokeLogPrefix} failed: 有效配置未创建 BulletEntity，operation={_operationMessage}");
            return false;
        }

        int firstFingerprint = firstBullet.EntityFingerprints;
        int afterFirstCount = CountBulletEntities(entitySystem, parentEntity);
        CreateBulletInWorld();
        BulletEntity secondBullet = FindLatestBulletEntity(entitySystem, parentEntity);

        if (secondBullet == null || secondBullet == firstBullet)
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentBulletSmokeLogPrefix} failed: 重复创建后未找到新的 BulletEntity，operation={_operationMessage}");
            return false;
        }

        int secondFingerprint = secondBullet.EntityFingerprints;
        int afterSecondCount = CountBulletEntities(entitySystem, parentEntity);

        if (firstFingerprint == secondFingerprint)
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentBulletSmokeLogPrefix} failed: 重复创建复用了同一指纹 fingerprint={firstFingerprint}");
            return false;
        }

        BulletUseMovementData = true;
        BulletMovementTime = 12;
        BulletMovementSpeed = 1f;
        BulletMovementType = DisplacementEnum.Line;
        CreateBulletInWorld();
        BulletEntity movementBullet = FindLatestBulletEntity(entitySystem, parentEntity);

        if (movementBullet == null || movementBullet == secondBullet)
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentBulletSmokeLogPrefix} failed: 运动子弹未创建新实体，operation={_operationMessage}");
            return false;
        }

        MovementData movementData = movementBullet.GetData<MovementData>(ComponentDataKey.MovementData);

        if (movementData == null || movementData.MoveTick != BulletMovementTime)
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentBulletSmokeLogPrefix} failed: 运动子弹 MovementData 不匹配，hasData={movementData != null}, operation={_operationMessage}");
            return false;
        }

        int afterMovementCount = CountBulletEntities(entitySystem, parentEntity);
        BulletUseMovementData = false;
        BulletConfigId = -1;
        CreateBulletInWorld();

        if (!_operationMessage.Contains("创建子弹失败") || !_operationMessage.Contains("子弹配置"))
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentBulletSmokeLogPrefix} failed: 无效子弹 ID 未产生失败提示，operation={_operationMessage}");
            return false;
        }

        int afterInvalidCount = CountBulletEntities(entitySystem, parentEntity);

        if (afterInvalidCount != afterMovementCount)
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentBulletSmokeLogPrefix} failed: 无效配置创建改变了子弹数量，count {afterMovementCount}->{afterInvalidCount}");
            return false;
        }

        GameLog.Info(GameLogChannel.AgentTest,
            $"{AgentBulletSmokeLogPrefix} success: parent={parentEntity.EntityType} #{parentEntity.EntityId}, config={validBulletConfigId}, count {beforeCount}->{afterFirstCount}->{afterSecondCount}->{afterMovementCount}, fingerprints={firstFingerprint}/{secondFingerprint}/{movementBullet.EntityFingerprints}, movementTick={movementData.MoveTick}");
        return true;
    }

    /// <summary>
    /// 重置会影响 Agent 子弹烟测选中实体保留的窗口筛选条件。
    /// </summary>
    private void ResetFiltersForAgentBulletSmoke()
    {
        EntityTypeFilter = AllFilterName;
        EntityUpdateTypeFilter = AllFilterName;
        ConfigIdFilter = 0;
        EntityIdFilter = 0;
        OnlySurvivalEntity = true;
    }

    /// <summary>
    /// 通过当前窗口的属性行公开按钮执行属性编辑验收路径。
    /// </summary>
    /// <param name="suppressWorldUnavailableLog">等待世界过程中是否暂时压制世界不可用日志。</param>
    /// <returns>烟测已经成功完成时返回 true。</returns>
    private bool TryRunPropertySmokeForAgent(bool suppressWorldUnavailableLog)
    {
        if (!TryGetCurrentEntitySystem(out EntitySystem entitySystem, out string unavailableMessage))
        {
            if (!suppressWorldUnavailableLog)
            {
                GameLog.Info(GameLogChannel.AgentTest, $"{AgentPropertySmokeLogPrefix} pending: {unavailableMessage}");
            }

            return false;
        }

        bool hasTestedTarget = false;

        if (entitySystem.ActorLocalEntity != null)
        {
            if (!TryRunPropertySmokeForTarget("ActorLocal", entitySystem.ActorLocalEntity))
            {
                return false;
            }

            hasTestedTarget = true;
        }
        else
        {
            GameLog.Warn(GameLogChannel.AgentTest, $"{AgentPropertySmokeLogPrefix} target=ActorLocal skipped: 未找到本地主角实体");
        }

        if (!hasTestedTarget)
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentPropertySmokeLogPrefix} failed: 没有可用于属性烟测的本地预测实体");
            return false;
        }

        if (!TryRunPropertyFailureSmoke(entitySystem, out string failureSmokeMessage))
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentPropertySmokeLogPrefix} failed: {failureSmokeMessage}");
            return false;
        }

        GameLog.Info(GameLogChannel.AgentTest, $"{AgentPropertySmokeLogPrefix} success: testedTargets=available, failureCases=passed");
        return true;
    }

    /// <summary>
    /// 对指定实体执行属性行按钮烟测。
    /// </summary>
    /// <param name="targetName">目标实体在 Agent 日志中的名称。</param>
    /// <param name="entity">准备测试的运行时实体。</param>
    /// <returns>目标实体属性烟测全部通过时返回 true。</returns>
    private bool TryRunPropertySmokeForTarget(string targetName, BaseEntity entity)
    {
        SelectEntity(entity, $"{targetName} 不存在");

        if (!TryGetSelectedEntity(out BaseEntity selectedEntity, out _) || selectedEntity != entity)
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentPropertySmokeLogPrefix} target={targetName} failed: 无法选中实体，operation={_operationMessage}");
            return false;
        }

        if (!TryGetFirstEditablePropertyRow(out EntityPropertyCheatRow propertyRow, out string failureMessage))
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentPropertySmokeLogPrefix} target={targetName} failed: {failureMessage}");
            return false;
        }

        PropertyKey propertyKey = propertyRow.Key;

        if (!propertyRow.TryReadSnapshot(out EntityPropertyCheatRow.EntityPropertySnapshot originalSnapshot, out failureMessage) ||
            !propertyRow.TryReadTargetSnapshots(out List<EntityPropertyCheatRow.EntityPropertyTargetSnapshot> originalTargetSnapshots, out failureMessage))
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentPropertySmokeLogPrefix} target={targetName} key={propertyKey} failed: 初始快照读取失败 {failureMessage}");
            return false;
        }

        fp range = originalSnapshot.MaxValue - originalSnapshot.MinValue;
        propertyRow.SetValue = (float)(originalSnapshot.MinValue + range * (fp)0.5f);
        propertyRow.ApplySetValue();

        if (!TryRequireOperationMessage("已设置", targetName, propertyKey, "set") ||
            !TryRefreshAndFindPropertyRow(entity, propertyKey, out propertyRow, out failureMessage))
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentPropertySmokeLogPrefix} target={targetName} key={propertyKey} failed: 设置后刷新失败 {failureMessage}, operation={_operationMessage}");
            return false;
        }

        fp delta = range * (fp)0.25f;
        propertyRow.DeltaValue = (float)delta;
        propertyRow.ApplyDeltaValue();

        if (!TryRequireOperationMessage("已增减", targetName, propertyKey, "delta") ||
            !TryRefreshAndFindPropertyRow(entity, propertyKey, out propertyRow, out failureMessage))
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentPropertySmokeLogPrefix} target={targetName} key={propertyKey} failed: 增减后刷新失败 {failureMessage}, operation={_operationMessage}");
            return false;
        }

        propertyRow.SetToMin();

        if (!TryRequireOperationMessage("设置为最小值", targetName, propertyKey, "setMin") ||
            !TryRefreshAndFindPropertyRow(entity, propertyKey, out propertyRow, out failureMessage))
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentPropertySmokeLogPrefix} target={targetName} key={propertyKey} failed: 最小值按钮后刷新失败 {failureMessage}, operation={_operationMessage}");
            return false;
        }

        propertyRow.SetToMax();

        if (!TryRequireOperationMessage("设置为最大值", targetName, propertyKey, "setMax") ||
            !TryRefreshAndFindPropertyRow(entity, propertyKey, out propertyRow, out failureMessage))
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentPropertySmokeLogPrefix} target={targetName} key={propertyKey} failed: 最大值按钮后刷新失败 {failureMessage}, operation={_operationMessage}");
            return false;
        }

        propertyRow.TargetMinValue = (float)(originalSnapshot.MinValue + range * (fp)0.1f);
        propertyRow.ApplySetMinValue();

        if (!TryRequireOperationMessage("最小边界", targetName, propertyKey, "setMinBound") ||
            !TryRefreshAndFindPropertyRow(entity, propertyKey, out propertyRow, out failureMessage))
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentPropertySmokeLogPrefix} target={targetName} key={propertyKey} failed: 最小边界设置后刷新失败 {failureMessage}, operation={_operationMessage}");
            return false;
        }

        propertyRow.TargetMaxValue = (float)(originalSnapshot.MaxValue - range * (fp)0.1f);
        propertyRow.ApplySetMaxValue();

        if (!TryRequireOperationMessage("最大边界", targetName, propertyKey, "setMaxBound") ||
            !TryRefreshAndFindPropertyRow(entity, propertyKey, out propertyRow, out failureMessage))
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentPropertySmokeLogPrefix} target={targetName} key={propertyKey} failed: 最大边界设置后刷新失败 {failureMessage}, operation={_operationMessage}");
            return false;
        }

        if (!propertyRow.TryRestoreTargetSnapshots(originalTargetSnapshots, out failureMessage))
        {
            GameLog.Error(GameLogChannel.AgentTest, $"{AgentPropertySmokeLogPrefix} target={targetName} key={propertyKey} failed: 恢复属性失败 {failureMessage}");
            return false;
        }

        RefreshData();
        GameLog.Info(GameLogChannel.AgentTest, $"{AgentPropertySmokeLogPrefix} target={targetName} entity={entity.EntityType} #{entity.EntityId} key={propertyKey} success: set/delta/min/max/bounds passed");
        return true;
    }

    /// <summary>
    /// 执行属性行失败分支烟测。
    /// </summary>
    /// <param name="entitySystem">当前运行世界的实体系统。</param>
    /// <param name="failureMessage">失败分支未按预期触发时的原因。</param>
    /// <returns>所有失败分支均产生预期提示时返回 true。</returns>
    private bool TryRunPropertyFailureSmoke(EntitySystem entitySystem, out string failureMessage)
    {
        failureMessage = null;
        BaseEntity targetEntity = entitySystem.ActorLocalEntity ?? entitySystem.HeroEntityList?.FirstOrDefault(entity =>
            entity != null && !entitySystem.IsActorEntity(entity));

        if (targetEntity == null)
        {
            failureMessage = "没有可用于失败分支烟测的实体";
            return false;
        }

        SelectEntity(targetEntity, "失败分支目标不存在");

        if (!TryGetFirstEditablePropertyRow(out EntityPropertyCheatRow validRow, out failureMessage))
        {
            return false;
        }

        validRow.SetValue = float.NaN;
        validRow.ApplySetValue();

        if (!_operationMessage.Contains("输入值必须是有限数"))
        {
            failureMessage = $"NaN 输入未产生有限数提示，operation={_operationMessage}";
            return false;
        }

        EntityPropertyCheatRow missingPropertyRow = new EntityPropertyCheatRow(
            CheatEntityOperationTargets.CreateSingle(targetEntity, "缺失属性烟测目标"),
            new EntityPropertyDebugInfo(PropertyKey.Null, 0, 0, 0),
            SetOperationMessage,
            GetEntityReferenceFailureMessage);
        missingPropertyRow.SetValue = 1f;
        missingPropertyRow.ApplySetValue();

        if (!_operationMessage.Contains("不存在") && !_operationMessage.Contains("无法读取"))
        {
            failureMessage = $"缺失属性未产生失败提示，operation={_operationMessage}";
            return false;
        }

        EntityPropertyCheatRow staleEntityRow = new EntityPropertyCheatRow(
            CheatEntityOperationTargets.CreateSingle(null, "失效实体烟测目标"),
            new EntityPropertyDebugInfo(validRow.Key, 0, 0, 1),
            SetOperationMessage,
            GetEntityReferenceFailureMessage);
        staleEntityRow.SetValue = 1f;
        staleEntityRow.ApplySetValue();

        if (!_operationMessage.Contains("实体引用为空"))
        {
            failureMessage = $"失效实体未产生失败提示，operation={_operationMessage}";
            return false;
        }

        RefreshData();
        return true;
    }

    /// <summary>
    /// 从当前属性行中获取第一个可执行编辑操作的属性。
    /// </summary>
    /// <param name="propertyRow">找到的属性编辑行。</param>
    /// <param name="failureMessage">无法找到属性行时的失败原因。</param>
    /// <returns>找到有效属性行时返回 true。</returns>
    private bool TryGetFirstEditablePropertyRow(out EntityPropertyCheatRow propertyRow, out string failureMessage)
    {
        propertyRow = _propertyRows?.FirstOrDefault(row => row != null && row.Key != PropertyKey.Null);
        failureMessage = propertyRow == null ? "选中实体没有可编辑属性行" : null;
        return propertyRow != null;
    }

    /// <summary>
    /// 刷新窗口数据并重新找到指定属性行。
    /// </summary>
    /// <param name="entity">属性所属实体。</param>
    /// <param name="propertyKey">需要重新查找的属性键。</param>
    /// <param name="propertyRow">刷新后找到的属性行。</param>
    /// <param name="failureMessage">无法重新找到属性行时的失败原因。</param>
    /// <returns>刷新后属性行仍存在时返回 true。</returns>
    private bool TryRefreshAndFindPropertyRow(
        BaseEntity entity,
        PropertyKey propertyKey,
        out EntityPropertyCheatRow propertyRow,
        out string failureMessage)
    {
        RefreshData();

        if (!TryGetSelectedEntity(out BaseEntity selectedEntity, out _) || selectedEntity != entity)
        {
            SelectEntity(entity, "属性烟测实体丢失");
        }

        propertyRow = _propertyRows?.FirstOrDefault(row => row != null && row.Key == propertyKey);
        failureMessage = propertyRow == null ? $"刷新后未找到属性行 {propertyKey}" : null;
        return propertyRow != null;
    }

    /// <summary>
    /// 检查最近操作消息是否包含预期关键词。
    /// </summary>
    /// <param name="expectedText">操作消息应包含的文本。</param>
    /// <param name="targetName">当前烟测目标名称。</param>
    /// <param name="propertyKey">当前烟测属性键。</param>
    /// <param name="stepName">当前烟测步骤名称。</param>
    /// <returns>操作消息包含预期文本时返回 true。</returns>
    private bool TryRequireOperationMessage(string expectedText, string targetName, PropertyKey propertyKey, string stepName)
    {
        if (_operationMessage.Contains(expectedText))
        {
            return true;
        }

        GameLog.Error(GameLogChannel.AgentTest, $"{AgentPropertySmokeLogPrefix} target={targetName} key={propertyKey} step={stepName} failed: operation message missing '{expectedText}', operation={_operationMessage}");
        return false;
    }

    /// <summary>
    /// 将烟测修改过的属性边界和当前值恢复到原始快照。
    /// </summary>
    /// <param name="entity">需要恢复属性的实体。</param>
    /// <param name="propertyKey">需要恢复的属性键。</param>
    /// <param name="snapshot">烟测前读取到的属性快照。</param>
    /// <param name="failureMessage">恢复失败时的原因。</param>
    /// <returns>属性边界和当前值恢复成功时返回 true。</returns>
    private static bool TryRestorePropertySnapshot(
        BaseEntity entity,
        PropertyKey propertyKey,
        EntityPropertyCheatRow.EntityPropertySnapshot snapshot,
        out string failureMessage)
    {
        failureMessage = null;

        if (entity == null)
        {
            failureMessage = "实体引用为空";
            return false;
        }

        if (!entity.TryGetPropertyValue(propertyKey, PropertyValueType.Min, out fp currentMin) ||
            !entity.TryGetPropertyValue(propertyKey, PropertyValueType.Max, out fp currentMax))
        {
            failureMessage = $"{propertyKey} 不存在或无法读取边界";
            return false;
        }

        if (!entity.TryChangePropertyValue(propertyKey, PropertyValueType.Max, snapshot.MaxValue - currentMax, out _) ||
            !entity.TryChangePropertyValue(propertyKey, PropertyValueType.Min, snapshot.MinValue - currentMin, out _))
        {
            failureMessage = $"{propertyKey} 边界恢复失败";
            return false;
        }

        if (!entity.TrySetProperty(propertyKey, snapshot.CurrentValue))
        {
            failureMessage = $"{propertyKey} 当前值恢复失败";
            return false;
        }

        return true;
    }

    /// <summary>
    /// 尝试读取当前运行中的世界。
    /// </summary>
    /// <param name="world">读取到的当前运行世界。</param>
    /// <param name="unavailableMessage">世界不可用时展示给编辑器用户的原因。</param>
    /// <returns>当前处于 Play Mode 且世界可用时返回 true。</returns>
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

    /// <summary>
    /// 尝试读取当前世界的实体系统。
    /// </summary>
    /// <param name="entitySystem">读取到的实体系统。</param>
    /// <param name="unavailableMessage">实体系统不可用时展示给编辑器用户的原因。</param>
    /// <returns>实体系统可用于实体查询时返回 true。</returns>
    private static bool TryGetCurrentEntitySystem(out EntitySystem entitySystem, out string unavailableMessage)
    {
        entitySystem = null;

        if (!TryGetCurrentWorld(out BaseWorld world, out unavailableMessage))
        {
            return false;
        }

        entitySystem = world.GetSystem<EntitySystem>();

        if (entitySystem == null)
        {
            unavailableMessage = "当前世界未找到实体系统";
            return false;
        }

        return true;
    }

    /// <summary>
    /// 清空运行时显示状态，并写入空状态提示。
    /// </summary>
    /// <param name="message">窗口中显示的空状态原因。</param>
    private void ClearRuntimeState(string message)
    {
        _worldState = message;
        SetOperationMessage(message, false);
        _localTick = 0;
        _authorityTick = 0;
        _entityCount = 0;
        _localPredictedEntityInfo = message;
        _localPredictedEntityState = message;
        _filteredEntities.Clear();
        SelectedEntityView = null;
        _selectedEntityUnavailableMessage = null;
        _selectedEntityInfo = "未选择实体";
        SetOperationTabStates(message);
        _propertyRows.Clear();
    }

    /// <summary>
    /// 判断实体是否满足当前窗口筛选条件。
    /// </summary>
    /// <param name="entity">准备检查的实体。</param>
    /// <returns>满足筛选条件时返回 true。</returns>
    private bool IsEntityMatched(BaseEntity entity)
    {
        if (entity == null)
        {
            return false;
        }

        if (OnlySurvivalEntity && entity.EntityState != EntityState.Survival)
        {
            return false;
        }

        if (TryParseFilter(EntityTypeFilter, out EntityType entityType) && entity.EntityType != entityType)
        {
            return false;
        }

        if (TryParseFilter(EntityUpdateTypeFilter, out EntityUpdateType updateType) && entity.EntityUpdateType != updateType)
        {
            return false;
        }

        if (ConfigIdFilter > 0 && entity.ConfigId != ConfigIdFilter)
        {
            return false;
        }

        if (EntityIdFilter > 0 && entity.EntityId != EntityIdFilter)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 尝试解析筛选器文本，全部选项会返回 false 表示不启用筛选。
    /// </summary>
    /// <typeparam name="T">需要解析的枚举类型。</typeparam>
    /// <param name="filterValue">筛选器文本。</param>
    /// <param name="value">解析后的枚举值。</param>
    /// <returns>筛选器需要生效时返回 true。</returns>
    private static bool TryParseFilter<T>(string filterValue, out T value) where T : struct, Enum
    {
        value = default;
        return !string.IsNullOrEmpty(filterValue) && filterValue != AllFilterName && Enum.TryParse(filterValue, out value);
    }

    /// <summary>
    /// 在刷新列表后优先锁定当前世界的本地预测实体，并在调试切换后尽量保留同一个实体选中项。
    /// </summary>
    /// <param name="entitySystem">当前运行世界的实体系统。</param>
    private void RefreshSelectedEntityReference(EntitySystem entitySystem)
    {
        BaseEntity localPredictedEntity = entitySystem?.ActorLocalEntity;
        _localPredictedEntityInfo = BuildLocalPredictedEntityInfo(entitySystem, localPredictedEntity);

        if (!AllowDebugEntitySelection)
        {
            TrySelectLocalPredictedEntity(entitySystem, localPredictedEntity);
            return;
        }

        RefreshLocalPredictedEntityState(entitySystem, localPredictedEntity);

        int selectedEntityId = SelectedEntityView?.EntityId ?? 0;
        EntityUpdateType? selectedUpdateType = SelectedEntityView?.UpdateType;

        if (selectedEntityId == 0)
        {
            TrySelectLocalPredictedEntity(entitySystem, localPredictedEntity);
            return;
        }

        int selectedFingerprints = SelectedEntityView?.Fingerprints ?? 0;
        EntityDebugViewData refreshedEntity = _filteredEntities.FirstOrDefault(entity =>
            entity.EntityId == selectedEntityId &&
            entity.UpdateType == selectedUpdateType &&
            entity.Fingerprints == selectedFingerprints);

        if (refreshedEntity == null)
        {
            _selectedEntityUnavailableMessage = $"选中实体已失效或不在当前筛选结果中：#{selectedEntityId}";
            SetOperationMessage(_selectedEntityUnavailableMessage, false);
        }
        else
        {
            _selectedEntityUnavailableMessage = null;
        }

        SelectedEntityView = refreshedEntity;
    }

    /// <summary>
    /// 尝试将当前实体上下文自动切换到本地预测实体。
    /// </summary>
    /// <param name="entitySystem">当前运行世界的实体系统。</param>
    /// <param name="localPredictedEntity">实体系统暴露的本地预测实体。</param>
    /// <returns>成功选中本地预测实体时返回 true。</returns>
    private bool TrySelectLocalPredictedEntity(EntitySystem entitySystem, BaseEntity localPredictedEntity)
    {
        if (localPredictedEntity == null)
        {
            _localPredictedEntityState = "当前世界尚未注册本地预测实体";
            _selectedEntityUnavailableMessage = _localPredictedEntityState;
            SelectedEntityView = null;
            return false;
        }

        string failureMessage = GetEntityReferenceFailureMessage(localPredictedEntity, entitySystem);

        if (failureMessage != null)
        {
            _localPredictedEntityState = failureMessage;
            _selectedEntityUnavailableMessage = failureMessage;
            SelectedEntityView = null;
            return false;
        }

        EntityDebugViewData localPredictedView = _filteredEntities.FirstOrDefault(view => IsEntityViewMatched(view, localPredictedEntity));

        if (localPredictedView == null)
        {
            localPredictedView = new EntityDebugViewData(localPredictedEntity);
        }

        if (!IsEntityViewMatched(SelectedEntityView, localPredictedEntity))
        {
            SelectedEntityView = localPredictedView;
            SetOperationMessage($"已默认选中本地预测实体：{SelectedEntityView}", false);
        }
        else
        {
            SelectedEntityView = localPredictedView;
        }

        _localPredictedEntityState = "本地预测实体可用";
        _selectedEntityUnavailableMessage = null;
        return true;
    }

    /// <summary>
    /// 仅刷新本地预测实体状态文本，不改变调试模式下手动选中的实体上下文。
    /// </summary>
    /// <param name="entitySystem">当前运行世界的实体系统。</param>
    /// <param name="localPredictedEntity">实体系统暴露的本地预测实体。</param>
    private void RefreshLocalPredictedEntityState(EntitySystem entitySystem, BaseEntity localPredictedEntity)
    {
        if (localPredictedEntity == null)
        {
            _localPredictedEntityState = "当前世界尚未注册本地预测实体";
            return;
        }

        _localPredictedEntityState = GetEntityReferenceFailureMessage(localPredictedEntity, entitySystem) ?? "本地预测实体可用";
    }

    /// <summary>
    /// 选中指定实体，并刷新属性数据。
    /// </summary>
    /// <param name="entity">需要选中的实体。</param>
    /// <param name="notFoundMessage">实体不存在时显示的提示。</param>
    private void SelectEntity(BaseEntity entity, string notFoundMessage)
    {
        RefreshData();

        if (entity == null)
        {
            _operationMessage = notFoundMessage;
            return;
        }

        SelectedEntityView = _filteredEntities.FirstOrDefault(view => IsEntityViewMatched(view, entity));

        if (SelectedEntityView == null)
        {
            SelectedEntityView = new EntityDebugViewData(entity);
        }

        _operationMessage = $"已选中：{SelectedEntityView}";
        RefreshSelectedEntityInfoAndProperties();
    }

    /// <summary>
    /// 尝试获取当前可操作的选中实体。
    /// </summary>
    /// <param name="entity">可安全执行编辑器金手指操作的实体。</param>
    /// <param name="failureMessage">无法获取实体时展示给用户的原因。</param>
    /// <returns>选中实体仍在当前实体系统中时返回 true。</returns>
    private bool TryGetSelectedEntity(out BaseEntity entity, out string failureMessage)
    {
        entity = null;

        if (!TryGetCurrentEntitySystem(out EntitySystem entitySystem, out failureMessage))
        {
            return false;
        }

        if (SelectedEntityView == null)
        {
            failureMessage = _selectedEntityUnavailableMessage ?? "未选择实体";
            return false;
        }

        if (!TryResolveEntityDebugView(SelectedEntityView, entitySystem, out entity))
        {
            failureMessage = $"选中实体已失效或不在当前实体系统中：{SelectedEntityView}";
            _selectedEntityUnavailableMessage = failureMessage;
            return false;
        }

        failureMessage = GetEntityReferenceFailureMessage(entity, entitySystem);
        _selectedEntityUnavailableMessage = failureMessage;
        return failureMessage == null;
    }

    /// <summary>
    /// 判断实体快照是否对应当前运行时实体。
    /// </summary>
    /// <param name="viewData">实体列表中的轻量快照。</param>
    /// <param name="entity">当前运行时实体。</param>
    /// <returns>快照与实体的稳定标识一致时返回 true。</returns>
    private static bool IsEntityViewMatched(EntityDebugViewData viewData, BaseEntity entity)
    {
        return viewData != null &&
               entity != null &&
               viewData.EntityId == entity.EntityId &&
               viewData.Fingerprints == entity.EntityFingerprints &&
               viewData.UpdateType == entity.EntityUpdateType;
    }

    /// <summary>
    /// 根据实体下拉快照在当前实体系统中重新解析运行时实体。
    /// </summary>
    /// <param name="viewData">实体下拉保存的轻量快照。</param>
    /// <param name="entitySystem">当前世界的实体系统。</param>
    /// <param name="entity">解析到的运行时实体。</param>
    /// <returns>当前实体系统中仍存在匹配实体时返回 true。</returns>
    private static bool TryResolveEntityDebugView(EntityDebugViewData viewData, EntitySystem entitySystem, out BaseEntity entity)
    {
        entity = null;

        if (viewData == null || entitySystem == null)
        {
            return false;
        }

        IReadOnlyList<BaseEntity> entities = entitySystem.GetExecutingEntitiesForDebug();

        for (int i = 0; i < entities.Count; i++)
        {
            if (IsEntityViewMatched(viewData, entities[i]))
            {
                entity = entities[i];
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 尝试解析金手指操作需要同步修改的实体集合，优先把本地预测实体和对应权威实体配对。
    /// </summary>
    /// <param name="sourceEntity">用户当前选中的运行时实体。</param>
    /// <param name="entitySystem">当前世界的实体系统。</param>
    /// <param name="targets">解析出的实体操作目标集合。</param>
    /// <param name="failureMessage">无法安全解析配对实体时展示给用户的原因。</param>
    /// <returns>实体集合可用于同步执行金手指操作时返回 true。</returns>
    private static bool TryResolveCheatEntityOperationTargets(
        BaseEntity sourceEntity,
        EntitySystem entitySystem,
        out CheatEntityOperationTargets targets,
        out string failureMessage)
    {
        targets = default;

        if (sourceEntity == null)
        {
            failureMessage = "实体引用为空";
            return false;
        }

        if (entitySystem == null)
        {
            failureMessage = "当前世界未找到实体系统";
            return false;
        }

        string sourceLabel = BuildEntityOperationLabel(sourceEntity);

        if (!TryValidateOperationTarget(sourceEntity, entitySystem, "选中实体", out failureMessage))
        {
            return false;
        }

        // 优先走世界会话的实体同步策略，单机模式下不再强制要求 Local/Authority 配对
        if (entitySystem.TryResolveCheatOperationTargets(sourceEntity, out IReadOnlyList<BaseEntity> resolvedTargets, out failureMessage))
        {
            List<BaseEntity> validatedTargets = new List<BaseEntity>(resolvedTargets.Count);
            for (int i = 0; i < resolvedTargets.Count; i++)
            {
                BaseEntity targetEntity = resolvedTargets[i];
                string targetLabel = BuildEntityOperationLabel(targetEntity);
                if (!TryValidateOperationTarget(targetEntity, entitySystem, targetLabel, out failureMessage))
                {
                    return false;
                }

                validatedTargets.Add(targetEntity);
            }

            if (validatedTargets.Count == 0)
            {
                failureMessage = "未解析到可操作的实体目标";
                return false;
            }

            if (validatedTargets.Count == 1)
            {
                targets = CheatEntityOperationTargets.CreateSingle(validatedTargets[0], sourceLabel);
            }
            else
            {
                targets = CheatEntityOperationTargets.CreatePaired(
                    validatedTargets[0],
                    validatedTargets[1],
                    $"{BuildEntityOperationLabel(validatedTargets[0])} + {BuildEntityOperationLabel(validatedTargets[1])}");
            }

            failureMessage = null;
            return true;
        }

        if (sourceEntity.ForecastEntityType == ForecastEntityType.DynamicEntity)
        {
            return TryResolveDynamicCheatTargets(sourceEntity, entitySystem, sourceLabel, out targets, out failureMessage);
        }

        if (sourceEntity.EntityUpdateType == EntityUpdateType.LocalEntity && sourceEntity == entitySystem.ActorLocalEntity)
        {
            if (!TryValidateOperationTarget(entitySystem.ActorAuthorityEntity, entitySystem, "主角权威实体", out failureMessage))
            {
                failureMessage = $"无法解析本地预测实体对应的权威实体：{failureMessage}";
                return false;
            }

            targets = CheatEntityOperationTargets.CreatePaired(
                sourceEntity,
                entitySystem.ActorAuthorityEntity,
                $"{sourceLabel} + {BuildEntityOperationLabel(entitySystem.ActorAuthorityEntity)}");
            failureMessage = null;
            return true;
        }

        if (sourceEntity.EntityUpdateType == EntityUpdateType.AuthorityEntity && sourceEntity == entitySystem.ActorAuthorityEntity)
        {
            if (!TryValidateOperationTarget(entitySystem.ActorLocalEntity, entitySystem, "主角本地预测实体", out failureMessage))
            {
                failureMessage = $"无法解析权威实体对应的本地预测实体：{failureMessage}";
                return false;
            }

            targets = CheatEntityOperationTargets.CreatePaired(
                sourceEntity,
                entitySystem.ActorLocalEntity,
                $"{sourceLabel} + {BuildEntityOperationLabel(entitySystem.ActorLocalEntity)}");
            failureMessage = null;
            return true;
        }

        targets = CheatEntityOperationTargets.CreateSingle(sourceEntity, sourceLabel);
        failureMessage = null;
        return true;
    }

    /// <summary>
    /// 按动态实体指纹解析本地预测实体和权威实体，避免只修改一侧造成预测校验差异。
    /// </summary>
    /// <param name="sourceEntity">用户当前选中的动态实体。</param>
    /// <param name="entitySystem">当前世界的实体系统。</param>
    /// <param name="sourceLabel">源实体的摘要文本。</param>
    /// <param name="targets">解析出的动态实体配对目标。</param>
    /// <param name="failureMessage">无法找到另一侧动态实体时展示给用户的原因。</param>
    /// <returns>动态实体配对目标可用于同步操作时返回 true。</returns>
    private static bool TryResolveDynamicCheatTargets(
        BaseEntity sourceEntity,
        EntitySystem entitySystem,
        string sourceLabel,
        out CheatEntityOperationTargets targets,
        out string failureMessage)
    {
        targets = default;
        BaseEntity localEntity = sourceEntity.EntityUpdateType == EntityUpdateType.LocalEntity
            ? sourceEntity
            : entitySystem.GetDynamicLocalEntity<BaseEntity>(sourceEntity.EntityFingerprints);
        BaseEntity authorityEntity = sourceEntity.EntityUpdateType == EntityUpdateType.AuthorityEntity
            ? sourceEntity
            : entitySystem.GetDynamicAuthorityEntity<BaseEntity>(sourceEntity.EntityFingerprints);

        if (!TryValidateOperationTarget(localEntity, entitySystem, "动态本地预测实体", out failureMessage))
        {
            failureMessage = $"{sourceLabel} 无法解析对应动态本地预测实体：{failureMessage}";
            return false;
        }

        if (!TryValidateOperationTarget(authorityEntity, entitySystem, "动态权威实体", out failureMessage))
        {
            failureMessage = $"{sourceLabel} 无法解析对应动态权威实体：{failureMessage}";
            return false;
        }

        targets = CheatEntityOperationTargets.CreatePaired(
            localEntity,
            authorityEntity,
            $"{BuildEntityOperationLabel(localEntity)} + {BuildEntityOperationLabel(authorityEntity)}");
        failureMessage = null;
        return true;
    }

    /// <summary>
    /// 校验某个同步操作目标是否仍在当前实体系统中且处于存活状态。
    /// </summary>
    /// <param name="entity">准备执行金手指操作的实体。</param>
    /// <param name="entitySystem">当前世界的实体系统。</param>
    /// <param name="targetName">提示中展示的目标侧名称。</param>
    /// <param name="failureMessage">目标不可用时展示给用户的原因。</param>
    /// <returns>目标实体可被安全修改时返回 true。</returns>
    private static bool TryValidateOperationTarget(BaseEntity entity, EntitySystem entitySystem, string targetName, out string failureMessage)
    {
        if (entity == null)
        {
            failureMessage = $"{targetName}不存在";
            return false;
        }

        failureMessage = GetEntityReferenceFailureMessage(entity, entitySystem);

        if (failureMessage != null)
        {
            failureMessage = $"{targetName}{failureMessage}";
            return false;
        }

        if (entity.EntityState != EntityState.Survival)
        {
            failureMessage = $"{targetName}不是存活状态：{entity.EntityState}";
            return false;
        }

        return true;
    }

    /// <summary>
    /// 为同步操作提示构建简短实体标签。
    /// </summary>
    /// <param name="entity">需要展示的实体。</param>
    /// <returns>包含类型、ID、更新域和指纹的实体摘要。</returns>
    private static string BuildEntityOperationLabel(BaseEntity entity)
    {
        return entity == null
            ? "空实体"
            : $"{entity.EntityType} #{entity.EntityId}({entity.EntityUpdateType}, Fingerprints:{entity.EntityFingerprints})";
    }

    /// <summary>
    /// 获取实体引用不可用时的失败提示。
    /// </summary>
    /// <param name="entity">需要校验的运行时实体引用。</param>
    /// <returns>实体引用不可操作时的原因；引用有效时为 null。</returns>
    private string GetEntityReferenceFailureMessage(BaseEntity entity)
    {
        if (!TryGetCurrentEntitySystem(out EntitySystem entitySystem, out string failureMessage))
        {
            return failureMessage;
        }

        return GetEntityReferenceFailureMessage(entity, entitySystem);
    }

    /// <summary>
    /// 基于已读取的实体系统校验实体引用。
    /// </summary>
    /// <param name="entity">需要校验的运行时实体引用。</param>
    /// <param name="entitySystem">当前世界的实体系统。</param>
    /// <returns>实体引用不可操作时的原因；引用有效时为 null。</returns>
    private static string GetEntityReferenceFailureMessage(BaseEntity entity, EntitySystem entitySystem)
    {
        if (entity == null)
        {
            return "实体引用为空";
        }

        IReadOnlyList<BaseEntity> entities = entitySystem.GetExecutingEntitiesForDebug();
        bool entityExists = entities.Any(currentEntity => currentEntity == entity);
        return entityExists ? null : $"实体引用已失效：{entity.EntityType} #{entity.EntityId}";
    }

    /// <summary>
    /// 在当前实体系统中查找适合执行 Buff 添加烟测的存活实体。
    /// </summary>
    /// <param name="entitySystem">当前运行世界的实体系统。</param>
    /// <param name="targetEntity">找到的目标实体。</param>
    /// <param name="failureMessage">没有可用实体时展示给自动化日志的原因。</param>
    /// <returns>找到带 BuffComponent 的存活实体时返回 true。</returns>
    private static bool TryFindBuffSmokeTarget(EntitySystem entitySystem, out BaseEntity targetEntity, out string failureMessage)
    {
        targetEntity = null;
        failureMessage = null;

        if (IsAgentSmokeTargetUsable(entitySystem?.ActorLocalEntity) &&
            entitySystem.ActorLocalEntity.GetComponent<BuffComponent>() != null)
        {
            targetEntity = entitySystem.ActorLocalEntity;
            return true;
        }

        IReadOnlyList<BaseEntity> entities = entitySystem.GetExecutingEntitiesForDebug();

        for (int i = 0; i < entities.Count; i++)
        {
            BaseEntity entity = entities[i];

            if (entity?.EntityState == EntityState.Survival && entity.GetComponent<BuffComponent>() != null)
            {
                targetEntity = entity;
                return true;
            }
        }

        failureMessage = "当前世界没有带 BuffComponent 的存活实体";
        return false;
    }

    /// <summary>
    /// 从运行时数据表中查找可由 BuffComponent 创建的有效 Buff 配置 ID。
    /// </summary>
    /// <param name="buffConfigId">找到的 Buff 配置 ID。</param>
    /// <param name="failureMessage">没有可用配置时展示给自动化日志的原因。</param>
    /// <returns>找到监听事件不为空的 Buff 配置时返回 true。</returns>
    private static bool TryFindValidBuffConfigId(out int buffConfigId, out string failureMessage)
    {
        buffConfigId = 0;
        failureMessage = null;

        if (GameEntry.DataTable == null)
        {
            failureMessage = "运行时数据表尚不可用";
            return false;
        }

        List<BuffAssetsConfig> buffConfigs = GameEntry.DataTable.GetAllDataTable<BuffAssetsConfig>();

        if (buffConfigs == null)
        {
            failureMessage = "Buff 配置表尚不可用";
            return false;
        }

        for (int i = 0; i < buffConfigs.Count; i++)
        {
            BuffAssetsConfig config = buffConfigs[i];

            if (config != null && config.assetsId > 0 && config.gameEventType != BattleExecuteTiming.Null)
            {
                buffConfigId = config.assetsId;
                return true;
            }
        }

        failureMessage = "未找到监听事件不为空的 Buff 配置";
        return false;
    }

    /// <summary>
    /// 在当前实体系统中查找适合执行子弹创建烟测的存活父实体。
    /// </summary>
    /// <param name="entitySystem">当前运行世界的实体系统。</param>
    /// <param name="targetEntity">找到的父实体。</param>
    /// <param name="failureMessage">没有可用父实体时展示给自动化日志的原因。</param>
    /// <returns>找到带 BulletControlCompinent 的存活实体时返回 true。</returns>
    private static bool TryFindBulletSmokeTarget(EntitySystem entitySystem, out BaseEntity targetEntity, out string failureMessage)
    {
        targetEntity = null;
        failureMessage = null;

        if (IsAgentSmokeTargetUsable(entitySystem?.ActorLocalEntity) &&
            entitySystem.ActorLocalEntity.GetComponent<BulletControlCompinent>() != null)
        {
            targetEntity = entitySystem.ActorLocalEntity;
            return true;
        }

        IReadOnlyList<BaseEntity> entities = entitySystem.GetExecutingEntitiesForDebug();

        for (int i = 0; i < entities.Count; i++)
        {
            BaseEntity entity = entities[i];

            if (entity?.EntityState == EntityState.Survival &&
                entity.GetComponent<BulletControlCompinent>() != null)
            {
                targetEntity = entity;
                return true;
            }
        }

        failureMessage = "当前世界没有带 BulletControlCompinent 的存活实体";
        return false;
    }

    /// <summary>
    /// 判断实体是否可作为 Agent 金手指烟测目标，避免使用空引用或非存活实体。
    /// </summary>
    /// <param name="entity">准备作为烟测目标的实体。</param>
    /// <returns>实体存在且处于存活状态时返回 true。</returns>
    private static bool IsAgentSmokeTargetUsable(BaseEntity entity)
    {
        return entity?.EntityState == EntityState.Survival;
    }

    /// <summary>
    /// 从运行时数据表中查找可由 BulletEntity 创建的有效子弹配置 ID。
    /// </summary>
    /// <param name="bulletConfigId">找到的子弹配置 ID。</param>
    /// <param name="failureMessage">没有可用配置时展示给自动化日志的原因。</param>
    /// <returns>找到子弹配置时返回 true。</returns>
    private static bool TryFindValidBulletConfigId(out int bulletConfigId, out string failureMessage)
    {
        bulletConfigId = 0;
        failureMessage = null;

        if (GameEntry.DataTable == null)
        {
            failureMessage = "运行时数据表尚不可用";
            return false;
        }

        List<BulletAssetsConfig> bulletConfigs = GameEntry.DataTable.GetAllDataTable<BulletAssetsConfig>();

        if (bulletConfigs == null)
        {
            failureMessage = "子弹配置表尚不可用";
            return false;
        }

        for (int i = 0; i < bulletConfigs.Count; i++)
        {
            BulletAssetsConfig config = bulletConfigs[i];

            if (config != null && config.assetsId > 0)
            {
                bulletConfigId = config.assetsId;
                return true;
            }
        }

        failureMessage = "未找到有效子弹配置";
        return false;
    }

    /// <summary>
    /// 查找目标实体通过指定配置创建出的 BuffEntity。
    /// </summary>
    /// <param name="entitySystem">当前运行世界的实体系统。</param>
    /// <param name="ownerEntity">Buff 持有者实体。</param>
    /// <param name="buffConfigId">Buff 配置 ID。</param>
    /// <returns>找到匹配 BuffEntity 时返回实体引用。</returns>
    private static BuffEntity FindBuffEntity(EntitySystem entitySystem, BaseEntity ownerEntity, int buffConfigId)
    {
        IReadOnlyList<BaseEntity> entities = entitySystem.GetExecutingEntitiesForDebug();

        for (int i = 0; i < entities.Count; i++)
        {
            if (entities[i] is BuffEntity buffEntity &&
                buffEntity.ParentEntity == ownerEntity &&
                buffEntity.ConfigId == buffConfigId &&
                buffEntity.EntityState == EntityState.Survival)
            {
                return buffEntity;
            }
        }

        return null;
    }

    /// <summary>
    /// 统计目标实体上指定配置的存活 BuffEntity 数量。
    /// </summary>
    /// <param name="entitySystem">当前运行世界的实体系统。</param>
    /// <param name="ownerEntity">Buff 持有者实体。</param>
    /// <param name="buffConfigId">Buff 配置 ID。</param>
    /// <returns>匹配条件的存活 BuffEntity 数量。</returns>
    private static int CountBuffEntities(EntitySystem entitySystem, BaseEntity ownerEntity, int buffConfigId)
    {
        IReadOnlyList<BaseEntity> entities = entitySystem.GetExecutingEntitiesForDebug();
        int count = 0;

        for (int i = 0; i < entities.Count; i++)
        {
            if (entities[i] is BuffEntity buffEntity &&
                buffEntity.ParentEntity == ownerEntity &&
                buffEntity.ConfigId == buffConfigId &&
                buffEntity.EntityState == EntityState.Survival)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 查找目标父实体最近创建出的存活 BulletEntity。
    /// </summary>
    /// <param name="entitySystem">当前运行世界的实体系统。</param>
    /// <param name="parentEntity">子弹父实体。</param>
    /// <returns>找到匹配 BulletEntity 时返回实体引用。</returns>
    private static BulletEntity FindLatestBulletEntity(EntitySystem entitySystem, BaseEntity parentEntity)
    {
        IReadOnlyList<BaseEntity> entities = entitySystem.GetExecutingEntitiesForDebug();

        for (int i = entities.Count - 1; i >= 0; i--)
        {
            if (entities[i] is BulletEntity bulletEntity &&
                bulletEntity.ParentEntity == parentEntity &&
                bulletEntity.EntityState == EntityState.Survival)
            {
                return bulletEntity;
            }
        }

        return null;
    }

    /// <summary>
    /// 统计目标父实体当前拥有的存活 BulletEntity 数量。
    /// </summary>
    /// <param name="entitySystem">当前运行世界的实体系统。</param>
    /// <param name="parentEntity">子弹父实体。</param>
    /// <returns>匹配条件的存活 BulletEntity 数量。</returns>
    private static int CountBulletEntities(EntitySystem entitySystem, BaseEntity parentEntity)
    {
        IReadOnlyList<BaseEntity> entities = entitySystem.GetExecutingEntitiesForDebug();
        int count = 0;

        for (int i = 0; i < entities.Count; i++)
        {
            if (entities[i] is BulletEntity bulletEntity &&
                bulletEntity.ParentEntity == parentEntity &&
                bulletEntity.EntityState == EntityState.Survival)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 获取当前可添加 Buff 的选中实体与 Buff 组件。
    /// </summary>
    /// <param name="entity">通过校验的选中实体。</param>
    /// <param name="buffComponent">选中实体上的 Buff 组件。</param>
    /// <param name="failureMessage">无法添加 Buff 时展示给用户的原因。</param>
    /// <returns>实体和 Buff 配置都可用于调用运行时 Buff 创建入口时返回 true。</returns>
    private bool TryGetSelectedEntityReadyForBuff(out CheatEntityOperationTargets targets, out string failureMessage)
    {
        targets = default;

        if (!TryGetSelectedEntity(out BaseEntity selectedEntity, out failureMessage))
        {
            return false;
        }

        if (!TryGetBuffConfig(BuffConfigId, out BuffAssetsConfig buffConfig, out failureMessage))
        {
            return false;
        }

        if (buffConfig.gameEventType == BattleExecuteTiming.Null)
        {
            failureMessage = $"Buff 配置 {BuffConfigId} 的监听事件为空，运行时不会创建 Buff";
            return false;
        }

        if (!TryGetCurrentEntitySystem(out EntitySystem entitySystem, out failureMessage))
        {
            return false;
        }

        if (!TryResolveCheatEntityOperationTargets(selectedEntity, entitySystem, out targets, out failureMessage))
        {
            return false;
        }

        for (int i = 0; i < targets.Entities.Count; i++)
        {
            BaseEntity targetEntity = targets.Entities[i];

            if (targetEntity.GetComponent<BuffComponent>() == null)
            {
                failureMessage = $"同步目标缺少 BuffComponent：{BuildEntityOperationLabel(targetEntity)}";
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 从运行时数据表读取 Buff 配置。
    /// </summary>
    /// <param name="buffConfigId">用户输入的 Buff 配置 ID。</param>
    /// <param name="buffConfig">读取到的 Buff 配置。</param>
    /// <param name="failureMessage">配置不可用时展示给用户的原因。</param>
    /// <returns>Buff 配置存在且数据表可访问时返回 true。</returns>
    private static bool TryGetBuffConfig(int buffConfigId, out BuffAssetsConfig buffConfig, out string failureMessage)
    {
        buffConfig = null;
        failureMessage = null;

        if (buffConfigId <= 0)
        {
            failureMessage = "请输入大于 0 的 Buff 配置 ID";
            return false;
        }

        if (GameEntry.DataTable == null)
        {
            failureMessage = "运行时数据表尚不可用";
            return false;
        }

        buffConfig = GameEntry.DataTable.GetDataTable<BuffAssetsConfig>(buffConfigId);

        if (buffConfig == null)
        {
            failureMessage = $"未找到 Buff 配置：{buffConfigId}";
            return false;
        }

        return true;
    }

    /// <summary>
    /// 根据当前选中实体刷新基础信息和属性表。
    /// </summary>
    private void RefreshSelectedEntityInfoAndProperties()
    {
        if (!TryGetSelectedEntity(out BaseEntity entity, out string failureMessage))
        {
            _selectedEntityInfo = failureMessage;
            SetOperationTabStates(failureMessage);
            _propertyRows.Clear();
            SetOperationMessage(failureMessage, false);
            return;
        }

        _selectedEntityInfo =
            $"EntityId: {entity.EntityId}\n" +
            $"Fingerprints: {entity.EntityFingerprints}\n" +
            $"ConfigId: {entity.ConfigId}\n" +
            $"EntityType: {entity.EntityType}\n" +
            $"Camp: {entity.CampEnum}\n" +
            $"UpdateType: {entity.EntityUpdateType}\n" +
            $"State: {entity.EntityState}\n" +
            $"Position: {FormatFp3(entity.transform.Position)}\n" +
            $"Rotation: {FormatFp3(entity.transform.EulerAngles)}\n" +
            $"ParentEntity: {FormatParentEntity(entity.ParentEntity)}";

        if (!TryGetCurrentEntitySystem(out EntitySystem entitySystem, out string targetsFailureMessage) ||
            !TryResolveCheatEntityOperationTargets(entity, entitySystem, out CheatEntityOperationTargets operationTargets, out targetsFailureMessage))
        {
            _selectedEntityInfo += $"\n同步目标: {targetsFailureMessage}";
            SetOperationTabStates(targetsFailureMessage);
            _propertyRows.Clear();
            return;
        }

        _selectedEntityInfo += $"\n同步目标: {operationTargets.BuildSummary()}";
        SetOperationTabStates($"当前操作目标：{operationTargets.BuildSummary()}");
        _propertyRows = entity.GetPropertyDebugInfos()
            .OrderBy(info => info.Key)
            .Select(info => new EntityPropertyCheatRow(operationTargets, info, SetOperationMessage, GetEntityReferenceFailureMessage))
            .ToList();
    }

    /// <summary>
    /// 同步 Buff、属性和子弹页签的选中实体依赖状态，确保切换页签时能看到当前不可操作原因。
    /// </summary>
    /// <param name="message">展示给各功能页签的当前实体状态或失败原因。</param>
    private void SetOperationTabStates(string message)
    {
        string stateMessage = string.IsNullOrWhiteSpace(message) ? "未选择实体" : message;
        _buffOperationState = stateMessage;
        _propertyOperationState = stateMessage;
        _bulletOperationState = stateMessage;
    }

    /// <summary>
    /// 写入操作结果提示，必要时同步刷新窗口数据。
    /// </summary>
    /// <param name="message">本次操作结果。</param>
    /// <param name="refreshAfterSet">写入提示后是否刷新实体和属性快照。</param>
    private void SetOperationMessage(string message, bool refreshAfterSet = true)
    {
        _operationMessage = message;

        if (refreshAfterSet)
        {
            RefreshData();
        }
    }

    /// <summary>
    /// 格式化定点三维向量。
    /// </summary>
    /// <param name="value">定点三维向量。</param>
    /// <returns>保留三位小数的字符串。</returns>
    private static string FormatFp3(fp3 value)
    {
        return $"({(float)value.x:0.###}, {(float)value.y:0.###}, {(float)value.z:0.###})";
    }

    /// <summary>
    /// 将运行时定点三维向量转换为编辑器 Vector3 输入值。
    /// </summary>
    /// <param name="value">运行时定点三维向量。</param>
    /// <returns>编辑器可显示和编辑的 Vector3。</returns>
    private static Vector3 Fp3ToVector3(fp3 value)
    {
        return new Vector3((float)value.x, (float)value.y, (float)value.z);
    }

    /// <summary>
    /// 格式化父实体基础信息。
    /// </summary>
    /// <param name="parentEntity">父实体引用。</param>
    /// <returns>父实体描述；不存在时返回空提示。</returns>
    private static string FormatParentEntity(BaseEntity parentEntity)
    {
        return parentEntity == null ? "无" : $"{parentEntity.EntityType} #{parentEntity.EntityId} ({parentEntity.EntityUpdateType})";
    }

    /// <summary>
    /// 构建本地预测实体及其关联权威实体的摘要文本。
    /// </summary>
    /// <param name="entitySystem">当前运行世界的实体系统。</param>
    /// <param name="localPredictedEntity">实体系统暴露的本地预测实体。</param>
    /// <returns>用于窗口顶部确认默认选择对象的多行摘要。</returns>
    private static string BuildLocalPredictedEntityInfo(EntitySystem entitySystem, BaseEntity localPredictedEntity)
    {
        if (entitySystem == null)
        {
            return "当前世界未找到实体系统";
        }

        if (localPredictedEntity == null)
        {
            return "当前世界尚未注册本地预测实体";
        }

        BaseEntity authorityEntity = entitySystem.ActorAuthorityEntity;
        string authoritySummary = authorityEntity == null
            ? "未注册"
            : $"{authorityEntity.EntityType} #{authorityEntity.EntityId} Config:{authorityEntity.ConfigId} {authorityEntity.EntityUpdateType} {authorityEntity.EntityState}";

        return
            $"本地预测: {localPredictedEntity.EntityType} #{localPredictedEntity.EntityId}\n" +
            $"Fingerprints: {localPredictedEntity.EntityFingerprints}\n" +
            $"ConfigId: {localPredictedEntity.ConfigId}\n" +
            $"UpdateType: {localPredictedEntity.EntityUpdateType}\n" +
            $"State: {localPredictedEntity.EntityState}\n" +
            $"关联权威: {authoritySummary}";
    }

    /// <summary>
    /// 获取实体类型筛选下拉数据。
    /// </summary>
    /// <returns>实体类型筛选项。</returns>
    private static IEnumerable<string> GetEntityTypeFilterOptions()
    {
        yield return AllFilterName;

        foreach (string name in Enum.GetNames(typeof(EntityType)))
        {
            yield return name;
        }
    }

    /// <summary>
    /// 获取实体更新域筛选下拉数据。
    /// </summary>
    /// <returns>实体更新域筛选项。</returns>
    private static IEnumerable<string> GetEntityUpdateTypeFilterOptions()
    {
        yield return AllFilterName;

        foreach (string name in Enum.GetNames(typeof(EntityUpdateType)))
        {
            yield return name;
        }
    }

    /// <summary>
    /// 获取当前实体选择下拉数据。
    /// </summary>
    /// <returns>实体选择项。</returns>
    private IEnumerable<ValueDropdownItem<EntityDebugViewData>> GetEntitySelectionOptions()
    {
        foreach (EntityDebugViewData entity in _filteredEntities)
        {
            yield return new ValueDropdownItem<EntityDebugViewData>(entity.ToString(), entity);
        }
    }

    /// <summary>
    /// 子弹父实体来源选项，用于决定创建上下文和更新域。
    /// </summary>
    public enum BulletOwnerSource
    {
        /// <summary>
        /// 使用当前金手指窗口选中的实体作为父实体。
        /// </summary>
        [LabelText("选中实体")]
        SelectedEntity,

        /// <summary>
        /// 使用当前世界的本地主角实体作为父实体。
        /// </summary>
        [LabelText("本地主角")]
        ActorLocal,

        /// <summary>
        /// 使用当前世界的权威主角实体作为父实体。
        /// </summary>
        [LabelText("权威主角")]
        ActorAuthority
    }

    /// <summary>
    /// 实体列表中的只读显示数据。
    /// </summary>
    [Serializable]
    public sealed class EntityDebugViewData
    {
        /// <summary>
        /// 创建一个实体显示项。
        /// </summary>
        /// <param name="entity">运行时实体引用。</param>
        public EntityDebugViewData(BaseEntity entity)
        {
            EntityId = entity?.EntityId ?? 0;
            Fingerprints = entity?.EntityFingerprints ?? 0;
            ConfigId = entity?.ConfigId ?? 0;
            EntityType = entity?.EntityType ?? default;
            Camp = entity?.CampEnum ?? default;
            UpdateType = entity?.EntityUpdateType ?? default;
            State = entity?.EntityState ?? default;
            Position = entity == null ? string.Empty : FormatFp3(entity.transform.Position);
        }

        /// <summary>
        /// 实体运行时 ID。
        /// </summary>
        [ReadOnly, LabelText("实体ID")]
        public int EntityId;

        /// <summary>
        /// 动态实体指纹。
        /// </summary>
        [ReadOnly, LabelText("指纹")]
        public int Fingerprints;

        /// <summary>
        /// 实体配置 ID。
        /// </summary>
        [ReadOnly, LabelText("配置ID")]
        public int ConfigId;

        /// <summary>
        /// 实体类型。
        /// </summary>
        [ReadOnly, LabelText("类型")]
        public EntityType EntityType;

        /// <summary>
        /// 实体阵营。
        /// </summary>
        [ReadOnly, LabelText("阵营")]
        public CampEnum Camp;

        /// <summary>
        /// 实体更新域。
        /// </summary>
        [ReadOnly, LabelText("更新域")]
        public EntityUpdateType UpdateType;

        /// <summary>
        /// 实体生命周期状态。
        /// </summary>
        [ReadOnly, LabelText("状态")]
        public EntityState State;

        /// <summary>
        /// 实体当前位置文本。
        /// </summary>
        [ReadOnly, LabelText("位置")]
        public string Position;

        /// <summary>
        /// 返回下拉框中显示的实体摘要。
        /// </summary>
        /// <returns>实体摘要文本。</returns>
        public override string ToString()
        {
            return $"{EntityType} #{EntityId} Config:{ConfigId} {UpdateType} {State}";
        }
    }

    /// <summary>
    /// 编辑器金手指一次操作需要同时写入的实体集合，用于保持预测实体和对应权威实体一致。
    /// </summary>
    public sealed class CheatEntityOperationTargets
    {
        /// <summary>
        /// 创建实体操作目标集合。
        /// </summary>
        /// <param name="entities">按操作顺序写入的实体列表。</param>
        /// <param name="summary">展示给编辑器用户的目标摘要。</param>
        private CheatEntityOperationTargets(IReadOnlyList<BaseEntity> entities, string summary)
        {
            Entities = entities;
            Summary = summary;
        }

        /// <summary>
        /// 按操作顺序写入的实体列表。
        /// </summary>
        public IReadOnlyList<BaseEntity> Entities { get; }

        /// <summary>
        /// 属性行快照读取时使用的主显示实体。
        /// </summary>
        public BaseEntity PrimaryEntity => Entities.Count > 0 ? Entities[0] : null;

        /// <summary>
        /// 展示给编辑器用户的目标摘要。
        /// </summary>
        public string Summary { get; }

        /// <summary>
        /// 创建只写入单个实体的操作目标，主要用于无法配对的调试实体和失败分支烟测。
        /// </summary>
        /// <param name="entity">需要写入的实体。</param>
        /// <param name="summary">展示给编辑器用户的目标摘要。</param>
        /// <returns>只包含单个实体的操作目标集合。</returns>
        public static CheatEntityOperationTargets CreateSingle(BaseEntity entity, string summary)
        {
            return new CheatEntityOperationTargets(new List<BaseEntity> { entity }, summary);
        }

        /// <summary>
        /// 创建同时写入两侧实体的操作目标，若两侧引用相同则自动去重。
        /// </summary>
        /// <param name="firstEntity">第一侧实体，通常是当前选中实体。</param>
        /// <param name="secondEntity">需要同步写入的对应实体。</param>
        /// <param name="summary">展示给编辑器用户的目标摘要。</param>
        /// <returns>包含一到两个实体的操作目标集合。</returns>
        public static CheatEntityOperationTargets CreatePaired(BaseEntity firstEntity, BaseEntity secondEntity, string summary)
        {
            List<BaseEntity> entities = new List<BaseEntity> { firstEntity };

            if (secondEntity != firstEntity)
            {
                entities.Add(secondEntity);
            }

            return new CheatEntityOperationTargets(entities, summary);
        }

        /// <summary>
        /// 构建包含影响数量的实体目标摘要。
        /// </summary>
        /// <returns>适合展示在操作结果中的目标摘要。</returns>
        public string BuildSummary()
        {
            return $"{Summary}（{Entities.Count} 个实体）";
        }
    }

    /// <summary>
    /// 单个实体属性的金手指编辑行。
    /// </summary>
    [Serializable]
    public sealed class EntityPropertyCheatRow
    {
        /// <summary>
        /// 当前属性操作需要同步写入的实体集合。
        /// </summary>
        private readonly CheatEntityOperationTargets _targets;

        /// <summary>
        /// 属性操作结果回调。
        /// </summary>
        private readonly Action<string, bool> _messageCallback;

        /// <summary>
        /// 属性按钮执行前用于判断实体引用是否仍然可操作。
        /// </summary>
        private readonly Func<BaseEntity, string> _entityFailureMessageProvider;

        /// <summary>
        /// 创建属性编辑行。
        /// </summary>
        /// <param name="targets">属性操作需要同步写入的实体集合。</param>
        /// <param name="debugInfo">属性调试快照。</param>
        /// <param name="messageCallback">操作结果回调。</param>
        /// <param name="entityFailureMessageProvider">实体引用失效原因读取器。</param>
        public EntityPropertyCheatRow(
            CheatEntityOperationTargets targets,
            EntityPropertyDebugInfo debugInfo,
            Action<string, bool> messageCallback,
            Func<BaseEntity, string> entityFailureMessageProvider)
        {
            _targets = targets;
            _messageCallback = messageCallback;
            _entityFailureMessageProvider = entityFailureMessageProvider;
            Key = debugInfo.Key;
            CurrentValue = (float)debugInfo.CurrentValue;
            MinValue = (float)debugInfo.MinValue;
            MaxValue = (float)debugInfo.MaxValue;
            SetValue = CurrentValue;
            TargetMinValue = MinValue;
            TargetMaxValue = MaxValue;
        }

        /// <summary>
        /// 属性键。
        /// </summary>
        [HorizontalGroup("Row", Width = 90)]
        [ReadOnly, LabelText("属性")]
        public PropertyKey Key;

        /// <summary>
        /// 属性当前值。
        /// </summary>
        [HorizontalGroup("Row", Width = 90)]
        [ReadOnly, LabelText("当前")]
        public float CurrentValue;

        /// <summary>
        /// 属性最小值。
        /// </summary>
        [HorizontalGroup("Row", Width = 90)]
        [ReadOnly, LabelText("最小")]
        public float MinValue;

        /// <summary>
        /// 属性最大值。
        /// </summary>
        [HorizontalGroup("Row", Width = 90)]
        [ReadOnly, LabelText("最大")]
        public float MaxValue;

        /// <summary>
        /// 准备设置为当前值的输入。
        /// </summary>
        [HorizontalGroup("Row", Width = 100)]
        [LabelText("设置")]
        public float SetValue;

        /// <summary>
        /// 准备增减当前值的输入。
        /// </summary>
        [HorizontalGroup("Row", Width = 100)]
        [LabelText("增减")]
        public float DeltaValue;

        /// <summary>
        /// 准备写入的属性最小边界。
        /// </summary>
        [HorizontalGroup("Bounds", Width = 100)]
        [LabelText("最小边界")]
        public float TargetMinValue;

        /// <summary>
        /// 准备写入的属性最大边界。
        /// </summary>
        [HorizontalGroup("Bounds", Width = 100)]
        [LabelText("最大边界")]
        public float TargetMaxValue;

        /// <summary>
        /// 将属性当前值设置为输入值。
        /// </summary>
        [HorizontalGroup("Row", Width = 58)]
        [Button("设置")]
        public void ApplySetValue()
        {
            if (!TryGetEditableTargets(out string failureMessage))
            {
                SendOperationResult($"属性设置失败：{failureMessage}");
                return;
            }

            if (!TryConvertFiniteInput(SetValue, "设置当前值", out fp requestedValue, out failureMessage))
            {
                SendOperationResult(failureMessage);
                return;
            }

            if (!TryReadTargetSnapshots(out List<EntityPropertyTargetSnapshot> beforeSnapshots, out failureMessage))
            {
                SendOperationResult($"属性设置失败：{failureMessage}");
                return;
            }

            List<EntityPropertyMutationResult> results = new List<EntityPropertyMutationResult>();

            for (int i = 0; i < beforeSnapshots.Count; i++)
            {
                BaseEntity entity = beforeSnapshots[i].Entity;
                EntityPropertySnapshot beforeSnapshot = beforeSnapshots[i].Snapshot;

                if (!entity.TrySetProperty(Key, requestedValue))
                {
                    SendOperationResult($"属性设置失败：{BuildEntityOperationLabel(entity)} {Key} 不存在");
                    return;
                }

                if (!TryReadSnapshot(entity, out EntityPropertySnapshot afterSnapshot, out failureMessage))
                {
                    SendOperationResult($"属性设置后刷新失败：{BuildEntityOperationLabel(entity)} {failureMessage}");
                    return;
                }

                results.Add(new EntityPropertyMutationResult(entity, beforeSnapshot, afterSnapshot, afterSnapshot.CurrentValue - beforeSnapshot.CurrentValue));
            }

            bool hasClamp = results.Any(result => result.AfterSnapshot.CurrentValue != requestedValue);
            string clampMessage = hasClamp ? "，至少一侧 PropertyData 已夹取请求值" : string.Empty;
            SendOperationResult($"已设置 {_targets.BuildSummary()} 的 {Key}：请求 {FormatFp(requestedValue)}，{BuildMutationSummary(results, result => result.AfterSnapshot.CurrentValue, result => result.AppliedValue)}{clampMessage}");
        }

        /// <summary>
        /// 按输入增量修改属性当前值。
        /// </summary>
        [HorizontalGroup("Row", Width = 58)]
        [Button("增减")]
        public void ApplyDeltaValue()
        {
            if (!TryGetEditableTargets(out string failureMessage))
            {
                SendOperationResult($"属性增减失败：{failureMessage}");
                return;
            }

            if (!TryConvertFiniteInput(DeltaValue, "增减当前值", out fp requestedDelta, out failureMessage))
            {
                SendOperationResult(failureMessage);
                return;
            }

            if (!TryReadTargetSnapshots(out List<EntityPropertyTargetSnapshot> beforeSnapshots, out failureMessage))
            {
                SendOperationResult($"属性增减失败：{failureMessage}");
                return;
            }

            List<EntityPropertyMutationResult> results = new List<EntityPropertyMutationResult>();

            for (int i = 0; i < beforeSnapshots.Count; i++)
            {
                BaseEntity entity = beforeSnapshots[i].Entity;
                EntityPropertySnapshot beforeSnapshot = beforeSnapshots[i].Snapshot;

                if (!entity.TryChangePropertyValue(Key, PropertyValueType.Current, requestedDelta, out fp appliedValue))
                {
                    SendOperationResult($"属性增减失败：{BuildEntityOperationLabel(entity)} {Key} 不存在");
                    return;
                }

                if (!TryReadSnapshot(entity, out EntityPropertySnapshot afterSnapshot, out failureMessage))
                {
                    SendOperationResult($"属性增减后刷新失败：{BuildEntityOperationLabel(entity)} {failureMessage}");
                    return;
                }

                results.Add(new EntityPropertyMutationResult(entity, beforeSnapshot, afterSnapshot, appliedValue));
            }

            bool hasClamp = results.Any(result => result.AppliedValue != requestedDelta);
            string clampMessage = hasClamp ? "，至少一侧 PropertyData 已夹取增量" : string.Empty;
            SendOperationResult($"已增减 {_targets.BuildSummary()} 的 {Key}：请求变化 {FormatFp(requestedDelta)}，{BuildMutationSummary(results, result => result.AppliedValue, result => result.AfterSnapshot.CurrentValue - result.BeforeSnapshot.CurrentValue)}{clampMessage}");
        }

        /// <summary>
        /// 将属性当前值设置为该属性最大值。
        /// </summary>
        [HorizontalGroup("Row", Width = 70)]
        [Button("最大值")]
        public void SetToMax()
        {
            if (!TryGetEditableTargets(out string failureMessage))
            {
                SendOperationResult($"属性设置失败：{failureMessage}");
                return;
            }

            if (!TryReadTargetSnapshots(out List<EntityPropertyTargetSnapshot> beforeSnapshots, out failureMessage))
            {
                SendOperationResult($"属性设置失败：{failureMessage}");
                return;
            }

            List<EntityPropertyMutationResult> results = new List<EntityPropertyMutationResult>();

            for (int i = 0; i < beforeSnapshots.Count; i++)
            {
                BaseEntity entity = beforeSnapshots[i].Entity;
                EntityPropertySnapshot beforeSnapshot = beforeSnapshots[i].Snapshot;

                if (!entity.TrySetProperty(Key, beforeSnapshot.MaxValue))
                {
                    SendOperationResult($"属性设置失败：{BuildEntityOperationLabel(entity)} {Key} 不存在");
                    return;
                }

                if (!TryReadSnapshot(entity, out EntityPropertySnapshot afterSnapshot, out failureMessage))
                {
                    SendOperationResult($"属性设置后刷新失败：{BuildEntityOperationLabel(entity)} {failureMessage}");
                    return;
                }

                results.Add(new EntityPropertyMutationResult(entity, beforeSnapshot, afterSnapshot, afterSnapshot.CurrentValue - beforeSnapshot.CurrentValue));
            }

            SendOperationResult($"已将 {_targets.BuildSummary()} 的 {Key} 设置为最大值：{BuildMutationSummary(results, result => result.AfterSnapshot.CurrentValue, result => result.AfterSnapshot.MaxValue)}");
        }

        /// <summary>
        /// 将属性当前值设置为该属性最小值。
        /// </summary>
        [HorizontalGroup("Row", Width = 70)]
        [Button("最小值")]
        public void SetToMin()
        {
            if (!TryGetEditableTargets(out string failureMessage))
            {
                SendOperationResult($"属性设置失败：{failureMessage}");
                return;
            }

            if (!TryReadTargetSnapshots(out List<EntityPropertyTargetSnapshot> beforeSnapshots, out failureMessage))
            {
                SendOperationResult($"属性设置失败：{failureMessage}");
                return;
            }

            List<EntityPropertyMutationResult> results = new List<EntityPropertyMutationResult>();

            for (int i = 0; i < beforeSnapshots.Count; i++)
            {
                BaseEntity entity = beforeSnapshots[i].Entity;
                EntityPropertySnapshot beforeSnapshot = beforeSnapshots[i].Snapshot;

                if (!entity.TrySetProperty(Key, beforeSnapshot.MinValue))
                {
                    SendOperationResult($"属性设置失败：{BuildEntityOperationLabel(entity)} {Key} 不存在");
                    return;
                }

                if (!TryReadSnapshot(entity, out EntityPropertySnapshot afterSnapshot, out failureMessage))
                {
                    SendOperationResult($"属性设置后刷新失败：{BuildEntityOperationLabel(entity)} {failureMessage}");
                    return;
                }

                results.Add(new EntityPropertyMutationResult(entity, beforeSnapshot, afterSnapshot, afterSnapshot.CurrentValue - beforeSnapshot.CurrentValue));
            }

            SendOperationResult($"已将 {_targets.BuildSummary()} 的 {Key} 设置为最小值：{BuildMutationSummary(results, result => result.AfterSnapshot.CurrentValue, result => result.AfterSnapshot.MinValue)}");
        }

        /// <summary>
        /// 将属性最小边界设置为输入值。
        /// </summary>
        [HorizontalGroup("Bounds", Width = 70)]
        [Button("设最小")]
        public void ApplySetMinValue()
        {
            ApplySetPropertyBoundary(PropertyValueType.Min, TargetMinValue, "最小边界");
        }

        /// <summary>
        /// 将属性最大边界设置为输入值。
        /// </summary>
        [HorizontalGroup("Bounds", Width = 70)]
        [Button("设最大")]
        public void ApplySetMaxValue()
        {
            ApplySetPropertyBoundary(PropertyValueType.Max, TargetMaxValue, "最大边界");
        }

        /// <summary>
        /// 校验属性操作目标集合中的实体是否仍然可写入。
        /// </summary>
        /// <param name="failureMessage">任一目标实体不可写入时的原因。</param>
        /// <returns>所有目标实体引用仍可写入时返回 true。</returns>
        private bool TryGetEditableTargets(out string failureMessage)
        {
            if (_targets == null || _targets.Entities.Count == 0)
            {
                failureMessage = "没有可写入的同步目标";
                return false;
            }

            for (int i = 0; i < _targets.Entities.Count; i++)
            {
                BaseEntity entity = _targets.Entities[i];
                failureMessage = _entityFailureMessageProvider?.Invoke(entity);

                if (failureMessage != null)
                {
                    return false;
                }
            }

            failureMessage = null;
            return true;
        }

        /// <summary>
        /// 按属性值类型设置属性边界。
        /// </summary>
        /// <param name="valueType">需要设置的边界类型，只允许 Min 或 Max。</param>
        /// <param name="targetValue">用户输入的目标边界值。</param>
        /// <param name="displayName">展示给编辑器用户的边界名称。</param>
        private void ApplySetPropertyBoundary(PropertyValueType valueType, float targetValue, string displayName)
        {
            if (valueType != PropertyValueType.Min && valueType != PropertyValueType.Max)
            {
                SendOperationResult($"属性边界设置失败：不支持的边界类型 {valueType}");
                return;
            }

            if (!TryGetEditableTargets(out string failureMessage))
            {
                SendOperationResult($"属性边界设置失败：{failureMessage}");
                return;
            }

            if (!TryConvertFiniteInput(targetValue, $"设置{displayName}", out fp requestedValue, out failureMessage))
            {
                SendOperationResult(failureMessage);
                return;
            }

            if (!TryReadTargetSnapshots(out List<EntityPropertyTargetSnapshot> beforeSnapshots, out failureMessage))
            {
                SendOperationResult($"属性边界设置失败：{failureMessage}");
                return;
            }

            List<EntityPropertyMutationResult> results = new List<EntityPropertyMutationResult>();

            for (int i = 0; i < beforeSnapshots.Count; i++)
            {
                BaseEntity entity = beforeSnapshots[i].Entity;
                EntityPropertySnapshot beforeSnapshot = beforeSnapshots[i].Snapshot;

                fp beforeValue = valueType == PropertyValueType.Min ? beforeSnapshot.MinValue : beforeSnapshot.MaxValue;
                fp requestedDelta = requestedValue - beforeValue;

                if (!entity.TryChangePropertyValue(Key, valueType, requestedDelta, out fp appliedValue))
                {
                    SendOperationResult($"属性边界设置失败：{BuildEntityOperationLabel(entity)} {Key} 不存在");
                    return;
                }

                if (!TryReadSnapshot(entity, out EntityPropertySnapshot afterSnapshot, out failureMessage))
                {
                    SendOperationResult($"属性边界设置后刷新失败：{BuildEntityOperationLabel(entity)} {failureMessage}");
                    return;
                }

                results.Add(new EntityPropertyMutationResult(entity, beforeSnapshot, afterSnapshot, appliedValue));
            }

            bool hasClamp = results.Any(result => GetSnapshotValue(result.AfterSnapshot, valueType) != requestedValue);
            bool hasCurrentChange = results.Any(result => result.AfterSnapshot.CurrentValue != result.BeforeSnapshot.CurrentValue);
            string currentChangeMessage = hasCurrentChange ? "，至少一侧当前值随边界调整" : string.Empty;
            string clampMessage = hasClamp ? "，至少一侧 PropertyData 已夹取边界" : string.Empty;
            SendOperationResult($"已设置 {_targets.BuildSummary()} 的 {Key} {displayName}：请求 {FormatFp(requestedValue)}，{BuildMutationSummary(results, result => GetSnapshotValue(result.AfterSnapshot, valueType), result => result.AppliedValue)}{currentChangeMessage}{clampMessage}");
        }

        /// <summary>
        /// 读取属性当前、最小和最大值。
        /// </summary>
        /// <param name="entity">需要读取属性的实体。</param>
        /// <param name="snapshot">读取到的属性三元值快照。</param>
        /// <param name="failureMessage">无法读取属性时的失败原因。</param>
        /// <returns>属性存在且三元值均可读取时返回 true。</returns>
        public bool TryReadSnapshot(BaseEntity entity, out EntityPropertySnapshot snapshot, out string failureMessage)
        {
            snapshot = default;

            if (entity == null)
            {
                failureMessage = "实体引用为空";
                return false;
            }

            if (!entity.TryGetPropertyValue(Key, PropertyValueType.Current, out fp currentValue) ||
                !entity.TryGetPropertyValue(Key, PropertyValueType.Min, out fp minValue) ||
                !entity.TryGetPropertyValue(Key, PropertyValueType.Max, out fp maxValue))
            {
                failureMessage = $"{Key} 不存在或无法读取";
                return false;
            }

            snapshot = new EntityPropertySnapshot(currentValue, minValue, maxValue);
            failureMessage = null;
            return true;
        }

        /// <summary>
        /// 读取当前属性行绑定实体的属性快照。
        /// </summary>
        /// <param name="snapshot">读取到的属性三元值快照。</param>
        /// <param name="failureMessage">无法读取属性时的失败原因。</param>
        /// <returns>属性存在且三元值均可读取时返回 true。</returns>
        public bool TryReadSnapshot(out EntityPropertySnapshot snapshot, out string failureMessage)
        {
            return TryReadSnapshot(_targets?.PrimaryEntity, out snapshot, out failureMessage);
        }

        /// <summary>
        /// 读取所有同步目标的属性快照，用于烟测后逐侧恢复。
        /// </summary>
        /// <param name="snapshots">每个同步目标对应的属性快照。</param>
        /// <param name="failureMessage">任一目标快照读取失败时的原因。</param>
        /// <returns>所有同步目标属性快照读取成功时返回 true。</returns>
        public bool TryReadTargetSnapshots(out List<EntityPropertyTargetSnapshot> snapshots, out string failureMessage)
        {
            snapshots = new List<EntityPropertyTargetSnapshot>();

            if (!TryGetEditableTargets(out failureMessage))
            {
                return false;
            }

            for (int i = 0; i < _targets.Entities.Count; i++)
            {
                BaseEntity entity = _targets.Entities[i];

                if (!TryReadSnapshot(entity, out EntityPropertySnapshot snapshot, out failureMessage))
                {
                    failureMessage = $"{BuildEntityOperationLabel(entity)} {failureMessage}";
                    return false;
                }

                snapshots.Add(new EntityPropertyTargetSnapshot(entity, snapshot));
            }

            return true;
        }

        /// <summary>
        /// 将所有同步目标恢复到指定属性快照。
        /// </summary>
        /// <param name="snapshots">需要恢复的实体和属性三元值快照。</param>
        /// <param name="failureMessage">任一目标恢复失败时的原因。</param>
        /// <returns>所有同步目标恢复成功时返回 true。</returns>
        public bool TryRestoreTargetSnapshots(IReadOnlyList<EntityPropertyTargetSnapshot> snapshots, out string failureMessage)
        {
            failureMessage = null;

            if (snapshots == null || snapshots.Count == 0)
            {
                failureMessage = "没有可恢复的属性快照";
                return false;
            }

            for (int i = 0; i < snapshots.Count; i++)
            {
                EntityPropertyTargetSnapshot targetSnapshot = snapshots[i];

                if (!TryRestorePropertySnapshot(targetSnapshot.Entity, Key, targetSnapshot.Snapshot, out failureMessage))
                {
                    failureMessage = $"{BuildEntityOperationLabel(targetSnapshot.Entity)} {failureMessage}";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 将编辑器浮点输入转换为定点数。
        /// </summary>
        /// <param name="inputValue">编辑器控件输入的浮点值。</param>
        /// <param name="operationName">当前操作名称，用于拼接失败提示。</param>
        /// <param name="fpValue">转换后的定点值。</param>
        /// <param name="failureMessage">输入非法或转换失败时的失败原因。</param>
        /// <returns>输入为有限数且可转换为定点数时返回 true。</returns>
        private bool TryConvertFiniteInput(float inputValue, string operationName, out fp fpValue, out string failureMessage)
        {
            fpValue = 0;

            if (float.IsNaN(inputValue) || float.IsInfinity(inputValue))
            {
                failureMessage = $"{operationName}失败：输入值必须是有限数";
                return false;
            }

            try
            {
                fpValue = (fp)inputValue;
            }
            catch (Exception exception)
            {
                failureMessage = $"{operationName}失败：输入值无法转换为定点数（{exception.Message}）";
                return false;
            }

            failureMessage = null;
            return true;
        }

        /// <summary>
        /// 向窗口写入操作结果，并要求刷新属性行显示。
        /// </summary>
        /// <param name="message">需要展示给编辑器用户的操作结果。</param>
        private void SendOperationResult(string message)
        {
            _messageCallback?.Invoke(message, true);
        }

        /// <summary>
        /// 按属性值类型从快照中读取对应数值。
        /// </summary>
        /// <param name="snapshot">属性三元值快照。</param>
        /// <param name="valueType">需要读取的属性值类型。</param>
        /// <returns>匹配值类型的属性数值。</returns>
        private static fp GetSnapshotValue(EntityPropertySnapshot snapshot, PropertyValueType valueType)
        {
            switch (valueType)
            {
                case PropertyValueType.Current:
                    return snapshot.CurrentValue;
                case PropertyValueType.Min:
                    return snapshot.MinValue;
                case PropertyValueType.Max:
                    return snapshot.MaxValue;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// 构建多目标属性修改结果摘要。
        /// </summary>
        /// <param name="results">每个目标实体的修改结果。</param>
        /// <param name="primaryValueSelector">需要展示的主结果值读取器。</param>
        /// <param name="deltaValueSelector">需要展示的变化值读取器。</param>
        /// <returns>包含每侧实体当前值和变化量的摘要文本。</returns>
        private static string BuildMutationSummary(
            IReadOnlyList<EntityPropertyMutationResult> results,
            Func<EntityPropertyMutationResult, fp> primaryValueSelector,
            Func<EntityPropertyMutationResult, fp> deltaValueSelector)
        {
            return string.Join("；", results.Select(result =>
                $"{BuildEntityOperationLabel(result.Entity)} 值 {FormatFp(primaryValueSelector(result))}，变化 {FormatFp(deltaValueSelector(result))}"));
        }

        /// <summary>
        /// 将定点数格式化为适合编辑器提示阅读的文本。
        /// </summary>
        /// <param name="value">需要格式化的定点数。</param>
        /// <returns>保留三位小数的数值文本。</returns>
        private static string FormatFp(fp value)
        {
            return ((float)value).ToString("0.###");
        }

        /// <summary>
        /// 单个实体属性修改前后的快照结果。
        /// </summary>
        private readonly struct EntityPropertyMutationResult
        {
            /// <summary>
            /// 创建一个属性修改结果快照。
            /// </summary>
            /// <param name="entity">被修改的实体。</param>
            /// <param name="beforeSnapshot">修改前的属性快照。</param>
            /// <param name="afterSnapshot">修改后的属性快照。</param>
            /// <param name="appliedValue">运行时 API 返回或计算得到的实际变化值。</param>
            public EntityPropertyMutationResult(
                BaseEntity entity,
                EntityPropertySnapshot beforeSnapshot,
                EntityPropertySnapshot afterSnapshot,
                fp appliedValue)
            {
                Entity = entity;
                BeforeSnapshot = beforeSnapshot;
                AfterSnapshot = afterSnapshot;
                AppliedValue = appliedValue;
            }

            /// <summary>
            /// 被修改的实体。
            /// </summary>
            public BaseEntity Entity { get; }

            /// <summary>
            /// 修改前的属性快照。
            /// </summary>
            public EntityPropertySnapshot BeforeSnapshot { get; }

            /// <summary>
            /// 修改后的属性快照。
            /// </summary>
            public EntityPropertySnapshot AfterSnapshot { get; }

            /// <summary>
            /// 运行时 API 返回或计算得到的实际变化值。
            /// </summary>
            public fp AppliedValue { get; }
        }

        /// <summary>
        /// 某个同步目标实体的属性快照。
        /// </summary>
        public readonly struct EntityPropertyTargetSnapshot
        {
            /// <summary>
            /// 创建同步目标属性快照。
            /// </summary>
            /// <param name="entity">需要恢复或校验的实体。</param>
            /// <param name="snapshot">实体对应的属性三元值快照。</param>
            public EntityPropertyTargetSnapshot(BaseEntity entity, EntityPropertySnapshot snapshot)
            {
                Entity = entity;
                Snapshot = snapshot;
            }

            /// <summary>
            /// 需要恢复或校验的实体。
            /// </summary>
            public BaseEntity Entity { get; }

            /// <summary>
            /// 实体对应的属性三元值快照。
            /// </summary>
            public EntityPropertySnapshot Snapshot { get; }
        }

        /// <summary>
        /// 属性行操作前后使用的运行时属性快照。
        /// </summary>
        public readonly struct EntityPropertySnapshot
        {
            /// <summary>
            /// 创建属性三元值快照。
            /// </summary>
            /// <param name="currentValue">属性当前值。</param>
            /// <param name="minValue">属性最小边界。</param>
            /// <param name="maxValue">属性最大边界。</param>
            public EntityPropertySnapshot(fp currentValue, fp minValue, fp maxValue)
            {
                CurrentValue = currentValue;
                MinValue = minValue;
                MaxValue = maxValue;
            }

            /// <summary>
            /// 属性当前值。
            /// </summary>
            public fp CurrentValue { get; }

            /// <summary>
            /// 属性最小边界。
            /// </summary>
            public fp MinValue { get; }

            /// <summary>
            /// 属性最大边界。
            /// </summary>
            public fp MaxValue { get; }
        }
    }
}
