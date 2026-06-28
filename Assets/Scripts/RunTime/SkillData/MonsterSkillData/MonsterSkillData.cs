using Ase.Serializing;
using Unity.Mathematics.FixedPoint;

/// <summary>
/// 怪物技能数据
/// </summary>
public class MonsterSkillData : IPool
{
    /// <summary>
    /// 技能执行器
    /// </summary>
    private SkillTimelineLauncher _skillTimeline;

    private int _skillId;

    public int SKillId => _skillId;

    /// <summary>
    /// 创建技能
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="skillConfig"></param>
    /// <returns></returns>
    public static MonsterSkillData Create(BaseEntity entity, MonsterSkillConfig skillConfig)
    {
        MonsterSkillData skillData = FPoolHelper.Get<MonsterSkillData>();
        
        skillData.InitAssets(entity, skillConfig);
        skillData._skillId = skillConfig.skillId;
        
        return skillData;
    }
    
    private async void InitAssets(BaseEntity entity,MonsterSkillConfig config)
    {
        _skillTimeline = await entity.GetSystem<SkillTimeLineSystem>().InitSkillTimeLine(config.skillAssetsPath, entity);
    }
    
    /// <summary>
    /// 执行技能
    /// </summary>
    public void ExecuteSkill()
    {
        if (_skillTimeline == null)
        {
            return;
        }
        
        _skillTimeline.RefreshInitState();
    }

    /// <summary>
    /// 刷新技能
    /// </summary>
    public PlayableStateEnum UpdateSkill(fp deltaTime)
    {
        if (_skillTimeline == null)
        {
            return PlayableStateEnum.Error;
        }

        return _skillTimeline.Tick(deltaTime);
    }

    /// <summary>
    /// 打断技能
    /// </summary>
    public void BreakSkill(bool isRollBackBreakSkill)
    {
        if (_skillTimeline != null)
        {
            _skillTimeline.ForceExecuteStop(isRollBackBreakSkill);
        }
    }
    
    public void Clear()
    {
        if (_skillTimeline != null)
        {
            _skillTimeline.Clear();
            _skillTimeline = null;
        }
    }

    public void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
    {
        _skillTimeline.TakeSnapShot(hardWriter, softWriter);
    }

    public void RollBackTo(PooledReader authoritySnapShot)
    {
        _skillTimeline.RollBackTo(authoritySnapShot);
    }
}
