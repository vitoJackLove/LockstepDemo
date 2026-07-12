using Unity.Mathematics.FixedPoint;

/// <summary>
/// 定点数盒体碰撞体，供 KCC 查询与角色阻挡。
/// </summary>
public sealed class FPBoxCollider : IFPCollider
{
    private readonly int _id;
    private fp3 _localCenter;
    private fp3 _halfExtents;
    private fp3 _position;
    private fpquaternion _rotation;

    public FPBoxCollider(FPCollisionLayer layer, fp3 localCenter, fp3 halfExtents, bool isTrigger = false)
    {
        _id = FPCollisionWorld.Instance.AllocateColliderId();
        Layer = layer;
        _localCenter = localCenter;
        _halfExtents = halfExtents;
        IsTrigger = isTrigger;
        _rotation = fpquaternion.identity;
    }

    public int Id => _id;

    public FPShapeType ShapeType => FPShapeType.Box;

    public FPCollisionLayer Layer { get; set; }

    public bool IsTrigger { get; set; }

    public FPDynamicRigidbody AttachedBody { get; set; }

    public FPPhysicsMover AttachedMover { get; set; }

    public fp3 Position => _position;

    public fpquaternion Rotation => _rotation;

    public void SetLocalShape(fp3 localCenter, fp3 halfExtents)
    {
        _localCenter = localCenter;
        _halfExtents = halfExtents;
    }

    public void SetWorldPose(fp3 position, fpquaternion rotation)
    {
        _position = position;
        _rotation = rotation;
    }

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

    public FPBoxShape GetBoxShape()
    {
        return new FPBoxShape
        {
            Center = _position,
            Rotation = _rotation,
            HalfExtents = _halfExtents,
        };
    }

    public FPCapsuleShape GetCapsuleShape() => default;

    public FPPlaneShape GetPlaneShape() => default;

    public FPSphereShape GetSphereShape() => default;

    private static fp3 ScaleVector(fp3 value, fp3 scale)
    {
        return new fp3(value.x * scale.x, value.y * scale.y, value.z * scale.z);
    }
}
