using System.Collections.Generic;
using Ase.Serializing;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// 实体物理注册运行时配置 DTO，可在实体创建后通过 <see cref="ComponentDataKey.PhysicsBodyData"/> 注入，
/// 覆盖 <see cref="HeroAssetsConfig"/> / <see cref="MonsterAssetsConfig"/> 中的默认物理段。
/// <para>
/// 由 <see cref="PhysicsBodyComponent.ResolveConfig"/> 读取；地图 Cube 等无资产配置的实体
/// 可回退到 <see cref="CreateDefaultStaticBox"/>。
/// </para>
/// </summary>
public class PhysicsEntityConfig
{
    /// <summary>
    /// 移动范式：CharacterController（Motor 单胶囊）或 Rigidbody（复合碰撞体）。二选一，不可混用身体阻挡核。
    /// </summary>
    public PhysicsMovementMode MovementMode;

    /// <summary>
    /// Character Controller 范式参数；<see cref="MovementMode"/> 为 CC 时使用，否则可为 null。
    /// </summary>
    public CharacterControllerSettings CharacterController;

    /// <summary>
    /// Rigidbody 范式参数；<see cref="MovementMode"/> 为 Rigidbody 时使用，否则可为 null。
    /// </summary>
    public PhysicsBodyConfig PhysicsBody;

    /// <summary>
    /// 为无资产配置的实体（如 <see cref="CubeEntity"/>）构造默认 Static 墙盒配置。
    /// </summary>
    /// <param name="entity">目标实体，当前实现未读取其字段，保留用于后续按尺寸定制。</param>
    /// <returns>movementMode=Rigidbody、bodyType=Static、单层 Wall 盒体的配置。</returns>
    public static PhysicsEntityConfig CreateDefaultStaticBox(BaseEntity entity)
    {
        var body = new PhysicsBodyConfig
        {
            bodyType = PhysicsBodyType.Static,
            colliders = new List<PhysicsColliderSetting>
            {
                new PhysicsColliderSetting
                {
                    key = "wall",
                    shape = PhysicsShapeType.Box,
                    halfExtents = new Vector3(0.5f, 0.5f, 0.5f),
                    layer = FPCollisionLayer.Wall,
                }
            }
        };
        return new PhysicsEntityConfig
        {
            MovementMode = PhysicsMovementMode.Rigidbody,
            PhysicsBody = body,
        };
    }
}

/// <summary>
/// ECS 统一物理注册组件，替代旧版 KccRegistrationComponent。
/// <para>
/// <b>CharacterController 范式</b>：注册 <see cref="FPKinematicCharacterMotor"/>，
/// 供 <see cref="MoveComponent"/> 与 <see cref="FPKinematicCharacterSystem"/> 驱动英雄移动与碰撞校正。
/// </para>
/// <para>
/// <b>Rigidbody 范式</b>：按 <see cref="PhysicsBodyConfig"/> 注册复合 <see cref="IFPCollider"/>，
/// 在 <see cref="OnFixedUpdate"/> 跟随实体 Transform；Kinematic / Dynamic 且启用重力时积分竖直速度并贴地。
/// </para>
/// <para>
/// 系统更新顺序（RogueWorld）：EntitySystem（寻路/AI）→ 本组件 OnFixedUpdate（碰撞体同步）→ FPKinematicCharacterSystem（Motor 模拟）。
/// </para>
/// </summary>
public class PhysicsBodyComponent : BaseComponent
{
    private PhysicsEntityConfig _config;
    private FPKinematicCharacterMotor _motor;
    private FPGravityCharacterController _characterController;
    private readonly List<IFPCollider> _colliders = new List<IFPCollider>(4);
    private FPDynamicRigidbody _dynamicBody;
    private FPPhysicsMover _physicsMover;
    private FPEntityPhysicsMoverController _moverController;
    private fp _kinematicVerticalVelocity;

    /// <summary>
    /// CharacterController 范式下的角色 Motor；Rigidbody 范式或未注册时为 null。
    /// <see cref="MoveComponent"/> 通过本属性获取 Motor 并写入移动/旋转目标。
    /// </summary>
    public FPKinematicCharacterMotor Motor => _motor;

    /// <summary>
    /// 当前生效的移动范式，来自解析后的 <see cref="PhysicsEntityConfig"/>。
    /// 配置缺失时默认为 CharacterController。
    /// </summary>
    public PhysicsMovementMode MovementMode => _config?.MovementMode ?? PhysicsMovementMode.CharacterController;

    /// <summary>
    /// Rigidbody 范式下已注册到 <see cref="FPCollisionWorld"/> 的复合碰撞体只读列表；CC 范式为空列表。
    /// </summary>
    public IReadOnlyList<IFPCollider> Colliders => _colliders;

    /// <summary>
    /// Kinematic 或 Dynamic 刚体包装对象；Static 范式为 null。
    /// </summary>
    public FPDynamicRigidbody DynamicBody => _dynamicBody;

    /// <summary>
    /// 组件启动时解析配置并按范式注册 Motor 或复合碰撞体；CC 模式下若误配 physicsBody 会打警告日志。
    /// </summary>
    /// <param name="data">基类预留参数，本组件未使用。</param>
    public override void OnStart(object data = null)
    {
        base.OnStart(data);
        _config = ResolveConfig();

        if (_config.MovementMode == PhysicsMovementMode.CharacterController)
        {
            RegisterCharacterMotor();
        }
        else if (_config.MovementMode == PhysicsMovementMode.Rigidbody)
        {
            RegisterRigidbodyBody();
        }

        if (_config.MovementMode == PhysicsMovementMode.CharacterController &&
            _config.PhysicsBody?.colliders != null &&
            _config.PhysicsBody.colliders.Count > 0)
        {
            GameLog.Warn(GameLogChannel.Battle,
                $"Entity {Entity.EntityId}: CharacterController mode ignores physicsBody.colliders per spec.");
        }
    }

    /// <summary>
    /// Rigidbody 范式每帧将碰撞体与刚体位姿同步到实体 Transform；CC 范式直接返回。
    /// </summary>
    /// <param name="deltaTime">逻辑帧间隔。</param>
    /// <param name="worldUpdateType">世界更新类型（本地/权威/回滚）。</param>
    public override void OnFixedUpdate(fp deltaTime, WorldUpdateType worldUpdateType)
    {
        base.OnFixedUpdate(deltaTime, worldUpdateType);
        if (_config.MovementMode != PhysicsMovementMode.Rigidbody)
        {
            return;
        }

        fp3 scale = Entity.transform.LocalScale;
        for (int i = 0; i < _colliders.Count; i++)
        {
            SyncColliderFromEntity(_colliders[i], _config.PhysicsBody.colliders[i], scale);
        }

        SimulateGravityAndGrounding(deltaTime);

        for (int i = 0; i < _colliders.Count; i++)
        {
            SyncColliderFromEntity(_colliders[i], _config.PhysicsBody.colliders[i], scale);
        }

        SyncDynamicBodyPose();
    }

    /// <summary>
    /// 实体销毁时注销 Motor、碰撞体并清空刚体/平台引用，避免泄漏到 <see cref="FPCollisionWorld"/>。
    /// </summary>
    public override void OnDispose()
    {
        UnregisterAll();
        base.OnDispose();
    }

    public override void HardRollBackTo(PooledReader authoritySnapShot)
    {
        base.HardRollBackTo(authoritySnapShot);

        if (_motor != null)
        {
            _motor.SetPositionAndRotation(Entity.transform.Position, Entity.transform.Rotation);
        }

        if (_dynamicBody != null)
        {
            _dynamicBody.SetPose(Entity.transform.Position, Entity.transform.Rotation);
        }
    }

    /// <summary>
    /// 按优先级解析物理配置：运行时 SetData → 英雄/怪物资产配置 → 默认 Static 盒体。
    /// </summary>
    /// <returns>本实体将使用的 <see cref="PhysicsEntityConfig"/> 副本或新建实例。</returns>
    private PhysicsEntityConfig ResolveConfig()
    {
        PhysicsEntityConfig runtime = Entity.GetData<PhysicsEntityConfig>(ComponentDataKey.PhysicsBodyData);
        if (runtime != null)
        {
            return runtime;
        }

        if (Entity is HeroEntity heroEntity)
        {
            HeroAssetsConfig hero = heroEntity.BattleHeroData?.HeroAssetsConfig;
            if (hero != null)
            {
                return new PhysicsEntityConfig
                {
                    MovementMode = hero.movementMode,
                    CharacterController = hero.characterController,
                    PhysicsBody = hero.physicsBody,
                };
            }
        }

        if (Entity is MonsterEntity monsterEntity)
        {
            MonsterAssetsConfig monster = monsterEntity.BattleMonsterData?.MonsterAssetsConfig;
            if (monster != null)
            {
                return new PhysicsEntityConfig
                {
                    MovementMode = monster.movementMode,
                    CharacterController = monster.characterController,
                    PhysicsBody = monster.physicsBody,
                };
            }
        }

        return PhysicsEntityConfig.CreateDefaultStaticBox(Entity);
    }

    /// <summary>
    /// 创建并注册 <see cref="FPKinematicCharacterMotor"/>，绑定当前实体与 <see cref="FPNullCharacterController"/>。
    /// </summary>
    private void RegisterCharacterMotor()
    {
        CharacterControllerSettings cc = _config.CharacterController ?? new CharacterControllerSettings();
        PhysicsConfigConverter.ToCharacterMotorDimensions(cc, out fp radius, out fp height, out fp yOffset);

        _characterController = new FPGravityCharacterController();
        _characterController.Configure(
            cc.useGravity,
            PhysicsConfigConverter.ResolveGravityAcceleration(cc));

        _motor = new FPKinematicCharacterMotor(_characterController)
        {
            CharacterLayer = cc.layer,
            CollisionInfluence = cc.collisionInfluence ?? PhysicsMotionInfluence.CreateCharacterControllerDefault(),
        };
        _characterController.BindMotor(_motor);
        _motor.SetCapsuleDimensions(radius, height, yOffset);
        _motor.RebuildCollidableLayers();
        _motor.StableGroundLayers = FPKinematicCharacterMotor.BuildDefaultStableGroundLayers(_motor.CollidableLayers);
        _motor.SetPositionAndRotation(Entity.transform.Position, Entity.transform.Rotation);
        _motor.BindEntity(Entity);
        _motor.Register();
    }

    /// <summary>
    /// 按 <see cref="PhysicsBodyConfig"/> 创建刚体（若非 Static）并注册全部复合碰撞体到世界。
    /// </summary>
    private void RegisterRigidbodyBody()
    {
        PhysicsBodyConfig bodyConfig = _config.PhysicsBody;
        if (bodyConfig == null || bodyConfig.colliders == null || bodyConfig.colliders.Count == 0)
        {
            GameLog.Error(GameLogChannel.Battle,
                $"PhysicsBodyComponent Rigidbody mode requires colliders. entityId={Entity.EntityId}");
            return;
        }

        switch (bodyConfig.bodyType)
        {
            case PhysicsBodyType.Dynamic:
                _dynamicBody = new FPDynamicRigidbody
                {
                    Id = FPCollisionWorld.Instance.AllocateBodyId(),
                    IsKinematic = false,
                    UseGravity = bodyConfig.useGravity,
                    GravityAcceleration = PhysicsConfigConverter.ResolveGravityAcceleration(bodyConfig),
                };
                _dynamicBody.SetPose(Entity.transform.Position, Entity.transform.Rotation);
                break;

            case PhysicsBodyType.Kinematic:
                _dynamicBody = new FPDynamicRigidbody
                {
                    Id = FPCollisionWorld.Instance.AllocateBodyId(),
                    IsKinematic = true,
                    UseGravity = bodyConfig.useGravity,
                    GravityAcceleration = PhysicsConfigConverter.ResolveGravityAcceleration(bodyConfig),
                };
                _dynamicBody.SetPose(Entity.transform.Position, Entity.transform.Rotation);
                break;

            case PhysicsBodyType.Static:
            default:
                _dynamicBody = null;
                break;
        }

        fp3 scale = Entity.transform.LocalScale;
        for (int i = 0; i < bodyConfig.colliders.Count; i++)
        {
            PhysicsColliderSetting setting = bodyConfig.colliders[i];
            IFPCollider collider = PhysicsConfigConverter.CreateCollider(setting);
            if (collider == null)
            {
                continue;
            }

            SyncColliderFromEntity(collider, setting, scale);
            if (_dynamicBody != null)
            {
                BindAttachedBody(collider, _dynamicBody);
            }

            _colliders.Add(collider);
            FPCollisionWorld.Instance.RegisterCollider(collider);
        }

        if (bodyConfig.usePhysicsMover)
        {
            RegisterPhysicsMover();
        }

        SyncDynamicBodyPose();
    }

    private void RegisterPhysicsMover()
    {
        _physicsMover = new FPPhysicsMover
        {
            Id = FPCollisionWorld.Instance.AllocateMoverId(),
        };
        _physicsMover.SetPose(Entity.transform.Position, Entity.transform.Rotation);
        _moverController = new FPEntityPhysicsMoverController(Entity);
        _physicsMover.Controller = _moverController;

        FPKinematicCharacterSystem kccSystem = Entity.BaseWorld?.GetSystem<FPKinematicCharacterSystem>();
        kccSystem?.RegisterPhysicsMover(_physicsMover);

        for (int i = 0; i < _colliders.Count; i++)
        {
            BindAttachedMover(_colliders[i], _physicsMover);
        }
    }

    private static void BindAttachedMover(IFPCollider collider, FPPhysicsMover mover)
    {
        switch (collider.ShapeType)
        {
            case FPShapeType.Box:
                ((FPBoxCollider)collider).AttachedMover = mover;
                break;
            case FPShapeType.Sphere:
                ((FPSphereCollider)collider).AttachedMover = mover;
                break;
            case FPShapeType.Capsule:
                ((FPCapsuleCollider)collider).AttachedMover = mover;
                break;
        }
    }

    /// <summary>
    /// 运行时调试：更新 Static Rigidbody 单碰撞体的世界位姿与形状参数，并立即同步到 <see cref="FPCollisionWorld"/>。
    /// </summary>
    public void ApplyStaticShape(fp3 position, fp3 eulerAngles, PhysicsColliderSetting setting)
    {
        if (_config == null ||
            _config.MovementMode != PhysicsMovementMode.Rigidbody ||
            _config.PhysicsBody?.colliders == null ||
            _config.PhysicsBody.colliders.Count == 0 ||
            _colliders.Count == 0 ||
            setting == null)
        {
            return;
        }

        Entity.transform.Position = position;
        Entity.transform.EulerAngles = eulerAngles;

        PhysicsColliderSetting target = _config.PhysicsBody.colliders[0];
        target.shape = setting.shape;
        target.halfExtents = setting.halfExtents;
        target.radius = setting.radius;
        target.capsuleRadius = setting.capsuleRadius;
        target.capsuleHeight = setting.capsuleHeight;
        target.directionAxis = setting.directionAxis;
        target.layer = setting.layer;
        target.isTrigger = setting.isTrigger;

        IFPCollider collider = _colliders[0];
        SetColliderLayerAndTrigger(collider, setting.layer, setting.isTrigger);

        fp3 scale = Entity.transform.LocalScale;
        SyncColliderFromEntity(collider, target, scale);
        SyncDynamicBodyPose();
    }

    /// <summary>
    /// 读取主碰撞体的世界位姿、形状类型与尺寸参数，供调试面板显示。
    /// </summary>
    public bool TryReadStaticShape(out SceneColliderShapeParams shapeParams)
    {
        shapeParams = default;
        if (_colliders.Count == 0 ||
            _config?.PhysicsBody?.colliders == null ||
            _config.PhysicsBody.colliders.Count == 0)
        {
            return false;
        }

        PhysicsColliderSetting setting = _config.PhysicsBody.colliders[0];
        IFPCollider collider = _colliders[0];

        shapeParams.shape = setting.shape;
        shapeParams.eulerAngles = fpmath1.Fp3ToVector3(Entity.transform.EulerAngles);
        shapeParams.layer = setting.layer;
        shapeParams.isTrigger = setting.isTrigger;
        shapeParams.halfExtents = setting.halfExtents;
        shapeParams.radius = setting.radius;
        shapeParams.capsuleRadius = setting.capsuleRadius;
        shapeParams.capsuleHeight = setting.capsuleHeight;
        shapeParams.directionAxis = setting.directionAxis;

        switch (collider.ShapeType)
        {
            case FPShapeType.Box:
                shapeParams.position = fpmath1.Fp3ToVector3(collider.GetBoxShape().Center);
                break;
            case FPShapeType.Sphere:
                shapeParams.position = fpmath1.Fp3ToVector3(collider.GetSphereShape().Center);
                break;
            case FPShapeType.Capsule:
                shapeParams.position = fpmath1.Fp3ToVector3(collider.Position);
                break;
            default:
                return false;
        }

        return true;
    }

    private static void SetColliderLayerAndTrigger(IFPCollider collider, FPCollisionLayer layer, bool isTrigger)
    {
        switch (collider.ShapeType)
        {
            case FPShapeType.Box:
                var box = (FPBoxCollider)collider;
                box.Layer = layer;
                box.IsTrigger = isTrigger;
                break;
            case FPShapeType.Sphere:
                var sphere = (FPSphereCollider)collider;
                sphere.Layer = layer;
                sphere.IsTrigger = isTrigger;
                break;
            case FPShapeType.Capsule:
                var capsule = (FPCapsuleCollider)collider;
                capsule.Layer = layer;
                capsule.IsTrigger = isTrigger;
                break;
        }
    }

    /// <summary>
    /// 根据实体当前 Transform 与 Authoring 设置，更新单个碰撞体的世界几何。
    /// </summary>
    /// <param name="collider">已创建的碰撞体实例。</param>
    /// <param name="setting">对应的配置项（与 collider 索引一致）。</param>
    /// <param name="scale">实体 LocalScale。</param>
    private void SyncColliderFromEntity(IFPCollider collider, PhysicsColliderSetting setting, fp3 scale)
    {
        fp3 localOffset = PhysicsConfigConverter.ToFp3(setting.localOffset);
        fpquaternion localRot = PhysicsConfigConverter.ToLocalRotation(setting.localEuler);
        fpquaternion worldRot = Entity.transform.Rotation * localRot;

        switch (collider.ShapeType)
        {
            case FPShapeType.Box:
                ((FPBoxCollider)collider).SyncFromTransform(
                    Entity.transform.Position,
                    worldRot,
                    localOffset,
                    PhysicsConfigConverter.ToFp3(setting.halfExtents),
                    scale);
                break;
            case FPShapeType.Sphere:
                ((FPSphereCollider)collider).SyncFromTransform(
                    Entity.transform.Position,
                    worldRot,
                    localOffset,
                    (fp)setting.radius,
                    scale);
                break;
            case FPShapeType.Capsule:
                ((FPCapsuleCollider)collider).SyncFromTransform(
                    Entity.transform.Position,
                    worldRot,
                    localOffset,
                    (fp)setting.capsuleRadius,
                    (fp)setting.capsuleHeight,
                    (fp)0,
                    scale);
                break;
        }
    }

    /// <summary>
    /// 将刚体引用写入具体碰撞体类型（接口 <see cref="IFPCollider.AttachedBody"/> 为只读）。
    /// </summary>
    /// <param name="collider">目标碰撞体。</param>
    /// <param name="body">已创建的 Kinematic/Dynamic 刚体。</param>
    private static void BindAttachedBody(IFPCollider collider, FPDynamicRigidbody body)
    {
        switch (collider.ShapeType)
        {
            case FPShapeType.Box:
                ((FPBoxCollider)collider).AttachedBody = body;
                break;
            case FPShapeType.Sphere:
                ((FPSphereCollider)collider).AttachedBody = body;
                break;
            case FPShapeType.Capsule:
                ((FPCapsuleCollider)collider).AttachedBody = body;
                break;
        }
    }

    /// <summary>
    /// 将 <see cref="FPDynamicRigidbody"/> 位姿与实体 Transform 对齐。
    /// </summary>
    private void SyncDynamicBodyPose()
    {
        if (_dynamicBody == null)
        {
            return;
        }

        _dynamicBody.SetPose(Entity.transform.Position, Entity.transform.Rotation);
    }

    /// <summary>
    /// 注销 Motor、全部碰撞体并释放内部引用；在 <see cref="OnDispose"/> 中调用。
    /// </summary>
    private void UnregisterAll()
    {
        if (_motor != null)
        {
            _motor.Unregister();
            _motor.UnbindEntity();
            _motor = null;
        }

        for (int i = 0; i < _colliders.Count; i++)
        {
            FPCollisionWorld.Instance.UnregisterCollider(_colliders[i]);
        }

        _colliders.Clear();

        if (_physicsMover != null)
        {
            Entity.BaseWorld?.GetSystem<FPKinematicCharacterSystem>()?.UnregisterPhysicsMover(_physicsMover);
        }

        _dynamicBody = null;
        _physicsMover = null;
        _moverController = null;
        _characterController = null;
        _kinematicVerticalVelocity = (fp)0;
    }

    private void SimulateGravityAndGrounding(fp deltaTime)
    {
        PhysicsBodyConfig bodyConfig = _config?.PhysicsBody;
        if (bodyConfig == null ||
            bodyConfig.bodyType == PhysicsBodyType.Static ||
            !bodyConfig.useGravity ||
            _colliders.Count == 0)
        {
            return;
        }

        PhysicsMotionInfluence influence = bodyConfig.collisionInfluence ?? PhysicsMotionInfluence.CreateRigidbodyDefault();
        if (!influence.positionY)
        {
            return;
        }

        fp gravity = PhysicsConfigConverter.ResolveGravityAcceleration(bodyConfig);
        if (gravity == (fp)0)
        {
            return;
        }

        fp verticalVelocity = ResolveVerticalVelocity(gravity, deltaTime);
        fp displacementY = verticalVelocity * deltaTime;
        if (displacementY == (fp)0)
        {
            return;
        }

        fp3 position = Entity.transform.Position;
        fp3 targetPosition = position + new fp3((fp)0, displacementY, (fp)0);
        if (displacementY <= (fp)0)
        {
            if (TryResolveGroundContact(position, targetPosition, out fp groundedY))
            {
                targetPosition.y = groundedY;
                verticalVelocity = (fp)0;
            }
        }

        Entity.transform.Position = influence.BlendPosition(position, targetPosition);
        WriteVerticalVelocity(verticalVelocity);
    }

    private fp ResolveVerticalVelocity(fp gravity, fp deltaTime)
    {
        if (_dynamicBody != null && !_dynamicBody.IsKinematic)
        {
            fp3 velocity = _dynamicBody.Velocity;
            velocity.y += gravity * deltaTime;
            _dynamicBody.Velocity = velocity;
            return velocity.y;
        }

        _kinematicVerticalVelocity += gravity * deltaTime;
        return _kinematicVerticalVelocity;
    }

    private void WriteVerticalVelocity(fp verticalVelocity)
    {
        if (_dynamicBody != null && !_dynamicBody.IsKinematic)
        {
            fp3 velocity = _dynamicBody.Velocity;
            velocity.y = verticalVelocity;
            _dynamicBody.Velocity = velocity;
            return;
        }

        _kinematicVerticalVelocity = verticalVelocity;
    }

    private bool TryResolveGroundContact(fp3 currentPosition, fp3 targetPosition, out fp groundedEntityY)
    {
        groundedEntityY = targetPosition.y;
        if (!TryGetPrimaryColliderBottom(currentPosition, out fp3 bottomPosition, out fp bottomHalfExtent))
        {
            return false;
        }

        fp entityToBottom = currentPosition.y - bottomPosition.y;
        if (entityToBottom < (fp)0)
        {
            entityToBottom = bottomHalfExtent;
        }

        fp3 down = -FPMathKCC.WorldUp;
        fp fallDistance = fpmath.max((fp)0, currentPosition.y - targetPosition.y);
        // 从实体 pivot 上方发射射线，避免碰撞体底部低于地板时向下射线永远碰不到地板。
        fp3 probeOrigin = currentPosition + FPMathKCC.WorldUp * FPMathKCC.GroundProbeSkin;
        fp probeDistance = entityToBottom + fallDistance + FPMathKCC.GroundProbeSkin * (fp)2;
        if (!TryGroundRaycast(probeOrigin, down, probeDistance, out FPRaycastHit hit))
        {
            return false;
        }

        fp groundedBottomY = hit.Point.y + FPMathKCC.GroundProbeSkin;
        fp targetBottomY = bottomPosition.y - fallDistance;
        if (targetBottomY > groundedBottomY)
        {
            return false;
        }

        groundedEntityY = currentPosition.y + (groundedBottomY - bottomPosition.y);
        return true;
    }

    private bool TryGetPrimaryColliderBottom(fp3 entityPosition, out fp3 bottomPosition, out fp bottomHalfExtent)
    {
        bottomPosition = entityPosition;
        bottomHalfExtent = (fp)0;

        IFPCollider collider = _colliders[0];
        switch (collider.ShapeType)
        {
            case FPShapeType.Box:
            {
                FPBoxShape box = collider.GetBoxShape();
                bottomPosition = box.Center - FPMathKCC.WorldUp * box.HalfExtents.y;
                bottomHalfExtent = box.HalfExtents.y;
                return true;
            }
            case FPShapeType.Sphere:
            {
                FPSphereShape sphere = collider.GetSphereShape();
                bottomPosition = sphere.Center - FPMathKCC.WorldUp * sphere.Radius;
                bottomHalfExtent = sphere.Radius;
                return true;
            }
            case FPShapeType.Capsule:
            {
                FPCapsuleShape shape = collider.GetCapsuleShape();
                FPCapsuleGeometry geometry = FPMathKCC.BuildCapsuleGeometry(
                    collider.Position,
                    collider.Rotation,
                    shape.Radius,
                    shape.Height,
                    shape.Center.y);
                bottomPosition = geometry.BottomHemiCenter;
                bottomHalfExtent = shape.Radius;
                return true;
            }
            default:
                return false;
        }
    }

    private bool TryGroundRaycast(fp3 origin, fp3 direction, fp distance, out FPRaycastHit hit)
    {
        hit = default;
        if (distance <= (fp)0)
        {
            return false;
        }

        FPLayerMask queryLayers = BuildGroundQueryMask();
        FPCollisionWorld world = FPCollisionWorld.Instance;
        fp bestDistance = distance + (fp)1;
        bool found = false;

        for (int i = 0; i < world.Colliders.Count; i++)
        {
            IFPCollider collider = world.Colliders[i];
            if (IsSelfCollider(collider) || collider.IsTrigger)
            {
                continue;
            }

            if (!queryLayers.Contains(collider.Layer))
            {
                continue;
            }

            FPRaycastHit candidate;
            if (!FPCapsuleCollision.Raycast(origin, direction, distance, collider, out candidate))
            {
                continue;
            }

            if (candidate.Distance < bestDistance)
            {
                bestDistance = candidate.Distance;
                hit = candidate;
                found = true;
            }
        }

        return found;
    }

    private bool IsSelfCollider(IFPCollider collider)
    {
        for (int i = 0; i < _colliders.Count; i++)
        {
            if (ReferenceEquals(_colliders[i], collider))
            {
                return true;
            }
        }

        return false;
    }

    private FPLayerMask BuildGroundQueryMask()
    {
        PhysicsColliderSetting setting = _config.PhysicsBody.colliders[0];
        int mask = 0;
        foreach (FPCollisionLayer layer in System.Enum.GetValues(typeof(FPCollisionLayer)))
        {
            if (layer == FPCollisionLayer.None || layer == FPCollisionLayer.All)
            {
                continue;
            }

            if (FPPhysicsQuery.GetLayerCollision(setting.layer, layer))
            {
                mask |= (int)layer;
            }
        }

        return new FPLayerMask(mask);
    }
}
