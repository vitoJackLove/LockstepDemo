using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

// 定点数四元数结构
[Serializable]
public struct fpquaternion : IEquatable<fpquaternion>
{
    public fp valuex;
    public fp valuey;
    public fp valuez;
    public fp valuew;

    public static readonly fpquaternion identity = new fpquaternion((fp)0, (fp)0, (fp)0, (fp)1);

    public fp x { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => valuex; set => valuex = value; }
    public fp y { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => valuey; set => valuey = value; }
    public fp z { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => valuez; set => valuez = value; }
    public fp w { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => valuew; set => valuew = value; }

    public fpquaternion(fp x, fp y, fp z, fp w)
    {
        valuex = x;
        valuey = y;
        valuez = z;
        valuew = w;
    }

    public fpquaternion(fp4 value)
    {
        valuex = value.x;
        valuey = value.y;
        valuez = value.z;
        valuew = value.w;
    }

    // 隐式转换
    public static implicit operator fpquaternion(Quaternion q) => new fpquaternion((fp)q.x, (fp)q.y, (fp)q.z, (fp)q.w);
    public static implicit operator Quaternion(fpquaternion q) => new Quaternion((float)q.x, (float)q.y, (float)q.z, (float)q.w);

    // 基本运算
    public static fpquaternion operator *(fpquaternion lhs, fpquaternion rhs)
    {
        return new fpquaternion(
            lhs.w * rhs.x + lhs.x * rhs.w + lhs.y * rhs.z - lhs.z * rhs.y,
            lhs.w * rhs.y + lhs.y * rhs.w + lhs.z * rhs.x - lhs.x * rhs.z,
            lhs.w * rhs.z + lhs.z * rhs.w + lhs.x * rhs.y - lhs.y * rhs.x,
            lhs.w * rhs.w - lhs.x * rhs.x - lhs.y * rhs.y - lhs.z * rhs.z
        );
    }

    public static fp3 operator *(fpquaternion rotation, fp3 point)
    {
        fp num = rotation.x * (fp)2;
        fp num2 = rotation.y * (fp)2;
        fp num3 = rotation.z * (fp)2;
        fp num4 = rotation.x * num;
        fp num5 = rotation.y * num2;
        fp num6 = rotation.z * num3;
        fp num7 = rotation.x * num2;
        fp num8 = rotation.x * num3;
        fp num9 = rotation.y * num3;
        fp num10 = rotation.w * num;
        fp num11 = rotation.w * num2;
        fp num12 = rotation.w * num3;
        
        fp3 result;
        result.x = ((fp)1 - (num5 + num6)) * point.x + (num7 - num12) * point.y + (num8 + num11) * point.z;
        result.y = (num7 + num12) * point.x + ((fp)1 - (num4 + num6)) * point.y + (num9 - num10) * point.z;
        result.z = (num8 - num11) * point.x + (num9 + num10) * point.y + ((fp)1 - (num4 + num5)) * point.z;
        return result;
    }

    public override string ToString() => $"fpquaternion({x}, {y}, {z}, {w})";
    public bool Equals(fpquaternion other) => x == other.x && y == other.y && z == other.z && w == other.w;
    
    public override bool Equals(object obj) => obj is fpquaternion other && Equals(other);
    
    public override int GetHashCode() => (x, y, z, w).GetHashCode();
}