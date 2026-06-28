using System;
using System.Collections.Generic;
using PrimitiveDetection;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

public struct PolyVer : IComparable
{
    public fp Slope { get; set; }
    public int SortIndex { get; set; }

    public int CompareTo(object obj)
    {
        int result = 1;

        if (obj != null && obj is PolyVer)
        {
            var ver = (PolyVer)obj;
            result = this.Slope.CompareTo(ver.Slope);

            if (result == 0)
                result = this.SortIndex.CompareTo(ver.SortIndex);
        }

        return result;
    }

    public void Copy(PolyVer obj)
    {
        Slope = obj.Slope;
        SortIndex = obj.SortIndex;
    }
}

public class PolygonPrimitive : BasePrimitive
{
    // 世界坐标系下的顶点坐标
    public fp3[] Vertices { get; private set; }

    // 本地坐标系下的顶点坐标
    private fp3[] TransformVers;

    // 坐标点顺序
    public int[] verSort { get; private set; }

    public PolygonPrimitive()
    {
    }

    public static PolygonPrimitive Create(List<fp3> vertices, fp3 origin, fpquaternion quaternion)
    {
        PolygonPrimitive data = FPoolHelper.Get<PolygonPrimitive>();
        data.Vertices = new fp3[vertices.Count];
        data.TransformVers = new fp3[vertices.Count];
        data.verSort = new int[vertices.Count];
        vertices.CopyTo(data.Vertices, 0);
        data.Transform.SetOrientationAndPos(quaternion, origin);
        data.PolygonTransform(data.Transform);
        return data;
    }

    // 根据特定矩阵变换多边形所有点
    public void PolygonTransform(Matrix4 matrix)
    {
        for (int i = 0; i < Vertices.Length; i++)
        {
            TransformVers[i] = matrix.TransformInverse(Vertices[i]);
        }

        // DrawDebugTools.DrawPoint(Vertices[0], 1, Color.black, 3);
        // DrawDebugTools.DrawPoint(Vertices[1], 1, Color.white, 3);
        // DrawDebugTools.DrawPoint(Vertices[2], 1, Color.yellow, 3);

        PolygonPointSort();
    }

    // 根据斜率构建多边形，重排列顶点顺序
    private void PolygonPointSort()
    {
        // 小于3个顶点，不排序了
        if (verSort.Length < 3)
        {
            for (int i = 0; i < verSort.Length; i++)
            {
                verSort[i] = i;
            }

            return;
        }

        // 按照斜率slope排序，取稳定排序方法。
        PolyVer[] sortArr = new PolyVer[verSort.Length - 1];

        // 每个顶点 与 坐标x值最大的顶点 连线形成的直线的斜率

        // x坐标最大的顶点
        int maxxPointIdx = 0;

        for (int i = 0; i < TransformVers.Length; i++)
        {
            if (TransformVers[maxxPointIdx].x < TransformVers[i].x)
            {
                maxxPointIdx = i;
            }
            else if (TransformVers[maxxPointIdx].x < TransformVers[i].x &&
                     TransformVers[i].y > TransformVers[maxxPointIdx].y)
            {
                maxxPointIdx = i;
            }
        }

        // 计算斜率
        for (int i = 0; i < TransformVers.Length; i++)
        {
            if (i == maxxPointIdx)
            {
                continue;
            }

            if (TransformVers[i].x == TransformVers[maxxPointIdx].x) //与最大x坐标的x相同的点,因为x坐标之差为零，所以取SLOPE最大值
            {
                if (i == TransformVers.Length - 1)
                {
                    sortArr[maxxPointIdx] = new PolyVer() { Slope = fp.max_value, SortIndex = i };
                    continue;
                }

                sortArr[i] = new PolyVer() { Slope = fp.max_value, SortIndex = i };
            }
            else //计算斜率，注意正切函数在-0.5Pi和0.5Pi之间是单调递增的
            {
                if (i == TransformVers.Length - 1)
                {
                    sortArr[maxxPointIdx] = new PolyVer()
                    {
                        Slope = (TransformVers[i].z - TransformVers[maxxPointIdx].z) /
                                (TransformVers[maxxPointIdx].x - TransformVers[i].x),
                        SortIndex = i
                    };

                    continue;
                }

                sortArr[i] = new PolyVer()
                {
                    Slope = (TransformVers[i].z - TransformVers[maxxPointIdx].z) /
                            (TransformVers[maxxPointIdx].x - TransformVers[i].x),
                    SortIndex = i
                };
            }
        }

        verSort[0] = maxxPointIdx;

        Sort(ref sortArr, 0, sortArr.Length - 1);

        for (int i = 0; i < sortArr.Length; i++)
        {
            verSort[i + 1] = sortArr[i].SortIndex;
        }
    }

    private void Sort(ref PolyVer[] lst, int start, int end)
    {
        if (start >= end)
        {
            return;
        }

        int key = UnitSort(ref lst, start, end);
        Sort(ref lst, start, key - 1);
        Sort(ref lst, key + 1, end);
    }

    private int UnitSort(ref PolyVer[] l1, int start, int end)
    {
        PolyVer temp = l1[start];

        while (start != end)
        {
            while (start < end && l1[end].CompareTo(temp) >= 0)
            {
                end--;
            }

            l1[start].Copy(l1[end]);

            while (start < end && l1[start].CompareTo(temp) < 0)
            {
                start++;
            }

            l1[end].Copy(l1[start]);
        }

        l1[start] = temp;
        return start;
    }

    /// <summary>
    /// 将多边形变换为关于本地xoy平面对称的多边形
    /// </summary>
    public void XAxisSymmetric()
    {
        // Transform *= symmetricMatrix;

        for (int i = 0; i < TransformVers.Length; i++)
        {
            TransformVers[i] = PrimitiveExtension.symmetricMatrix * TransformVers[i];
            Vertices[i] = Transform.Transform(TransformVers[i]);
        }
    }
#if UNITY_EDITOR

    public override void PrimitiveDebug(Color color)
    {
        base.PrimitiveDebug(color);

        for (int i = 0; i < verSort.Length; i++)
        {
            DrawDebugTools.DrawLine(fpmath1.Fp3ToVector3(Vertices[verSort[i]]), fpmath1.Fp3ToVector3(Vertices[verSort[i + 1 < verSort.Length ? i + 1 : 0]]),
                Color.yellow, this.leftTime);
        }
    }

    public override void OnDrawGizmos()
    {
        if (!ShowInfo.isDrawShow || Vertices == null || verSort == null || verSort.Length < 2)
        {
            return;
        }

        Gizmos.color = ShowInfo.drawColor;

        for (int i = 0; i < verSort.Length; i++)
        {
            int currentIndex = verSort[i];
            int nextIndex = verSort[i + 1 < verSort.Length ? i + 1 : 0];

            if (currentIndex < 0 || currentIndex >= Vertices.Length || nextIndex < 0 || nextIndex >= Vertices.Length)
            {
                continue;
            }

            Gizmos.DrawLine(fpmath1.Fp3ToVector3(Vertices[currentIndex]), fpmath1.Fp3ToVector3(Vertices[nextIndex]));
        }
    }
#endif
    public override bool InternalCheckPrimitive()
    {
        return true;
    }

    public override void OnDispose()
    {
        FPoolHelper.Release<PolygonPrimitive>(this);
    }

    public override void Clear()
    {
        base.Clear();
        Vertices = null;
        TransformVers = null;
        verSort = null;
    }
}
