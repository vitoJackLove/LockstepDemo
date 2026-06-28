using System;

[Serializable] 
public class SkillFloatVariable : SkillBlackVariable
{
    /// <summary>
    /// 变量Value
    /// </summary>
    public float variableValue = 0.0f;

    public override Type VariableType()
    {
        return typeof(float);
    }

    public override string InitVariableKey => "FloatVariable";
    
    public override object GetValue => variableValue;
    
    public override void SetValue(object obj)
    {
        variableValue = (float)obj;
    }
}
