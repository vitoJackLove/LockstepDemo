using Unity.Mathematics.FixedPoint;
using UnityEngine;

[ClipName("修改动画混合树参数")]
public class ChangeAnimatorParamClip : TaskClip
{
    [VariableName("移动速度")] public float moveSpeed;

    [VariableName("旋转速度")] public float rotateSpeed;

    public override void OnRunTimeEnter(BaseEntity context, int fps)
    {
        base.OnRunTimeEnter(context, fps);
        
        context.GetTypeOfComponent<AnimatorComponent>().PlayMixer();
    }

    public override void RollBackEnter(BaseEntity context, int fps)
    {
        base.RollBackEnter(context, fps);
        
        context.GetTypeOfComponent<AnimatorComponent>().PlayMixer();
    }

    public override void RunTimeTick(int currentFrameID, int fps, fp deltaTime, BaseEntity context)
    {
        context.GetTypeOfComponent<AnimatorComponent>().SetBlendTreeParam(new Vector2(moveSpeed,rotateSpeed));
    }

    public override void OnRunTimeExit(BaseEntity context)
    {
        base.OnRunTimeExit(context);
        
        context.GetTypeOfComponent<AnimatorComponent>().SetBlendTreeParam(new Vector2(0,0));
    }

    public override void RollBackExit(BaseEntity context)
    {
        base.RollBackExit(context);
        
        context.GetTypeOfComponent<AnimatorComponent>().SetBlendTreeParam(new Vector2(0,0));
    }
}

public enum ParamType
{
    Bool,
    Float,
    Int,
    String,
}
