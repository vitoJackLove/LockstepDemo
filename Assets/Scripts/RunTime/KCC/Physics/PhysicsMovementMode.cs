/// <summary>
/// 实体物理移动范式，与 Unity CharacterController / Rigidbody 对齐。
/// </summary>
public enum PhysicsMovementMode
{
    /// <summary>Motor 内置单胶囊（对齐 Unity Character Controller）。</summary>
    CharacterController = 0,

    /// <summary>PhysicsBodyConfig + 复合碰撞体（对齐 Unity Rigidbody）。</summary>
    Rigidbody = 1,
}

/// <summary>
/// 刚体类型，定义在实体级 PhysicsBodyConfig。
/// </summary>
public enum PhysicsBodyType
{
    /// <summary>固定位姿，无 Rigidbody 模拟。</summary>
    Static = 0,

    /// <summary>Transform 驱动，可推 Dynamic，不被 Dynamic 推。</summary>
    Kinematic = 1,

    /// <summary>物理模拟，可被 Kinematic 推。</summary>
    Dynamic = 2,
}

/// <summary>
/// 复合碰撞体形状类型。
/// </summary>
public enum PhysicsShapeType
{
    Box = 0,
    Sphere = 1,
    Capsule = 2,
}
