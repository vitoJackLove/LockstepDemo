using Unity.Mathematics.FixedPoint;

/// <summary>
/// 定點數三維整數坐標，用於與 fp3/Vector3 轉換。取代原 A* Pathfinding Project 的 Pathfinding.Int3。
/// </summary>
public struct Int3 : System.IEquatable<Int3>
{
    public fp x;
    public fp y;
    public fp z;

    public Int3(fp _x, fp _y, fp _z)
    {
        x = _x;
        y = _y;
        z = _z;
    }

    public static bool operator ==(Int3 lhs, Int3 rhs) =>
        lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z;

    public static bool operator !=(Int3 lhs, Int3 rhs) =>
        lhs.x != rhs.x || lhs.y != rhs.y || lhs.z != rhs.z;

    public bool Equals(Int3 other) => x == other.x && y == other.y && z == other.z;

    public override bool Equals(object obj) =>
        obj is Int3 other && Equals(other);

    public override int GetHashCode() =>
        ((int)x * 73856093) ^ ((int)y * 19349663) ^ ((int)z * 83492791);
}
