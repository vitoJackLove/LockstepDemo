using System;
using UnityEngine;


[Serializable] 
public class SkillVector3Variable : SkillBlackVariable
{
    /// <summary>
    /// 变量Value
    /// </summary>
    public Vector3 variableValue = Vector3.zero;

    public override Type VariableType()
    {
        return typeof(Vector3);
    }


    public override string InitVariableKey => "Vector2Variable";
    
    public override object GetValue => variableValue;
    
    public override void SetValue(object obj)
    {
        variableValue = (Vector3)obj;
    }
}
