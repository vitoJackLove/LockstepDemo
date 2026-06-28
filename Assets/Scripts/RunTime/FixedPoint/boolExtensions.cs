using Unity.Mathematics;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

public static class boolExtensions
{
    public static bool IsEquality(fp4 fp41, fp4 fp42)
    {
        return fpmath.abs(fp41.x - fp42.x) < (fp)0.01f &&
               fpmath.abs(fp41.y - fp42.y) < (fp)0.01f &&
               fpmath.abs(fp41.z - fp42.z) < (fp)0.01f &&
               fpmath.abs(fp41.w - fp42.w) < (fp)0.01f;
    }
    
    public static bool Bool3ToBool(this bool3 valueBool3, bool value = true)
    {
        bool3 bool3 = valueBool3 == value;

        return bool3 is { x: true, y: true, z: true };
    }

    public static bool Bool2ToBool(this bool2 valueBool2, bool value = true)
    {
        bool2 bool3 = valueBool2 == value;

        return bool3 is { x: true, y: true };
    }
    
    public static bool Bool4ToBool(this bool4 valueBool4, bool value = true)
    {
        bool4 bool4 = valueBool4 == value;

        return bool4 is { x: true, y: true, z : true, w : true };
    }

    public static Vector3 ToVector3(this fp3 valueFp3)
    {
        return new Vector3((float)valueFp3.x, (float)valueFp3.y, (float)valueFp3.z);
    }

    public static fp3 ToFp3(this Vector3 valueFp3)
    {
        return new fp3((fp)valueFp3.x, (fp)valueFp3.y, (fp)valueFp3.z);
    }
    
    public static fp2 ToFp2(this Vector3 valueFp3)
    {
        return new fp2((fp)valueFp3.x, (fp)valueFp3.y);
    }

    public static fp3 ToFp3(this fp2 valueFp2)
    {
        return new fp3(valueFp2.x, valueFp2.y, 0);
    }

    public static fp3 ToFp3(this Int3 valueFp3)
    {
        return new fp3((fp)valueFp3.x, (fp)valueFp3.y, (fp)valueFp3.z);
    }
    
    public static fp2 ToFp2(this Vector2 valueVector2)
    {
        return new fp2((fp)valueVector2.x, (fp)valueVector2.y);
    }
    
    public static Int3 ToInt3(this fp3 valueFp3)
    {
        return new Int3(valueFp3.x, valueFp3.y, valueFp3.z);
    }
    
    public static fp ToFp(this int valueInt)
    {
        return (fp)valueInt;
    }
    
    public static Vector3 ToVector3(this Int3 valueInt3)
    {
        return new Vector3((float)valueInt3.x, (float)valueInt3.y, (float)valueInt3.z);
    }
}