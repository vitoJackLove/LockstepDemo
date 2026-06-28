using System;

/// <summary>
/// 魔方实体
/// </summary>
public class CubeEntity : BaseEntity
{
    protected override Type[] GetComponentTypes()
    {
        return new Type[]
        {
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
