using Unity.Mathematics.FixedPoint;

/// <summary>
/// 定点数胶囊碰撞体，供 compound rigidbody 注册。
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
    /// 创建胶囊碰撞体。
    /// </summary>
    /// <param name="layer">碰撞层。</param>
    /// <param name="localCenter">局部中心偏移。</param>
    /// <param name="radius">局部半径。</param>
    /// <param name="height">胶囊总高度（含两端半球）。</param>
    /// <param name="yOffset">沿 Y 轴的额外局部偏移。</param>
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

    /// <inheritdoc />
    public int Id => _id;

    /// <inheritdoc />
    public FPShapeType ShapeType => FPShapeType.Capsule;

    /// <inheritdoc />
    public FPCollisionLayer Layer { get; set; }

    /// <inheritdoc />
    public bool IsTrigger { get; set; }

    /// <inheritdoc />
    public FPDynamicRigidbody AttachedBody { get; set; }

    /// <inheritdoc />
    public FPPhysicsMover AttachedMover { get; set; }

    /// <inheritdoc />
    public fp3 Position => _position;

    /// <inheritdoc />
    public fpquaternion Rotation => _rotation;

    /// <summary>
    /// 从 Transform 同步位姿；Y 轴缩放影响高度，X/Z 轴缩放影响半径。
    /// </summary>
    /// <param name="position">世界位置。</param>
    /// <param name="rotation">世界旋转。</param>
    /// <param name="localCenter">局部中心偏移。</param>
    /// <param name="baseRadius">未缩放的基础半径。</param>
    /// <param name="baseHeight">未缩放的基础高度。</param>
    /// <param name="yOffset">沿 Y 轴的额外局部偏移。</param>
    /// <param name="scale">各轴缩放。</param>
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
    /// 直接设置世界位姿（不应用局部中心与缩放）。
    /// </summary>
    /// <param name="position">世界位置。</param>
    /// <param name="rotation">世界旋转。</param>
    public void SetWorldPose(fp3 position, fpquaternion rotation)
    {
        _position = position;
        _rotation = rotation;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public FPBoxShape GetBoxShape() => default;

    /// <inheritdoc />
    public FPSphereShape GetSphereShape() => default;

    /// <inheritdoc />
    public FPPlaneShape GetPlaneShape() => default;
}
