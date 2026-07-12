using System;

public partial class HeroEntity : BaseEntity
{
    private BattleHeroData _battleHeroData;
    
    public override void OnInit(object data = null)
    {
        base.OnInit(data);

        HeroAssetsConfig heroConfig = (HeroAssetsConfig)Config;
        _battleHeroData = BattleHeroData.Creat(heroConfig);
        SetData(ComponentDataKey.LifeTime, 100);
        SetData(ComponentDataKey.ColliderData, heroConfig.colliderDataList);
        SetData(ComponentDataKey.StateData, heroConfig.stateList);
        SetData(ComponentDataKey.BlendTreeData, heroConfig.blendTree);

        if (heroConfig.characterController.layer == FPCollisionLayer.Default)
        {
            heroConfig.characterController.layer = FPCollisionLayer.Hero;
        }
    }

    protected override BattleEntityData EntityPropertyData => _battleHeroData;
    
    protected override Type[] GetComponentTypes()
    {
        return new Type[]
        {
            typeof(PhysicsBodyComponent),
            typeof(MoveComponent),
            typeof(TransformComponent),
            typeof(HeroAnimatorComponent),
            typeof(ColliderComponent),
            typeof(HeroStateComponent),
            typeof(SkillComponent),
            typeof(DisplacementComponent),
            typeof(HeroHitComponent),
            typeof(BulletControlCompinent),
            typeof(BuffComponent),
        };
    }

    protected override Type EntityView()
    {
        return typeof(HeroEntityView);
    }

    public override EntityType EntityType
    {
        get => EntityType.HeroEntity;
    }
    
    public override CampEnum CampEnum => ((HeroAssetsConfig)Config).campEnum;
    public BattleHeroData BattleHeroData => _battleHeroData;
    public override ForecastEntityType ForecastEntityType => ForecastEntityType.StaticEntity;
}
