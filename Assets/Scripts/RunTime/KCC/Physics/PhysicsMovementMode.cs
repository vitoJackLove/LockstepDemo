/// <summary>
/// 实体物理移动范式，与 Unity CharacterController / Rigidbody 组件模型对齐。
/// <para>
/// 在 <see cref="HeroAssetsConfig"/> / <see cref="MonsterAssetsConfig"/> 上配置，
/// 由 <see cref="PhysicsBodyComponent"/> 在实体启动时择一分支注册，禁止同一实体同时使用 Motor 胶囊与复合刚体身体。
/// </para>
/// </summary>
public enum PhysicsMovementMode
{
    /// <summary>
    /// Motor 内置单胶囊（对齐 Unity Character Controller）。
    /// 注册 <see cref="FPKinematicCharacterMotor"/>，由 <see cref="MoveComponent"/> 写入目标位姿，
    /// <see cref="FPKinematicCharacterSystem"/> 负责 sweep 与碰撞校正。典型：玩家英雄。
    /// </summary>
    CharacterController = 0,

    /// <summary>
    /// PhysicsBodyConfig + 复合碰撞体（对齐 Unity Rigidbody + Collider 组合）。
    /// 按 <see cref="PhysicsBodyConfig"/> 注册多个 <see cref="IFPCollider"/>，Transform 驱动位姿同步。
    /// 典型：怪物（Kinematic 盒）、地图 Cube（Static 墙）。
    /// </summary>
    Rigidbody = 1,
}

/// <summary>
/// 刚体类型，定义在实体级 <see cref="PhysicsBodyConfig.bodyType"/>，而非单个 Collider 上。
/// <para>
/// 决定 <see cref="PhysicsBodyComponent"/> 是否创建 <see cref="FPDynamicRigidbody"/> 以及碰撞体与刚体的绑定关系。
/// </para>
/// </summary>
public enum PhysicsBodyType
{
    /// <summary>
    /// 固定位姿：不创建 <see cref="FPDynamicRigidbody"/>，仅将碰撞体注册到世界。
    /// 位姿由实体 Transform 或地图逻辑直接设置，用于墙体、地板等静态阻挡。
    /// </summary>
    Static = 0,

    /// <summary>
    /// 运动学刚体：Transform 每帧驱动，碰撞体跟随实体移动。
    /// 可阻挡 Dynamic 物体，自身不被 Dynamic 推动；怪物寻路直写 Transform 时使用此类型。
    /// </summary>
    Kinematic = 1,

    /// <summary>
    /// 动态刚体：受重力与速度积分；可与环境碰撞体发生推挤（solver 持续完善中）。
    /// </summary>
    Dynamic = 2,
}

/// <summary>
/// 复合碰撞体 Authoring 形状类型，对应 <see cref="PhysicsColliderSetting.shape"/>。
/// <para>
/// 与运行时 <see cref="FPShapeType"/> 通过具体 Collider 实现类桥接（如 Box → <see cref="FPBoxCollider"/>）。
/// </para>
/// </summary>
public enum PhysicsShapeType
{
    /// <summary>轴对齐盒体（经实体旋转后为 OBB），使用 <see cref="PhysicsColliderSetting.halfExtents"/>。</summary>
    Box = 0,

    /// <summary>球体，使用 <see cref="PhysicsColliderSetting.radius"/>；各向缩放取最大轴影响半径。</summary>
    Sphere = 1,

    /// <summary>胶囊体，使用 <see cref="PhysicsColliderSetting.capsuleRadius"/> 与 <see cref="PhysicsColliderSetting.capsuleHeight"/>。</summary>
    Capsule = 2,
}
