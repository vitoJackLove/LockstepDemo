using Unity.Mathematics.FixedPoint;

/// <summary>
/// 将 ECS 实体 <c>FTransform</c> 作为运动学平台目标位姿的 <see cref="IFPPhysicsMoverController"/> 实现。
/// <para>
/// 用于升降台、移动地板等：实体逻辑（技能、行为树或地图系统）更新 <c>transform</c>，
/// <see cref="FPPhysicsMover"/> 在 <see cref="FPKinematicCharacterSystem"/> 中读取目标位姿并推导线速度/角速度，
/// 供 <see cref="FPKinematicCharacterMotor"/> 在 Phase1 继承平台运动。
/// </para>
/// </summary>
public sealed class FPEntityPhysicsMoverController : IFPPhysicsMoverController
{
    private readonly BaseEntity _entity;

    /// <summary>
    /// 绑定提供平台位姿的 ECS 实体。
    /// </summary>
    /// <param name="entity">平台实体，其 <c>transform</c> 每帧由游戏逻辑更新；不可为 null。</param>
    public FPEntityPhysicsMoverController(BaseEntity entity)
    {
        _entity = entity;
    }

    /// <summary>
    /// 输出实体当前世界位姿作为平台本帧目标；不自行积分运动。
    /// </summary>
    /// <param name="goalPosition">输出目标世界位置，取自 <c>_entity.transform.Position</c>。</param>
    /// <param name="goalRotation">输出目标世界旋转，取自 <c>_entity.transform.Rotation</c>。</param>
    /// <param name="deltaTime">逻辑帧间隔；本实现不用于插值，仅满足接口签名。</param>
    public void UpdateMovement(out fp3 goalPosition, out fpquaternion goalRotation, fp deltaTime)
    {
        if (_entity?.transform == null)
        {
            goalPosition = fp3.zero;
            goalRotation = fpquaternion.identity;
            return;
        }

        goalPosition = _entity.transform.Position;
        goalRotation = _entity.transform.Rotation;
    }
}
