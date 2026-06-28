
[ClipName("修改运动开关")]
public class OpenMovementClip : TaskClip
{
    [VariableName("移动开关")] public bool moveEnable;
    [VariableName("旋转开关")] public bool rotateEnable;
    [VariableName("TimeLine结束还原")] public bool onSkillTimeEnd;

    public override void OnRunTimeEnter(BaseEntity context, int fps)
    {
        base.OnRunTimeEnter(context, fps);

        MoveComponent moveComponent = context.GetComponent<MoveComponent>();

        if (moveComponent != null)
        {
            moveComponent.SetMovementState(moveEnable, rotateEnable);
        }
    }
}
