using UnityEngine;

[ClipName("执行技能状态")]
public class ExecuteSkillStateClip : TaskClip
{
    [VariableName("技能ID")]
    public int skillId;

    [VariableName("技能标签")]
    public string skillLabel;

    [VariableName("技能位置")]
    public Vector3 skillPosition;
}
