using System;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// 英雄动画组件
/// </summary>
public class HeroAnimatorComponent : AnimatorComponent
{
    private fp3 _currentVelocity = fp3.zero;
    
    /// <summary>
    /// 当前插值移速
    /// </summary>
    private float _lerpMoveSpeed;
    
    public override void OnUpdate(fp deltaTime)
    {
        base.OnUpdate(deltaTime);

        LerpAnimatorParamMoveSpeed(Entity.MoveEnable);
    }
    
    public override void OnExecuteLocalCommand(CommandData commandData)
    {
        base.OnExecuteLocalCommand(commandData);
        
        PlayMoveAnimator(commandData);
    }

    private void PlayMoveAnimator(CommandData commandData)
    {
        _currentVelocity = commandData.MoveDir;
    }
    
    /// <summary>
    /// 插值移动动画参数
    /// </summary>
    private void LerpAnimatorParamMoveSpeed(bool isOpenMove)
    {
        if (isOpenMove && (_currentVelocity != fp3.zero).Bool3ToBool())
        {
            if (Math.Abs(1 - _lerpMoveSpeed) > 0.01f)
            {
                _lerpMoveSpeed = TsUtil.MoveSpeedLerp(_lerpMoveSpeed, 1, 0.2f);
            }
        }
        else
        {
            if (_lerpMoveSpeed != 0)
            {
                _lerpMoveSpeed = TsUtil.MoveSpeedLerp(_lerpMoveSpeed, 0f, 0.2f);
            }
        }

        SetBlendTreeParam(new Vector2(_lerpMoveSpeed, 0));
    }
}
