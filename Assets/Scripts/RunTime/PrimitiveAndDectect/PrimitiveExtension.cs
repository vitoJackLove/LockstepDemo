using PrimitiveDetection;
using Unity.Mathematics.FixedPoint;
using UnityEngine;


public class PrimitiveExtension
{
    /// <summary>
    /// 关于本地xoy平面对称  矩阵
    /// </summary>
    public static Matrix4 symmetricMatrix = new Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, -1, 0);

    /// <summary>
    /// box的边
    /// </summary>
    public static int[,] Lines = new int[,]
    {
        { 0, 1 }, { 0, 3 }, { 0, 4 }, { 1, 2 }, { 1, 5 }, { 2, 3 }, { 2, 6 }, { 3, 7 }, { 4, 5 }, { 4, 7 }, { 5, 6 },
        { 6, 7 }
    };

    static int[,] Mults = new int[,]
    {
        { 1, 1, 1 }, { -1, 1, 1 }, { -1, 1, -1 }, { 1, 1, -1 },
        { 1, -1, 1 }, { -1, -1, 1 }, { -1, -1, -1 }, { 1, -1, -1 }
    };

    public static fp BoxVMul(int i, int order, fp halfsize)
    {
        return Mults[i, order] * halfsize;
    }

    /// <summary>
    /// 向量哈达玛积
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <returns></returns>
    public static fp3 VMul(fp3 v1, fp3 v2)
    {
        return new fp3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
    }

    /// <summary>
    /// 行列式的值
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <returns></returns>
    public static fp Determinant(fp3 v1, fp3 v2)
    {
        return v1.y * v2.z - v1.z * v2.y +
            v1.z * v2.x - v1.x * v2.z +
            v1.x * v2.y - v1.y * v2.x;
    }
}