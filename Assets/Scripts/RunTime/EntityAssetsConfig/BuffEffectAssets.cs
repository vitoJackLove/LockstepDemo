using System;

/// <summary>
/// Buff 效果配置，用于描述执行模式、目标和属性数值变化。
/// </summary>
[Serializable]
public class BuffEffectAssets
{
    /// <summary>
    /// Buff 效果执行模式，旧资源缺省时按 Instant 处理。
    /// </summary>
    public BuffEffectMode effectMode;

    /// <summary>
    /// Buff 执行的目标集合。
    /// </summary>
    public BuffExecuteTarget buffExecuteTarget;

    /// <summary>
    /// 需要被修改的属性键。
    /// </summary>
    public PropertyKey executeProperty;

    /// <summary>
    /// 是否直接使用 normalValue 作为修改值；为 false 时根据 influenceProperty 和 rate 计算。
    /// </summary>
    public bool isCommonValue;

    /// <summary>
    /// 直接属性修改值，在 isCommonValue 为 true 时生效。
    /// </summary>
    public int normalValue;

    /// <summary>
    /// 影响属性修改值的来源属性，在 isCommonValue 为 false 时生效。
    /// </summary>
    public PropertyKey influenceProperty;

    /// <summary>
    /// influenceProperty 对最终修改值的倍率。
    /// </summary>
    public int rate;
}
