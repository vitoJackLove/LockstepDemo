using Unity.Mathematics.FixedPoint;

/// <summary>
/// 空实现角色控制器，供本项目 ECS 位移驱动架构使用。
/// <para>
/// 逻辑位移由 <see cref="MoveComponent"/> 在命令帧计算后写入
/// <see cref="FPKinematicCharacterMotor.SetMovePositionTarget"/> /
/// <see cref="FPKinematicCharacterMotor.SetMoveRotationTarget"/>；
/// KCC 系统在帧末做碰撞 sweep 并回写 <c>Entity.FTransform</c>。
/// 本类不参与速度积分与旋转策略，仅满足 <see cref="IFPCharacterController"/> 回调契约。
/// </para>
/// </summary>
public sealed class FPNullCharacterController : IFPCharacterController
{
    /// <summary>
    /// 不修改旋转；朝向由 <see cref="MoveComponent"/> 通过 Motor 旋转目标驱动。
    /// </summary>
    /// <param name="currentRotation">Motor 当前旋转，本实现不写入。</param>
    /// <param name="deltaTime">逻辑帧间隔。</param>
    public void UpdateRotation(ref fpquaternion currentRotation, fp deltaTime) { }

    /// <summary>
    /// 将速度清零，避免 Motor 内部速度积分与外部目标位姿驱动冲突。
    /// </summary>
    /// <param name="currentVelocity">Motor 当前速度，被设为 <c>fp3.zero</c>。</param>
    /// <param name="deltaTime">逻辑帧间隔。</param>
    public void UpdateVelocity(ref fp3 currentVelocity, fp deltaTime)
    {
        currentVelocity = fp3.zero;
    }

    /// <summary>Phase1 开始前无预处理。</summary>
    /// <param name="deltaTime">逻辑帧间隔。</param>
    public void BeforeCharacterUpdate(fp deltaTime) { }

    /// <summary>地面探测完成后无额外逻辑。</summary>
    /// <param name="deltaTime">逻辑帧间隔。</param>
    public void PostGroundingUpdate(fp deltaTime) { }

    /// <summary>Phase2 移动解算结束后无后处理。</summary>
    /// <param name="deltaTime">逻辑帧间隔。</param>
    public void AfterCharacterUpdate(fp deltaTime) { }

    /// <summary>默认所有碰撞体均参与阻挡，不做阵营/层自定义过滤。</summary>
    /// <param name="coll">待检测碰撞体。</param>
    /// <returns>恒为 true。</returns>
    public bool IsColliderValidForCollisions(IFPCollider coll) => true;

    /// <summary>着地回调空实现；可在子类中扩展音效或 Buff。</summary>
    public void OnGroundHit(IFPCollider hitCollider, fp3 hitNormal, fp3 hitPoint, ref FPHitStabilityReport hitStabilityReport) { }

    /// <summary>移动碰撞回调空实现。</summary>
    public void OnMovementHit(IFPCollider hitCollider, fp3 hitNormal, fp3 hitPoint, ref FPHitStabilityReport hitStabilityReport) { }

    /// <summary>稳定性报告处理空实现，使用 Motor 默认台阶/ledge 判定。</summary>
    public void ProcessHitStabilityReport(IFPCollider hitCollider, fp3 hitNormal, fp3 hitPoint, fp3 atCharacterPosition, fpquaternion atCharacterRotation, ref FPHitStabilityReport hitStabilityReport) { }

    /// <summary>离散重叠检测回调空实现。</summary>
    /// <param name="hitCollider">重叠的碰撞体。</param>
    public void OnDiscreteCollisionDetected(IFPCollider hitCollider) { }
}
