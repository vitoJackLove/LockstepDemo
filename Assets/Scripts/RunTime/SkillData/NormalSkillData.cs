/// <summary>
/// 普通技能
/// </summary>
public class NormalSkillData : BaseSkillData
{
    protected override void StartExecuteSkill(WorldContent.CommandExecuteState commandState)
    {
        base.StartExecuteSkill(commandState);
        
        BaseEntity.GetComponent<SkillComponent>().RefreshAttack();
    }

    public override void BreakSkill(bool isRollBackBreakSkill)
    {
        base.BreakSkill(isRollBackBreakSkill);
        
        BaseEntity.GetComponent<SkillComponent>().RefreshAttack();
    }

    public static NormalSkillData Create(HeroSkillConfig config, BaseEntity entity) 
    {
        NormalSkillData data = FPoolHelper.Get<NormalSkillData>();
        data.Id = config.skillId;
        data.BaseEntity = entity;
        data.CommandType = config.keyCode;
        data.BaseSkillExecute = data.CreateSkillExecute(config);
        return data;
    }
}
