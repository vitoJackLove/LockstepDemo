using System;

[Serializable] 
public class SkillBoolVariable : SkillBlackVariable
{
    /// <summary>
    /// 变量Value
    /// </summary>
    public bool variableValue = false;

    public override Type VariableType()
    {
        return typeof(bool);
    }

    public override string InitVariableKey => "BoolVariable";

    public override object GetValue => variableValue;
    
    public override void SetValue(object obj)
    {
        variableValue = (bool)obj;
    }
}
