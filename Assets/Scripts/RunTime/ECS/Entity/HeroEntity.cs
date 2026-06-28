using System;

public partial class HeroEntity : BaseEntity
{
    private BattleHeroData _battleHeroData;
    
    public override void OnInit(object data = null)
    {
        base.OnInit(data);

        _battleHeroData = BattleHeroData.Creat((HeroAssetsConfig)Config);
        SetData(ComponentDataKey.LifeTime, 100);
        SetData(ComponentDataKey.ColliderData, ((HeroAssetsConfig)Config).colliderDataList);
        SetData(ComponentDataKey.StateData, ((HeroAssetsConfig)Config).stateList);
        SetData(ComponentDataKey.BlendTreeData, ((HeroAssetsConfig)Config).blendTree);
    }

    protected override BattleEntityData EntityPropertyData => _battleHeroData;
    
    protected override Type[] GetComponentTypes()
    {
        return new []
        {
            typeof(CommandMoveComponent),
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
