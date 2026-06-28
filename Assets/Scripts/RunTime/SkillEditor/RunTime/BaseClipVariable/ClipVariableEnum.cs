using System;
using UnityEngine;

/// <summary>
/// 节点变量
/// </summary>
[Serializable]
public class ClipVariableEnum<T> : BaseClipVariable
{
    public int intValue = 0;

    public string enumType;

    public ClipVariableEnum()
    {
        enumType = typeof(T).FullName;
    }
    
    /// <summary>
    /// 具体枚举类型
    /// </summary>
    private Type _enumType;
    
    public override Type SVariableType()
    {
        if (_enumType == null)
        {
            _enumType = Type.GetType(enumType);

            if (_enumType == null)
            {
                System.Reflection.Assembly assembly = System.Reflection.Assembly.Load("Unity.Model.Codes");

                _enumType = assembly.GetType(enumType);
            }

        }

        return _enumType;
    }

    public override object GetVariableValue()
    {
        if (SVariableType() == null)
        {
            Debug.LogError($"技能编辑器找不到对应的类型：{enumType}");
            
            return null;
        }
        
        Array array = Enum.GetValues(SVariableType());

        return array.GetValue(intValue);
    }

    public override void SetVariableValue(object obj)
    {
        if (obj == null)
        {
            return;
        }
        
        this.intValue = (int)obj;
    }
}
