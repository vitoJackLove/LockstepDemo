using Unity.Mathematics.FixedPoint;

/// <summary>
/// 定点数轴对齐盒体（OBB）碰撞体，供 KCC 阻挡查询与 Rigidbody 范式复合身体注册。
/// <para>
/// 实现 <see cref="IFPCollider"/>，由 <see cref="PhysicsConfigConverter.CreateCollider"/> 或
/// 地图逻辑直接构造，经 <see cref="FPCollisionWorld.RegisterCollider"/> 进入世界。
/// 位姿通过 <see cref="SyncFromTransform"/> 与实体 <c>FTransform</c> 保持同步。
/// </para>
/// </summary>
public sealed class FPBoxCollider : IFPCollider
{
    private readonly int _id;
    private fp3 _localCenter;
    private fp3 _halfExtents;
    private fp3 _position;
    private fpquaternion _rotation;

    /// <summary>
    /// 创建盒体碰撞体并分配全局唯一 <see cref="Id"/>。
    /// </summary>
    /// <param name="layer">初始碰撞层，参与 <see cref="FPCollisionWorld"/> 层过滤。</param>
    /// <param name="localCenter">相对实体原点的局部中心偏移（定点）。</param>
    /// <param name="halfExtents">未缩放半尺寸（各轴中心到面的距离）。</param>
    /// <param name="isTrigger">为 true 时不作为物理阻挡体，仅作触发用途。</param>
    public FPBoxCollider(FPCollisionLayer layer, fp3 localCenter, fp3 halfExtents, bool isTrigger = false)
    {
        _id = FPCollisionWorld.Instance.AllocateColliderId();
        Layer = layer;
        _localCenter = localCenter;
        _halfExtents = halfExtents;
        IsTrigger = isTrigger;
        _rotation = fpquaternion.identity;
    }

    /// <summary>碰撞体在 <see cref="FPCollisionWorld"/> 中的唯一标识，构造时分配，生命周期内不变。</summary>
    public int Id => _id;

    /// <summary>固定为 <see cref="FPShapeType.Box"/>，供查询与相交分支分发。</summary>
    public FPShapeType ShapeType => FPShapeType.Box;

    /// <summary>所属碰撞层，可运行时修改以切换阻挡关系。</summary>
    public FPCollisionLayer Layer { get; set; }

    /// <summary>是否为触发器；触发器不参与 Motor sweep 阻挡解算。</summary>
    public bool IsTrigger { get; set; }

    /// <summary>
    /// 绑定的动态/运动学刚体。Kinematic 怪物身体注册时由 <see cref="PhysicsBodyComponent"/> 赋值，
    /// 用于推挤与速度继承查询；Static 体为 null。
    /// </summary>
    public FPDynamicRigidbody AttachedBody { get; set; }

    /// <summary>
    /// 绑定的运动学平台（如升降台）。角色站在平台上时 Motor 可读取平台速度；
    /// 未挂接平台时为 null。
    /// </summary>
    public FPPhysicsMover AttachedMover { get; set; }

    /// <summary>当前世界空间中心位置（已含局部中心偏移与缩放）。</summary>
    public fp3 Position => _position;

    /// <summary>当前世界空间旋转（OBB 朝向）。</summary>
    public fpquaternion Rotation => _rotation;

    /// <summary>
    /// 仅更新局部形状参数，不修改世界位姿。
    /// 用于 Authoring 热更或编辑器预览，运行时同步优先使用 <see cref="SyncFromTransform"/>。
    /// </summary>
    /// <param name="localCenter">新的局部中心偏移。</param>
    /// <param name="halfExtents">新的未缩放半尺寸。</param>
    public void SetLocalShape(fp3 localCenter, fp3 halfExtents)
    {
        _localCenter = localCenter;
        _halfExtents = halfExtents;
    }

    /// <summary>
    /// 直接设置世界位姿，不重新计算局部中心与缩放。
    /// 用于 <see cref="PhysicsConfigConverter.CreateCollider"/> 初始化或调试放置。
    /// </summary>
    /// <param name="position">世界中心位置。</param>
    /// <param name="rotation">世界旋转。</param>
    public void SetWorldPose(fp3 position, fpquaternion rotation)
    {
        _position = position;
        _rotation = rotation;
    }

    /// <summary>
    /// 从实体 Transform 同步世界位姿与缩放后的半尺寸。
    /// <see cref="PhysicsBodyComponent"/> 在 <c>OnFixedUpdate</c> 对 Rigidbody 范式每帧调用。
    /// </summary>
    /// <param name="position">实体世界位置（通常为 pivot）。</param>
    /// <param name="rotation">实体世界旋转（已含碰撞体局部欧拉）。</param>
    /// <param name="localCenter">配置中的局部中心偏移。</param>
    /// <param name="baseHalfExtents">未缩放的基础半尺寸。</param>
    /// <param name="scale">实体 <c>LocalScale</c>，各轴绝对值乘以对应半尺寸。</param>
    public void SyncFromTransform(fp3 position, fpquaternion rotation, fp3 localCenter, fp3 baseHalfExtents, fp3 scale)
    {
        _localCenter = localCenter;
        _position = position + rotation * ScaleVector(localCenter, scale);
        _rotation = rotation;
        _halfExtents = new fp3(
            fpmath.abs(scale.x) * baseHalfExtents.x,
            fpmath.abs(scale.y) * baseHalfExtents.y,
            fpmath.abs(scale.z) * baseHalfExtents.z);
    }

    /// <summary>
    /// 导出当前世界空间盒体形状，供 <see cref="FPCapsuleCollision"/> 等相交例程使用。
    /// </summary>
    /// <returns>包含世界中心、旋转与半尺寸的 <see cref="FPBoxShape"/>。</returns>
    public FPBoxShape GetBoxShape()
    {
        return new FPBoxShape
        {
            Center = _position,
            Rotation = _rotation,
            HalfExtents = _halfExtents,
        };
    }

    /// <summary>本碰撞体非胶囊，返回 default。</summary>
    /// <returns>默认 <see cref="FPCapsuleShape"/>。</returns>
    public FPCapsuleShape GetCapsuleShape() => default;

    /// <summary>本碰撞体非平面，返回 default。</summary>
    /// <returns>默认 <see cref="FPPlaneShape"/>。</returns>
    public FPPlaneShape GetPlaneShape() => default;

    /// <summary>本碰撞体非球体，返回 default。</summary>
    /// <returns>默认 <see cref="FPSphereShape"/>。</returns>
    public FPSphereShape GetSphereShape() => default;

    private static fp3 ScaleVector(fp3 value, fp3 scale)
    {
        return new fp3(value.x * scale.x, value.y * scale.y, value.z * scale.z);
    }
}
