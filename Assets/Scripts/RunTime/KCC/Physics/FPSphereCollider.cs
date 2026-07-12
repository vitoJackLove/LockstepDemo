using Unity.Mathematics.FixedPoint;

/// <summary>
/// 定点数球体碰撞体，供 KCC 查询与角色阻挡。
/// </summary>
public sealed class FPSphereCollider : IFPCollider
{
    private readonly int _id;
    private fp3 _localCenter;
    private fp _radius;
    private fp3 _position;
    private fpquaternion _rotation;

    /// <summary>
    /// 创建球体碰撞体。
    /// </summary>
    /// <param name="layer">碰撞层。</param>
    /// <param name="localCenter">局部中心偏移。</param>
    /// <param name="radius">局部半径。</param>
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

    /// <inheritdoc />
    public int Id => _id;

    /// <inheritdoc />
    public FPShapeType ShapeType => FPShapeType.Sphere;

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
    /// 从实体 Transform 同步世界位姿与缩放后半径。
    /// </summary>
    /// <param name="position">世界位置。</param>
    /// <param name="rotation">世界旋转。</param>
    /// <param name="localCenter">局部中心偏移。</param>
    /// <param name="baseRadius">未缩放的基础半径。</param>
    /// <param name="scale">各轴缩放。</param>
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
    public FPSphereShape GetSphereShape()
    {
        return new FPSphereShape { Center = _position, Radius = _radius };
    }

    /// <inheritdoc />
    public FPBoxShape GetBoxShape() => default;

    /// <inheritdoc />
    public FPCapsuleShape GetCapsuleShape() => default;

    /// <inheritdoc />
    public FPPlaneShape GetPlaneShape() => default;

    private static fp3 ScaleVector(fp3 value, fp3 scale)
    {
        return new fp3(value.x * scale.x, value.y * scale.y, value.z * scale.z);
    }
}
