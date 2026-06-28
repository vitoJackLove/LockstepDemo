using System;
using Ase.Serializing;

/// <summary>
/// 子弹实体
/// </summary>
public class BulletEntity : BaseEntity
{
    public override void OnInit(object data = null)
    {
        base.OnInit(data);

        InitComponentData(Config as BulletAssetsConfig);

        ParentEntity.GetComponent<BulletControlCompinent>().RegisterBullet(this);
    }

    private void InitComponentData(BulletAssetsConfig config)
    {        
        this.SetData(ComponentDataKey.AttackNumber,config.attackNumber);
        this.SetData(ComponentDataKey.LifeTime, config.lifeTime);
        this.SetData(ComponentDataKey.ColliderData, ((BulletAssetsConfig)Config).colliderDataList);
        this.SetData(ComponentDataKey.MovementData, (MovementData)CustomData);
    }

    protected override Type[] GetComponentTypes()
    {
        return new Type[]
        {
            typeof(ColliderComponent),
            typeof(DisplacementComponent),
            typeof(BulletTriggerComponent),
            typeof(EntityLifeTimeComponent),
            typeof(TransformComponent)
        };
    }

    public override void DoEntityDead(object data = null)
    {
        base.DoEntityDead(data);
        
        ParentEntity?.GetComponent<BulletControlCompinent>().UnRegisterBullet(this);
    }

    protected override Type EntityView()
    {
        return typeof(BulletEntityView);
    }
    

    public override EntityType EntityType => EntityType.BulletEntity;
    public override ForecastEntityType ForecastEntityType => ForecastEntityType.DynamicEntity;
    protected override BattleEntityData EntityPropertyData => null;
    public override CampEnum CampEnum => ParentEntity?.CampEnum ?? CampEnum.NeutralCamp;
}