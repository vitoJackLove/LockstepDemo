using System;

/// <summary>
/// 节点变量Int
/// </summary>
[Serializable]
public class ClipVariableInt : BaseClipVariable
{
    public int intValue = 0;
    
    public override Type SVariableType()
    {
        return typeof(int);
    }

    public override object GetVariableValue()
    {
        return intValue;
    }

    public override void SetVariableValue(object obj)
    {
        this.intValue = (int)obj;
    }
}
