using System;
using Animancer;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

[Serializable]
[ClipName("播放动画片段")]
public class PlayAnimationClip : TaskClip
{
    [VariableName("动画片段")] public AnimationClip clip;
    [VariableName("融合时间")] public float fadeDuration = 0.2f;
    [VariableName("融合类型")] public FadeMode fadeMode;

    private AnimancerState _state;
    
    public override void OnRunTimeEnter(BaseEntity context, int fps)
    {
        base.OnRunTimeEnter(context, fps);

        _state = context.GetTypeOfComponent<AnimatorComponent>().PlayAnimatorClip(clip, fadeDuration, fadeMode);
    }

    public override void RollBackEnter(BaseEntity context, int fps)
    {
        base.RollBackEnter(context, fps);
        
        _state = context.GetTypeOfComponent<AnimatorComponent>().PlayAnimatorClip(clip, fadeDuration, fadeMode);
    }

    public override void RunTimeTick(int currentFrameID, int fps, fp deltaTime, BaseEntity context)
    {
        if (context == null) return;
        if (clip == null) return;
        
        float time = (float)currentFrameID / taskDuration * clip.length;

        _state.Time = time;
    }

    public override void EditorTick(int currentFrameID, int fps, fp deltaTime, GameObject context)
    {
        if (context == null) return;
        if (clip == null) return;
        
        if (_state == null)
        {
            _state = context.GetComponent<AnimancerComponent>().Play(clip,fadeDuration);
        }
        
        float time = (float)currentFrameID / taskDuration * clip.length;

        _state.Speed = 0;
        _state.Time = time;
    }

    public override void OnRunTimeExit(BaseEntity context)
    {
        base.OnRunTimeExit(context);

        context.GetTypeOfComponent<AnimatorComponent>().PlayMixer();
    }

    public override void EditorExit(GameObject context, int fps, int currentFrameID)
    {
        base.EditorExit(context, fps, currentFrameID);
        
        if (_state != null)
        {    
            _state.Destroy();
            _state = null;
        }
    }

    public override void RollBackExit(BaseEntity context)
    {
        base.RollBackExit(context);
        
        context.GetTypeOfComponent<AnimatorComponent>().PlayMixer();
    }
}