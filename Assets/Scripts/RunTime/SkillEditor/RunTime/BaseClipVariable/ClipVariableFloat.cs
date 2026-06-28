using System;

/// <summary>
/// 节点变量Int
/// </summary>
[Serializable]
public class ClipVariableFloat : BaseClipVariable
{
    public float floatValue = 0;
    
    public override Type SVariableType()
    {
        return typeof(float);
    }

    public override object GetVariableValue()
    {
        return floatValue;
    }

    public override void SetVariableValue(object obj)
    {
        this.floatValue = (int)obj;
    }
}
