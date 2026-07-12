using System;

/// <summary>
/// 魔方实体
/// </summary>
public class CubeEntity : BaseEntity
{
    public override void OnInit(object data = null)
    {
        base.OnInit(data);
        PhysicsEntityConfig physicsConfig = CustomData as PhysicsEntityConfig
            ?? PhysicsEntityConfig.CreateDefaultStaticBox(this);
        SetData(ComponentDataKey.PhysicsBodyData, physicsConfig);
    }

    protected override Type[] GetComponentTypes()
    {
        return new Type[]
        {
            typeof(PhysicsBodyComponent),
            typeof(TransformComponent),
        };
    }

    protected override Type EntityView()
    {
        return null;
    }

    public override EntityType EntityType => EntityType.CubeEntity;
    
    protected override BattleEntityData EntityPropertyData => null;
    public override ForecastEntityType ForecastEntityType => ForecastEntityType.StaticEntity;
}

/// <summary>
/// 场景调试用运动学平台实体：带碰撞核与往复运动组件。
/// </summary>
public sealed class ScenePlatformCubeEntity : CubeEntity
{
    public override void OnInit(object data = null)
    {
        base.OnInit(data);

        if (CustomData is ScenePlatformSpawnData spawnData)
        {
            SetData(ComponentDataKey.PhysicsBodyData, spawnData.physicsConfig);
            SetData(ComponentDataKey.SceneDebugPlatformMotion, spawnData.motionData);
        }
    }

    protected override Type[] GetComponentTypes()
    {
        return new Type[]
        {
            typeof(SceneDebugPlatformMotionComponent),
            typeof(PhysicsBodyComponent),
            typeof(TransformComponent),
        };
    }
}

/// <summary>
/// 场景运动学平台生成数据，经 <see cref="EntityCreateData.EntityData"/> 传入。
/// </summary>
public sealed class ScenePlatformSpawnData
{
    public PhysicsEntityConfig physicsConfig;
    public SceneDebugPlatformMotionData motionData;
}
