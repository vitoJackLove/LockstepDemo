/// <summary>
/// 标记Editor模式下使用的变量
/// </summary>
public class EditorVariableAttribute : System.Attribute
{
    public string VariableName;

    public EditorVariableAttribute(string variableName)
    {
        this.VariableName = variableName;
    }

    public EditorVariableAttribute()
    {
        
    }
}