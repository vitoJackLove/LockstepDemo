
[ClipName("开启技能派生窗口Clip")]
public class SkillDeroveWindowClip : TaskClip
{
    [VariableName("技能ID")] public int skillId;

    [VariableName("派生的技能ID")] public int deriveSkillId;
    
    [VariableName("派生的时间")] public float deriveTime;

    [VariableName("还原时间")] public bool restoreDeriveTime;
    
    [VariableName("派生次数")] public int deriveNumber;

    [VariableName("跟随行为树关闭窗口")]public bool restoreWindow;
    
    [VariableName("是否是状态派生")]public bool isStateDerive;

}
