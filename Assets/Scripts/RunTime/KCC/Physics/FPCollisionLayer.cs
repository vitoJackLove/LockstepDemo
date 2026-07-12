using System;

/// <summary>
/// KCC 定点数碰撞层。可在编辑器批量配置碰撞矩阵。
/// </summary>
[Flags]
public enum FPCollisionLayer : int
{
    /// <summary>无层。</summary>
    None = 0,

    /// <summary>默认层。</summary>
    Default = 1 << 0,

    /// <summary>墙体层。</summary>
    Wall = 1 << 1,

    /// <summary>英雄/怪物碰撞层；MoveComponent.ResolveCharacterLayer 按 EntityType 赋值。</summary>
    Hero = 1 << 2,

    /// <summary>怪物碰撞层。</summary>
    Monster = 1 << 3,

    /// <summary>运动学平台层。</summary>
    KinematicPlatform = 1 << 4,

    /// <summary>动态刚体层。</summary>
    DynamicBody = 1 << 5,

    /// <summary>触发器层。</summary>
    Trigger = 1 << 6,

    /// <summary>所有层。</summary>
    All = ~0,
}

/// <summary>
/// 层掩码。用于物理查询时过滤碰撞层。
/// </summary>
public struct FPLayerMask
{
    /// <summary>掩码位值。</summary>
    public int Value;

    /// <summary>
    /// 构造层掩码。
    /// </summary>
    /// <param name="value">掩码位值。</param>
    public FPLayerMask(int value)
    {
        Value = value;
    }

    /// <summary>从碰撞层枚举隐式转换。</summary>
    /// <param name="layer">碰撞层。</param>
    public static implicit operator FPLayerMask(FPCollisionLayer layer) => new FPLayerMask((int)layer);

    /// <summary>从整型隐式转换。</summary>
    /// <param name="value">掩码位值。</param>
    public static implicit operator FPLayerMask(int value) => new FPLayerMask(value);

    /// <summary>
    /// 判断掩码是否包含指定层。
    /// </summary>
    /// <param name="layer">待检测的碰撞层。</param>
    /// <returns>若包含则返回 true。</returns>
    public bool Contains(FPCollisionLayer layer)
    {
        int bit = (int)layer;
        return (Value & bit) == bit;
    }

    /// <summary>按位或合并两个层掩码。</summary>
    /// <param name="a">掩码 A。</param>
    /// <param name="b">掩码 B。</param>
    /// <returns>合并后的掩码。</returns>
    public static FPLayerMask operator |(FPLayerMask a, FPLayerMask b) => new FPLayerMask(a.Value | b.Value);

    /// <summary>按位与求两个层掩码的交集。</summary>
    /// <param name="a">掩码 A。</param>
    /// <param name="b">掩码 B。</param>
    /// <returns>交集掩码。</returns>
    public static FPLayerMask operator &(FPLayerMask a, FPLayerMask b) => new FPLayerMask(a.Value & b.Value);
}
