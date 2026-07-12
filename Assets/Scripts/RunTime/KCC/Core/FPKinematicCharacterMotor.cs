using System;
using System.Collections.Generic;
using Unity.Mathematics.FixedPoint;
#if UNITY_EDITOR
using UnityEngine;
#endif

/// <summary>
/// 角色运动马达的完整仿真状态快照，用于回滚、存档或跨帧恢复。
/// </summary>
public struct FPKinematicCharacterMotorState
{
    /// <summary>瞬态世界位置。</summary>
    public fp3 Position;
    /// <summary>瞬态世界旋转。</summary>
    public fpquaternion Rotation;
    /// <summary>角色自身（非附着体）基础速度。</summary>
    public fp3 BaseVelocity;
    /// <summary>是否强制离地。</summary>
    public bool MustUnground;
    /// <summary>强制离地剩余时间。</summary>
    public fp MustUngroundTime;
    /// <summary>最近一次移动迭代是否检测到地面。</summary>
    public bool LastMovementIterationFoundAnyGround;
    /// <summary>当前接地状态报告。</summary>
    public FPCharacterTransientGroundingReport GroundingStatus;
    /// <summary>当前附着的动态刚体。</summary>
    public FPDynamicRigidbody AttachedRigidbody;
    /// <summary>来自附着刚体/移动平台的速度。</summary>
    public fp3 AttachedRigidbodyVelocity;
    /// <summary>当前附着的物理移动平台。</summary>
    public FPPhysicsMover AttachedMover;
}

/// <summary>
/// 角色胶囊与碰撞体发生重叠时的解算结果。
/// </summary>
public struct FPOverlapResult
{
    /// <summary>解重叠使用的分离法线方向。</summary>
    public fp3 Normal;
    /// <summary>发生重叠的碰撞体。</summary>
    public IFPCollider Collider;

    /// <summary>构造重叠结果。</summary>
    public FPOverlapResult(fp3 normal, IFPCollider collider)
    {
        Normal = normal;
        Collider = collider;
    }
}

/// <summary>
/// 与可交互刚体碰撞时的投影命中信息，用于后续速度/冲量处理。
/// </summary>
public struct FPRigidbodyProjectionHit
{
    /// <summary>被命中的动态刚体。</summary>
    public FPDynamicRigidbody Rigidbody;
    /// <summary>命中点世界坐标。</summary>
    public fp3 HitPoint;
    /// <summary>用于速度投影的有效法线。</summary>
    public fp3 EffectiveHitNormal;
    /// <summary>命中时的角色速度。</summary>
    public fp3 HitVelocity;
    /// <summary>命中面是否可视为稳定地面。</summary>
    public bool StableOnHit;
}

/// <summary>
/// KCC 定点数向量投影辅助数学工具。
/// </summary>
public static class FPKCCMath
{
    /// <summary>将向量投影到指定法线方向上。</summary>
    public static fp3 Project(fp3 vector, fp3 onNormal)
    {
        fp3 n = fpmath.normalize(onNormal);
        return n * fpmath.dot(vector, n);
    }
}

/// <summary>
/// 定点数 Kinematic Character Controller 核心运动马达。
/// 调用链：MoveComponent.OnStart 创建并 Register → 每帧命令写入 SetMovePositionTarget/SetMoveRotationTarget
/// → FPKinematicCharacterSystem.Simulate（按 WorldUpdateType 过滤 Local/Authority/RollBack）
/// → UpdatePhase1（位移/接地/附着体）→ UpdatePhase2（旋转/速度 Sweep）→ 回写 BoundEntity.FTransform。
/// </summary>
public class FPKinematicCharacterMotor
{
    /// <summary>角色自身碰撞层，用于构建可碰撞层掩码。</summary>
    public FPCollisionLayer CharacterLayer = FPCollisionLayer.Hero;

    /// <summary>
    /// 物理碰撞/Motor 回写时可改写的 Transform 轴；由 <see cref="PhysicsBodyComponent"/> 从配置注入。
    /// </summary>
    public PhysicsMotionInfluence CollisionInfluence = PhysicsMotionInfluence.CreateCharacterControllerDefault();

    /// <summary>当前附着的物理移动平台。</summary>
    private FPPhysicsMover _attachedMover;
    /// <summary>上一帧附着的物理移动平台，用于动量保留判定。</summary>
    private FPPhysicsMover _lastAttachedMover;
    /// <summary>当前附着的物理移动平台（只读）。</summary>
    public FPPhysicsMover AttachedMover => _attachedMover;
    /// <summary>强制指定附着的移动平台，覆盖地面自动检测。</summary>
    public FPPhysicsMover AttachedMoverOverride;

    /// <summary>
    /// 绑定的 ECS 实体。FPKinematicCharacterSystem 校验后会回写其 FTransform。
    /// </summary>
    public BaseEntity BoundEntity { get; private set; }

    /// <summary>绑定 ECS 实体，便于系统回写 FTransform。</summary>
    public void BindEntity(BaseEntity entity)
    {
        BoundEntity = entity;
    }

    /// <summary>解除与 ECS 实体的绑定。</summary>
    public void UnbindEntity()
    {
        BoundEntity = null;
    }

    /// <summary>
    /// 业务层设置本帧目标位置，由 Simulate 做碰撞校验。
    /// </summary>
    public void SetMovePositionTarget(fp3 target)
    {
        _movePositionTarget = target;
        _movePositionDirty = true;
    }

    /// <summary>
    /// 业务层设置本帧目标旋转，由 Simulate 做碰撞校验。
    /// </summary>
    public void SetMoveRotationTarget(fpquaternion target)
    {
        _moveRotationTarget = target;
        _moveRotationDirty = true;
    }

    /// <summary>创建马达并初始化默认胶囊尺寸与可碰撞层。</summary>
    public FPKinematicCharacterMotor(IFPCharacterController controller = null)
    {
        CharacterController = controller;
        TransientRotation = fpquaternion.identity;
        RebuildCollidableLayers();
        StableGroundLayers = BuildDefaultStableGroundLayers(CollidableLayers);
        SetCapsuleDimensions((fp)0.5f, (fp)2f, (fp)1f);
    }

    /// <summary>向 FPKinematicCharacterSystem 注册本马达。</summary>
    public void Register()
    {
        BoundEntity?.BaseWorld?.GetSystem<FPKinematicCharacterSystem>()?.RegisterCharacterMotor(this);
    }

    /// <summary>从 FPKinematicCharacterSystem 注销本马达。</summary>
    public void Unregister()
    {
        BoundEntity?.BaseWorld?.GetSystem<FPKinematicCharacterSystem>()?.UnregisterCharacterMotor(this);
    }

    /// <summary>根据 CharacterLayer 与物理层碰撞矩阵重建 CollidableLayers。</summary>
    public void RebuildCollidableLayers()
    {
        int mask = 0;
        foreach (FPCollisionLayer layer in Enum.GetValues(typeof(FPCollisionLayer)))
        {
            if (layer == FPCollisionLayer.None || layer == FPCollisionLayer.All)
            {
                continue;
            }

            if (FPPhysicsQuery.GetLayerCollision(CharacterLayer, layer))
            {
                mask |= (int)layer;
            }
        }

        CollidableLayers = new FPLayerMask(mask);
    }

    /// <summary>
    /// 从可碰撞层中排除不宜作为「稳定地面」的层（怪物/英雄/动态体/触发器），供 ProbeGround 与台阶检测使用。
    /// </summary>
    public static FPLayerMask BuildDefaultStableGroundLayers(FPLayerMask collidableLayers)
    {
        int excluded =
            (int)FPCollisionLayer.Monster |
            (int)FPCollisionLayer.Hero |
            (int)FPCollisionLayer.DynamicBody |
            (int)FPCollisionLayer.Trigger;
        return new FPLayerMask(collidableLayers.Value & ~excluded);
    }

    private void SyncTransientPositionFromEntityIfDesynced()
    {
        if (BoundEntity?.transform == null)
        {
            return;
        }

        fp3 entityPosition = BoundEntity.transform.Position;
        if (fpmath.length(entityPosition - _transientPosition) > MaxMotorEntityDesyncDistance)
        {
            _transientPosition = entityPosition;
            _initialSimulationPosition = entityPosition;
        }
    }

        /// <summary>角色胶囊体半径。</summary>
        public fp CapsuleRadius = (fp)0.5f;
        /// <summary>角色胶囊体高度（含上下半球）。</summary>
        public fp CapsuleHeight = (fp)2f;
        /// <summary>胶囊中心相对角色变换原点的本地 Y 偏移。</summary>
        public fp CapsuleYOffset = (fp)1f;
        /// <summary>地面检测额外下探距离，高速移动时便于贴地吸附。</summary>
        public fp GroundDetectionExtraDistance = (fp)0;
        /// <summary>角色可稳定站立的最大坡度角（度）。</summary>
        public fp MaxStableSlopeAngle = (fp)60f;
        /// <summary>可视为稳定地面的碰撞层掩码。</summary>
        public FPLayerMask StableGroundLayers = (int)FPCollisionLayer.All;
        /// <summary>检测到离散碰撞时是否通知 CharacterController。</summary>
        public bool DiscreteCollisionEvents = false;


        /// <summary>台阶处理方式；开启后性能开销略增。</summary>
        public FPStepHandlingMethod StepHandling = FPStepHandlingMethod.Standard;
        /// <summary>可攀爬台阶的最大高度。</summary>
        public fp MaxStepHeight = (fp)0.5f;
        /// <summary>未稳定接地时是否仍允许上台阶。</summary>
        public bool AllowSteppingWithoutStableGrounding = false;
        /// <summary>Extra 台阶模式下可识别台阶的最小深度（可小于胶囊半径）。</summary>
        [Tooltip("Extra 台阶模式下可识别台阶的最小深度；用于识别小于胶囊半径的台阶。")]
        public fp MinRequiredStepDepth = (fp)0.1f;


        /// <summary>是否启用ledge（边缘）与落差角处理；开启后性能开销略增。</summary>
        public bool LedgeAndDenivelationHandling = true;
        /// <summary>ledge 外侧仍可视为稳定的最大水平距离。</summary>
        public fp MaxStableDistanceFromLedge = (fp)0.5f;
        /// <summary>超过该速度时禁止 ledge 贴地吸附。</summary>
        public fp MaxVelocityForLedgeSnap = (fp)0;
        /// <summary>内外地面法线落差超过该角度时禁止贴地。</summary>
        public fp MaxStableDenivelationAngle = (fp)180f;


        /// <summary>是否处理与 PhysicsMover/动态刚体的交互（站立、被推、推物体）。</summary>
        public bool InteractiveRigidbodyHandling = true;
        /// <summary>
        /// 与非 kinematic 刚体的交互模式。
        /// Kinematic：以无限力推动刚体；SimulatedDynamic：按模拟质量参与冲量交换。
        /// </summary>
        [Tooltip("与非 kinematic 刚体的交互模式。Kinematic 以无限力推动；SimulatedDynamic 按模拟质量交换冲量。")]
        public FPRigidbodyInteractionType FPRigidbodyInteractionType;
        /// <summary>SimulatedDynamic 模式下角色的模拟质量。</summary>
        public fp SimulatedCharacterMass = (fp)1f;
        /// <summary>脱离移动平台/附着刚体时是否保留其带来的动量。</summary>
        public bool PreserveAttachedRigidbodyMomentum = true;


        /// <summary>是否启用平面运动约束。</summary>
        public bool HasPlanarConstraint = false;
        /// <summary>平面约束的法线轴（HasPlanarConstraint 为 true 时生效）。</summary>
        public fp3 PlanarConstraintAxis = FPMathKCC.WorldForward;

        /// <summary>单次更新内移动 Sweep 的最大迭代次数。</summary>
        public int MaxMovementIterations = 5;
        /// <summary>单次更新内解重叠的最大迭代次数。</summary>
        public int MaxDecollisionIterations = 1;
        /// <summary>
        /// Sweep 前是否先做重叠检测，防止已嵌入碰撞体时穿模（有性能开销，但更安全）。
        /// </summary>
        [Tooltip("Sweep 前检测初始重叠，避免已嵌入碰撞体时穿模；有性能开销但更安全。")]
        public bool CheckMovementInitialOverlaps = true;
        /// <summary>超过最大移动迭代次数时是否清零速度。</summary>
        public bool KillVelocityWhenExceedMaxMovementIterations = true;
        /// <summary>超过最大移动迭代次数时是否丢弃剩余位移。</summary>
        public bool KillRemainingMovementWhenExceedMaxMovementIterations = true;

        /// <summary>当前帧接地状态报告。</summary>
        public FPCharacterGroundingReport GroundingStatus = new FPCharacterGroundingReport();
        /// <summary>上一帧接地状态报告。</summary>
        public FPCharacterTransientGroundingReport LastGroundingStatus = new FPCharacterTransientGroundingReport();
        /// <summary>移动算法可检测碰撞的层掩码，默认由 CharacterLayer 碰撞矩阵推导。</summary>
        public FPLayerMask CollidableLayers = -1;
        /// <summary>角色更新阶段的目标/瞬态位置（每帧更新内始终最新）。</summary>
        public fp3 TransientPosition { get { return _transientPosition; } }
        private fp3 _transientPosition;
        /// <summary>角色局部 Up 方向（更新阶段内随 TransientRotation 刷新）。</summary>
        public fp3 CharacterUp { get { return _characterUp; } }
        private fp3 _characterUp;
        /// <summary>角色局部 Forward 方向。</summary>
        public fp3 CharacterForward { get { return _characterForward; } }
        private fp3 _characterForward;
        /// <summary>角色局部 Right 方向。</summary>
        public fp3 CharacterRight { get { return _characterRight; } }
        private fp3 _characterRight;
        /// <summary>本帧移动计算开始前的位置。</summary>
        public fp3 InitialSimulationPosition { get { return _initialSimulationPosition; } }
        private fp3 _initialSimulationPosition;
        /// <summary>本帧移动计算开始前的旋转。</summary>
        public fpquaternion InitialSimulationRotation { get { return _initialSimulationRotation; } }
        private fpquaternion _initialSimulationRotation;
        /// <summary>当前站立/附着的动态刚体。</summary>
        public FPDynamicRigidbody AttachedRigidbody { get { return _attachedRigidbody; } }
        private FPDynamicRigidbody _attachedRigidbody;
        /// <summary>角色变换原点到胶囊几何中心的偏移。</summary>
        public fp3 CharacterTransformToCapsuleCenter { get { return _characterTransformToCapsuleCenter; } }
        private fp3 _characterTransformToCapsuleCenter;
        /// <summary>角色变换原点到胶囊底端的偏移。</summary>
        public fp3 CharacterTransformToCapsuleBottom { get { return _characterTransformToCapsuleBottom; } }
        private fp3 _characterTransformToCapsuleBottom;
        /// <summary>角色变换原点到胶囊顶端的偏移。</summary>
        public fp3 CharacterTransformToCapsuleTop { get { return _characterTransformToCapsuleTop; } }
        private fp3 _characterTransformToCapsuleTop;
        /// <summary>角色变换原点到下半球中心的偏移。</summary>
        public fp3 CharacterTransformToCapsuleBottomHemi { get { return _characterTransformToCapsuleBottomHemi; } }
        private fp3 _characterTransformToCapsuleBottomHemi;
        /// <summary>角色变换原点到上半球中心的偏移。</summary>
        public fp3 CharacterTransformToCapsuleTopHemi { get { return _characterTransformToCapsuleTopHemi; } }
        private fp3 _characterTransformToCapsuleTopHemi;
        /// <summary>来自站立刚体或 PhysicsMover 的附加速度。</summary>
        public fp3 AttachedRigidbodyVelocity { get { return _attachedRigidbodyVelocity; } }
        private fp3 _attachedRigidbodyVelocity;
        /// <summary>本帧更新内已记录的重叠数量（每帧初重置）。</summary>
        public int OverlapsCount { get { return _overlapsCount; } }
        private int _overlapsCount;
        /// <summary>本帧更新内记录的重叠结果数组。</summary>
        public FPOverlapResult[] Overlaps { get { return _overlaps; } }
        private FPOverlapResult[] _overlaps = new FPOverlapResult[MaxRigidbodyOverlapsCount];

        /// <summary>绑定的角色控制器，处理速度/旋转/碰撞回调。</summary>
        public IFPCharacterController CharacterController;
        /// <summary>最近一次移动 Sweep 迭代是否检测到地面。</summary>
        public bool LastMovementIterationFoundAnyGround;
        /// <summary>在 FPKinematicCharacterSystem 数组中的索引。</summary>
        public int IndexInCharacterSystem;
        /// <summary>整 tick 仿真开始前记住的初始位置（用于插值）。</summary>
        public fp3 InitialTickPosition;
        /// <summary>整 tick 仿真开始前记住的初始旋转。</summary>
        public fpquaternion InitialTickRotation;
        /// <summary>强制指定附着的动态刚体，覆盖地面自动检测。</summary>
        public FPDynamicRigidbody AttachedRigidbodyOverride;
        /// <summary>角色自身（不含附着体）的基础速度。</summary>
        public fp3 BaseVelocity;

        // 私有运行时缓存与标志
        private FPRaycastHit[] _internalCharacterHits = new FPRaycastHit[MaxHitsBudget];
        private IFPCollider[] _internalProbedColliders = new IFPCollider[MaxCollisionBudget];
        private List<FPDynamicRigidbody> _rigidbodiesPushedThisMove = new List<FPDynamicRigidbody>(16);
        private FPRigidbodyProjectionHit[] _internalRigidbodyProjectionHits = new FPRigidbodyProjectionHit[MaxRigidbodyOverlapsCount];
        private FPDynamicRigidbody _lastAttachedRigidbody;
        private bool _solveMovementCollisions = true;
        private bool _solveGrounding = true;
        private bool _movePositionDirty = false;
        private fp3 _movePositionTarget = fp3.zero;
        private bool _moveRotationDirty = false;
        private fpquaternion _moveRotationTarget = fpquaternion.identity;
        private bool _lastSolvedOverlapNormalDirty = false;
        private fp3 _lastSolvedOverlapNormal = FPMathKCC.WorldForward;
        private int _rigidbodyProjectionHitCount = 0;
        private bool _isMovingFromAttachedRigidbody = false;
        private bool _mustUnground = false;
        private fp _mustUngroundTimeCounter = (fp)0;
        private fp3 _cachedWorldUp = FPMathKCC.WorldUp;
        private fp3 _cachedWorldForward = FPMathKCC.WorldForward;
        private fp3 _cachedWorldRight = FPMathKCC.WorldRight;
        private fp3 _cachedZeroVector = fp3.zero;

        private fpquaternion _transientRotation;
        /// <summary>角色更新阶段的目标/瞬态旋转。</summary>
        public fpquaternion TransientRotation
        {
            get
            {
                return _transientRotation;
            }
            private set
            {
                _transientRotation = value;
                _characterUp = _transientRotation * _cachedWorldUp;
                _characterForward = _transientRotation * _cachedWorldForward;
                _characterRight = _transientRotation * _cachedWorldRight;
            }
        }

        /// <summary>角色总速度 = BaseVelocity + 附着体速度。</summary>
        public fp3 Velocity
        {
            get
            {
                return BaseVelocity + _attachedRigidbodyVelocity;
            }
        }

        // 警告：除非完全理解算法，否则不要修改这些常量！
        /// <summary>单次查询最大命中数预算。</summary>
        public const int MaxHitsBudget = 16;

        /// <summary>单次查询最大碰撞体数量预算。</summary>
        public const int MaxCollisionBudget = 16;

        /// <summary>地面探测 Sweep 最大迭代次数。</summary>
        public const int MaxGroundingSweepIterations = 2;

        /// <summary>台阶检测 Sweep 最大迭代次数。</summary>
        public const int MaxSteppingSweepIterations = 3;

        /// <summary>与刚体重叠记录的最大数量。</summary>
        public const int MaxRigidbodyOverlapsCount = 16;

        /// <summary>碰撞解算时的安全分离偏移。</summary>
        public static readonly fp CollisionOffset = (fp)0.01f;

        /// <summary>单帧解穿透最大位移，防止异常穿透深度把角色弹出世界。</summary>
        public static readonly fp MaxDepenetrationDistance = (fp)10;

        /// <summary>MovePosition 等效速度计算时，单帧最大位移修正（防止 Motor/Entity 不同步时速度爆炸）。</summary>
        public static readonly fp MaxPositionCorrectionPerFrame = (fp)2;

        /// <summary>Motor 与实体 Transform 允许的最大偏差；超过则强制对齐实体位置。</summary>
        public static readonly fp MaxMotorEntityDesyncDistance = (fp)1;

        /// <summary>地面探测反弹时的剩余距离上限。</summary>
        public static readonly fp GroundProbeReboundDistance = (fp)0.02f;

        /// <summary>地面探测最小下探距离。</summary>
        public static readonly fp MinimumGroundProbingDistance = (fp)0.005f;

        /// <summary>地面 Sweep 起始回退距离。</summary>
        public static readonly fp GroundProbingBackstepDistance = (fp)0.1f;

        /// <summary>移动 Sweep 起始回退距离。</summary>
        public static readonly fp SweepProbingBackstepDistance = (fp)0.002f;

        /// <summary>二级探测（ledge 等）垂直偏移。</summary>
        public static readonly fp SecondaryProbesVertical = (fp)0.02f;

        /// <summary>二级探测水平偏移。</summary>
        public static readonly fp SecondaryProbesHorizontal = (fp)0.001f;

        /// <summary>低于该速度模长视为零速度。</summary>
        public static readonly fp MinVelocityMagnitude = (fp)0.01f;

        /// <summary>上台阶时向前探测的距离。</summary>
        public static readonly fp SteppingForwardDistance = (fp)0.03f;

        /// <summary>ledge 检测的最小距离阈值。</summary>
        public static readonly fp MinDistanceForLedge = (fp)0.05f;

        /// <summary>判定为垂直障碍的相关性阈值。</summary>
        public static readonly fp CorrelationForVerticalObstruction = (fp)0.01f;

        /// <summary>Extra 台阶模式额外前探距离。</summary>
        public static readonly fp ExtraSteppingForwardDistance = (fp)0.01f;

        /// <summary>台阶高度检测时的额外 padding。</summary>
        public static readonly fp ExtraStepHeightPadding = (fp)0.01f;
        

        /// <summary>设置移动时是否解算碰撞。</summary>
        public void SetMovementCollisionsSolvingActivation(bool movementCollisionsSolvingActive)
        {
            _solveMovementCollisions = movementCollisionsSolvingActive;
        }

        /// <summary>设置是否对所有命中进行接地/稳定性评估。</summary>
        public void SetGroundSolvingActivation(bool stabilitySolvingActive)
        {
            _solveGrounding = stabilitySolvingActive;
        }

        /// <summary>直接设置角色位置。</summary>
        public void SetPosition(fp3 position, bool bypassInterpolation = true)
        {
            _transientPosition = position;
            _initialSimulationPosition = position;
            _transientPosition = position;

            if (bypassInterpolation)
            {
                InitialTickPosition = position;
            }
        }

        /// <summary>直接设置角色旋转。</summary>
        public void SetRotation(fpquaternion rotation, bool bypassInterpolation = true)
        {
            _transientRotation = rotation;
            _initialSimulationRotation = rotation;
            TransientRotation = rotation;

            if (bypassInterpolation)
            {
                InitialTickRotation = rotation;
            }
        }

        /// <summary>同时设置角色位置与旋转。</summary>
        public void SetPositionAndRotation(fp3 position, fpquaternion rotation, bool bypassInterpolation = true)
        {
            // 内部同步初始与瞬态变换
            _initialSimulationPosition = position;
            _initialSimulationRotation = rotation;
            _transientPosition = position;
            TransientRotation = rotation;

            if (bypassInterpolation)
            {
                InitialTickPosition = position;
                InitialTickRotation = rotation;
            }
        }

        /// <summary>请求移动到目标位置；实际移动在下次马达更新时执行并做碰撞解算。</summary>
        public void MoveCharacter(fp3 toPosition)
        {
            SetMovePositionTarget(toPosition);
        }

        /// <summary>请求旋转到目标朝向；实际旋转在下次马达更新时执行。</summary>
        public void RotateCharacter(fpquaternion toRotation)
        {
            SetMoveRotationTarget(toRotation);
        }

        /// <summary>导出与仿真相关的完整马达状态。</summary>
        public FPKinematicCharacterMotorState GetState()
        {
            FPKinematicCharacterMotorState state = new FPKinematicCharacterMotorState();

            state.Position = _transientPosition;
            state.Rotation = _transientRotation;

            state.BaseVelocity = BaseVelocity;
            state.AttachedRigidbodyVelocity = _attachedRigidbodyVelocity;

            state.MustUnground = _mustUnground;
            state.MustUngroundTime = _mustUngroundTimeCounter;
            state.LastMovementIterationFoundAnyGround = LastMovementIterationFoundAnyGround;
            state.GroundingStatus.CopyFrom(GroundingStatus);
            state.AttachedRigidbody = _attachedRigidbody;
            state.AttachedMover = _attachedMover;

            return state;
        }

        /// <summary>立即应用马达状态快照。</summary>
        public void ApplyState(FPKinematicCharacterMotorState state, bool bypassInterpolation = true)
        {
            SetPositionAndRotation(state.Position, state.Rotation, bypassInterpolation);

            BaseVelocity = state.BaseVelocity;
            _attachedRigidbodyVelocity = state.AttachedRigidbodyVelocity;

            _mustUnground = state.MustUnground;
            _mustUngroundTimeCounter = state.MustUngroundTime;
            LastMovementIterationFoundAnyGround = state.LastMovementIterationFoundAnyGround;
            GroundingStatus.CopyFrom(state.GroundingStatus);
            _attachedRigidbody = state.AttachedRigidbody;
            _attachedMover = state.AttachedMover;
        }

        /// <summary>设置胶囊尺寸并缓存几何偏移数据。</summary>
        public void SetCapsuleDimensions(fp radius, fp height, fp yOffset)
        {
            height = fpmath.max(height, (radius * (fp)2) + (fp)0.01f);

            CapsuleRadius = radius;
            CapsuleHeight = height;
            CapsuleYOffset = yOffset;

            fp3 capsuleCenter = new fp3((fp)0, CapsuleYOffset, (fp)0);
            fp clampedHeight = fpmath.clamp(CapsuleHeight, CapsuleRadius * (fp)2, CapsuleHeight);
            _characterTransformToCapsuleCenter = capsuleCenter;
            _characterTransformToCapsuleBottom = capsuleCenter + (-_cachedWorldUp * (clampedHeight * (fp)0.5f));
            _characterTransformToCapsuleTop = capsuleCenter + (_cachedWorldUp * (clampedHeight * (fp)0.5f));
            _characterTransformToCapsuleBottomHemi = capsuleCenter + (-_cachedWorldUp * (clampedHeight * (fp)0.5f)) + (_cachedWorldUp * CapsuleRadius);
            _characterTransformToCapsuleTopHemi = capsuleCenter + (_cachedWorldUp * (clampedHeight * (fp)0.5f)) + (-_cachedWorldUp * CapsuleRadius);
        }

        /// <summary>
        /// 更新阶段一：在 PhysicsMover 计算完速度之后、模拟目标位姿之前调用。负责：
        /// 初始化本帧状态、处理 MovePosition、解初始重叠、地面探测、检测可交互刚体。
        /// </summary>
        public void UpdatePhase1(fp deltaTime)
        {
            _rigidbodiesPushedThisMove.Clear();

            // 更新前回调
            CharacterController.BeforeCharacterUpdate(deltaTime);

            SyncTransientPositionFromEntityIfDesynced();

            _initialSimulationPosition = _transientPosition;
            _initialSimulationRotation = _transientRotation;
            _rigidbodyProjectionHitCount = 0;
            _overlapsCount = 0;
            _lastSolvedOverlapNormalDirty = false;

            #region 处理 MovePosition
            if (_movePositionDirty)
            {
                if (_solveMovementCollisions)
                {
                    // 将目标位移转为等效速度并做碰撞移动
                    fp3 tmpVelocity = GetVelocityFromMovement(_movePositionTarget - _transientPosition, deltaTime);
                    if (InternalCharacterMove(ref tmpVelocity, deltaTime))
                    {
                        if (InteractiveRigidbodyHandling)
                        {
                            ProcessVelocityForRigidbodyHits(ref tmpVelocity, deltaTime);
                        }
                    }
                }
                else
                {
                    _transientPosition = _movePositionTarget;
                }

                _movePositionDirty = false;
            }
            #endregion

            LastGroundingStatus.CopyFrom(GroundingStatus);
            GroundingStatus = new FPCharacterGroundingReport();
            GroundingStatus.GroundNormal = _characterUp;

            if (_solveMovementCollisions)
            {
                #region 解初始重叠
                fp3 resolutionDirection = _cachedWorldUp;
                fp resolutionDistance = (fp)0;
                int iterationsMade = 0;
                bool overlapSolved = false;
                while (iterationsMade < MaxDecollisionIterations && !overlapSolved)
                {
                    int nbOverlaps = CharacterCollisionsOverlap(_transientPosition, _transientRotation, _internalProbedColliders);

                    if (nbOverlaps > 0)
                    {
                        // 优先解算非动态刚体/移动平台的重叠
                        for (int i = 0; i < nbOverlaps; i++)
                        {
                            if (GetInteractiveRigidbody(_internalProbedColliders[i]) == null)
                            {
                                // 计算穿透深度与分离方向
                                IFPCollider overlappedCollider = _internalProbedColliders[i];
                                if (TryGetDepenetration(
                                        _transientPosition,
                                        _transientRotation,
                                        overlappedCollider,
                                        out resolutionDirection,
                                        out resolutionDistance))
                                {
                                    // 沿障碍法线方向分离
                                    FPHitStabilityReport mockReport = new FPHitStabilityReport();
                                    mockReport.IsStable = IsStableOnNormal(resolutionDirection);
                                    resolutionDirection = GetObstructionNormal(resolutionDirection, mockReport.IsStable);

                                    // 应用分离位移
                                    fp3 resolutionMovement = resolutionDirection * (resolutionDistance + CollisionOffset);
                                    _transientPosition += resolutionMovement;

                                    // 记录重叠供后续 Sweep 投影使用
                                    if (_overlapsCount < _overlaps.Length)
                                    {
                                        _overlaps[_overlapsCount] = new FPOverlapResult(resolutionDirection, _internalProbedColliders[i]);
                                        _overlapsCount++;
                                    }

                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        overlapSolved = true;
                    }

                    iterationsMade++;
                }
                #endregion
            }

            #region 地面探测与吸附
            // 处理强制离地
            if (_solveGrounding)
            {
                if (MustUnground())
                {
                    _transientPosition += _characterUp * (MinimumGroundProbingDistance * (fp)1.5f);
                }
                else
                {
                    // 根据上一帧接地状态选择下探距离
                    fp selectedGroundProbingDistance = MinimumGroundProbingDistance;
                    if (!LastGroundingStatus.SnappingPrevented && (LastGroundingStatus.IsStableOnGround || LastMovementIterationFoundAnyGround))
                    {
                        if (StepHandling != FPStepHandlingMethod.None)
                        {
                            selectedGroundProbingDistance = fpmath.max(CapsuleRadius, MaxStepHeight);
                        }
                        else
                        {
                            selectedGroundProbingDistance = CapsuleRadius;
                        }

                        selectedGroundProbingDistance += GroundDetectionExtraDistance;
                    }

                    ProbeGround(ref _transientPosition, _transientRotation, selectedGroundProbingDistance, ref GroundingStatus);

                    if (!LastGroundingStatus.IsStableOnGround && GroundingStatus.IsStableOnGround)
                    {
                        // 稳定着陆：将速度投影到地面切平面
                        BaseVelocity = FPMathKCC.ProjectOnPlane(BaseVelocity, CharacterUp);
                        BaseVelocity = GetDirectionTangentToSurface(BaseVelocity, GroundingStatus.GroundNormal) * fpmath.length(BaseVelocity);
                    }
                }
            }

            LastMovementIterationFoundAnyGround = false;

            if (_mustUngroundTimeCounter > (fp)0)
            {
                _mustUngroundTimeCounter -= deltaTime;
            }
            _mustUnground = false;
            #endregion

            if (_solveGrounding)
            {
                CharacterController.PostGroundingUpdate(deltaTime);
            }

            if (InteractiveRigidbodyHandling)
            {
                #region 可交互刚体处理
                _lastAttachedRigidbody = _attachedRigidbody;
                _lastAttachedMover = _attachedMover;
                if (AttachedRigidbodyOverride != null || AttachedMoverOverride != null)
                {
                    _attachedRigidbody = AttachedRigidbodyOverride;
                    _attachedMover = AttachedMoverOverride;
                }
                else
                {
                    _attachedRigidbody = null;
                    _attachedMover = null;
                    if (GroundingStatus.IsStableOnGround && GroundingStatus.GroundCollider != null)
                    {
                        IFPCollider groundCollider = GroundingStatus.GroundCollider;
                        if (groundCollider.AttachedMover != null)
                        {
                            _attachedMover = groundCollider.AttachedMover;
                            _attachedRigidbody = groundCollider.AttachedBody;
                        }
                        else
                        {
                            _attachedRigidbody = GetInteractiveRigidbody(groundCollider);
                        }
                    }
                }

                fp3 tmpVelocityFromCurrentAttachedRigidbody = fp3.zero;
                fp3 tmpAngularVelocityFromCurrentAttachedRigidbody = fp3.zero;
                if (_attachedMover != null)
                {
                    GetVelocityFromMoverMovement(_attachedMover, _transientPosition, deltaTime, out tmpVelocityFromCurrentAttachedRigidbody, out tmpAngularVelocityFromCurrentAttachedRigidbody);
                }
                else if (_attachedRigidbody != null)
                {
                    GetVelocityFromRigidbodyMovement(_attachedRigidbody, _transientPosition, deltaTime, out tmpVelocityFromCurrentAttachedRigidbody, out tmpAngularVelocityFromCurrentAttachedRigidbody);
                }

                // 脱离附着体时保留动量
                if (PreserveAttachedRigidbodyMomentum && (_lastAttachedRigidbody != null || _lastAttachedMover != null) && (_attachedRigidbody != _lastAttachedRigidbody || _attachedMover != _lastAttachedMover))
                {
                    BaseVelocity += _attachedRigidbodyVelocity;
                    BaseVelocity -= tmpVelocityFromCurrentAttachedRigidbody;
                }

                // 累加来自附着体/移动平台的速度与旋转
                _attachedRigidbodyVelocity = _cachedZeroVector;
                if (_attachedRigidbody != null || _attachedMover != null)
                {
                    _attachedRigidbodyVelocity = tmpVelocityFromCurrentAttachedRigidbody;

                    // 附着体角速度带来的朝向变化
                    fp3 projectedForward = FPMathKCC.ProjectOnPlane(fpmath1.EulerXYZ(tmpAngularVelocityFromCurrentAttachedRigidbody * deltaTime) * _characterForward, _characterUp);
                    if (fpmath1.sqrMagnitude(projectedForward) > (fp)0)
                    {
                        fp3 newForward = fpmath.normalize(projectedForward);
                        TransientRotation = fpmath1.LookRotation(newForward, _characterUp);
                    }
                }

                // 刚落到移动平台上时，抵消水平附着速度分量
                if (GroundingStatus.GroundCollider != null &&
                    (GroundingStatus.GroundCollider.AttachedBody == _attachedRigidbody || GroundingStatus.GroundCollider.AttachedMover == _attachedMover) &&
                    (_attachedRigidbody != null || _attachedMover != null) &&
                    _lastAttachedRigidbody == null && _lastAttachedMover == null)
                {
                    BaseVelocity -= FPMathKCC.ProjectOnPlane(_attachedRigidbodyVelocity, _characterUp);
                }

                // 按附着体速度移动角色
                if (fpmath1.sqrMagnitude(_attachedRigidbodyVelocity) > (fp)0)
                {
                    _isMovingFromAttachedRigidbody = true;

                    if (_solveMovementCollisions)
                    {
                        InternalCharacterMove(ref _attachedRigidbodyVelocity, deltaTime);
                    }
                    else
                    {
                        _transientPosition += _attachedRigidbodyVelocity * deltaTime;
                    }

                    _isMovingFromAttachedRigidbody = false;
                }
                #endregion
            }
        }

        /// <summary>
        /// 更新阶段二：在 PhysicsMover 模拟完目标位姿之后调用。
        /// 结束时 TransientPosition/Rotation 即为本帧最终位姿。负责：
        /// 旋转解算、MoveRotation、附着刚体重叠解算、速度移动、平面约束。
        /// </summary>
        public void UpdatePhase2(fp deltaTime)
        {
            // 由控制器更新旋转
            CharacterController.UpdateRotation(ref _transientRotation, deltaTime);
            TransientRotation = _transientRotation;

            // 处理 MoveRotation 请求
            if (_moveRotationDirty)
            {
                TransientRotation = _moveRotationTarget;
                _moveRotationDirty = false;
            }

            if (_solveMovementCollisions && InteractiveRigidbodyHandling)
            {
                if (InteractiveRigidbodyHandling)
                {
                    #region 解算附着刚体/移动平台可能造成的重叠
                    if (_attachedRigidbody != null || _attachedMover != null)
                    {
                        fp upwardsOffset = CapsuleRadius;

                        FPRaycastHit closestHit;
                        if (CharacterGroundSweep(
                            _transientPosition + (_characterUp * upwardsOffset),
                            _transientRotation,
                            -_characterUp,
                            upwardsOffset,
                            out closestHit))
                        {
                            if ((closestHit.Collider.AttachedBody == _attachedRigidbody || closestHit.Collider.AttachedMover == _attachedMover) && IsStableOnNormal(closestHit.Normal))
                            {
                                fp distanceMovedUp = (upwardsOffset - closestHit.Distance);
                                _transientPosition = _transientPosition + (_characterUp * distanceMovedUp) + (_characterUp * CollisionOffset);
                            }
                        }
                    }
                    #endregion
                }

                if (InteractiveRigidbodyHandling)
                {
                    #region 解算旋转或移动平台模拟导致的重叠
                    fp3 resolutionDirection = _cachedWorldUp;
                    fp resolutionDistance = (fp)0;
                    int iterationsMade = 0;
                    bool overlapSolved = false;
                    while (iterationsMade < MaxDecollisionIterations && !overlapSolved)
                    {
                        int nbOverlaps = CharacterCollisionsOverlap(_transientPosition, _transientRotation, _internalProbedColliders);
                        if (nbOverlaps > 0)
                        {
                            for (int i = 0; i < nbOverlaps; i++)
                            {
                                // 处理单个重叠
                                IFPCollider overlappedCollider = _internalProbedColliders[i];
                                if (TryGetDepenetration(
                                        _transientPosition,
                                        _transientRotation,
                                        overlappedCollider,
                                        out resolutionDirection,
                                        out resolutionDistance))
                                {
                                    // 沿障碍法线分离
                                    FPHitStabilityReport mockReport = new FPHitStabilityReport();
                                    mockReport.IsStable = IsStableOnNormal(resolutionDirection);
                                    resolutionDirection = GetObstructionNormal(resolutionDirection, mockReport.IsStable);

                                    // 应用分离位移
                                    fp3 resolutionMovement = resolutionDirection * (resolutionDistance + CollisionOffset);
                                    _transientPosition += resolutionMovement;

                                    // 可交互刚体：记录命中供后续速度处理
                                    if (InteractiveRigidbodyHandling)
                                    {
                                        FPDynamicRigidbody probedRigidbody = GetInteractiveRigidbody(_internalProbedColliders[i]);
                                        if (probedRigidbody != null)
                                        {
                                            FPHitStabilityReport tmpReport = new FPHitStabilityReport();
                                            tmpReport.IsStable = IsStableOnNormal(resolutionDirection);
                                            if (tmpReport.IsStable)
                                            {
                                                LastMovementIterationFoundAnyGround = tmpReport.IsStable;
                                            }
                                            if (probedRigidbody != _attachedRigidbody)
                                            {
                                                fp3 characterCenter = _transientPosition + (_transientRotation * _characterTransformToCapsuleCenter);
                                                fp3 estimatedCollisionPoint = _transientPosition;


                                                StoreRigidbodyHit(
                                                    probedRigidbody,
                                                    Velocity,
                                                    estimatedCollisionPoint,
                                                    resolutionDirection,
                                                    tmpReport);
                                            }
                                        }
                                    }

                                    // 记录重叠
                                    if (_overlapsCount < _overlaps.Length)
                                    {
                                        _overlaps[_overlapsCount] = new FPOverlapResult(resolutionDirection, _internalProbedColliders[i]);
                                        _overlapsCount++;
                                    }

                                    break;
                                }
                            }
                        }
                        else
                        {
                            overlapSolved = true;
                        }

                        iterationsMade++;
                    }
                    #endregion
                }
            }

            // 由控制器更新速度
            CharacterController.UpdateVelocity(ref BaseVelocity, deltaTime);

            if (fpmath.length(BaseVelocity) < MinVelocityMagnitude)
            {
                BaseVelocity = fp3.zero;
            }

            #region 由基础速度驱动角色移动
            // 按 BaseVelocity 执行碰撞移动
            if (fpmath1.sqrMagnitude(BaseVelocity) > (fp)0)
            {
                if (_solveMovementCollisions)
                {
                    InternalCharacterMove(ref BaseVelocity, deltaTime);
                }
                else
                {
                    _transientPosition += BaseVelocity * deltaTime;
                }
            }

            // 处理与动态刚体碰撞对速度的影响
            if (InteractiveRigidbodyHandling)
            {
                ProcessVelocityForRigidbodyHits(ref BaseVelocity, deltaTime);
            }
            #endregion

            // 应用平面运动约束
            if (HasPlanarConstraint)
            {
                _transientPosition = _initialSimulationPosition + FPMathKCC.ProjectOnPlane(_transientPosition - _initialSimulationPosition, PlanarConstraintAxis);
            }

            // 离散碰撞检测（重叠即回调，非 Sweep）
            if (DiscreteCollisionEvents)
            {
                int nbOverlaps = CharacterCollisionsOverlap(_transientPosition, _transientRotation, _internalProbedColliders, CollisionOffset * (fp)2);
                for (int i = 0; i < nbOverlaps; i++)
                {
                    CharacterController.OnDiscreteCollisionDetected(_internalProbedColliders[i]);
                }
            }

            CharacterController.AfterCharacterUpdate(deltaTime);
        }

        /// <summary>判断给定法线坡度是否在 MaxStableSlopeAngle 内可稳定站立。</summary>
        private bool IsStableOnNormal(fp3 normal)
        {
            return FPMathKCC.Angle(_characterUp, normal) <= MaxStableSlopeAngle;
        }

        /// <summary>在 ledge/落差等特殊情况下进一步判定稳定性。</summary>
        private bool IsStableWithSpecialCases(ref FPHitStabilityReport stabilityReport, fp3 velocity)
        {
            if (LedgeAndDenivelationHandling)
            {
                if (stabilityReport.LedgeDetected)
                {
                    if (stabilityReport.IsMovingTowardsEmptySideOfLedge)
                    {
                        // ledge 外侧：速度过大则禁止贴地
                        fp3 velocityOnLedgeNormal = FPKCCMath.Project(velocity, stabilityReport.LedgeFacingDirection);
                        if (fpmath.length(velocityOnLedgeNormal) >= MaxVelocityForLedgeSnap)
                        {
                            return false;
                        }
                    }

                    // 距 ledge 边缘过远则不稳定
                    if (stabilityReport.IsOnEmptySideOfLedge && stabilityReport.DistanceFromLedge > MaxStableDistanceFromLedge)
                    {
                        return false;
                    }
                }

                // 内外法线落差角过大时禁止贴地（防止“弹射”离坡）
                if (LastGroundingStatus.FoundAnyGround && fpmath1.sqrMagnitude(stabilityReport.InnerNormal) != (fp)0 && fpmath1.sqrMagnitude(stabilityReport.OuterNormal) != (fp)0)
                {
                    fp denivelationAngle = FPMathKCC.Angle(stabilityReport.InnerNormal, stabilityReport.OuterNormal);
                    if (denivelationAngle > MaxStableDenivelationAngle)
                    {
                        return false;
                    }
                    else
                    {
                        denivelationAngle = FPMathKCC.Angle(LastGroundingStatus.InnerGroundNormal, stabilityReport.OuterNormal);
                        if (denivelationAngle > MaxStableDenivelationAngle)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>向下探测有效地面；若可吸附则修改 probingPosition。</summary>
        public void ProbeGround(ref fp3 probingPosition, fpquaternion atRotation, fp probingDistance, ref FPCharacterGroundingReport groundingReport)
        {
            if (probingDistance < MinimumGroundProbingDistance)
            {
                probingDistance = MinimumGroundProbingDistance;
            }

            int groundSweepsMade = 0;
            FPRaycastHit groundSweepHit = new FPRaycastHit();
            bool groundSweepingIsOver = false;
            fp3 groundSweepPosition = probingPosition;
            fp3 groundSweepDirection = (atRotation * -_cachedWorldUp);
            fp groundProbeDistanceRemaining = probingDistance;
            while (groundProbeDistanceRemaining > 0 && (groundSweepsMade <= MaxGroundingSweepIterations) && !groundSweepingIsOver)
            {
                // 向下 Sweep 检测地面
                if (CharacterGroundSweep(
                        groundSweepPosition,
                        atRotation,
                        groundSweepDirection,
                        groundProbeDistanceRemaining,
                        out groundSweepHit))
                {
                    fp3 targetPosition = groundSweepPosition + (groundSweepDirection * groundSweepHit.Distance);
                    FPHitStabilityReport groundHitStabilityReport = new FPHitStabilityReport();
                    EvaluateHitStability(groundSweepHit.Collider, groundSweepHit.Normal, groundSweepHit.Point, targetPosition, _transientRotation, BaseVelocity, ref groundHitStabilityReport);

                    groundingReport.FoundAnyGround = true;
                    groundingReport.GroundNormal = groundSweepHit.Normal;
                    groundingReport.InnerGroundNormal = groundHitStabilityReport.InnerNormal;
                    groundingReport.OuterGroundNormal = groundHitStabilityReport.OuterNormal;
                    groundingReport.GroundCollider = groundSweepHit.Collider;
                    groundingReport.GroundPoint = groundSweepHit.Point;
                    groundingReport.SnappingPrevented = false;

                    // 找到稳定地面
                    if (groundHitStabilityReport.IsStable)
                    {
                        // 特殊情况下取消贴地吸附
                        groundingReport.SnappingPrevented = !IsStableWithSpecialCases(ref groundHitStabilityReport, BaseVelocity);

                        groundingReport.IsStableOnGround = true;

                        // 执行地面吸附
                        if (!groundingReport.SnappingPrevented)
                        {
                            probingPosition = groundSweepPosition + (groundSweepDirection * (groundSweepHit.Distance - CollisionOffset));
                        }

                        CharacterController.OnGroundHit(groundSweepHit.Collider, groundSweepHit.Normal, groundSweepHit.Point, ref groundHitStabilityReport);
                        groundSweepingIsOver = true;
                    }
                    else
                    {
                        // 沿命中面“爬过”后继续探测
                        fp3 sweepMovement = (groundSweepDirection * groundSweepHit.Distance) + ((atRotation * _cachedWorldUp) * fpmath.max(CollisionOffset, groundSweepHit.Distance));
                        groundSweepPosition = groundSweepPosition + sweepMovement;

                        // 更新剩余探测距离
                        groundProbeDistanceRemaining = fpmath.min(GroundProbeReboundDistance, fpmath.max(groundProbeDistanceRemaining - fpmath.length(sweepMovement), (fp)0));

                        // 沿命中面重定向探测方向
                        groundSweepDirection = FPMathKCC.ProjectOnPlane(groundSweepDirection, groundSweepHit.Normal);
                    }
                }
                else
                {
                    groundSweepingIsOver = true;
                }

                groundSweepsMade++;
            }
        }

        /// <summary>强制在下次接地更新时离地；time 为持续时间，0 则默认 0.1 秒。</summary>
        public void ForceUnground(fp time = default)
        {
            _mustUnground = true;
            _mustUngroundTimeCounter = time == (fp)0 ? (fp)0.1f : time;
        }

        /// <summary>当前是否处于强制离地状态（含计时）。</summary>
        public bool MustUnground()
        {
            return _mustUnground || _mustUngroundTimeCounter > (fp)0;
        }

        private bool TryGetDepenetration(
            fp3 position,
            fpquaternion rotation,
            IFPCollider collider,
            out fp3 direction,
            out fp distance)
        {
            direction = fp3.zero;
            distance = (fp)0;

            if (!FPPhysicsQuery.ComputePenetration(
                    position,
                    rotation,
                    CapsuleRadius,
                    CapsuleHeight,
                    CapsuleYOffset,
                    collider,
                    out direction,
                    out distance))
            {
                return false;
            }

            if (fpmath1.sqrMagnitude(direction) <= (fp)0.0000001f || distance <= (fp)0)
            {
                direction = fp3.zero;
                distance = (fp)0;
                return false;
            }

            direction = fpmath.normalize(direction);
            distance = fpmath.min(distance, MaxDepenetrationDistance);
            return true;
        }

        /// <summary>
        /// 将方向调整为相对角色 Up 与给定表面法线的切向方向；
        /// 用于在斜面上重定向移动而不产生横向漂移。
        /// </summary>
        public fp3 GetDirectionTangentToSurface(fp3 direction, fp3 surfaceNormal)
        {
            fp3 directionRight = fpmath.cross(direction, _characterUp);
            return FPMathKCC.NormalizeSafe(fpmath.cross(surfaceNormal, directionRight));
        }

        /// <summary>
        /// 按给定速度移动角色，含碰撞 Sweep、台阶处理与速度投影。
        /// </summary>
        /// <returns>若未能在迭代限制内完全解算移动则返回 false。</returns>
        private bool InternalCharacterMove(ref fp3 transientVelocity, fp deltaTime)
        {
            if (deltaTime <= (fp)0)
                return false;

            // 平面约束：先投影速度
            if (HasPlanarConstraint)
            {
                transientVelocity = FPMathKCC.ProjectOnPlane(transientVelocity, PlanarConstraintAxis);
            }

            fp remainingMovementMagnitude = fpmath.length(transientVelocity) * deltaTime;
            if (remainingMovementMagnitude <= (fp)0)
            {
                return true;
            }

            bool wasCompleted = true;
            fp3 remainingMovementDirection = fpmath.normalize(transientVelocity);
            fp3 originalVelocityDirection = remainingMovementDirection;
            int sweepsMade = 0;
            bool hitSomethingThisSweepIteration = true;
            fp3 tmpMovedPosition = _transientPosition;
            bool previousHitIsStable = false;
            fp3 previousVelocity = _cachedZeroVector;
            fp3 previousObstructionNormal = _cachedZeroVector;
            FPMovementSweepState sweepState = FPMovementSweepState.Initial;

            // Sweep 前先对已有重叠做速度投影
            for (int i = 0; i < _overlapsCount; i++)
            {
                fp3 overlapNormal = _overlaps[i].Normal;
                if (fpmath.dot(remainingMovementDirection, overlapNormal) < (fp)0)
                {
                    bool stableOnHit = IsStableOnNormal(overlapNormal) && !MustUnground();
                    fp3 velocityBeforeProjection = transientVelocity;
                    fp3 obstructionNormal = GetObstructionNormal(overlapNormal, stableOnHit);

                    InternalHandleVelocityProjection(
                        stableOnHit,
                        overlapNormal,
                        obstructionNormal,
                        originalVelocityDirection,
                        ref sweepState,
                        previousHitIsStable,
                        previousVelocity,
                        previousObstructionNormal,
                        ref transientVelocity,
                        ref remainingMovementMagnitude,
                        ref remainingMovementDirection);

                    previousHitIsStable = stableOnHit;
                    previousVelocity = velocityBeforeProjection;
                    previousObstructionNormal = obstructionNormal;
                }
            }

            // 迭代 Sweep 检测碰撞并解算移动
            while (remainingMovementMagnitude > (fp)0 &&
                (sweepsMade <= MaxMovementIterations) &&
                hitSomethingThisSweepIteration)
            {
                bool foundClosestHit = false;
                fp3 closestSweepHitPoint = default;
                fp3 closestSweepHitNormal = default;
                fp closestSweepHitDistance = (fp)0;
                fp closestSweepHitPenetration = (fp)0;
                IFPCollider closestSweepHitCollider = null;

                if (CheckMovementInitialOverlaps)
                {
                    int numOverlaps = CharacterCollisionsOverlap(
                                        tmpMovedPosition,
                                        _transientRotation,
                                        _internalProbedColliders,
                                        (fp)0,
                                        false);
                    if (numOverlaps > 0)
                    {
                        closestSweepHitDistance = (fp)0;

                        fp mostObstructingOverlapNormalDotProduct = (fp)2;

                        for (int i = 0; i < numOverlaps; i++)
                        {
                            IFPCollider tmpCollider = _internalProbedColliders[i];

                            if (TryGetDepenetration(
                                tmpMovedPosition,
                                _transientRotation,
                                tmpCollider,
                                out fp3 resolutionDirection,
                                out fp resolutionDistance))
                            {
                                fp dotProduct = fpmath.dot(remainingMovementDirection, resolutionDirection);
                                if (dotProduct < (fp)0 && dotProduct < mostObstructingOverlapNormalDotProduct)
                                {
                                    mostObstructingOverlapNormalDotProduct = dotProduct;

                                    closestSweepHitNormal = resolutionDirection;
                                    closestSweepHitCollider = tmpCollider;
                                    closestSweepHitPenetration = resolutionDistance;
                                    closestSweepHitPoint = tmpMovedPosition + (_transientRotation * CharacterTransformToCapsuleCenter) + (resolutionDirection * resolutionDistance);

                                    foundClosestHit = true;
                                }
                            }
                        }
                    }
                }

                if (!foundClosestHit && CharacterCollisionsSweep(
                        tmpMovedPosition,
                        _transientRotation,
                        remainingMovementDirection,
                        remainingMovementMagnitude + CollisionOffset,
                        out FPRaycastHit closestSweepHit,
                        _internalCharacterHits)
                    > 0)
                {
                    closestSweepHitNormal = closestSweepHit.Normal;
                    closestSweepHitDistance = closestSweepHit.Distance;
                    closestSweepHitCollider = closestSweepHit.Collider;
                    closestSweepHitPoint = closestSweepHit.Point;

                    foundClosestHit = true;
                }

                if (foundClosestHit)
                {
                    bool blockedByOverlap = closestSweepHitDistance <= CollisionOffset && closestSweepHitPenetration > (fp)0;
                    if (blockedByOverlap)
                    {
                        tmpMovedPosition += closestSweepHitNormal * (closestSweepHitPenetration + CollisionOffset);
                        remainingMovementMagnitude = (fp)0;
                    }

                    // 本迭代可移动距离
                    fp3 sweepMovement = (remainingMovementDirection * (fpmath.max((fp)0, closestSweepHitDistance - CollisionOffset)));
                    tmpMovedPosition += sweepMovement;
                    remainingMovementMagnitude -= fpmath.length(sweepMovement);

                    // 评估命中稳定性（含台阶/ledge）
                    FPHitStabilityReport moveHitStabilityReport = new FPHitStabilityReport();
                    EvaluateHitStability(closestSweepHitCollider, closestSweepHitNormal, closestSweepHitPoint, tmpMovedPosition, _transientRotation, transientVelocity, ref moveHitStabilityReport);

                    // 尝试上台阶（障碍高于胶囊底半径时）
                    bool foundValidStepHit = false;
                    if (_solveGrounding && StepHandling != FPStepHandlingMethod.None && moveHitStabilityReport.ValidStepDetected)
                    {
                        fp obstructionCorrelation = fpmath.abs(fpmath.dot(closestSweepHitNormal, _characterUp));
                        if (obstructionCorrelation <= CorrelationForVerticalObstruction)
                        {
                            fp3 stepForwardDirectionPlane = FPMathKCC.ProjectOnPlane(-closestSweepHitNormal, _characterUp);
                            if (fpmath1.sqrMagnitude(stepForwardDirectionPlane) > (fp)0)
                            {
                            fp3 stepForwardDirection = fpmath.normalize(stepForwardDirectionPlane);
                            fp3 stepCastStartPoint = (tmpMovedPosition + (stepForwardDirection * SteppingForwardDistance)) +
                                (_characterUp * MaxStepHeight);

                            // 从台阶高度顶部向下 Cast
                            int nbStepHits = CharacterCollisionsSweep(
                                                stepCastStartPoint,
                                                _transientRotation,
                                                -_characterUp,
                                                MaxStepHeight,
                                                out FPRaycastHit closestStepHit,
                                                _internalCharacterHits,
                                                (fp)0,
                                                true);

                            // 匹配 DetectSteps 识别的台阶碰撞体
                            for (int i = 0; i < nbStepHits; i++)
                            {
                                if (_internalCharacterHits[i].Collider == moveHitStabilityReport.SteppedCollider)
                                {
                                    fp3 endStepPosition = stepCastStartPoint + (-_characterUp * (_internalCharacterHits[i].Distance - CollisionOffset));
                                    tmpMovedPosition = endStepPosition;
                                    foundValidStepHit = true;

                                    // 台阶顶面：速度投影到水平面
                                    transientVelocity = FPMathKCC.ProjectOnPlane(transientVelocity, CharacterUp);
                                    remainingMovementDirection = transientVelocity;

                                    break;
                                }
                            }
                            }
                        }
                    }

                    // 未上台阶则按常规碰撞解算
                    if (!foundValidStepHit)
                    {
                        fp3 obstructionNormal = GetObstructionNormal(closestSweepHitNormal, moveHitStabilityReport.IsStable);

                        // 移动命中回调
                        CharacterController.OnMovementHit(closestSweepHitCollider, closestSweepHitNormal, closestSweepHitPoint, ref moveHitStabilityReport);

                        // 记录可交互刚体命中
                        if (InteractiveRigidbodyHandling && GetInteractiveRigidbody(closestSweepHitCollider) != null)
                        {
                            StoreRigidbodyHit(
                                GetInteractiveRigidbody(closestSweepHitCollider),
                                transientVelocity,
                                closestSweepHitPoint,
                                obstructionNormal,
                                moveHitStabilityReport);
                        }

                        bool stableOnHit = moveHitStabilityReport.IsStable && !MustUnground();
                        fp3 velocityBeforeProj = transientVelocity;

                        // 投影速度供下一迭代使用
                        InternalHandleVelocityProjection(
                            stableOnHit,
                            closestSweepHitNormal,
                            obstructionNormal,
                            originalVelocityDirection,
                            ref sweepState,
                            previousHitIsStable,
                            previousVelocity,
                            previousObstructionNormal,
                            ref transientVelocity,
                            ref remainingMovementMagnitude,
                            ref remainingMovementDirection);

                        previousHitIsStable = stableOnHit;
                        previousVelocity = velocityBeforeProj;
                        previousObstructionNormal = obstructionNormal;
                    }
                }
                // 未命中任何碰撞体
                else
                {
                    hitSomethingThisSweepIteration = false;
                }

                // 超过最大 Sweep 次数的安全处理
                sweepsMade++;
                if (sweepsMade > MaxMovementIterations)
                {
                    if (KillRemainingMovementWhenExceedMaxMovementIterations)
                    {
                        remainingMovementMagnitude = (fp)0;
                    }

                    if (KillVelocityWhenExceedMaxMovementIterations)
                    {
                        transientVelocity = fp3.zero;
                    }
                    wasCompleted = false;
                }
            }

            // 应用剩余位移
            tmpMovedPosition += (remainingMovementDirection * remainingMovementMagnitude);
            _transientPosition = tmpMovedPosition;

            return wasCompleted;
        }

        /// <summary>
        /// 根据当前接地状态计算移动阻挡的有效法线。
        /// 稳定接地且命中面不稳定时，将障碍法线投影到地面切平面，避免贴墙滑动时速度被错误清零。
        /// </summary>
        private fp3 GetObstructionNormal(fp3 hitNormal, bool stableOnHit)
        {
            // 默认使用命中法线作为阻挡法线
            fp3 obstructionNormal = hitNormal;
            if (GroundingStatus.IsStableOnGround && !MustUnground() && !stableOnHit)
            {
                fp3 obstructionLeftAlongGround = fpmath.cross(GroundingStatus.GroundNormal, obstructionNormal);
                obstructionNormal = fpmath.cross(obstructionLeftAlongGround, _characterUp);
            }

            // 平行法线叉积为零时回退到原始命中法线
            if (fpmath1.sqrMagnitude(obstructionNormal) <= (fp)0)
            {
                obstructionNormal = hitNormal;
            }

            return obstructionNormal;
        }

        /// <summary>
        /// 记录与可交互刚体的碰撞命中，供 Phase2 末尾 ProcessVelocityForRigidbodyHits 做冲量交换。
        /// </summary>
        private void StoreRigidbodyHit(FPDynamicRigidbody hitRigidbody, fp3 hitVelocity, fp3 hitPoint, fp3 obstructionNormal, FPHitStabilityReport FPHitStabilityReport)
        {
            if (_rigidbodyProjectionHitCount < _internalRigidbodyProjectionHits.Length)
            {
                if (!IsBodyOwnedByCharacterMotor(hitRigidbody))
                {
                    FPRigidbodyProjectionHit rph = new FPRigidbodyProjectionHit();
                    rph.Rigidbody = hitRigidbody;
                    rph.HitPoint = hitPoint;
                    rph.EffectiveHitNormal = obstructionNormal;
                    rph.HitVelocity = hitVelocity;
                    rph.StableOnHit = FPHitStabilityReport.IsStable;

                    _internalRigidbodyProjectionHits[_rigidbodyProjectionHitCount] = rph;
                    _rigidbodyProjectionHitCount++;
                }
            }
        }

        /// <summary>直接设置瞬态位置（不经碰撞解算，供调试或特殊逻辑使用）。</summary>
        public void SetTransientPosition(fp3 newPos)
        {
            _transientPosition = newPos;
        }

        /// <summary>
        /// 命中后处理速度投影，并维护 Sweep 状态机（折痕/角落阻塞）。
        /// 由 InternalCharacterMove 在每轮 Sweep 命中或初始重叠时调用。
        /// </summary>
        private void InternalHandleVelocityProjection(bool stableOnHit, fp3 hitNormal, fp3 obstructionNormal, fp3 originalDirection,
            ref FPMovementSweepState sweepState, bool previousHitIsStable, fp3 previousVelocity, fp3 previousObstructionNormal,
            ref fp3 transientVelocity, ref fp remainingMovementMagnitude, ref fp3 remainingMovementDirection)
        {
            if (fpmath1.sqrMagnitude(transientVelocity) <= (fp)0)
            {
                return;
            }

            fp3 velocityBeforeProjection = transientVelocity;

            if (stableOnHit)
            {
                LastMovementIterationFoundAnyGround = true;
                HandleVelocityProjection(ref transientVelocity, obstructionNormal, stableOnHit);
            }
            else
            {
                // 首次非稳定命中：直接做法线投影
                if (sweepState == FPMovementSweepState.Initial)
                {
                    HandleVelocityProjection(ref transientVelocity, obstructionNormal, stableOnHit);
                    sweepState = FPMovementSweepState.AfterFirstHit;
                }
                // 第二次命中：尝试沿两面交线（折痕）滑动
                else if (sweepState == FPMovementSweepState.AfterFirstHit)
                {
                    EvaluateCrease(
                        transientVelocity,
                        previousVelocity,
                        obstructionNormal,
                        previousObstructionNormal,
                        stableOnHit,
                        previousHitIsStable,
                        GroundingStatus.IsStableOnGround && !MustUnground(),
                        out bool foundCrease,
                        out fp3 creaseDirection);

                    if (foundCrease)
                    {
                        if (GroundingStatus.IsStableOnGround && !MustUnground())
                        {
                            transientVelocity = fp3.zero;
                            sweepState = FPMovementSweepState.FoundBlockingCorner;
                        }
                        else
                        {
                            transientVelocity = FPKCCMath.Project(transientVelocity, creaseDirection);
                            sweepState = FPMovementSweepState.FoundBlockingCrease;
                        }
                    }
                    else
                    {
                        HandleVelocityProjection(ref transientVelocity, obstructionNormal, stableOnHit);
                    }
                }
                // 折痕也无法通过：视为角落阻塞，清零速度
                else if (sweepState == FPMovementSweepState.FoundBlockingCrease)
                {
                    transientVelocity = fp3.zero;
                    sweepState = FPMovementSweepState.FoundBlockingCorner;
                }
            }

            if (HasPlanarConstraint)
            {
                transientVelocity = FPMathKCC.ProjectOnPlane(transientVelocity, PlanarConstraintAxis);
            }

            fp newVelocityFactor = fpmath.length(transientVelocity) / fpmath.max(fpmath.length(velocityBeforeProjection), (fp)0.0000001f);
            remainingMovementMagnitude *= newVelocityFactor;
            if (fpmath.length(transientVelocity) > (fp)0)
            {
                remainingMovementDirection = fpmath.normalize(transientVelocity);
            }
        }

        /// <summary>
        /// 评估连续两次命中是否形成可沿之滑动的折痕方向（两面交线）。
        /// </summary>
        private void EvaluateCrease(
            fp3 currentCharacterVelocity,
            fp3 previousCharacterVelocity,
            fp3 currentHitNormal,
            fp3 previousHitNormal,
            bool currentHitIsStable,
            bool previousHitIsStable,
            bool characterIsStable,
            out bool isValidCrease,
            out fp3 creaseDirection)
        {
            isValidCrease = false;
            creaseDirection = default;

            if (!characterIsStable || !currentHitIsStable || !previousHitIsStable)
            {
                fp3 tmpBlockingCreaseDirection = fpmath.cross(currentHitNormal, previousHitNormal);
                fp dotPlanes = fpmath.dot(currentHitNormal, previousHitNormal);
                bool isVelocityConstrainedByCrease = false;

                // 两面近乎平行时无需折痕计算
                if (dotPlanes < (fp)0.999f)
                {
                    fp3 normalAOnCreasePlane = FPMathKCC.ProjectOnPlane(currentHitNormal, tmpBlockingCreaseDirection);
                    fp3 normalBOnCreasePlane = FPMathKCC.ProjectOnPlane(previousHitNormal, tmpBlockingCreaseDirection);
                    fp dotPlanesOnCreasePlane = fpmath.dot(normalAOnCreasePlane, normalBOnCreasePlane);

                    fp3 enteringVelocityDirectionOnCreasePlane = FPMathKCC.ProjectOnPlane(previousCharacterVelocity, tmpBlockingCreaseDirection);

                    if (dotPlanesOnCreasePlane <= (fpmath.dot(-enteringVelocityDirectionOnCreasePlane, normalAOnCreasePlane) + (fp)0.001f) &&
                        dotPlanesOnCreasePlane <= (fpmath.dot(-enteringVelocityDirectionOnCreasePlane, normalBOnCreasePlane) + (fp)0.001f))
                    {
                        isVelocityConstrainedByCrease = true;
                    }
                }

                if (isVelocityConstrainedByCrease)
                {
                    // 翻转折痕方向，使其与速度投影后的实际滑动方向一致
                    if (fpmath.dot(tmpBlockingCreaseDirection, currentCharacterVelocity) < (fp)0)
                    {
                        tmpBlockingCreaseDirection = -tmpBlockingCreaseDirection;
                    }

                    isValidCrease = true;
                    creaseDirection = tmpBlockingCreaseDirection;
                }
            }
        }

        /// <summary>
        /// 将速度投影到障碍法线上。可被子类重写以自定义斜坡/墙面滑动行为。
        /// 稳定接地时在地面切平面内重定向；空中时按命中稳定性选择贴地或纯法线投影。
        /// </summary>
        public virtual void HandleVelocityProjection(ref fp3 velocity, fp3 obstructionNormal, bool stableOnHit)
        {
            if (GroundingStatus.IsStableOnGround && !MustUnground())
            {
                // 稳定坡面：沿坡面切向重定向，不损失速度模长
                if (stableOnHit)
                {
                    velocity = GetDirectionTangentToSurface(velocity, obstructionNormal) * fpmath.length(velocity);
                }
                // 稳定接地但命中墙面：沿地面-墙面交线滑动
                else
                {
                    fp3 obstructionRightAlongGround = FPMathKCC.NormalizeSafe(fpmath.cross(obstructionNormal, GroundingStatus.GroundNormal));
                    fp3 obstructionUpAlongGround = FPMathKCC.NormalizeSafe(fpmath.cross(obstructionRightAlongGround, obstructionNormal));
                    velocity = GetDirectionTangentToSurface(velocity, obstructionUpAlongGround) * fpmath.length(velocity);
                    velocity = FPMathKCC.ProjectOnPlane(velocity, obstructionNormal);
                }
            }
            else
            {
                if (stableOnHit)
                {
                    // 空中落到稳定坡面：先去掉竖直分量再沿坡面切向
                    velocity = FPMathKCC.ProjectOnPlane(velocity, CharacterUp);
                    velocity = GetDirectionTangentToSurface(velocity, obstructionNormal) * fpmath.length(velocity);
                }
                // 一般阻挡：法线平面投影
                else
                {
                    velocity = FPMathKCC.ProjectOnPlane(velocity, obstructionNormal);
                }
            }
        }

        /// <summary>
        /// SimulatedDynamic 模式下可重写，自定义角色推刚体后的速度修正。
        /// 需修改 processedVelocity 才能影响角色最终速度。
        /// </summary>
        public virtual void HandleSimulatedRigidbodyInteraction(ref fp3 processedVelocity, FPRigidbodyProjectionHit hit, fp deltaTime)
        {
        }

        /// <summary>
        /// 汇总本帧记录的刚体命中，按质量比做冲量交换并写回角色/刚体速度。
        /// 在 UpdatePhase1 处理 MovePosition 后及 UpdatePhase2 基础速度移动后调用。
        /// </summary>
        private void ProcessVelocityForRigidbodyHits(ref fp3 processedVelocity, fp deltaTime)
        {
            for (int i = 0; i < _rigidbodyProjectionHitCount; i++)
            {
                FPRigidbodyProjectionHit bodyHit = _internalRigidbodyProjectionHits[i];

                if (bodyHit.Rigidbody != null && !_rigidbodiesPushedThisMove.Contains(bodyHit.Rigidbody))
                {
                    if (_internalRigidbodyProjectionHits[i].Rigidbody != _attachedRigidbody)
                    {
                        _rigidbodiesPushedThisMove.Add(bodyHit.Rigidbody);

                        fp characterMass = SimulatedCharacterMass;
                        fp3 characterVelocity = bodyHit.HitVelocity;

                        FPKinematicCharacterMotor hitCharacterMotor = FindCharacterMotorForBody(bodyHit.Rigidbody);
                        bool hitBodyIsCharacter = hitCharacterMotor != null;
                        bool hitBodyIsDynamic = !bodyHit.Rigidbody.IsKinematic;
                        fp hitBodyMass = bodyHit.Rigidbody.Mass;
                        fp hitBodyMassAtPoint = bodyHit.Rigidbody.Mass;
                        fp3 hitBodyVelocity = bodyHit.Rigidbody.Velocity;
                        if (hitBodyIsCharacter)
                        {
                            hitBodyMass = hitCharacterMotor.SimulatedCharacterMass;
                            hitBodyMassAtPoint = hitCharacterMotor.SimulatedCharacterMass;
                            hitBodyVelocity = hitCharacterMotor.BaseVelocity;
                        }
                        else if (!hitBodyIsDynamic)
                        {
                            IFPCollider hitCollider = FindColliderForBody(bodyHit.Rigidbody);
                            if (hitCollider != null && hitCollider.AttachedMover != null)
                            {
                                hitBodyVelocity = hitCollider.AttachedMover.Velocity;
                            }
                        }

                        // 按角色与刚体质量计算冲量分配比例
                        fp characterToBodyMassRatio = (fp)1;
                        {
                            if (characterMass + hitBodyMassAtPoint > (fp)0)
                            {
                                characterToBodyMassRatio = characterMass / (characterMass + hitBodyMassAtPoint);
                            }
                            else
                            {
                                characterToBodyMassRatio = (fp)0.5f;
                            }

                            // 命中非动态体：角色不承担推动质量
                            if (!hitBodyIsDynamic)
                            {
                                characterToBodyMassRatio = (fp)0;
                            }
                            // Kinematic 模式：角色以无限质量推动普通动态刚体
                            else if (FPRigidbodyInteractionType == FPRigidbodyInteractionType.Kinematic && !hitBodyIsCharacter)
                            {
                                characterToBodyMassRatio = (fp)1;
                            }
                        }

                        ComputeCollisionResolutionForHitBody(
                            bodyHit.EffectiveHitNormal,
                            characterVelocity,
                            hitBodyVelocity,
                            characterToBodyMassRatio,
                            out fp3 velocityChangeOnCharacter,
                            out fp3 velocityChangeOnBody);

                        processedVelocity += velocityChangeOnCharacter;

                        if (hitBodyIsCharacter)
                        {
                            hitCharacterMotor.BaseVelocity += velocityChangeOnCharacter;
                        }
                        else if (hitBodyIsDynamic)
                        {
                            bodyHit.Rigidbody.Velocity += velocityChangeOnBody;
                        }

                        if (FPRigidbodyInteractionType == FPRigidbodyInteractionType.SimulatedDynamic)
                        {
                            HandleSimulatedRigidbodyInteraction(ref processedVelocity, bodyHit, deltaTime);
                        }
                    }
                }
            }

        }

        /// <summary>
        /// 按命中法线与双方速度计算角色/刚体的速度修正量（一维冲量近似）。
        /// </summary>
        public void ComputeCollisionResolutionForHitBody(
            fp3 hitNormal,
            fp3 characterVelocity,
            fp3 bodyVelocity,
            fp characterToBodyMassRatio,
            out fp3 velocityChangeOnCharacter,
            out fp3 velocityChangeOnBody)
        {
            velocityChangeOnCharacter = default;
            velocityChangeOnBody = default;

            fp bodyToCharacterMassRatio = (fp)1 - characterToBodyMassRatio;
            fp characterVelocityMagnitudeOnHitNormal = fpmath.dot(characterVelocity, hitNormal);
            fp bodyVelocityMagnitudeOnHitNormal = fpmath.dot(bodyVelocity, hitNormal);

            // 角色速度朝阻挡方向的分量：恢复移动阶段被投影掉的部分
            if (characterVelocityMagnitudeOnHitNormal < (fp)0)
            {
                fp3 restoredCharacterVelocity = hitNormal * characterVelocityMagnitudeOnHitNormal;
                velocityChangeOnCharacter += restoredCharacterVelocity;
            }

            // 刚体法向速度更大时：按质量比分配相对冲击速度
            if (bodyVelocityMagnitudeOnHitNormal > characterVelocityMagnitudeOnHitNormal)
            {
                fp3 relativeImpactVelocity = hitNormal * (bodyVelocityMagnitudeOnHitNormal - characterVelocityMagnitudeOnHitNormal);
                velocityChangeOnCharacter += relativeImpactVelocity * bodyToCharacterMassRatio;
                velocityChangeOnBody += -relativeImpactVelocity * characterToBodyMassRatio;
            }
        }

        /// <summary>判断碰撞体是否参与角色碰撞查询（含控制器自定义过滤）。</summary>
        /// <returns>有效则返回 true。</returns>
        private bool CheckIfColliderValidForCollisions(IFPCollider coll)
        {
            if (!InternalIsColliderValidForCollisions(coll))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 内部碰撞有效性判定：附着体移动中忽略自身、Kinematic 模式忽略动态刚体，并委托 CharacterController 过滤。
        /// </summary>
        private bool InternalIsColliderValidForCollisions(IFPCollider coll)
        {
            FPDynamicRigidbody colliderAttachedRigidbody = coll.AttachedBody;
            if (colliderAttachedRigidbody != null)
            {
                bool isRigidbodyKinematic = colliderAttachedRigidbody.IsKinematic;

                // 正被附着体带动移动时，忽略该附着刚体（及同批非 kinematic 体）的碰撞
                if (_isMovingFromAttachedRigidbody && (!isRigidbodyKinematic || colliderAttachedRigidbody == _attachedRigidbody))
                {
                    return false;
                }

                // Kinematic 交互模式：不与非 kinematic 动态刚体发生阻挡碰撞
                if (FPRigidbodyInteractionType == FPRigidbodyInteractionType.Kinematic && !isRigidbodyKinematic)
                {
                    if (coll.AttachedBody != null)
                    {}

                    return false;
                }
            }

            // 自定义控制器过滤（FPNullCharacterController 恒为 true）
            bool colliderValid = CharacterController.IsColliderValidForCollisions(coll);
            if (!colliderValid)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 评估命中是否可稳定站立，并填充 ledge/台阶信息到 stabilityReport。
        /// 由 ProbeGround、InternalCharacterMove 的 Sweep 命中处调用。
        /// </summary>
        public void EvaluateHitStability(IFPCollider hitCollider, fp3 hitNormal, fp3 hitPoint, fp3 atCharacterPosition, fpquaternion atCharacterRotation, fp3 withCharacterVelocity, ref FPHitStabilityReport stabilityReport)
        {
            if (!_solveGrounding)
            {
                stabilityReport.IsStable = false;
                return;
            }

            fp3 atCharacterUp = atCharacterRotation * _cachedWorldUp;
            fp3 innerHitDirection = FPMathKCC.ProjectOnPlane(hitNormal, atCharacterUp);

            stabilityReport.IsStable = this.IsStableOnNormal(hitNormal);

            stabilityReport.FoundInnerNormal = false;
            stabilityReport.FoundOuterNormal = false;
            stabilityReport.InnerNormal = hitNormal;
            stabilityReport.OuterNormal = hitNormal;

            // ledge 边缘检测：内外侧二次射线比较坡度稳定性
            if (LedgeAndDenivelationHandling)
            {
                fp ledgeCheckHeight = MinDistanceForLedge;
                if (StepHandling != FPStepHandlingMethod.None)
                {
                    ledgeCheckHeight = MaxStepHeight;
                }

                bool isStableLedgeInner = false;
                bool isStableLedgeOuter = false;

                if (CharacterCollisionsRaycast(
                        hitPoint + (atCharacterUp * SecondaryProbesVertical) + (innerHitDirection * SecondaryProbesHorizontal),
                        -atCharacterUp,
                        ledgeCheckHeight + SecondaryProbesVertical,
                        out FPRaycastHit innerLedgeHit,
                        _internalCharacterHits) > 0)
                {
                    fp3 innerLedgeNormal = innerLedgeHit.Normal;
                    stabilityReport.InnerNormal = innerLedgeNormal;
                    stabilityReport.FoundInnerNormal = true;
                    isStableLedgeInner = IsStableOnNormal(innerLedgeNormal);
                }

                if (CharacterCollisionsRaycast(
                        hitPoint + (atCharacterUp * SecondaryProbesVertical) + (-innerHitDirection * SecondaryProbesHorizontal),
                        -atCharacterUp,
                        ledgeCheckHeight + SecondaryProbesVertical,
                        out FPRaycastHit outerLedgeHit,
                        _internalCharacterHits) > 0)
                {
                    fp3 outerLedgeNormal = outerLedgeHit.Normal;
                    stabilityReport.OuterNormal = outerLedgeNormal;
                    stabilityReport.FoundOuterNormal = true;
                    isStableLedgeOuter = IsStableOnNormal(outerLedgeNormal);
                }

                stabilityReport.LedgeDetected = (isStableLedgeInner != isStableLedgeOuter);
                if (stabilityReport.LedgeDetected)
                {
                    stabilityReport.IsOnEmptySideOfLedge = isStableLedgeOuter && !isStableLedgeInner;
                    stabilityReport.LedgeGroundNormal = isStableLedgeOuter ? stabilityReport.OuterNormal : stabilityReport.InnerNormal;
                    fp3 ledgeRight = fpmath.cross(hitNormal, stabilityReport.LedgeGroundNormal);
                    if (fpmath1.sqrMagnitude(ledgeRight) > (fp)0)
                    {
                        stabilityReport.LedgeRightDirection = fpmath.normalize(ledgeRight);
                        fp3 ledgeFacing = FPMathKCC.ProjectOnPlane(fpmath.cross(stabilityReport.LedgeGroundNormal, stabilityReport.LedgeRightDirection), CharacterUp);
                        if (fpmath1.sqrMagnitude(ledgeFacing) > (fp)0)
                        {
                            stabilityReport.LedgeFacingDirection = fpmath.normalize(ledgeFacing);
                        }
                    }

                    stabilityReport.DistanceFromLedge = fpmath.length(FPMathKCC.ProjectOnPlane((hitPoint - (atCharacterPosition + (atCharacterRotation * _characterTransformToCapsuleBottom))), atCharacterUp));
                    stabilityReport.IsMovingTowardsEmptySideOfLedge = fpmath.dot(withCharacterVelocity, stabilityReport.LedgeFacingDirection) > (fp)0;
                }

                if (stabilityReport.IsStable)
                {
                    stabilityReport.IsStable = IsStableWithSpecialCases(ref stabilityReport, withCharacterVelocity);
                }
            }

            // 台阶检测：当前命中不稳定时尝试识别可攀爬台阶
            if (StepHandling != FPStepHandlingMethod.None && !stabilityReport.IsStable)
            {
                // 动态刚体表面不支持自动上台阶
                FPDynamicRigidbody hitRigidbody = hitCollider.AttachedBody;
                if (!(hitRigidbody != null && !hitRigidbody.IsKinematic))
                {
                    DetectSteps(atCharacterPosition, atCharacterRotation, hitPoint, innerHitDirection, ref stabilityReport);

                    if (stabilityReport.ValidStepDetected)
                    {
                        stabilityReport.IsStable = true;
                    }
                }
            }

            CharacterController.ProcessHitStabilityReport(hitCollider, hitNormal, hitPoint, atCharacterPosition, atCharacterRotation, ref stabilityReport);
        }

        /// <summary>在命中点附近用胶囊 Sweep 检测是否存在可攀爬台阶（Standard/Extra 两档策略）。</summary>
        private void DetectSteps(fp3 characterPosition, fpquaternion characterRotation, fp3 hitPoint, fp3 innerHitDirection, ref FPHitStabilityReport stabilityReport)
        {
            int nbStepHits = 0;
            IFPCollider tmpCollider;
            FPRaycastHit outerStepHit;
            fp3 characterUp = characterRotation * _cachedWorldUp;
            fp3 verticalCharToHit = FPKCCMath.Project((hitPoint - characterPosition), characterUp);
            fp3 horizontalCharToHit = FPMathKCC.ProjectOnPlane((hitPoint - characterPosition), characterUp);
            if (fpmath1.sqrMagnitude(horizontalCharToHit) <= (fp)0)
            {
                return;
            }

            fp3 horizontalCharToHitDirection = fpmath.normalize(horizontalCharToHit);
            fp3 stepCheckStartPos = (hitPoint - verticalCharToHit) + (characterUp * MaxStepHeight) + (horizontalCharToHitDirection * CollisionOffset * (fp)3);

            // 标准模式：从障碍前方高处向下 Sweep
            nbStepHits = CharacterCollisionsSweep(
                            stepCheckStartPos,
                            characterRotation,
                            -characterUp,
                            MaxStepHeight + CollisionOffset,
                            out outerStepHit,
                            _internalCharacterHits,
                            (fp)0,
                            true);

            // 验证台阶落点无重叠且上行通道畅通
            if (CheckStepValidity(nbStepHits, characterPosition, characterRotation, innerHitDirection, stepCheckStartPos, out tmpCollider))
            {
                stabilityReport.ValidStepDetected = true;
                stabilityReport.SteppedCollider = tmpCollider;
            }

            if (StepHandling == FPStepHandlingMethod.Extra && !stabilityReport.ValidStepDetected)
            {
                // Extra 模式：从角色身后最小深度处再探一次（识别浅台阶）
                stepCheckStartPos = characterPosition + (characterUp * MaxStepHeight) + (-innerHitDirection * MinRequiredStepDepth);
                nbStepHits = CharacterCollisionsSweep(
                                stepCheckStartPos,
                                characterRotation,
                                -characterUp,
                                MaxStepHeight - CollisionOffset,
                                out outerStepHit,
                                _internalCharacterHits,
                                (fp)0,
                                true);

                // 同样验证 Extra 探测结果
                if (CheckStepValidity(nbStepHits, characterPosition, characterRotation, innerHitDirection, stepCheckStartPos, out tmpCollider))
                {
                    stabilityReport.ValidStepDetected = true;
                    stabilityReport.SteppedCollider = tmpCollider;
                }
            }
        }

        /// <summary>从 Sweep 命中中选取最远有效台阶落点，并验证外侧坡度与内侧接地。</summary>
        private bool CheckStepValidity(int nbStepHits, fp3 characterPosition, fpquaternion characterRotation, fp3 innerHitDirection, fp3 stepCheckStartPos, out IFPCollider hitCollider)
        {
            hitCollider = null;
            fp3 characterUp = characterRotation * FPMathKCC.WorldUp;

            // 优先尝试最远的台阶落点
            bool foundValidStepPosition = false;

            while (nbStepHits > 0 && !foundValidStepPosition)
            {
                // 在剩余命中中取距离最大者
                FPRaycastHit farthestHit = new FPRaycastHit();
                fp farthestDistance = (fp)0;
                int farthestIndex = 0;
                for (int i = 0; i < nbStepHits; i++)
                {
                    fp hitDistance = _internalCharacterHits[i].Distance;
                    if (hitDistance > farthestDistance)
                    {
                        farthestDistance = hitDistance;
                        farthestHit = _internalCharacterHits[i];
                        farthestIndex = i;
                    }
                }

                fp3 characterPositionAtHit = stepCheckStartPos + (-characterUp * (farthestHit.Distance - CollisionOffset));

                int atStepOverlaps = CharacterCollisionsOverlap(characterPositionAtHit, characterRotation, _internalProbedColliders);
                if (atStepOverlaps <= 0)
                {
                    // 台阶外侧坡度须稳定
                    if (CharacterCollisionsRaycast(
                            farthestHit.Point + (characterUp * SecondaryProbesVertical) + (-innerHitDirection * SecondaryProbesHorizontal),
                            -characterUp,
                            MaxStepHeight + SecondaryProbesVertical,
                            out FPRaycastHit outerSlopeHit,
                            _internalCharacterHits,
                            true) > 0)
                    {
                        if (IsStableOnNormal(outerSlopeHit.Normal))
                        {
                            // 向上 Sweep 确认站到台阶上时头顶无阻挡
                            if (CharacterCollisionsSweep(
                                                characterPosition, // position
                                                characterRotation, // rotation
                                                characterUp, // direction
                                                MaxStepHeight - farthestHit.Distance, // distance
                                                out FPRaycastHit tmpUpObstructionHit, // closest hit
                                                _internalCharacterHits) // all hits
                                    <= 0)
                            {
                                // 内侧接地检测
                                bool innerStepValid = false;
                                FPRaycastHit innerStepHit;

                                if (AllowSteppingWithoutStableGrounding)
                                {
                                    innerStepValid = true;
                                }
                                else
                                {
                                    // 在台阶高度处的胶囊中心向下打点
                                    if (CharacterCollisionsRaycast(
                                            characterPosition + FPKCCMath.Project((characterPositionAtHit - characterPosition), characterUp),
                                            -characterUp,
                                            MaxStepHeight,
                                            out innerStepHit,
                                            _internalCharacterHits,
                                            true) > 0)
                                    {
                                        if (IsStableOnNormal(innerStepHit.Normal))
                                        {
                                            innerStepValid = true;
                                        }
                                    }
                                }

                                if (!innerStepValid)
                                {
                                    // 在台阶前缘内侧再向下打点
                                    if (CharacterCollisionsRaycast(
                                            farthestHit.Point + (innerHitDirection * SecondaryProbesHorizontal),
                                            -characterUp,
                                            MaxStepHeight,
                                            out innerStepHit,
                                            _internalCharacterHits,
                                            true) > 0)
                                    {
                                        if (IsStableOnNormal(innerStepHit.Normal))
                                        {
                                            innerStepValid = true;
                                        }
                                    }
                                }

                                // 内外侧均通过则确认有效台阶
                                if (innerStepValid)
                                {
                                    hitCollider = farthestHit.Collider;
                                    foundValidStepPosition = true;
                                    return true;
                                }
                            }
                        }
                    }
                }

                // 当前最远命中无效则剔除后重试
                if (!foundValidStepPosition)
                {
                    nbStepHits--;
                    if (farthestIndex < nbStepHits)
                    {
                        _internalCharacterHits[farthestIndex] = _internalCharacterHits[nbStepHits];
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 在 FPKinematicCharacterSystem.Active 已注册的 Motor 中查找附着指定刚体的角色。
        /// 用于刚体-角色互撞时识别对方是否也是 KCC 角色。
        /// </summary>
        private static FPKinematicCharacterMotor FindCharacterMotorForBody(FPDynamicRigidbody body)
        {
            if (body == null)
            {
                return null;
            }

            FPKinematicCharacterSystem system = FPKinematicCharacterSystem.Active;
            if (system == null)
            {
                return null;
            }

            IReadOnlyList<FPKinematicCharacterMotor> motors = system.CharacterMotors;
            for (int i = 0; i < motors.Count; i++)
            {
                FPKinematicCharacterMotor motor = motors[i];
                if (motor != null && motor._attachedRigidbody == body)
                {
                    return motor;
                }
            }

            return null;
        }

        /// <summary>判断刚体是否已被某个角色 Motor 作为附着体引用。</summary>
        private static bool IsBodyOwnedByCharacterMotor(FPDynamicRigidbody body)
        {
            return FindCharacterMotorForBody(body) != null;
        }

        /// <summary>在 FPCollisionWorld 中查找挂载指定刚体的碰撞体。</summary>
        private static IFPCollider FindColliderForBody(FPDynamicRigidbody body)
        {
            if (body == null)
            {
                return null;
            }

            IReadOnlyList<IFPCollider> colliders = FPCollisionWorld.Instance.Colliders;
            for (int i = 0; i < colliders.Count; i++)
            {
                if (colliders[i].AttachedBody == body)
                {
                    return colliders[i];
                }
            }

            return null;
        }

        /// <summary>计算角色站立在动态刚体上时，该点处的线速度与角速度（供 UpdatePhase1 附着体带动）。</summary>
        public void GetVelocityFromRigidbodyMovement(FPDynamicRigidbody interactiveRigidbody, fp3 atPoint, fp deltaTime, out fp3 linearVelocity, out fp3 angularVelocity)
        {
            GetVelocityFromAttachedMovement(interactiveRigidbody, null, atPoint, deltaTime, out linearVelocity, out angularVelocity);
        }

        /// <summary>计算角色站立在运动学平台上时，该点处的线速度与角速度。</summary>
        public void GetVelocityFromMoverMovement(FPPhysicsMover mover, fp3 atPoint, fp deltaTime, out fp3 linearVelocity, out fp3 angularVelocity)
        {
            GetVelocityFromAttachedMovement(null, mover, atPoint, deltaTime, out linearVelocity, out angularVelocity);
        }

        /// <summary>
        /// 根据刚体/平台的线速度、角速度，计算指定世界坐标点在 deltaTime 内的等效线速度。
        /// 角速度非零时会叠加旋转带来的切向位移。
        /// </summary>
        private void GetVelocityFromAttachedMovement(FPDynamicRigidbody body, FPPhysicsMover mover, fp3 atPoint, fp deltaTime, out fp3 linearVelocity, out fp3 angularVelocity)
        {
            linearVelocity = fp3.zero;
            angularVelocity = fp3.zero;
            if (deltaTime <= (fp)0)
            {
                return;
            }

            fp3 centerOfRotation;
            if (mover != null)
            {
                linearVelocity = mover.Velocity;
                angularVelocity = mover.AngularVelocity;
                centerOfRotation = mover.TransientPosition;
            }
            else if (body != null)
            {
                linearVelocity = body.Velocity;
                angularVelocity = body.AngularVelocity;
                centerOfRotation = body.Position;
            }
            else
            {
                return;
            }

            if (fpmath1.sqrMagnitude(angularVelocity) > (fp)0 && deltaTime > (fp)0)
            {
                // 将角速度折算为点位置的附加线速度
                fp3 centerOfRotationToPoint = atPoint - centerOfRotation;
                fpquaternion rotationFromInteractiveRigidbody = fpmath1.EulerXYZ(angularVelocity * deltaTime);
                fp3 finalPointPosition = centerOfRotation + (rotationFromInteractiveRigidbody * centerOfRotationToPoint);
                linearVelocity += (finalPointPosition - atPoint) / deltaTime;
            }
        }

        /// <summary>
        /// 获取碰撞体关联的可交互刚体：移动平台附着的刚体，或非 kinematic 动态刚体。
        /// 静态墙/kinematic 障碍返回 null，解重叠时优先处理。
        /// </summary>
        private FPDynamicRigidbody GetInteractiveRigidbody(IFPCollider onCollider)
        {
            if (onCollider == null)
            {
                return null;
            }

            if (onCollider.AttachedMover != null)
            {
                return onCollider.AttachedBody;
            }

            FPDynamicRigidbody body = onCollider.AttachedBody;
            if (body != null && !body.IsKinematic)
            {
                return body;
            }

            return null;
        }

        /// <summary>
        /// 由起止位置与 deltaTime 计算等效移动速度。
        /// MoveComponent 在 Phase1 将 SetMovePositionTarget 的位移转为速度时走此路径。
        /// </summary>
        public fp3 GetVelocityForMovePosition(fp3 fromPosition, fp3 toPosition, fp deltaTime)
        {
            return GetVelocityFromMovement(toPosition - fromPosition, deltaTime);
        }

        /// <summary>将位移向量除以 deltaTime 得到速度；deltaTime 无效时返回零。</summary>
        public fp3 GetVelocityFromMovement(fp3 movement, fp deltaTime)
        {
            if (deltaTime <= (fp)0)
                return fp3.zero;

            fp movementMagnitude = fpmath.length(movement);
            if (movementMagnitude > MaxPositionCorrectionPerFrame)
            {
                movement = movement * (MaxPositionCorrectionPerFrame / movementMagnitude);
            }

            return movement / deltaTime;
        }

        /// <summary>按平面符号限制向量各轴分量（用于平面约束辅助）。</summary>
        private void RestrictVectorToPlane(ref fp3 vector, fp3 toPlane)
        {
            if (vector.x > (fp)0 != toPlane.x > (fp)0)
            {
                vector.x = (fp)0;
            }
            if (vector.y > (fp)0 != toPlane.y > (fp)0)
            {
                vector.y = (fp)0;
            }
            if (vector.z > (fp)0 != toPlane.z > (fp)0)
            {
                vector.z = (fp)0;
            }
        }

        /// <summary>
        /// 检测角色胶囊与 CollidableLayers 中碰撞体的重叠数。
        /// 会过滤 IsColliderValidForCollisions 判定为无效的碰撞体。
        /// </summary>
        /// <returns>有效重叠数量。</returns>
        public int CharacterCollisionsOverlap(fp3 position, fpquaternion rotation, IFPCollider[] overlappedColliders, fp inflate = default, bool acceptOnlyStableGroundLayer = false)
        {
            FPLayerMask queryLayers = CollidableLayers;
            if (acceptOnlyStableGroundLayer)
            {
                queryLayers = CollidableLayers & StableGroundLayers;
            }

            fp radius = CapsuleRadius + inflate;
            int nbUnfilteredHits = FPPhysicsQuery.OverlapCapsuleNonAlloc(
                position,
                rotation,
                radius,
                CapsuleHeight + inflate * (fp)2,
                CapsuleYOffset,
                overlappedColliders,
                queryLayers);

            int nbHits = nbUnfilteredHits;
            for (int i = nbUnfilteredHits - 1; i >= 0; i--)
            {
                if (!CheckIfColliderValidForCollisions(overlappedColliders[i]))
                {
                    nbHits--;
                    if (i < nbHits)
                    {
                        overlappedColliders[i] = overlappedColliders[nbHits];
                    }
                }
            }

            return nbHits;
        }

        /// <summary>按指定层掩码做胶囊重叠查询（不经碰撞有效性过滤，供外部调试）。</summary>
        public int CharacterOverlap(fp3 position, fpquaternion rotation, IFPCollider[] overlappedColliders, FPLayerMask layers, bool queryTriggers = false, fp inflate = default)
        {
            return FPPhysicsQuery.OverlapCapsuleNonAlloc(
                position,
                rotation,
                CapsuleRadius + inflate,
                CapsuleHeight + inflate * (fp)2,
                CapsuleYOffset,
                overlappedColliders,
                layers,
                queryTriggers);
        }

        /// <summary>
        /// 角色碰撞 Sweep：使用 CollidableLayers，带回退距离补偿，并过滤无效碰撞体。
        /// InternalCharacterMove、DetectSteps 的主要移动检测入口。
        /// </summary>
        public int CharacterCollisionsSweep(fp3 position, fpquaternion rotation, fp3 direction, fp distance, out FPRaycastHit closestHit, FPRaycastHit[] hits, fp inflate = default, bool acceptOnlyStableGroundLayer = false)
        {
            FPLayerMask queryLayers = CollidableLayers;
            if (acceptOnlyStableGroundLayer)
            {
                queryLayers = CollidableLayers & StableGroundLayers;
            }

            fp3 sweepPosition = position - direction * SweepProbingBackstepDistance;
            fp sweepDistance = distance + SweepProbingBackstepDistance;
            int nbUnfilteredHits = FPPhysicsQuery.CapsuleCastNonAlloc(
                sweepPosition,
                rotation,
                CapsuleRadius + inflate,
                CapsuleHeight + inflate * (fp)2,
                CapsuleYOffset,
                direction,
                sweepDistance,
                hits,
                queryLayers);

            closestHit = default;
            fp closestDistance = (fp)999999;
            int nbHits = 0;
            for (int i = 0; i < nbUnfilteredHits; i++)
            {
                FPRaycastHit hit = hits[i];
                hit.Distance -= SweepProbingBackstepDistance;
                if (hit.Distance <= (fp)0 || !CheckIfColliderValidForCollisions(hit.Collider))
                {
                    continue;
                }

                if (nbHits < hits.Length)
                {
                    hits[nbHits++] = hit;
                }

                if (hit.Distance < closestDistance)
                {
                    closestDistance = hit.Distance;
                    closestHit = hit;
                }
            }

            return nbHits;
        }

        /// <summary>按自定义层掩码做胶囊 Sweep（不经 InternalIsColliderValidForCollisions 过滤）。</summary>
        public int CharacterSweep(fp3 position, fpquaternion rotation, fp3 direction, fp distance, out FPRaycastHit closestHit, FPRaycastHit[] hits, FPLayerMask layers, bool queryTriggers = false, fp inflate = default)
        {
            closestHit = default;
            int nbUnfilteredHits = FPPhysicsQuery.CapsuleCastNonAlloc(
                position,
                rotation,
                CapsuleRadius + inflate,
                CapsuleHeight + inflate * (fp)2,
                CapsuleYOffset,
                direction,
                distance,
                hits,
                layers,
                queryTriggers);

            fp closestDistance = (fp)999999;
            int nbHits = 0;
            for (int i = 0; i < nbUnfilteredHits; i++)
            {
                FPRaycastHit hit = hits[i];
                if (hit.Distance <= (fp)0)
                {
                    continue;
                }

                if (nbHits < hits.Length)
                {
                    hits[nbHits++] = hit;
                }

                if (hit.Distance < closestDistance)
                {
                    closestDistance = hit.Distance;
                    closestHit = hit;
                }
            }

            return nbHits;
        }

        /// <summary>仅对 StableGroundLayers 做向下地面 Sweep，供 ProbeGround 使用。</summary>
        private bool CharacterGroundSweep(fp3 position, fpquaternion rotation, fp3 direction, fp distance, out FPRaycastHit closestHit)
        {
            closestHit = default;
            fp3 sweepPosition = position - direction * GroundProbingBackstepDistance;
            fp sweepDistance = distance + GroundProbingBackstepDistance;
            FPLayerMask queryLayers = CollidableLayers & StableGroundLayers;

            int nbUnfilteredHits = FPPhysicsQuery.CapsuleCastNonAlloc(
                sweepPosition,
                rotation,
                CapsuleRadius,
                CapsuleHeight,
                CapsuleYOffset,
                direction,
                sweepDistance,
                _internalCharacterHits,
                queryLayers);

            bool foundValidHit = false;
            fp closestDistance = (fp)999999;
            for (int i = 0; i < nbUnfilteredHits; i++)
            {
                FPRaycastHit hit = _internalCharacterHits[i];
                if (hit.Distance <= (fp)0 || !CheckIfColliderValidForCollisions(hit.Collider))
                {
                    continue;
                }

                if (hit.Distance < closestDistance)
                {
                    closestHit = hit;
                    closestHit.Distance -= GroundProbingBackstepDistance;
                    closestDistance = hit.Distance;
                    foundValidHit = true;
                }
            }

            return foundValidHit;
        }

        /// <summary>角色碰撞射线检测，用于 ledge/台阶内外侧二次探测。</summary>
        public int CharacterCollisionsRaycast(fp3 position, fp3 direction, fp distance, out FPRaycastHit closestHit, FPRaycastHit[] hits, bool acceptOnlyStableGroundLayer = false)
        {
            FPLayerMask queryLayers = CollidableLayers;
            if (acceptOnlyStableGroundLayer)
            {
                queryLayers = CollidableLayers & StableGroundLayers;
            }

            int nbUnfilteredHits = FPPhysicsQuery.RaycastNonAlloc(
                position,
                direction,
                distance,
                hits,
                queryLayers);

            closestHit = default;
            fp closestDistance = (fp)999999;
            int nbHits = 0;
            for (int i = 0; i < nbUnfilteredHits; i++)
            {
                FPRaycastHit hit = hits[i];
                if (hit.Distance <= (fp)0 || !CheckIfColliderValidForCollisions(hit.Collider))
                {
                    continue;
                }

                if (nbHits < hits.Length)
                {
                    hits[nbHits++] = hit;
                }

                if (hit.Distance < closestDistance)
                {
                    closestDistance = hit.Distance;
                    closestHit = hit;
                }
            }

            return nbHits;
        }
    }
