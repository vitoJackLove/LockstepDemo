using System;

/// <summary>
/// 魔方实体
/// </summary>
public class CubeEntity : BaseEntity
{
    public override void OnInit(object data = null)
    {
        base.OnInit(data);
        SetData(ComponentDataKey.PhysicsBodyData, PhysicsEntityConfig.CreateDefaultStaticBox(this));
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
