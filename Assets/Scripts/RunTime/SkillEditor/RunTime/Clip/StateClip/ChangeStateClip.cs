[ClipName("切换状态")]
public class ChangeStateClip : TaskClip
{
    [VariableName("状态ID")] public int stateId;
    
    [VariableName("TimeLine结束还原")] public bool onSkillTimeEnd;
    
    public override void OnRunTimeEnter(BaseEntity context, int fps)
    {
        base.OnRunTimeEnter(context, fps);

        context.GetTypeOfComponent<StateComponent>().ChangeState(stateId, false, false);
    }

    public override void OnRunTimeExit(BaseEntity context)
    {
        base.OnRunTimeExit(context);
        
        if (onSkillTimeEnd)
        {
            context.GetTypeOfComponent<StateComponent>().EnterNormalState();
        }
    }
}
