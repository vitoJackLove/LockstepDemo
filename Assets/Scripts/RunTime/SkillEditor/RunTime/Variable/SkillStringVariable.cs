using System;

[Serializable] 
public class SkillStringVariable : SkillBlackVariable
{
    /// <summary>
    /// 变量Value
    /// </summary>
    public string variableValue = "test";

    public override Type VariableType()
    {
        return typeof(string);
    }

    public override string InitVariableKey => "stringVariable";
    
    public override object GetValue => variableValue;
    
    public override void SetValue(object obj)
    {
        variableValue = (string)obj;
    }
}
