using Unity.Mathematics.FixedPoint;

/// <summary>
/// 定点数球体碰撞体，供 KCC 查询、Rigidbody 范式复合身体及球-胶囊相交检测。
/// <para>
/// 世界半径取 <c>max(|scale.x|, |scale.y|, |scale.z|) * baseRadius</c>，保证各向缩放后仍为球体。
/// </para>
/// </summary>
public sealed class FPSphereCollider : IFPCollider
{
    private readonly int _id;
    private fp3 _localCenter;
    private fp _radius;
    private fp3 _position;
    private fpquaternion _rotation;

    /// <summary>
    /// 创建球体碰撞体并分配全局唯一 <see cref="Id"/>。
    /// </summary>
    /// <param name="layer">碰撞层。</param>
    /// <param name="localCenter">相对实体原点的局部中心偏移。</param>
    /// <param name="radius">未缩放的基础半径。</param>
    /// <param name="isTrigger">是否为触发器。</param>
    public FPSphereCollider(FPCollisionLayer layer, fp3 localCenter, fp radius, bool isTrigger = false)
    {
        _id = FPCollisionWorld.Instance.AllocateColliderId();
        Layer = layer;
        _localCenter = localCenter;
        _radius = radius;
        IsTrigger = isTrigger;
        _rotation = fpquaternion.identity;
        _position = fp3.zero;
    }

    /// <summary>碰撞体在 <see cref="FPCollisionWorld"/> 中的唯一标识。</summary>
    public int Id => _id;

    /// <summary>固定为 <see cref="FPShapeType.Sphere"/>。</summary>
    public FPShapeType ShapeType => FPShapeType.Sphere;

    /// <summary>所属碰撞层。</summary>
    public FPCollisionLayer Layer { get; set; }

    /// <summary>是否为触发器。</summary>
    public bool IsTrigger { get; set; }

    /// <summary>绑定的刚体；Kinematic/Dynamic 实体注册时由 <see cref="PhysicsBodyComponent"/> 设置。</summary>
    public FPDynamicRigidbody AttachedBody { get; set; }

    /// <summary>绑定的运动学平台。</summary>
    public FPPhysicsMover AttachedMover { get; set; }

    /// <summary>球心世界坐标（已含局部偏移与实体位姿）。</summary>
    public fp3 Position => _position;

    /// <summary>世界旋转；球体对称时主要供复合体朝向一致性，相交仍用球心+半径。</summary>
    public fpquaternion Rotation => _rotation;

    /// <summary>
    /// 从实体 Transform 同步世界位姿与缩放后的半径。
    /// </summary>
    /// <param name="position">实体世界位置。</param>
    /// <param name="rotation">实体世界旋转。</param>
    /// <param name="localCenter">局部中心偏移。</param>
    /// <param name="baseRadius">未缩放基础半径。</param>
    /// <param name="scale">实体 LocalScale，三轴绝对值最大值缩放半径。</param>
    public void SyncFromTransform(fp3 position, fpquaternion rotation, fp3 localCenter, fp baseRadius, fp3 scale)
    {
        _localCenter = localCenter;
        _rotation = rotation;
        _position = position + rotation * ScaleVector(localCenter, scale);
        fp sx = fpmath.abs(scale.x);
        fp sy = fpmath.abs(scale.y);
        fp sz = fpmath.abs(scale.z);
        _radius = baseRadius * fpmath.max(sx, fpmath.max(sy, sz));
    }

    /// <summary>
    /// 直接设置世界位姿，不应用局部中心与缩放。
    /// </summary>
    /// <param name="position">世界位置（球心）。</param>
    /// <param name="rotation">世界旋转。</param>
    public void SetWorldPose(fp3 position, fpquaternion rotation)
    {
        _position = position;
        _rotation = rotation;
    }

    /// <summary>
    /// 导出世界空间球体形状，供 <see cref="FPCapsuleCollision"/> 球-胶囊相交等例程使用。
    /// </summary>
    /// <returns>含球心与半径的 <see cref="FPSphereShape"/>。</returns>
    public FPSphereShape GetSphereShape()
    {
        return new FPSphereShape { Center = _position, Radius = _radius };
    }

    /// <summary>本碰撞体非盒体，返回 default。</summary>
    public FPBoxShape GetBoxShape() => default;

    /// <summary>本碰撞体非胶囊，返回 default。</summary>
    public FPCapsuleShape GetCapsuleShape() => default;

    /// <summary>本碰撞体非平面，返回 default。</summary>
    public FPPlaneShape GetPlaneShape() => default;

    private static fp3 ScaleVector(fp3 value, fp3 scale)
    {
        return new fp3(value.x * scale.x, value.y * scale.y, value.z * scale.z);
    }
}
