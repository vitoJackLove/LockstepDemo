[ClipName("创建Buff")]
public class PlayBuffClip : TaskClip
{
    [VariableName("BuffID")] public int buffId;

    public override void OnRunTimeEnter(BaseEntity context, int fps)
    {
        base.OnRunTimeEnter(context, fps);

        context.GetComponent<BuffComponent>().CreateBuff(buffId);
    }
}
