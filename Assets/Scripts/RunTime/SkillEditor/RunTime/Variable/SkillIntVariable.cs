using System;

[Serializable] 
public class SkillIntVariable : SkillBlackVariable
{
    /// <summary>
    /// 变量Value
    /// </summary>
    public int variableValue = 0;

    public override Type VariableType()
    {
        return  typeof(int);
    }

    public override string InitVariableKey => "IntVariable";
    
    public override object GetValue => variableValue;
    
    public override void SetValue(object obj)
    {
        variableValue = (int)obj;
    }
}
