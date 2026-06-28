using System;

/// <summary>
/// 节点变量String
/// </summary>
[Serializable]
public class ClipVariableString : BaseClipVariable
{
    public string stringValue;
    
    public override Type SVariableType()
    {
        return typeof(string);
    }

    public override object GetVariableValue()
    {
        return stringValue;
    }

    public override void SetVariableValue(object obj)
    {
        this.stringValue = (string)obj;
    }
}
