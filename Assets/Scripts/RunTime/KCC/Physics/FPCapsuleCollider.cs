using Unity.Mathematics.FixedPoint;

/// <summary>
/// 定点数胶囊碰撞体，供 Rigidbody 范式复合身体注册与 KCC 相交查询。
/// <para>
/// 与 Motor 内置胶囊不同：本类为独立 <see cref="IFPCollider"/>，可与其他 Box/Sphere 组合；
/// 几何由 <see cref="FPCapsuleShape"/> 描述，相交走 <see cref="FPCapsuleCollision"/>。
/// </para>
/// </summary>
public sealed class FPCapsuleCollider : IFPCollider
{
    private readonly int _id;
    private fp3 _localCenter;
    private fp _radius;
    private fp _height;
    private int _directionAxis;
    private fp3 _position;
    private fpquaternion _rotation;

    /// <summary>
    /// 创建胶囊碰撞体并分配全局唯一 <see cref="Id"/>。
    /// </summary>
    /// <param name="layer">碰撞层。</param>
    /// <param name="localCenter">局部中心偏移（不含 yOffset）。</param>
    /// <param name="radius">局部半径（圆柱段，不含半球帽）。</param>
    /// <param name="height">胶囊总高度（含两端半球）。</param>
    /// <param name="yOffset">沿 Y 轴额外局部偏移，会累加到 <paramref name="localCenter"/>。</param>
    /// <param name="directionAxis">主轴方向索引（0=X, 1=Y, 2=Z）。</param>
    /// <param name="isTrigger">是否为触发器。</param>
    public FPCapsuleCollider(
        FPCollisionLayer layer,
        fp3 localCenter,
        fp radius,
        fp height,
        fp yOffset,
        int directionAxis = 1,
        bool isTrigger = false)
    {
        _id = FPCollisionWorld.Instance.AllocateColliderId();
        Layer = layer;
        _localCenter = localCenter + new fp3((fp)0, yOffset, (fp)0);
        _radius = radius;
        _height = height;
        _directionAxis = directionAxis;
        IsTrigger = isTrigger;
        _rotation = fpquaternion.identity;
    }

    /// <summary>碰撞体在 <see cref="FPCollisionWorld"/> 中的唯一标识，构造时分配。</summary>
    public int Id => _id;

    /// <summary>固定为 <see cref="FPShapeType.Capsule"/>。</summary>
    public FPShapeType ShapeType => FPShapeType.Capsule;

    /// <summary>所属碰撞层。</summary>
    public FPCollisionLayer Layer { get; set; }

    /// <summary>是否为触发器。</summary>
    public bool IsTrigger { get; set; }

    /// <summary>绑定的刚体；由 <see cref="PhysicsBodyComponent"/> 在 Kinematic/Dynamic 注册时设置。</summary>
    public FPDynamicRigidbody AttachedBody { get; set; }

    /// <summary>绑定的运动学平台；角色站立时可继承平台速度。</summary>
    public FPPhysicsMover AttachedMover { get; set; }

    /// <summary>当前世界空间参考位置（同步自实体 Transform）。</summary>
    public fp3 Position => _position;

    /// <summary>当前世界空间旋转。</summary>
    public fpquaternion Rotation => _rotation;

    /// <summary>
    /// 从实体 Transform 同步位姿与缩放后的半径、高度。
    /// Y 轴缩放影响高度，X/Z 缩放影响半径（取较大轴）。
    /// </summary>
    /// <param name="position">实体世界位置。</param>
    /// <param name="rotation">实体世界旋转（含局部欧拉）。</param>
    /// <param name="localCenter">配置局部中心。</param>
    /// <param name="baseRadius">未缩放基础半径。</param>
    /// <param name="baseHeight">未缩放基础高度。</param>
    /// <param name="yOffset">Y 轴额外偏移。</param>
    /// <param name="scale">实体 LocalScale。</param>
    public void SyncFromTransform(
        fp3 position,
        fpquaternion rotation,
        fp3 localCenter,
        fp baseRadius,
        fp baseHeight,
        fp yOffset,
        fp3 scale)
    {
        _localCenter = localCenter + new fp3((fp)0, yOffset, (fp)0);
        _position = position + rotation * new fp3(
            localCenter.x * scale.x,
            localCenter.y * scale.y,
            localCenter.z * scale.z);
        _rotation = rotation;
        _radius = baseRadius * fpmath.max(fpmath.abs(scale.x), fpmath.abs(scale.z));
        _height = baseHeight * fpmath.abs(scale.y);
    }

    /// <summary>
    /// 直接设置世界位姿，不应用局部中心与缩放。
    /// </summary>
    /// <param name="position">世界位置。</param>
    /// <param name="rotation">世界旋转。</param>
    public void SetWorldPose(fp3 position, fpquaternion rotation)
    {
        _position = position;
        _rotation = rotation;
    }

    /// <summary>
    /// 导出胶囊形状参数，供 <see cref="FPMathKCC.BuildCapsuleGeometry"/> 与相交检测使用。
    /// </summary>
    /// <returns>含局部中心、旋转、半径、高度与主轴的 <see cref="FPCapsuleShape"/>。</returns>
    public FPCapsuleShape GetCapsuleShape()
    {
        return new FPCapsuleShape
        {
            Center = _localCenter,
            Rotation = _rotation,
            Radius = _radius,
            Height = _height,
            DirectionAxis = _directionAxis,
        };
    }

    /// <summary>本碰撞体非盒体，返回 default。</summary>
    public FPBoxShape GetBoxShape() => default;

    /// <summary>本碰撞体非球体，返回 default。</summary>
    public FPSphereShape GetSphereShape() => default;

    /// <summary>本碰撞体非平面，返回 default。</summary>
    public FPPlaneShape GetPlaneShape() => default;
}
