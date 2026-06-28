using System;
using UnityEngine;

[Serializable] 
public class SkillCuveVariable : SkillBlackVariable
{
    /// <summary>
    /// 变量Value
    /// </summary>
    public AnimationCurve variableValue = AnimationCurve.Linear(0, 0, 1, 1);

    public override Type VariableType()
    {
        return typeof(AnimationCurve);
    }
    
    public override string InitVariableKey => "CuveVariable";
    
    public override object GetValue => variableValue;
    
    public override void SetValue(object obj)
    {
        variableValue = (AnimationCurve)obj;
    }
}
