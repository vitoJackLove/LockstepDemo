using System;

/// <summary>
/// 节点变量
/// </summary>
[Serializable]
public class ClipVariableBool : BaseClipVariable
{
    public bool boolValue = true;
    
    public override Type SVariableType()
    {
        return typeof(bool);
    }

    public override object GetVariableValue()
    {
        return boolValue;
    }

    public override void SetVariableValue(object obj)
    {
        this.boolValue = (bool)obj;
    }
}
