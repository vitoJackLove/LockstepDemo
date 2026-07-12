using Unity.Mathematics.FixedPoint;

/// <summary>
/// KCC 碰撞体接口。描述形状、层、位姿及关联刚体/平台。
/// </summary>
public interface IFPCollider
{
    /// <summary>碰撞体唯一标识。</summary>
    int Id { get; }

    /// <summary>碰撞形状类型。</summary>
    FPShapeType ShapeType { get; }

    /// <summary>所属碰撞层。</summary>
    FPCollisionLayer Layer { get; }

    /// <summary>是否为触发器（不参与物理阻挡）。</summary>
    bool IsTrigger { get; }

    /// <summary>关联的动态刚体，无则为 null。</summary>
    FPDynamicRigidbody AttachedBody { get; }

    /// <summary>关联的运动学平台，无则为 null。</summary>
    FPPhysicsMover AttachedMover { get; }

    /// <summary>世界空间位置。</summary>
    fp3 Position { get; }

    /// <summary>世界空间旋转。</summary>
    fpquaternion Rotation { get; }

    /// <summary>
    /// 获取盒体形状数据。
    /// </summary>
    /// <returns>盒体形状。</returns>
    FPBoxShape GetBoxShape();

    /// <summary>
    /// 获取胶囊形状数据。
    /// </summary>
    /// <returns>胶囊形状。</returns>
    FPCapsuleShape GetCapsuleShape();

    /// <summary>
    /// 获取平面形状数据。
    /// </summary>
    /// <returns>平面形状。</returns>
    FPPlaneShape GetPlaneShape();

    /// <summary>
    /// 获取球体形状数据。
    /// </summary>
    /// <returns>球体形状。</returns>
    FPSphereShape GetSphereShape();
}
