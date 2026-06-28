using System.Collections.Generic;
using Unity.Mathematics.FixedPoint;

/// <summary>
/// 战斗实体数据
/// </summary>
public abstract class BattleEntityData : ModelBase ,IPool
{
    /// <summary>
    /// 实体属性
    /// </summary>
    protected Dictionary<string, PropertyData> EntityPropertyDataDic =
        new Dictionary<string, PropertyData>();

    /// <summary>
    /// 初始化属性
    /// </summary>
    public abstract void InitProperty();
    
    public Dictionary<string, PropertyData> EntityPropertyData => EntityPropertyDataDic;

    public void ChangeProperty(PropertyKey key, fp value)
    {
        if (EntityPropertyDataDic.TryGetValue(key.ToString(), out var propertyData))
        {
            propertyData.ChangeProperty(value);
        }
    }

    /// <summary>
    /// 按属性值类型修改实体属性，并输出实际生效的变化量。
    /// </summary>
    /// <param name="key">需要修改的属性键。</param>
    /// <param name="valueType">需要修改的属性值类型，支持 Current、Max 和 Min。</param>
    /// <param name="value">本次尝试追加的变化量，可为正数或负数。</param>
    /// <param name="appliedValue">实际生效的变化量，用于临时属性回滚。</param>
    /// <returns>属性存在且值类型有效时返回 true，否则返回 false。</returns>
    public bool TryChangePropertyValue(PropertyKey key, PropertyValueType valueType, fp value, out fp appliedValue)
    {
        appliedValue = 0;

        if (EntityPropertyDataDic.TryGetValue(key.ToString(), out var propertyData))
        {
            return propertyData.TryChangePropertyValue(valueType, value, out appliedValue);
        }

        return false;
    }
    
    public void SetProperty(PropertyKey key, fp value)
    {
        if (EntityPropertyDataDic.TryGetValue(key.ToString(), out var propertyData))
        {
             propertyData.SetProperty(value);
        }
    }

    public fp GetProperty(PropertyKey key)
    {
        if (EntityPropertyDataDic.TryGetValue(key.ToString(), out var propertyData))
        {
            return propertyData.GetProperty();
        }

        return 0;
    }

    public fp GetProperty(string key)
    {
        if (EntityPropertyDataDic.TryGetValue(key, out var propertyData))
        {
            return propertyData.GetProperty();
        }

        return 0;
    }
    
    public void Clear() { }
}
