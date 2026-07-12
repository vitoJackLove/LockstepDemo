using System;
using Ase.Serializing;
using Unity.Mathematics.FixedPoint;

/// <summary>
/// 怪物实体
/// </summary>
public class MonsterEntity : BaseEntity
{
    private BattleMonsterData _battleMonsterData;

    protected override BattleEntityData EntityPropertyData => _battleMonsterData;

    public override void OnInit(object data = null)
    {
        base.OnInit(data);

        MonsterAssetsConfig config = (MonsterAssetsConfig)Config;
        
        if (config != null)
        {
            _battleMonsterData = BattleMonsterData.Creat(config);
            SetData(ComponentDataKey.ColliderData, config.colliderDataList);
            SetData(ComponentDataKey.BehaviourTreeConfig, config.treeAssetsPath);
            SetData(ComponentDataKey.StateData, config.stateList);
            SetData(ComponentDataKey.BlendTreeData, config.blendTree);
            SetData(ComponentDataKey.PhysicsBodyData, new PhysicsEntityConfig
            {
                MovementMode = config.movementMode,
                CharacterController = config.characterController,
                PhysicsBody = config.physicsBody,
            });
        }
    }

    public override void TakeSnapShot(uint tick, BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
    {
        base.TakeSnapShot(tick, hardWriter, softWriter);
        
        fp hp = GetProperty(PropertyKey.Hp);
        
        softWriter.WriterFpData($"Hp", hp);
    }

    public override void SoftRollBack(PooledReader authoritySnapShot)
    {
        base.SoftRollBack(authoritySnapShot);

        fp authorityHp = BaseSnapShotData.ReadFpData(authoritySnapShot);
        
        SetProperty(PropertyKey.Hp, authorityHp);
    }

    protected override Type[] GetComponentTypes()
    {
        return new Type[]
        {
            typeof(TransformComponent),
            typeof(AnimatorComponent),
            typeof(AiStateComponent),
            typeof(ColliderComponent),
            typeof(MonsterHitComponent),
            typeof(BulletControlCompinent),
            typeof(PathFindingComponent),
            typeof(AiComponent),
            typeof(MonsterSkillComponent),
            typeof(EnemyDetectionComponent),
            typeof(PhysicsBodyComponent),
        };
    }

    protected override Type EntityView()
    {
        return typeof(MonsterEntityView);
    }

    public override EntityType EntityType
    {
        get => EntityType.MonsterEntity;
    }

    public override CampEnum CampEnum => ((MonsterAssetsConfig)Config).campEnum;
    public BattleMonsterData BattleMonsterData => _battleMonsterData;
    public override ForecastEntityType ForecastEntityType => ForecastEntityType.StaticEntity;
}
