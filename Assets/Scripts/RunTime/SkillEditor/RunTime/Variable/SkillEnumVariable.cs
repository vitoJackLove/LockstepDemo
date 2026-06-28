using System;
using UnityEngine;

[Serializable] 
public class SkillEnumVariable : SkillBlackVariable
{
    /// <summary>
    /// 变量Value
    /// </summary>
    public int variableValue;

    /// <summary>
    /// 具体枚举类型
    /// </summary>
    private Type _enumType;
    
    public override Type VariableType()
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

    /// <summary>
    /// 枚举类型
    /// </summary>
    public string enumType;

    public void SetEnumType(string type)
    {
        enumType = type;
    }

    public override string InitVariableKey => "EnumVariable";

    public override object GetValue => GetEnumValue();
    
    public override void SetValue(object obj)
    {
        if (obj == null)
        {
            return;
        }
        
        variableValue = (int)obj;
    }

    public object GetEnumValue()
    {
        if (VariableType() == null)
        {
            Debug.LogError($"技能编辑器找不到对应的类型：{enumType}");
            return null;
        }

        Array array = Enum.GetValues(VariableType());

        return array.GetValue(variableValue);
    }
}
