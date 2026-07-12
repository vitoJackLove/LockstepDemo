using System;

/// <summary>
/// 组件数据
/// </summary>
public static class ComponentDataKey
{
    /// <summary>
    /// 位移数据
    /// </summary>
    public const string MovementData = "MovementData";

    /// <summary>
    /// 攻击次数
    /// </summary>
    public const string AttackNumber = "AttackNumber";

    /// <summary>
    /// 死亡道具
    /// </summary>
    public const string LifeTime = "LifeTime";

    /// <summary>
    /// 受击数据
    /// </summary>
    public const string ColliderData = "ColliderData";

    /// <summary>
    /// 行为树ID
    /// </summary>
    public const string BehaviourTreeConfig = "BehaviourTreeConfig";

    /// <summary>
    /// 状态数据
    /// </summary>
    public const string StateData = "StateData";
    
    /// <summary>
    /// 动画混合树数据
    /// </summary>
    public const string BlendTreeData = "BlendTreeData";

    /// <summary>
    /// KCC 注册配置（CharacterMotor / StaticBox / KinematicPlatform）
    /// </summary>
    [Obsolete("Use PhysicsBodyData")]
    public const string KccBodyData = "KccBodyData";

    /// <summary>
    /// 物理体注册配置（movementMode + CC/Body 设置）。
    /// </summary>
    public const string PhysicsBodyData = "PhysicsBodyData";
}
