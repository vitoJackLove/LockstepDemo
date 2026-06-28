using Ase.Serializing;

/// <summary>
/// 按下技能
/// </summary>
public class DownExecuteSkill : BaseSkillExecute
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
    public static DownExecuteSkill Create(BaseSkillData skillData, HeroSkillConfig config, BaseEntity entity)
    {
        DownExecuteSkill daSkill = FPoolHelper.Get<DownExecuteSkill>();
        daSkill.InitAssets(config, entity);
        return daSkill;
    }

    private async void InitAssets(HeroSkillConfig config,BaseEntity entity)
    {
        _skillTimeLineLauncher = await entity.GetSystem<SkillTimeLineSystem>().InitSkillTimeLine(config.skillDownAssetsPath, entity);
    }
    
    public override bool IsCanExecuteCommand(WorldContent.CommandExecuteState commandState)
    {
        return commandState == WorldContent.CommandExecuteState.OnlyDown;
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
        if (commandState == WorldContent.CommandExecuteState.OnlyDown)
        {
            _skillTimeLineLauncher.RefreshInitState();
        }
    }

    public override PlayableStateEnum TickSkill()
    {
        PlayableStateEnum state = _skillTimeLineLauncher.Tick(fpmath1.LogicDeltaTime);
        
        return state;
    }
    
    public override void BreakSkill(bool isRollBackBreakSkill)
    {
        _skillTimeLineLauncher.ForceExecuteStop(isRollBackBreakSkill);
    }

    public override void Clear()
    {
    }
}
