using Unity.Mathematics.FixedPoint;

/// <summary>
/// KCC 物理形状类型枚举。
/// </summary>
public enum FPShapeType
{
    /// <summary>轴对齐或定向盒体。</summary>
    Box = 0,

    /// <summary>胶囊体。</summary>
    Capsule = 1,

    /// <summary>无限平面。</summary>
    Plane = 2,

    /// <summary>球体。</summary>
    Sphere = 3,
}

/// <summary>
/// 射线/胶囊 cast 命中结果。
/// </summary>
public struct FPRaycastHit
{
    /// <summary>命中的碰撞体。</summary>
    public IFPCollider Collider;

    /// <summary>命中点世界坐标。</summary>
    public fp3 Point;

    /// <summary>命中面法线。</summary>
    public fp3 Normal;

    /// <summary>从起点到命中点的距离。</summary>
    public fp Distance;
}

/// <summary>
/// 胶囊几何体（由上下半球中心与半径定义）。
/// </summary>
public struct FPCapsuleGeometry
{
    /// <summary>下半球中心。</summary>
    public fp3 BottomHemiCenter;

    /// <summary>上半球中心。</summary>
    public fp3 TopHemiCenter;

    /// <summary>胶囊半径。</summary>
    public fp Radius;

    /// <summary>胶囊几何中心（上下半球中心的中点）。</summary>
    public fp3 Center => (BottomHemiCenter + TopHemiCenter) * (fp)0.5f;
}

/// <summary>
/// 盒体碰撞形状（局部空间定义）。
/// </summary>
public struct FPBoxShape
{
    /// <summary>盒体中心（世界空间）。</summary>
    public fp3 Center;

    /// <summary>盒体旋转。</summary>
    public fpquaternion Rotation;

    /// <summary>各轴半尺寸。</summary>
    public fp3 HalfExtents;
}

/// <summary>
/// 胶囊碰撞形状（局部空间定义）。
/// </summary>
public struct FPCapsuleShape
{
    /// <summary>局部中心偏移。</summary>
    public fp3 Center;

    /// <summary>胶囊旋转。</summary>
    public fpquaternion Rotation;

    /// <summary>胶囊半径。</summary>
    public fp Radius;

    /// <summary>胶囊总高度（含两端半球）。</summary>
    public fp Height;

    /// <summary>主轴方向索引（0=X, 1=Y, 2=Z）。</summary>
    public int DirectionAxis;
}

/// <summary>
/// 平面碰撞形状。
/// </summary>
public struct FPPlaneShape
{
    /// <summary>平面法线（归一化）。</summary>
    public fp3 Normal;

    /// <summary>平面上一点。</summary>
    public fp3 Point;
}

/// <summary>
/// 球体碰撞形状（世界空间）。
/// </summary>
public struct FPSphereShape
{
    /// <summary>球心世界坐标。</summary>
    public fp3 Center;

    /// <summary>球体半径。</summary>
    public fp Radius;
}
