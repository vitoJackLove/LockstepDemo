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

    /// <summary>CharacterController 范式下的 Motor；Rigidbody 范式为 null。</summary>
    public FPKinematicCharacterMotor Motor => _motor;

    /// <summary>当前移动范式。</summary>
    public PhysicsMovementMode MovementMode => _config?.MovementMode ?? PhysicsMovementMode.CharacterController;

    public override void OnStart(object data = null)
    {
        base.OnStart(data);
        _config = ResolveConfig();

        if (_config.MovementMode == PhysicsMovementMode.CharacterController)
        {
            RegisterCharacterMotor();
        }
        // Rigidbody 分支在 Task 9 实现
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

    private void UnregisterAll()
    {
        if (_motor != null)
        {
            _motor.Unregister();
            _motor.UnbindEntity();
            _motor = null;
        }
    }
}
