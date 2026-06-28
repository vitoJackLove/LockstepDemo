/// <summary>
/// 普攻技能
/// </summary>
public class AttackSkillData : BaseSkillData
{
    protected override void StartExecuteSkill(WorldContent.CommandExecuteState commandState)
    {
        base.StartExecuteSkill(commandState);
        
        BaseEntity.GetComponent<SkillComponent>().ExecuteAttack();
        
        BaseEntity.GetComponent<SkillComponent>().AttackTickCounting(false);
    }

    public static AttackSkillData Create(HeroSkillConfig config, BaseEntity entity) 
    {
        AttackSkillData data = FPoolHelper.Get<AttackSkillData>();
        data.Id = config.skillId;
        data.BaseEntity = entity;
        data.CommandType = config.keyCode;
        data.BaseSkillExecute = data.CreateSkillExecute(config);
        return data;
    }

    public override void OnSkillEnd()
    {
        BaseEntity.GetComponent<SkillComponent>().AttackTickCounting(true);
    }
}
