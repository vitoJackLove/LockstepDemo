
/// <summary>
/// 变量名字
/// </summary>
public class VariableNameAttribute : System.Attribute
{
    public string VariableName;

    public VariableNameAttribute(string variableName)
    {
        this.VariableName = variableName;
    }
}
