using Ase.Serializing;

/// <summary>
/// 抬起技能
/// </summary>
public class UpExecuteSkill : BaseSkillExecute
{
    /// <summary>
    /// 技能资产
    /// </summary>
    private SkillTimelineLauncher _skillTimeLineLauncher;
    
    /// <summary>
    /// 创建
    /// </summary>
    /// <param name="skillData"></param>
    /// <param name="config"></param>
    /// <param name="entity"></param>
    /// <returns></returns>
    public static UpExecuteSkill Create(BaseSkillData skillData, HeroSkillConfig config, BaseEntity entity)
    {
        UpExecuteSkill daSkill = FPoolHelper.Get<UpExecuteSkill>();
        daSkill.InitAssets(config, entity);
        return daSkill;
    }

    private async void InitAssets(HeroSkillConfig config,BaseEntity entity)
    {
        _skillTimeLineLauncher = await entity.GetSystem<SkillTimeLineSystem>().InitSkillTimeLine(config.skillDownAssetsPath, entity);
    }
    
    public override bool IsCanExecuteCommand(WorldContent.CommandExecuteState commandState)
    {
        return commandState == WorldContent.CommandExecuteState.OnlyUp;
    }

    public override void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
    {
        _skillTimeLineLauncher.TakeSnapShot(hardWriter, softWriter);
    }

    public override void RollBackTo(PooledReader authoritySnapShot)
    {
        _skillTimeLineLauncher.RollBackTo(authoritySnapShot);
    }

    public override void ExecuteSkill(WorldContent.CommandExecuteState commandState)
    {
        _skillTimeLineLauncher.RefreshInitState();
    }

    public override void BreakSkill(bool isRollBackBreakSkill)
    {
        _skillTimeLineLauncher.ForceExecuteStop(isRollBackBreakSkill);
    }

    public override PlayableStateEnum TickSkill()
    {
        PlayableStateEnum state = _skillTimeLineLauncher.Tick(fpmath1.LogicDeltaTime);
        return state;
    }

    public override void Clear()
    {
        
    }
}
