using System.Collections.Generic;
using UnityEngine;

[ClipName("技能打断窗口")]
public class SkillBreakWindowClip : TaskClip
{
    [VariableName("指令类型")] public CommandType skillId;
    [VariableName("打断指令组")] public List<CommandType> breakSkillIdList = new List<CommandType>();
    [VariableName("开启打断")] public bool openBreak;
    [VariableName("节点执行结束还原")] public bool onSkillTimeEnd;

    public override void OnRunTimeEnter(BaseEntity context, int fps)
    {
        base.OnRunTimeEnter(context, fps);

        context.GetComponent<SkillComponent>().SetCommandBreak(skillId,breakSkillIdList,openBreak);
        
        Debug.Log($"技能打断窗口 ： skillId = {skillId}  breakSkillIdList.Count = {breakSkillIdList.Count}");
    }

    public override void RollBackEnter(BaseEntity context, int fps)
    {
        base.RollBackEnter(context, fps);
        
        context.GetComponent<SkillComponent>().SetCommandBreak(skillId,breakSkillIdList,openBreak);
        
        Debug.Log($"技能打断窗口 ： skillId = {skillId}  breakSkillIdList.Count = {breakSkillIdList.Count}");
    }

    public override void OnRunTimeExit(BaseEntity context)
    {
        base.OnRunTimeExit(context);
        
        if (onSkillTimeEnd)
        {
            context.GetComponent<SkillComponent>().SetCommandBreak(skillId,breakSkillIdList,!openBreak);
        }
    }

    public override void RollBackExit(BaseEntity context)
    {
        base.RollBackExit(context);
        
        context.GetComponent<SkillComponent>().SetCommandBreak(skillId,breakSkillIdList,!openBreak);
    }
}