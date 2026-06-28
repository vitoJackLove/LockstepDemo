
using Unity.Mathematics.FixedPoint;
using UnityEngine;

[ClipName("设置技能加成")]
public class SetSkillAdditionClip : TaskClip
{
    [VariableName("是否是全局加成")]
    public bool isGlobalAddition;
    
        
    [VariableName("加成")]
    public float addition;
    
    public override void RunTimeTick(int currentFrameID, int fps, fp deltaTime, BaseEntity context)
    {
        
    }

    public override void EditorTick(int currentFrameID, int fps, fp deltaTime, GameObject context)
    {
        
    }
}
