using System.Collections.Generic;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// 实体物理注册运行时配置（可由 Entity.SetData 注入，覆盖资产默认）。
/// </summary>
public class PhysicsEntityConfig
{
    public PhysicsMovementMode MovementMode;
    public CharacterControllerSettings CharacterController;
    public PhysicsBodyConfig PhysicsBody;

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
/// 统一物理注册：CharacterController（Motor 单胶囊）或 Rigidbody（复合碰撞体）。
/// </summary>
public class PhysicsBodyComponent : BaseComponent
{
    private static readonly FPNullCharacterController NullController = new FPNullCharacterController();

    private PhysicsEntityConfig _config;
    private FPKinematicCharacterMotor _motor;
    private readonly List<IFPCollider> _colliders = new List<IFPCollider>(4);
    private FPDynamicRigidbody _dynamicBody;
    private FPPhysicsMover _physicsMover;
    private FPEntityPhysicsMoverController _moverController;

    /// <summary>CharacterController 范式下的 Motor；Rigidbody 范式为 null。</summary>
    public FPKinematicCharacterMotor Motor => _motor;

    /// <summary>当前移动范式。</summary>
    public PhysicsMovementMode MovementMode => _config?.MovementMode ?? PhysicsMovementMode.CharacterController;

    /// <summary>Rigidbody 范式下注册的复合碰撞体列表。</summary>
    public IReadOnlyList<IFPCollider> Colliders => _colliders;

    /// <summary>Dynamic/Kinematic 刚体；Static 范式为 null。</summary>
    public FPDynamicRigidbody DynamicBody => _dynamicBody;

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
            GameLog.Warning(GameLogChannel.Battle,
                $"Entity {Entity.EntityId}: CharacterController mode ignores physicsBody.colliders per spec.");
        }
    }

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

        SyncDynamicBodyPose();
    }

    public override void OnDispose()
    {
        UnregisterAll();
        base.OnDispose();
    }

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

        // CubeEntity 等：默认 Static Rigidbody 盒体（Task 11 细化）
        return PhysicsEntityConfig.CreateDefaultStaticBox(Entity);
    }

    private void RegisterCharacterMotor()
    {
        CharacterControllerSettings cc = _config.CharacterController ?? new CharacterControllerSettings();
        PhysicsConfigConverter.ToCharacterMotorDimensions(cc, out fp radius, out fp height, out fp yOffset);

        _motor = new FPKinematicCharacterMotor(NullController)
        {
            CharacterLayer = cc.layer,
        };
        _motor.SetCapsuleDimensions(radius, height, yOffset);
        _motor.SetPositionAndRotation(Entity.transform.Position, Entity.transform.Rotation);
        _motor.BindEntity(Entity);
        _motor.Register();
    }

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
                };
                _dynamicBody.SetPose(Entity.transform.Position, Entity.transform.Rotation);
                break;

            case PhysicsBodyType.Kinematic:
                _dynamicBody = new FPDynamicRigidbody
                {
                    Id = FPCollisionWorld.Instance.AllocateBodyId(),
                    IsKinematic = true,
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
                collider.AttachedBody = _dynamicBody;
            }

            _colliders.Add(collider);
            FPCollisionWorld.Instance.RegisterCollider(collider);
        }

        SyncDynamicBodyPose();
    }

    private void SyncColliderFromEntity(IFPCollider collider, PhysicsColliderSetting setting, fp3 scale)
    {
        fp3 localOffset = PhysicsConfigConverter.ToFp3(setting.localOffset);
        fpquaternion localRot = fpquaternion.Euler(
            (fp)setting.localEuler.x * fpmath.Deg2Rad,
            (fp)setting.localEuler.y * fpmath.Deg2Rad,
            (fp)setting.localEuler.z * fpmath.Deg2Rad);
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

    private void SyncDynamicBodyPose()
    {
        if (_dynamicBody == null)
        {
            return;
        }

        _dynamicBody.SetPose(Entity.transform.Position, Entity.transform.Rotation);
    }

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
        _dynamicBody = null;
        _physicsMover = null;
        _moverController = null;
    }
}
