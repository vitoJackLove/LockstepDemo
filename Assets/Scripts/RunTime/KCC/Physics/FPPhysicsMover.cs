using Unity.Mathematics.FixedPoint;

/// <summary>
/// 运动学平台控制器接口。由平台在每帧提供目标位姿。
/// </summary>
public interface IFPPhysicsMoverController
{
    /// <summary>
    /// 更新平台目标位姿。
    /// </summary>
    /// <param name="goalPosition">输出目标位置。</param>
    /// <param name="goalRotation">输出目标旋转。</param>
    /// <param name="deltaTime">帧间隔时间。</param>
    void UpdateMovement(out fp3 goalPosition, out fpquaternion goalRotation, fp deltaTime);
}

/// <summary>
/// 运动学平台，不依赖 Unity Rigidbody，由 <see cref="IFPPhysicsMoverController"/> 提供目标位姿。
/// <para>
/// 在 <see cref="FPKinematicCharacterSystem"/> 本地更新阶段调用 <see cref="VelocityUpdate"/> 推导线/角速度，
/// 角色 Motor 在 Phase1 可继承平台运动，实现电梯、移动地板等确定性行为。
/// </para>
/// </summary>
public class FPPhysicsMover
{
    /// <summary>平台唯一标识。</summary>
    public int Id { get; internal set; }

    /// <summary>
    /// 平台运动控制器，每帧由 <see cref="VelocityUpdate"/> 调用以获取目标位姿。
    /// 典型实现：<see cref="FPEntityPhysicsMoverController"/>（读取 ECS 实体 Transform）。
    /// 为 null 时平台速度归零，不参与角色继承运动。
    /// </summary>
    public IFPPhysicsMoverController Controller;

    /// <summary>当前瞬态位置。</summary>
    public fp3 TransientPosition { get; private set; }

    /// <summary>当前瞬态旋转。</summary>
    public fpquaternion TransientRotation { get; private set; }

    /// <summary>线速度。</summary>
    public fp3 Velocity { get; private set; }

    /// <summary>角速度。</summary>
    public fp3 AngularVelocity { get; private set; }

    /// <summary>本 tick 初始位置。</summary>
    public fp3 InitialTickPosition { get; internal set; }

    /// <summary>本 tick 初始旋转。</summary>
    public fpquaternion InitialTickRotation { get; internal set; }

    /// <summary>插值产生的位置增量。</summary>
    public fp3 PositionDeltaFromInterpolation { get; internal set; }

    /// <summary>插值产生的旋转增量。</summary>
    public fpquaternion RotationDeltaFromInterpolation { get; internal set; }

    /// <summary>本帧模拟开始时的位置。</summary>
    private fp3 _initialSimulationPosition;

    /// <summary>本帧模拟开始时的旋转。</summary>
    private fpquaternion _initialSimulationRotation;

    /// <summary>
    /// 设置平台位姿并重置模拟初始状态。
    /// </summary>
    /// <param name="position">世界位置。</param>
    /// <param name="rotation">世界旋转。</param>
    public void SetPose(fp3 position, fpquaternion rotation)
    {
        TransientPosition = position;
        TransientRotation = rotation;
        _initialSimulationPosition = position;
        _initialSimulationRotation = rotation;
    }

    /// <summary>
    /// 根据控制器目标位姿更新速度与角速度。
    /// </summary>
    /// <param name="deltaTime">帧间隔时间。</param>
    public void VelocityUpdate(fp deltaTime)
    {
        if (Controller == null)
        {
            Velocity = fp3.zero;
            AngularVelocity = fp3.zero;
            return;
        }

        fp3 goalPosition;
        fpquaternion goalRotation;
        Controller.UpdateMovement(out goalPosition, out goalRotation, deltaTime);

        // 由目标位姿反推线速度
        fp3 positionDelta = goalPosition - TransientPosition;
        Velocity = deltaTime > (fp)0 ? positionDelta / deltaTime : fp3.zero;

        // 由旋转增量反推角速度
        fpquaternion rotationDelta = goalRotation * fpquaternionKCCExtensions.Inverse(TransientRotation);
        AngularVelocity = FPMathKCC.QuaternionToAngularVelocity(rotationDelta, deltaTime);

        TransientPosition = goalPosition;
        TransientRotation = goalRotation;
    }

    /// <summary>
    /// 记录本帧模拟结束时的位姿，供后续插值或碰撞计算使用。
    /// </summary>
    public void ApplySimulationPose()
    {
        _initialSimulationPosition = TransientPosition;
        _initialSimulationRotation = TransientRotation;
    }
}
