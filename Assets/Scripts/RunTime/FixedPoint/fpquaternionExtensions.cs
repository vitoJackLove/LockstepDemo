using Unity.Mathematics.FixedPoint;

// 扩展方法，提供更自然的语法
public static class fpquaternionExtensions
{
    public static fpquaternion Euler(this fpquaternion q, fp x, fp y, fp z)
    {
        return fpmath1.EulerXYZ(new fp3(x, y, z));
    }

    public static fpquaternion Euler(this fpquaternion q, fp3 euler)
    {
        return fpmath1.EulerXYZ(euler);
    }

    public static fp3 ToEulerAngles(this fpquaternion q)
    {
        return fpmath1.toEulerAngles(q);
    }
}