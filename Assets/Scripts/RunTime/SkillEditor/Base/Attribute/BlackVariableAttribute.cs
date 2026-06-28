
using System;

/// <summary>
/// 黑板变量
/// </summary>
public class BlackVariableAttribute : System.Attribute
{
    public Type VariableType;

    public BlackVariableAttribute(Type variableType)
    {
        this.VariableType = variableType;
    }

    public BlackVariableAttribute()
    {
        
    }
}
