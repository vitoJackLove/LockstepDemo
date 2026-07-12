using Unity.Mathematics.FixedPoint;

/// <summary>
/// 定点数 KCC 角色控制器接口。
/// 由 <see cref="FPKinematicCharacterMotor"/> 在 UpdatePhase1/Phase2 各阶段回调；
/// 本项目默认实现为 <see cref="FPNullCharacterController"/>，位移由 <c>MoveComponent</c> 写入 Motor 目标位姿，控制器仅清空速度。
/// </summary>
public interface IFPCharacterController
{
    /// <summary>UpdatePhase2 中调用，更新角色旋转（本项目空实现，旋转由 MoveComponent.SetMoveRotationTarget 驱动）。</summary>
    /// <param name="currentRotation">当前旋转，可被修改。</param>
    /// <param name="deltaTime">帧间隔时间。</param>
    void UpdateRotation(ref fpquaternion currentRotation, fp deltaTime);

    /// <summary>UpdatePhase2 中调用，更新 BaseVelocity（本项目空实现并清零，逻辑速度由外部位移目标产生）。</summary>
    /// <param name="currentVelocity">当前速度，可被修改。</param>
    /// <param name="deltaTime">帧间隔时间。</param>
    void UpdateVelocity(ref fp3 currentVelocity, fp deltaTime);

    /// <summary>UpdatePhase1 开头调用，本帧模拟开始前的预处理。</summary>
    /// <param name="deltaTime">帧间隔时间。</param>
    void BeforeCharacterUpdate(fp deltaTime);

    /// <summary>UpdatePhase1 地面探测（ProbeGround）完成后调用。</summary>
    /// <param name="deltaTime">帧间隔时间。</param>
    void PostGroundingUpdate(fp deltaTime);

    /// <summary>UpdatePhase2 末尾调用，本帧移动与碰撞解算结束后的后处理。</summary>
    /// <param name="deltaTime">帧间隔时间。</param>
    void AfterCharacterUpdate(fp deltaTime);

    /// <summary>
    /// 判断指定碰撞体是否应参与与角色的碰撞检测。
    /// </summary>
    /// <param name="coll">待检测的碰撞体。</param>
    /// <returns>若应参与碰撞则返回 true。</returns>
    bool IsColliderValidForCollisions(IFPCollider coll);

    /// <summary>
    /// 角色与地面发生碰撞时的回调。
    /// </summary>
    /// <param name="hitCollider">命中的碰撞体。</param>
    /// <param name="hitNormal">碰撞法线。</param>
    /// <param name="hitPoint">碰撞点。</param>
    /// <param name="hitStabilityReport">命中稳定性报告，可被修改。</param>
    void OnGroundHit(IFPCollider hitCollider, fp3 hitNormal, fp3 hitPoint, ref FPHitStabilityReport hitStabilityReport);

    /// <summary>
    /// 角色移动过程中发生碰撞时的回调。
    /// </summary>
    /// <param name="hitCollider">命中的碰撞体。</param>
    /// <param name="hitNormal">碰撞法线。</param>
    /// <param name="hitPoint">碰撞点。</param>
    /// <param name="hitStabilityReport">命中稳定性报告，可被修改。</param>
    void OnMovementHit(IFPCollider hitCollider, fp3 hitNormal, fp3 hitPoint, ref FPHitStabilityReport hitStabilityReport);

    /// <summary>
    /// 处理命中稳定性报告，可用于自定义台阶、ledge 等判定逻辑。
    /// </summary>
    /// <param name="hitCollider">命中的碰撞体。</param>
    /// <param name="hitNormal">碰撞法线。</param>
    /// <param name="hitPoint">碰撞点。</param>
    /// <param name="atCharacterPosition">碰撞发生时角色位置。</param>
    /// <param name="atCharacterRotation">碰撞发生时角色旋转。</param>
    /// <param name="hitStabilityReport">命中稳定性报告，可被修改。</param>
    void ProcessHitStabilityReport(IFPCollider hitCollider, fp3 hitNormal, fp3 hitPoint, fp3 atCharacterPosition, fpquaternion atCharacterRotation, ref FPHitStabilityReport hitStabilityReport);

    /// <summary>
    /// 检测到离散碰撞（非 sweep 路径）时的回调。
    /// </summary>
    /// <param name="hitCollider">命中的碰撞体。</param>
    void OnDiscreteCollisionDetected(IFPCollider hitCollider);
}

/// <summary>
/// 命中稳定性报告。记录地面/移动碰撞的稳定性、台阶与 ledge 等信息。
/// </summary>
public struct FPHitStabilityReport
{
    /// <summary>是否稳定（可站立）。</summary>
    public bool IsStable;

    /// <summary>是否找到内侧法线。</summary>
    public bool FoundInnerNormal;

    /// <summary>内侧法线方向。</summary>
    public fp3 InnerNormal;

    /// <summary>是否找到外侧法线。</summary>
    public bool FoundOuterNormal;

    /// <summary>外侧法线方向。</summary>
    public fp3 OuterNormal;

    /// <summary>是否检测到有效台阶。</summary>
    public bool ValidStepDetected;

    /// <summary>台阶对应的碰撞体。</summary>
    public IFPCollider SteppedCollider;

    /// <summary>是否检测到 ledge（边缘）。</summary>
    public bool LedgeDetected;

    /// <summary>是否位于 ledge 的空侧。</summary>
    public bool IsOnEmptySideOfLedge;

    /// <summary>到 ledge 的距离。</summary>
    public fp DistanceFromLedge;

    /// <summary>是否朝 ledge 空侧移动。</summary>
    public bool IsMovingTowardsEmptySideOfLedge;

    /// <summary>ledge 处地面法线。</summary>
    public fp3 LedgeGroundNormal;

    /// <summary>ledge 右侧方向。</summary>
    public fp3 LedgeRightDirection;

    /// <summary>ledge 朝向方向。</summary>
    public fp3 LedgeFacingDirection;
}

/// <summary>
/// 角色地面检测报告（对外暴露）。包含地面法线、碰撞体及接触点等信息。
/// </summary>
public struct FPCharacterGroundingReport
{
    /// <summary>是否检测到任意地面。</summary>
    public bool FoundAnyGround;

    /// <summary>是否稳定站立在地面上。</summary>
    public bool IsStableOnGround;

    /// <summary>是否阻止了地面吸附（snapping）。</summary>
    public bool SnappingPrevented;

    /// <summary>地面法线。</summary>
    public fp3 GroundNormal;

    /// <summary>内侧地面法线。</summary>
    public fp3 InnerGroundNormal;

    /// <summary>外侧地面法线。</summary>
    public fp3 OuterGroundNormal;

    /// <summary>地面碰撞体。</summary>
    public IFPCollider GroundCollider;

    /// <summary>地面接触点。</summary>
    public fp3 GroundPoint;

    /// <summary>
    /// 从瞬时地面报告复制数据（不含碰撞体与接触点）。
    /// </summary>
    /// <param name="report">源瞬时报告。</param>
    public void CopyFrom(FPCharacterTransientGroundingReport report)
    {
        FoundAnyGround = report.FoundAnyGround;
        IsStableOnGround = report.IsStableOnGround;
        SnappingPrevented = report.SnappingPrevented;
        GroundNormal = report.GroundNormal;
        InnerGroundNormal = report.InnerGroundNormal;
        OuterGroundNormal = report.OuterGroundNormal;
        GroundCollider = null;
        GroundPoint = fp3.zero;
    }
}

/// <summary>
/// 角色瞬时地面检测报告（模拟内部使用）。不含碰撞体引用与接触点。
/// </summary>
public struct FPCharacterTransientGroundingReport
{
    /// <summary>是否检测到任意地面。</summary>
    public bool FoundAnyGround;

    /// <summary>是否稳定站立在地面上。</summary>
    public bool IsStableOnGround;

    /// <summary>是否阻止了地面吸附（snapping）。</summary>
    public bool SnappingPrevented;

    /// <summary>地面法线。</summary>
    public fp3 GroundNormal;

    /// <summary>内侧地面法线。</summary>
    public fp3 InnerGroundNormal;

    /// <summary>外侧地面法线。</summary>
    public fp3 OuterGroundNormal;

    /// <summary>
    /// 从完整地面报告复制数据。
    /// </summary>
    /// <param name="report">源地面报告。</param>
    public void CopyFrom(FPCharacterGroundingReport report)
    {
        FoundAnyGround = report.FoundAnyGround;
        IsStableOnGround = report.IsStableOnGround;
        SnappingPrevented = report.SnappingPrevented;
        GroundNormal = report.GroundNormal;
        InnerGroundNormal = report.InnerGroundNormal;
        OuterGroundNormal = report.OuterGroundNormal;
    }
}

/// <summary>
/// 刚体交互类型。定义角色与动态刚体的交互方式。
/// </summary>
public enum FPRigidbodyInteractionType
{
    /// <summary>无交互。</summary>
    None,

    /// <summary>运动学模式交互。</summary>
    Kinematic,

    /// <summary>模拟动态刚体交互。</summary>
    SimulatedDynamic,
}

/// <summary>
/// 台阶处理方式。
/// </summary>
public enum FPStepHandlingMethod
{
    /// <summary>不处理台阶。</summary>
    None,

    /// <summary>标准台阶处理。</summary>
    Standard,

    /// <summary>额外台阶处理（更高台阶）。</summary>
    Extra,
}

/// <summary>
/// 移动 sweep 状态。用于迭代解决移动碰撞时的状态机。
/// </summary>
public enum FPMovementSweepState
{
    /// <summary>初始 sweep。</summary>
    Initial,

    /// <summary>首次命中后的 sweep。</summary>
    AfterFirstHit,

    /// <summary>发现阻塞折痕（crease）。</summary>
    FoundBlockingCrease,

    /// <summary>发现阻塞角落（corner）。</summary>
    FoundBlockingCorner,
}
