using System;
using System.Collections.Generic;
using Unity.Mathematics.FixedPoint;

#if UNITY_EDITOR
/// <summary>
/// 实体属性调试快照，仅供 Unity 编辑器金手指安全读取属性三元值。
/// </summary>
public readonly struct EntityPropertyDebugInfo
{
    /// <summary>
    /// 创建一个编辑器属性调试快照。
    /// </summary>
    /// <param name="key">属性枚举键。</param>
    /// <param name="currentValue">属性当前值。</param>
    /// <param name="minValue">属性最小值。</param>
    /// <param name="maxValue">属性最大值。</param>
    public EntityPropertyDebugInfo(PropertyKey key, fp currentValue, fp minValue, fp maxValue)
    {
        Key = key;
        CurrentValue = currentValue;
        MinValue = minValue;
        MaxValue = maxValue;
    }

    /// <summary>
    /// 属性枚举键。
    /// </summary>
    public PropertyKey Key { get; }

    /// <summary>
    /// 属性当前值。
    /// </summary>
    public fp CurrentValue { get; }

    /// <summary>
    /// 属性最小值。
    /// </summary>
    public fp MinValue { get; }

    /// <summary>
    /// 属性最大值。
    /// </summary>
    public fp MaxValue { get; }
}
#endif

/// <summary>
/// 实体属性接口
/// </summary>
public abstract partial class BaseEntity
{
    /// <summary>
    /// 实体属性
    /// </summary>
    protected abstract BattleEntityData EntityPropertyData { get;}

    /// <summary>
    /// 修改实体属性的当前值，内部会按属性最小值和最大值进行夹取。
    /// </summary>
    /// <param name="key">需要修改的属性键。</param>
    /// <param name="value">追加到当前值上的变化量，可为正数或负数。</param>
    public void ChangeProperty(PropertyKey key, fp value)
    {
        TryChangeProperty(key, value);
    }

    /// <summary>
    /// 设置实体属性的当前值，内部会按属性最小值和最大值进行夹取。
    /// </summary>
    /// <param name="key">需要设置的属性键。</param>
    /// <param name="value">准备写入的当前值。</param>
    public void SetProperty(PropertyKey key, fp value)
    {
        TrySetProperty(key, value);
    }

    /// <summary>
    /// 读取实体属性的当前值，属性不存在时返回 0。
    /// </summary>
    /// <param name="key">需要读取的属性键。</param>
    /// <returns>属性当前值；属性不存在时返回 0。</returns>
    public fp GetProperty(PropertyKey key)
    {
        TryGetPropertyValue(key, PropertyValueType.Current, out fp value);

        return value;
    }

    /// <summary>
    /// 尝试修改实体属性的当前值。
    /// </summary>
    /// <param name="key">需要修改的属性键。</param>
    /// <param name="value">追加到当前值上的变化量，可为正数或负数。</param>
    /// <returns>属性存在并完成修改时返回 true，否则返回 false。</returns>
    public bool TryChangeProperty(PropertyKey key, fp value)
    {
        if (!TryGetPropertyData(key, out PropertyData propertyData))
        {
            return false;
        }

        propertyData.ChangeProperty(value);

        return true;
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

        if (!TryGetPropertyData(key, out PropertyData propertyData))
        {
            return false;
        }

        return propertyData.TryChangePropertyValue(valueType, value, out appliedValue);
    }

    /// <summary>
    /// 尝试设置实体属性的当前值。
    /// </summary>
    /// <param name="key">需要设置的属性键。</param>
    /// <param name="value">准备写入的当前值。</param>
    /// <returns>属性存在并完成设置时返回 true，否则返回 false。</returns>
    public bool TrySetProperty(PropertyKey key, fp value)
    {
        if (!TryGetPropertyData(key, out PropertyData propertyData))
        {
            return false;
        }

        propertyData.SetProperty(value);

        return true;
    }

    /// <summary>
    /// 尝试读取实体属性指定值类型的数值。
    /// </summary>
    /// <param name="key">需要读取的属性键。</param>
    /// <param name="valueType">需要读取的属性值类型，支持 Current、Max 和 Min。</param>
    /// <param name="value">读取到的属性值；读取失败时为 0。</param>
    /// <returns>属性存在且值类型有效时返回 true，否则返回 false。</returns>
    public bool TryGetPropertyValue(PropertyKey key, PropertyValueType valueType, out fp value)
    {
        value = 0;

        if (!TryGetPropertyData(key, out PropertyData propertyData))
        {
            return false;
        }

        switch (valueType)
        {
            case PropertyValueType.Current:
                value = propertyData.CurrentValue;
                return true;
            case PropertyValueType.Max:
                value = propertyData.MaxValue;
                return true;
            case PropertyValueType.Min:
                value = propertyData.MinValue;
                return true;
            default:
                return false;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 枚举实体当前实际拥有的属性快照，仅供编辑器调试界面展示。
    /// </summary>
    /// <returns>实体属性调试快照集合；无属性数据时返回空集合。</returns>
    public IReadOnlyList<EntityPropertyDebugInfo> GetPropertyDebugInfos()
    {
        if (EntityPropertyData == null || EntityPropertyData.EntityPropertyData == null)
        {
            return Array.Empty<EntityPropertyDebugInfo>();
        }

        List<EntityPropertyDebugInfo> propertyDebugInfos = new List<EntityPropertyDebugInfo>();

        foreach (KeyValuePair<string, PropertyData> propertyPair in EntityPropertyData.EntityPropertyData)
        {
            if (propertyPair.Value == null ||
                !Enum.TryParse(propertyPair.Key, out PropertyKey propertyKey) ||
                propertyKey == PropertyKey.Null)
            {
                continue;
            }

            propertyDebugInfos.Add(new EntityPropertyDebugInfo(
                propertyKey,
                propertyPair.Value.CurrentValue,
                propertyPair.Value.MinValue,
                propertyPair.Value.MaxValue));
        }

        return propertyDebugInfos;
    }
#endif

    /// <summary>
    /// 尝试从实体属性容器中取得指定属性数据对象。
    /// </summary>
    /// <param name="key">需要查找的属性键。</param>
    /// <param name="propertyData">查找到的属性数据对象；查找失败时为 null。</param>
    /// <returns>属性键有效且属性数据存在时返回 true，否则返回 false。</returns>
    private bool TryGetPropertyData(PropertyKey key, out PropertyData propertyData)
    {
        propertyData = null;

        if (key == PropertyKey.Null || EntityPropertyData == null)
        {
            return false;
        }

        return EntityPropertyData.EntityPropertyData.TryGetValue(key.ToString(), out propertyData) &&
               propertyData != null;
    }
}
