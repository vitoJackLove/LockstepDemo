using Unity.Mathematics.FixedPoint;

/// <summary>
/// 支持可配置重力的 ECS 角色控制器。
/// <para>
/// 水平位移仍由 <see cref="MoveComponent"/> 写入 Motor 目标位姿（Phase1）；
/// 本类在 UpdatePhase2 的 <see cref="UpdateVelocity"/> 中对 Y 轴积分重力，并通过 Motor sweep 贴地。
/// <see cref="useGravity"/> 为 false 时行为与 <see cref="FPNullCharacterController"/> 一致。
/// </para>
/// </summary>
public sealed class FPGravityCharacterController : IFPCharacterController
{
    private FPKinematicCharacterMotor _motor;
    private bool _useGravity;
    private fp _gravityAcceleration;

    /// <summary>配置重力开关与加速度。</summary>
    public void Configure(bool useGravity, fp gravityAcceleration)
    {
        _useGravity = useGravity;
        _gravityAcceleration = gravityAcceleration;
    }

    /// <summary>绑定 Motor，用于读取接地状态。</summary>
    public void BindMotor(FPKinematicCharacterMotor motor)
    {
        _motor = motor;
    }

    public void UpdateRotation(ref fpquaternion currentRotation, fp deltaTime) { }

    public void UpdateVelocity(ref fp3 currentVelocity, fp deltaTime)
    {
        if (!_useGravity ||
            _gravityAcceleration == (fp)0 ||
            (_motor != null && _motor.CollisionInfluence != null && !_motor.CollisionInfluence.positionY))
        {
            currentVelocity = fp3.zero;
            return;
        }

        bool isStableOnGround = _motor != null && _motor.GroundingStatus.IsStableOnGround;
        fp verticalVelocity = currentVelocity.y;

        if (!isStableOnGround)
        {
            verticalVelocity += _gravityAcceleration * deltaTime;
        }
        else if (verticalVelocity < (fp)0)
        {
            verticalVelocity = (fp)0;
        }

        // 水平位移由 MoveComponent 在 Phase1 写入目标位姿，速度仅保留竖直分量。
        currentVelocity = new fp3((fp)0, verticalVelocity, (fp)0);
    }

    public void BeforeCharacterUpdate(fp deltaTime) { }

    public void PostGroundingUpdate(fp deltaTime) { }

    public void AfterCharacterUpdate(fp deltaTime) { }

    public bool IsColliderValidForCollisions(IFPCollider coll) => true;

    public void OnGroundHit(IFPCollider hitCollider, fp3 hitNormal, fp3 hitPoint, ref FPHitStabilityReport hitStabilityReport) { }

    public void OnMovementHit(IFPCollider hitCollider, fp3 hitNormal, fp3 hitPoint, ref FPHitStabilityReport hitStabilityReport) { }

    public void ProcessHitStabilityReport(IFPCollider hitCollider, fp3 hitNormal, fp3 hitPoint, fp3 atCharacterPosition, fpquaternion atCharacterRotation, ref FPHitStabilityReport hitStabilityReport) { }

    public void OnDiscreteCollisionDetected(IFPCollider hitCollider) { }
}
