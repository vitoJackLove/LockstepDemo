using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Mathematics.FixedPoint;

public static partial class fpmath1
{
    
    public static readonly fp4x4 identityMatrix = new fp4x4(new fp4(1, 0, 0, 0), 
        new fp4(0, 1, 0, 0), new fp4(0, 0, 1, 0), new fp4(0, 0, 0, 1));
    
    public static fp3 MultiplyPoint(fp3 point, fp4x4 fp4X4)
    {
        fp3 vector3;
        vector3.x = fp4X4.c0.x * point.x + fp4X4.c0.y * point.y + fp4X4.c0.z *  point.z + fp4X4.c0.w;
        vector3.y = fp4X4.c1.x *  point.x + fp4X4.c1.y *  point.y + fp4X4.c1.z *  point.z + fp4X4.c1.w;
        vector3.z = fp4X4.c2.x * point.x +fp4X4.c2.y *  point.y + fp4X4.c2.z * point.z + fp4X4.c2.w;
        fp num = 1 / (fp4X4.c3.x *  point.x +  fp4X4.c3.y  *  point.y + fp4X4.c3.z  *  point.z + fp4X4.c3.w);
        vector3.x *= num;
        vector3.y *= num;
        vector3.z *= num;
        return vector3;
    }
    
    public static fp4x4 inverse(fp4x4 m)
    {
        fp4 c0 = m.c0;
        fp4 c1 = m.c1;
        fp4 c2 = m.c2;
        fp4 c3 = m.c3;

        fp4 r0y_r1y_r0x_r1x = movelh(c1, c0);
        fp4 r0z_r1z_r0w_r1w = movelh(c2, c3);
        fp4 r2y_r3y_r2x_r3x = movehl(c0, c1);
        fp4 r2z_r3z_r2w_r3w = movehl(c3, c2);

        fp4 r1y_r2y_r1x_r2x = shuffle(c1, c0, ShuffleComponent.LeftY, ShuffleComponent.LeftZ,
            ShuffleComponent.RightY, ShuffleComponent.RightZ);
        fp4 r1z_r2z_r1w_r2w = shuffle(c2, c3, ShuffleComponent.LeftY, ShuffleComponent.LeftZ,
            ShuffleComponent.RightY, ShuffleComponent.RightZ);
        fp4 r3y_r0y_r3x_r0x = shuffle(c1, c0, ShuffleComponent.LeftW, ShuffleComponent.LeftX,
            ShuffleComponent.RightW, ShuffleComponent.RightX);
        fp4 r3z_r0z_r3w_r0w = shuffle(c2, c3, ShuffleComponent.LeftW, ShuffleComponent.LeftX,
            ShuffleComponent.RightW, ShuffleComponent.RightX);

        fp4 r0_wzyx = shuffle(r0z_r1z_r0w_r1w, r0y_r1y_r0x_r1x, ShuffleComponent.LeftZ, ShuffleComponent.LeftX,
            ShuffleComponent.RightX, ShuffleComponent.RightZ);
        fp4 r1_wzyx = shuffle(r0z_r1z_r0w_r1w, r0y_r1y_r0x_r1x, ShuffleComponent.LeftW, ShuffleComponent.LeftY,
            ShuffleComponent.RightY, ShuffleComponent.RightW);
        fp4 r2_wzyx = shuffle(r2z_r3z_r2w_r3w, r2y_r3y_r2x_r3x, ShuffleComponent.LeftZ, ShuffleComponent.LeftX,
            ShuffleComponent.RightX, ShuffleComponent.RightZ);
        fp4 r3_wzyx = shuffle(r2z_r3z_r2w_r3w, r2y_r3y_r2x_r3x, ShuffleComponent.LeftW, ShuffleComponent.LeftY,
            ShuffleComponent.RightY, ShuffleComponent.RightW);
        fp4 r0_xyzw = shuffle(r0y_r1y_r0x_r1x, r0z_r1z_r0w_r1w, ShuffleComponent.LeftZ, ShuffleComponent.LeftX,
            ShuffleComponent.RightX, ShuffleComponent.RightZ);

        // Calculate remaining inner term pairs. inner terms have zw=-xy, so we only have to calculate xy and can pack two pairs per vector.
        fp4 inner12_23 = r1y_r2y_r1x_r2x * r2z_r3z_r2w_r3w - r1z_r2z_r1w_r2w * r2y_r3y_r2x_r3x;
        fp4 inner02_13 = r0y_r1y_r0x_r1x * r2z_r3z_r2w_r3w - r0z_r1z_r0w_r1w * r2y_r3y_r2x_r3x;
        fp4 inner30_01 = r3z_r0z_r3w_r0w * r0y_r1y_r0x_r1x - r3y_r0y_r3x_r0x * r0z_r1z_r0w_r1w;

        // Expand inner terms back to 4 components. zw signs still need to be flipped
        fp4 inner12 = shuffle(inner12_23, inner12_23, ShuffleComponent.LeftX, ShuffleComponent.LeftZ,
            ShuffleComponent.RightZ, ShuffleComponent.RightX);
        fp4 inner23 = shuffle(inner12_23, inner12_23, ShuffleComponent.LeftY, ShuffleComponent.LeftW,
            ShuffleComponent.RightW, ShuffleComponent.RightY);

        fp4 inner02 = shuffle(inner02_13, inner02_13, ShuffleComponent.LeftX, ShuffleComponent.LeftZ,
            ShuffleComponent.RightZ, ShuffleComponent.RightX);
        fp4 inner13 = shuffle(inner02_13, inner02_13, ShuffleComponent.LeftY, ShuffleComponent.LeftW,
            ShuffleComponent.RightW, ShuffleComponent.RightY);

        // Calculate minors
        fp4 minors0 = r3_wzyx * inner12 - r2_wzyx * inner13 + r1_wzyx * inner23;

        fp4 denom = r0_xyzw * minors0;

        // Horizontal sum of denominator. Free sign flip of z and w compensates for missing flip in inner terms.
        denom = denom + shuffle(denom, denom, ShuffleComponent.LeftY, ShuffleComponent.LeftX, ShuffleComponent.RightW,
            ShuffleComponent.RightZ); // x+y        x+y            z+w            z+w
        denom = denom - shuffle(denom, denom, ShuffleComponent.LeftZ, ShuffleComponent.LeftZ, ShuffleComponent.RightX,
            ShuffleComponent.RightX); // x+y-z-w  x+y-z-w        z+w-x-y        z+w-x-y

        fp4 rcp_denom_ppnn = new fp4(1) / denom;
        fp4x4 res;
        res.c0 = minors0 * rcp_denom_ppnn;

        fp4 inner30 = shuffle(inner30_01, inner30_01, ShuffleComponent.LeftX, ShuffleComponent.LeftZ,
            ShuffleComponent.RightZ, ShuffleComponent.RightX);
        fp4 inner01 = shuffle(inner30_01, inner30_01, ShuffleComponent.LeftY, ShuffleComponent.LeftW,
            ShuffleComponent.RightW, ShuffleComponent.RightY);

        fp4 minors1 = r2_wzyx * inner30 - r0_wzyx * inner23 - r3_wzyx * inner02;
        res.c1 = minors1 * rcp_denom_ppnn;

        fp4 minors2 = r0_wzyx * inner13 - r1_wzyx * inner30 - r3_wzyx * inner01;
        res.c2 = minors2 * rcp_denom_ppnn;

        fp4 minors3 = r1_wzyx * inner02 - r0_wzyx * inner12 + r2_wzyx * inner01;
        res.c3 = minors3 * rcp_denom_ppnn;
        return res;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static fp4 movelh(fp4 a, fp4 b)
    {
        return shuffle(a, b, ShuffleComponent.LeftX
            , ShuffleComponent.LeftY, ShuffleComponent.RightX, ShuffleComponent.RightY);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static fp4 movehl(fp4 a, fp4 b)
    {
        return shuffle(b, a, ShuffleComponent.LeftZ,
            ShuffleComponent.LeftW, ShuffleComponent.RightZ, ShuffleComponent.RightW);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fp4 shuffle(fp4 left, fp4 right, ShuffleComponent x,
        ShuffleComponent y, ShuffleComponent z, ShuffleComponent w)
    {
        return new fp4(
            select_shuffle_component(left, right, x),
            select_shuffle_component(left, right, y),
            select_shuffle_component(left, right, z),
            select_shuffle_component(left, right, w));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static fp select_shuffle_component(fp4 a, fp4 b, ShuffleComponent component)
    {
        switch (component)
        {
            case ShuffleComponent.LeftX:
                return a.x;
            case ShuffleComponent.LeftY:
                return a.y;
            case ShuffleComponent.LeftZ:
                return a.z;
            case ShuffleComponent.LeftW:
                return a.w;
            case ShuffleComponent.RightX:
                return b.x;
            case ShuffleComponent.RightY:
                return b.y;
            case ShuffleComponent.RightZ:
                return b.z;
            case ShuffleComponent.RightW:
                return b.w;
            default:
                throw new System.ArgumentException("Invalid shuffle component: " + component);
        }
    }
    
    /// <summary>
    ///   <para>Creates a scaling matrix.</para>
    /// </summary>
    /// <param name="vector"></param>
    public static fp4x4 Scale(fp3 vector)
    {
        fp4x4 fp4x4;
        fp4x4.c0.x = vector.x;
        fp4x4.c0.y = 0;
        fp4x4.c0.z = 0;
        fp4x4.c0.w = 0;
        fp4x4.c1.x = 0;
        fp4x4.c1.y = vector.y;
        fp4x4.c1.z = 0;
        fp4x4.c1.w = 0;
        fp4x4.c2.x = 0;
        fp4x4.c2.y = 0;
        fp4x4.c2.z = vector.z;
        fp4x4.c2.w = 0;
        fp4x4.c3.x = 0;
        fp4x4.c3.y = 0;
        fp4x4.c3.z = 0;
        fp4x4.c3.w = 1;
        return fp4x4;
    }
    
    public static fp4x4 Multiplication(fp4x4 lhs, fp4x4 rhs)
    { 
        fp4x4 fp4x4;
        
        fp4x4.c0.x = lhs.c0.x * rhs.c0.x + lhs.c0.y * rhs.c1.x + lhs.c0.z * rhs.c2.x + lhs.c0.w * rhs.c3.x;
        fp4x4.c0.y = lhs.c0.x * rhs.c0.y + lhs.c0.y * rhs.c1.y + lhs.c0.z * rhs.c2.y + lhs.c0.w * rhs.c3.y;
        fp4x4.c0.z = lhs.c0.x * rhs.c0.z + lhs.c0.y * rhs.c1.z + lhs.c0.z * rhs.c2.z + lhs.c0.w * rhs.c3.z;
        fp4x4.c0.w = lhs.c0.x * rhs.c0.w + lhs.c0.y * rhs.c1.w + lhs.c0.z * rhs.c2.w + lhs.c0.w * rhs.c3.w;
        fp4x4.c1.x = lhs.c1.x * rhs.c0.x + lhs.c1.y * rhs.c1.x + lhs.c1.z * rhs.c2.x + lhs.c1.w * rhs.c3.x;
        fp4x4.c1.y = lhs.c1.x * rhs.c0.y + lhs.c1.y * rhs.c1.y + lhs.c1.z * rhs.c2.y + lhs.c1.w * rhs.c3.y;
        fp4x4.c1.z = lhs.c1.x * rhs.c0.z + lhs.c1.y * rhs.c1.z + lhs.c1.z * rhs.c2.z + lhs.c1.w * rhs.c3.z;
        fp4x4.c1.w = lhs.c1.x * rhs.c0.w + lhs.c1.y * rhs.c1.w + lhs.c1.z * rhs.c2.w + lhs.c1.w * rhs.c3.w;
        fp4x4.c2.x = lhs.c2.x * rhs.c0.x + lhs.c2.y * rhs.c1.x + lhs.c2.z * rhs.c2.x + lhs.c2.w * rhs.c3.x;
        fp4x4.c2.y = lhs.c2.x * rhs.c0.y + lhs.c2.y * rhs.c1.y + lhs.c2.z * rhs.c2.y + lhs.c2.w * rhs.c3.y;
        fp4x4.c2.z = lhs.c2.x * rhs.c0.z + lhs.c2.y * rhs.c1.z + lhs.c2.z * rhs.c2.z + lhs.c2.w * rhs.c3.z;
        fp4x4.c2.w = lhs.c2.x * rhs.c0.w + lhs.c2.y * rhs.c1.w + lhs.c2.z * rhs.c2.w + lhs.c2.w * rhs.c3.w;
        fp4x4.c3.x = lhs.c3.x * rhs.c0.x + lhs.c3.y * rhs.c1.x + lhs.c3.z * rhs.c2.x + lhs.c3.w * rhs.c3.x;
        fp4x4.c3.y = lhs.c3.x * rhs.c0.y + lhs.c3.y * rhs.c1.y + lhs.c3.z * rhs.c2.y + lhs.c3.w * rhs.c3.y;
        fp4x4.c3.z = lhs.c3.x * rhs.c0.z + lhs.c3.y * rhs.c1.z + lhs.c3.z * rhs.c2.z + lhs.c3.w * rhs.c3.z;
        fp4x4.c3.w = lhs.c3.x * rhs.c0.w + lhs.c3.y * rhs.c1.w + lhs.c3.z * rhs.c2.w + lhs.c3.w * rhs.c3.w;
        
        return fp4x4;
    }

    public enum RotationOrder : byte
    {
        /// <summary>Extrinsic rotation around the x axis, then around the y axis and finally around the z axis.</summary>
        XYZ,

        /// <summary>Extrinsic rotation around the x axis, then around the z axis and finally around the y axis.</summary>
        XZY,

        /// <summary>Extrinsic rotation around the y axis, then around the x axis and finally around the z axis.</summary>
        YXZ,

        /// <summary>Extrinsic rotation around the y axis, then around the z axis and finally around the x axis.</summary>
        YZX,

        /// <summary>Extrinsic rotation around the z axis, then around the x axis and finally around the y axis.</summary>
        ZXY,

        /// <summary>Extrinsic rotation around the z axis, then around the y axis and finally around the x axis.</summary>
        ZYX,

        /// <summary>Unity default rotation order. Extrinsic Rotation around the z axis, then around the x axis and finally around the y axis.</summary>
        Default = ZXY
    };

    public enum ShuffleComponent : byte
    {
        /// <summary>Specified the x component of the left vector.</summary>
        LeftX,

        /// <summary>Specified the y component of the left vector.</summary>
        LeftY,

        /// <summary>Specified the z component of the left vector.</summary>
        LeftZ,

        /// <summary>Specified the w component of the left vector.</summary>
        LeftW,

        /// <summary>Specified the x component of the right vector.</summary>
        RightX,

        /// <summary>Specified the y component of the right vector.</summary>
        RightY,

        /// <summary>Specified the z component of the right vector.</summary>
        RightZ,

        /// <summary>Specified the w component of the right vector.</summary>
        RightW
    };

    public static bool IsIdentity(fp4x4 fp4X4)
    {
        return fp4X4.c0.x == 1 && fp4X4.c0.y == 0 && fp4X4.c0.z == 0 && fp4X4.c0.w == 0 &&
               fp4X4.c1.x == 0 && fp4X4.c1.y == 1 && fp4X4.c1.z == 0 && fp4X4.c1.w == 0 &&
               fp4X4.c2.x == 0 && fp4X4.c2.y == 0 && fp4X4.c2.z == 1 && fp4X4.c2.w == 0 &&
               fp4X4.c3.x == 0 && fp4X4.c3.y == 0 && fp4X4.c3.z == 0 && fp4X4.c3.w == 1;
    }

    public static fp3 MultiplyPoint3x4(fp3 point, fp4x4 fp4X4)
    {
        fp3 vector3;
        vector3.x = fp4X4.c0.x * point.x + fp4X4.c0.y * point.y + fp4X4.c0.z * point.z + fp4X4.c0.w;
        vector3.y = fp4X4.c1.x * point.x + fp4X4.c1.y * point.y + fp4X4.c1.z * point.z + fp4X4.c1.w;
        vector3.z = fp4X4.c2.x * point.x + fp4X4.c2.y * point.y + fp4X4.c2.z * point.z + fp4X4.c2.w;
        return vector3;
    }

    /// <summary>
    ///   <para>Transforms a direction by this matrix.</para>
    /// </summary>
    /// <param name="vector"></param>
    public static fp3 MultiplyVector(fp3 vector, fp4x4 fp4X4)
    {
        fp3 vector3;
        vector3.x = fp4X4.c0.x * vector.x + fp4X4.c0.y * vector.y + fp4X4.c0.z * vector.z;
        vector3.y = fp4X4.c1.x * vector.x + fp4X4.c1.y * vector.y + fp4X4.c1.z * vector.z;
        vector3.z = fp4X4.c2.x * vector.x + fp4X4.c2.y * vector.y + fp4X4.c2.z * vector.z;
        return vector3;
    }

    /// <summary>
    ///   <para>Get a column of the matrix.</para>
    /// </summary>
    /// <param name="index"></param>
    public static fp4 GetColumn(int index, fp4x4 fp4X4)
    {
        switch (index)
        {
            case 0:
                return new fp4(fp4X4.c0.x, fp4X4.c1.x, fp4X4.c2.x, fp4X4.c3.x);
            case 1:
                return new fp4(fp4X4.c0.y, fp4X4.c1.y, fp4X4.c2.y, fp4X4.c3.y);
            case 2:
                return new fp4(fp4X4.c0.z, fp4X4.c1.z, fp4X4.c2.z, fp4X4.c3.z);
            case 3:
                return new fp4(fp4X4.c0.w, fp4X4.c1.w, fp4X4.c2.w, fp4X4.c3.w);
            default:
                throw new IndexOutOfRangeException("Invalid column index!");
        }
    }
}