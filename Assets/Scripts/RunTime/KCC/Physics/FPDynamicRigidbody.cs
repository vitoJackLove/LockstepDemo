using Unity.Mathematics.FixedPoint;

/// <summary>
/// 定点数刚体包装，替代 Unity Rigidbody 供 KCC 与 <see cref="PhysicsBodyComponent"/> 使用。
/// <para>
/// 不依赖 Unity 物理引擎；位姿由 ECS Transform 或本类 <see cref="SetPose"/> 驱动。
/// Kinematic 体随实体每帧同步；Dynamic 体可接收 <see cref="ApplyImpulse"/>（完整积分待 solver 接入）。
/// </para>
/// </summary>
public class FPDynamicRigidbody
{
    /// <summary>
    /// 刚体在 <see cref="FPCollisionWorld"/> 中的唯一标识，由 <see cref="FPCollisionWorld.AllocateBodyId"/> 分配。
    /// </summary>
    public int Id { get; internal set; }

    /// <summary>
    /// 质量（千克等价逻辑单位）。仅 Dynamic 且非 Kinematic 时参与冲量计算；默认 1。
    /// </summary>
    public fp Mass = (fp)1;

    /// <summary>
    /// 世界空间线速度。Kinematic 体通常由 Transform 差分间接体现，本字段供推挤与继承查询。
    /// </summary>
    public fp3 Velocity;

    /// <summary>
    /// 世界空间角速度（弧度/秒逻辑单位）。Dynamic 模拟或平台继承时使用。
    /// </summary>
    public fp3 AngularVelocity;

    /// <summary>
    /// 是否为运动学刚体。为 true 时 <see cref="ApplyImpulse"/> 无效，位姿由外部 Transform 驱动。
    /// 怪物身体典型为 Kinematic。
    /// </summary>
    public bool IsKinematic;

    /// <summary>是否受重力影响（由 <see cref="PhysicsBodyComponent"/> 读取配置并积分）。</summary>
    public bool UseGravity = true;

    /// <summary>世界 Y 轴重力加速度；未配置时使用 <see cref="FPMathKCC.DefaultGravity"/>。</summary>
    public fp GravityAcceleration = FPMathKCC.DefaultGravity;

    /// <summary>当前世界位置，与绑定实体 <c>FTransform.Position</c> 由 <see cref="PhysicsBodyComponent"/> 同步。</summary>
    public fp3 Position { get; internal set; }

    /// <summary>当前世界旋转，与绑定实体 <c>FTransform.Rotation</c> 同步。</summary>
    public fpquaternion Rotation { get; internal set; }

    /// <summary>本逻辑 tick 开始时的位置快照，供插值或帧同步回滚比对（内部使用）。</summary>
    internal fp3 InitialTickPosition;

    /// <summary>本逻辑 tick 开始时的旋转快照，供插值或帧同步回滚比对（内部使用）。</summary>
    internal fpquaternion InitialTickRotation;

    /// <summary>
    /// 设置刚体世界位姿。注册时与每帧 <see cref="PhysicsBodyComponent.SyncDynamicBodyPose"/> 调用。
    /// </summary>
    /// <param name="position">世界位置。</param>
    /// <param name="rotation">世界旋转。</param>
    public void SetPose(fp3 position, fpquaternion rotation)
    {
        Position = position;
        Rotation = rotation;
    }

    /// <summary>
    /// 对 Dynamic 刚体施加瞬时冲量，按 <see cref="Mass"/> 累加到 <see cref="Velocity"/>。
    /// Kinematic、质量为零或负时不生效。
    /// </summary>
    /// <param name="impulse">世界空间冲量向量。</param>
    public void ApplyImpulse(fp3 impulse)
    {
        if (IsKinematic || Mass <= (fp)0)
        {
            return;
        }

        Velocity += impulse / Mass;
    }
}
