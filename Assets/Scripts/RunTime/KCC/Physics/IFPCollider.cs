using Unity.Mathematics.FixedPoint;

/// <summary>
/// KCC 碰撞体统一接口，抽象定点数物理世界中的可查询几何体。
/// <para>
/// 实现类：<see cref="FPBoxCollider"/>、<see cref="FPSphereCollider"/>、<see cref="FPCapsuleCollider"/>。
/// 由 <see cref="FPCollisionWorld"/> 集中注册，供 <see cref="FPKinematicCharacterMotor"/> sweep、
/// <see cref="FPCapsuleCollision"/> 相交与 <see cref="FPPhysicsQuery"/> 射线检测使用。
/// </para>
/// <para>
/// 与受击 <c>VolumeSystem</c> 使用的 <c>colliderDataList</c> 独立：本接口仅服务 KCC 阻挡与刚体绑定。
/// </para>
/// </summary>
public interface IFPCollider
{
    /// <summary>
    /// 碰撞体在 <see cref="FPCollisionWorld"/> 中的唯一标识，构造时分配，用于注销与调试。
    /// </summary>
    int Id { get; }

    /// <summary>
    /// 碰撞形状类型，决定 <see cref="GetBoxShape"/> 等哪个方法返回有效数据及相交分支。
    /// </summary>
    FPShapeType ShapeType { get; }

    /// <summary>
    /// 所属碰撞层（位标志枚举），与 <see cref="FPLayerMask"/> 组合过滤查询对象。
    /// </summary>
    FPCollisionLayer Layer { get; }

    /// <summary>
    /// 是否为触发器。为 true 时不应作为 Motor 移动阻挡体，仅用于重叠类逻辑（若查询路径支持）。
    /// </summary>
    bool IsTrigger { get; }

    /// <summary>
    /// 关联的 <see cref="FPDynamicRigidbody"/>（Kinematic 或 Dynamic）。
    /// Static 碰撞体为 null；接口为只读，赋值需通过具体实现类属性。
    /// </summary>
    FPDynamicRigidbody AttachedBody { get; }

    /// <summary>
    /// 关联的 <see cref="FPPhysicsMover"/> 运动学平台。角色站立时可继承平台速度；无平台为 null。
    /// </summary>
    FPPhysicsMover AttachedMover { get; }

    /// <summary>碰撞体参考点世界坐标（盒/球为几何中心，胶囊为同步后的实体相关位置）。</summary>
    fp3 Position { get; }

    /// <summary>碰撞体世界旋转；盒体与胶囊用于 OBB/胶囊轴向计算。</summary>
    fpquaternion Rotation { get; }

    /// <summary>
    /// 获取盒体形状快照。非盒体实现应返回 default。
    /// </summary>
    /// <returns>世界空间 <see cref="FPBoxShape"/>。</returns>
    FPBoxShape GetBoxShape();

    /// <summary>
    /// 获取胶囊形状快照。非胶囊实现应返回 default。
    /// </summary>
    /// <returns>含半径、高度与主轴的 <see cref="FPCapsuleShape"/>。</returns>
    FPCapsuleShape GetCapsuleShape();

    /// <summary>
    /// 获取无限平面形状快照。当前项目未实现平面 Collider，通常返回 default。
    /// </summary>
    /// <returns><see cref="FPPlaneShape"/> 或 default。</returns>
    FPPlaneShape GetPlaneShape();

    /// <summary>
    /// 获取球体形状快照。非球体实现应返回 default。
    /// </summary>
    /// <returns>世界球心与半径 <see cref="FPSphereShape"/>。</returns>
    FPSphereShape GetSphereShape();
}
