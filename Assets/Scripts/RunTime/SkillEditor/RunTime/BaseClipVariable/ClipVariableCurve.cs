using System;
using UnityEngine;

/// <summary>
/// 节点变量
/// </summary>
[Serializable]
public class ClipVariableCurve : BaseClipVariable
{
    public AnimationCurve curveValue;
    
    public override Type SVariableType()
    {
        return typeof(AnimationCurve);
    }

    public override object GetVariableValue()
    {
        return curveValue;
    }

    public override void SetVariableValue(object obj)
    {
        this.curveValue = (AnimationCurve)obj;
    }
}
