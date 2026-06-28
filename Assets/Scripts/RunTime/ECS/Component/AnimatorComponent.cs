using Animancer;
using UnityEngine;

/// <summary>
/// 动画组件
/// </summary>
public class AnimatorComponent : BaseComponent
{
    /// <summary>
    /// 动画组件
    /// </summary>
    private AnimancerComponent _animatorComponent;

    /// <summary>
    /// 动画混合器
    /// </summary>
    private BaseMixer _baseMixer;
    
    public void RegisterAnimator(AnimancerComponent animator)
    {
        _animatorComponent = animator;
    }
    
    public override void OnStart(object data = null)
    {
        base.OnStart(data);

        if (_animatorComponent == null)
        {
            return;
        }
        
        AnimatorBlendTree blendTreeConfig = Entity.GetData<AnimatorBlendTree>(ComponentDataKey.BlendTreeData);

        InitBlendTree(blendTreeConfig);
    }

    private void InitBlendTree(AnimatorBlendTree blendTreeConfig)
    {
        switch (blendTreeConfig.blendTreeType)
        {
            case BlendTreeType.Line:

                _baseMixer = new LinearMixer(blendTreeConfig.transitionAssetBase, blendTreeConfig.valueOne,
                    _animatorComponent);
                
                break;
            
            case BlendTreeType.Mixer2D:

                _baseMixer = new Mixer2D(blendTreeConfig.transitionAssetBase, blendTreeConfig.valueOne,
                    blendTreeConfig.valueTwo, _animatorComponent, blendTreeConfig.smoothTime);
                
                break;
        }
    }

    /// <summary>
    /// 设置混合树参数
    /// </summary>
    /// <param name="param"></param>
    public void SetBlendTreeParam(Vector2 param)
    {
        if (_baseMixer == null)
        {
            return;
        }
        
        _baseMixer.SetValue(param);
    }

    public void PlayMixer()
    {
        if (_baseMixer.TransitionAssetBase != null)
        {
            _animatorComponent.Play(_baseMixer.TransitionAssetBase);
        }
    }

    public AnimancerState PlayAnimatorClip(AnimationClip animationClip,float fadeDuration,FadeMode fadeMode = default)
    {
        return _animatorComponent.Play(animationClip, fadeDuration, fadeMode);
    }

    public override void OnDispose()
    {
        base.OnDispose();

        if (_baseMixer != null)
        {
            _baseMixer.DisPose();
            _baseMixer = null;
        }

        _animatorComponent = null;
    }
}