using System;

/// <summary>
/// Buff 条件配置，用于描述触发角色、目标属性和比较规则。
/// </summary>
[Serializable]
public class BuffConditionAssets
{
    /// <summary>
    /// Buff 持有者在触发事件中的角色要求，旧资源缺省时按 Any 处理。
    /// </summary>
    public BuffTriggerRole triggerRole;

    /// <summary>
    /// 条件检查读取属性的目标集合。
    /// </summary>
    public BuffExecuteTarget buffConditionTarget;

    /// <summary>
    /// 条件检查读取的属性键，Null 表示不检查属性条件。
    /// </summary>
    public PropertyKey propertyKey;

    /// <summary>
    /// 条件检查读取当前值、最大值或最小值。
    /// </summary>
    public PropertyValueType propertyValueType;

    /// <summary>
    /// 属性值与配置值之间使用的比较方式。
    /// </summary>
    public CompareMethod compareMethod;

    /// <summary>
    /// 条件比较使用的整数配置值，运行时会转换为定点数。
    /// </summary>
    public int value;
}
