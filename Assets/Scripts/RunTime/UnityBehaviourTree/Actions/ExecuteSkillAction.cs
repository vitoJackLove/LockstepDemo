using TheKiwiCoder;

[System.Serializable]
public class ExecuteSkillAction : ActionNode
{
    public int skillId;
    
    protected override void OnStart(WorldUpdateType worldUpdateType)
    {
        context.Entity.GetComponent<MonsterSkillComponent>().ExecuteSkill(skillId,false);
    }

    protected override void OnStop()
    {
        
    }

    protected override State OnUpdate(uint tick,WorldUpdateType worldUpdateType)
    {
        return State.Success;
    }
}
