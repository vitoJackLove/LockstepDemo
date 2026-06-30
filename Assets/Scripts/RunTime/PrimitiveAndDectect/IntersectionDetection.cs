using System;
using System.Collections.Generic;
using PrimitiveDetection;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// 相交检测
/// </summary>
public static class IntersectionDetection
{
    #region 3D-3D

    private static fp SqrDistance(fp3 a, fp3 b)
    {
        fp num = a.x - b.x;
        fp num2 = a.y - b.y;
        fp num3 = a.z - b.z;
        return num * num + num2 * num2 + num3 * num3;
    }
    private static fp SqrNumber(fp n)
    {
        //Log.Assert(n >= 0, " [InterSection check number is negative!!] ");
        return n * n;
    }
    
    #region BOX

    public static bool BoxAndBox(BoxPrimitive one, BoxPrimitive two)
    {
        if (!BoxAndSphere(one, two.boxCenter, fpmath1.magnitude(two.HalfSize)) &&
            !BoxAndSphere(two, one.boxCenter, fpmath1.magnitude(one.HalfSize)))
        {
            return false;
        }

        fp3 toCentre = two.GetAxis(3) - one.GetAxis(3);
        fp3 onex = one.GetAxis(0);
        fp3 oney = one.GetAxis(1);
        fp3 onez = one.GetAxis(2);

        fp3 twox = two.GetAxis(0);
        fp3 twoy = two.GetAxis(1);
        fp3 twoz = two.GetAxis(2);

        // one的三个面分离轴测试
        if (!TryAxis(one, two, onex, toCentre)) return false;
        if (!TryAxis(one, two, oney, toCentre)) return false;
        if (!TryAxis(one, two, onez, toCentre)) return false;

        // two的三个面分离轴测试
        if (!TryAxis(one, two, twox, toCentre)) return false;
        if (!TryAxis(one, two, twoy, toCentre)) return false;
        if (!TryAxis(one, two, twoz, toCentre)) return false;

        // 9条边边叉积轴分离轴测试
        if (!TryAxis(one, two, fpmath.cross(onex, twox), toCentre)) return false;
        if (!TryAxis(one, two, fpmath.cross(onex, twoy), toCentre)) return false;
        if (!TryAxis(one, two, fpmath.cross(onex, twoz), toCentre)) return false;
        if (!TryAxis(one, two, fpmath.cross(oney, twox), toCentre)) return false;
        if (!TryAxis(one, two, fpmath.cross(oney, twoy), toCentre)) return false;
        if (!TryAxis(one, two, fpmath.cross(oney, twoz), toCentre)) return false;
        if (!TryAxis(one, two, fpmath.cross(onez, twox), toCentre)) return false;
        if (!TryAxis(one, two, fpmath.cross(onez, twoy), toCentre)) return false;
        if (!TryAxis(one, two, fpmath.cross(onez, twoz), toCentre)) return false;

        // 两条边的最近点作为碰撞点
        return true;
    }

    /// <summary>
    /// 盒体和盒体的分离轴测试
    /// </summary>
    /// <param name="one"></param>
    /// <param name="two"></param>
    /// <param name="axis"></param>
    /// <param name="toCentre"></param>
    /// <returns></returns>
    private static bool TryAxis(BoxPrimitive one, BoxPrimitive two, fp3 axis, fp3 toCentre)
    {
        // 两条边叉积接近0，为平行轴，不检测
        fp value = (fp)0.0001f;
        
        if (fpmath.sqrt(fpmath1.sqrMagnitude(axis)) <value) return true;

        axis = fpmath.normalize(axis);
        
        if (PenetrationOnAxis(one, two, axis, toCentre) < 0) return false;

        return true;
    }

    /// <summary>
    /// 盒体和盒体在一轴上的相交部分的投影
    /// </summary>
    /// <param name="one"></param>
    /// <param name="two"></param>
    /// <param name="axis"></param>
    /// <param name="toCentre"></param>
    /// <returns></returns>
    private static fp PenetrationOnAxis(BoxPrimitive one, BoxPrimitive two, fp3 axis, fp3 toCentre)
    {
        return TransformToAxis(one, axis) + TransformToAxis(two, axis) - ABS(fpmath.dot(toCentre, axis));
    }

    /// <summary>
    /// 盒体在一轴上的半投影
    /// </summary>
    /// <param name="box"></param>
    /// <param name="axis"></param>
    /// <returns></returns>
    private static fp TransformToAxis(BoxPrimitive box, fp3 axis)
    {
        return
            box.HalfSize.x * ABS(fpmath.dot(axis, box.GetAxis(0))) +
            box.HalfSize.y * ABS(fpmath.dot(axis, box.GetAxis(1))) +
            box.HalfSize.z * ABS(fpmath.dot(axis, box.GetAxis(2)));
    }

    /// <summary>
    /// 球体与box
    /// </summary>
    /// <param name="boxPrimitive"></param>
    /// <param name="spherePrimitive"></param>
    /// <returns></returns>
    public static bool BoxAndSphere(BoxPrimitive boxPrimitive, SpherePrimitive spherePrimitive)
    {
        return BoxAndSphere(boxPrimitive, spherePrimitive.SphereCenter, spherePrimitive.Radius);
    }

    /// <summary>
    /// 适用2d
    /// </summary>
    /// <param name="sphereCenter"></param>
    /// <param name="sphereRadius"></param>
    /// <param name="boxCenter"></param>
    /// <param name="boxHalfSize"></param>
    /// <returns></returns>
    public static bool BoxAndSphere(Vector3 sphereCenter, float sphereRadius, Vector3 boxCenter,
        Vector3 boxHalfSize)
    {
        Vector3 v = Vector3.Max(sphereCenter - boxCenter, boxCenter - sphereCenter); // = Abs(p - c);

        // 圆心点与AABB的最短矢量 u
        Vector3 u = Vector3.Max(v - boxHalfSize, Vector3.zero);

        // TODO:碰撞点求解
        return Vector3.SqrMagnitude(u) <= sphereRadius * sphereRadius;
    }

    /// <summary>
    /// 球体与box
    /// </summary>
    /// <param name="boxPrimitive"></param>
    /// <param name="sphereCenter"></param>
    /// <param name="sphereRadius"></param>
    /// <returns></returns>
    public static bool BoxAndSphere(BoxPrimitive boxPrimitive, fp3 sphereCenter, fp sphereRadius)
    {
        /*Vector3 v = Vector3.Max(sphereCenter - boxPrimitive.boxCenter,
            boxPrimitive.boxCenter - sphereCenter); // = Abs(p - c);

        // 圆心点与AABB的最短矢量 u
        Vector3 u = Vector3.Max(v - boxPrimitive.HalfSize, Vector3.zero);

        // TODO:碰撞点求解
        return Vector3.SqrMagnitude(u) <= sphereRadius * sphereRadius;*/

        // 将球心从世界坐标系转换到 OBB 的局部坐标系中
        fp3 localSphereCenter = boxPrimitive.Transform.TransformInverse(sphereCenter);

        // 计算球心在 OBB 的局部坐标系下的最近点
        fp3 closestPoint = new fp3(
            fpmath.clamp(localSphereCenter.x, -boxPrimitive.HalfSize.x, boxPrimitive.HalfSize.x),
            fpmath.clamp(localSphereCenter.y, -boxPrimitive.HalfSize.y, boxPrimitive.HalfSize.y),
            fpmath.clamp(localSphereCenter.z, -boxPrimitive.HalfSize.z, boxPrimitive.HalfSize.z)
        );

        // 将最近点从 OBB 的局部坐标系转换到世界坐标系中
        fp3 worldClosestPoint = boxPrimitive.Transform.Transform(closestPoint);

        // 计算球心与最近点之间的距离
        fp distance = fpmath.distance(sphereCenter, worldClosestPoint);

        // 判断距离是否小于等于球体半径，若是则相交，否则不相交
        return distance <= sphereRadius;
    }

    /// <summary>
    /// 盒体表面距离球体最近的点
    /// </summary>
    /// <param name="p"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static fp3 ClosestPointOnBox(SpherePrimitive p, BoxPrimitive b)
    {
        ClosestPointBoxSphere(p, b, out fp3 vBox, out fp3 vSphere);
        return vBox;
    }

    /// <summary>
    /// 盒子与直线
    /// </summary>
    /// <param name="point"></param>
    /// <param name="direct"></param>
    /// <param name="boxPrimitive"></param>
    /// <returns></returns>
    public static bool BoxAndLine(fp3 point, fp3 direct, BoxPrimitive boxPrimitive)
    {
        // Slabs Method - AABB碰撞检测法
        fp3 inv_dir = new fp3(1 / direct.x, 1 / direct.y, 1 / direct.z);
        fp3 cubeMin = boxPrimitive.Vertices[0];
        fp3 cubeMax = boxPrimitive.Vertices[0];

        for (int i = 0; i < boxPrimitive.Vertices.Length; i++)
        {
            if (ComparePosition(boxPrimitive.Vertices[i], cubeMin))
            {
                cubeMin = boxPrimitive.Vertices[i];
            }

            if (ComparePosition(cubeMax, boxPrimitive.Vertices[i]))
            {
                cubeMax = boxPrimitive.Vertices[i];
            }
        }

        fp3 tMin = PrimitiveExtension.VMul(cubeMin - point, inv_dir);
        fp3 tMax = PrimitiveExtension.VMul(cubeMax - point, inv_dir);
        fp3 t1 = fpmath.min(tMin, tMax);
        fp3 t2 = fpmath.max(tMin, tMax);
        fp tNear = fpmath.max(fpmath.max(t1.x, t1.y), t1.z);
        fp tFar = fpmath.min(fpmath.min(t2.x, t2.y), t2.z);

        return tNear <= tFar;
    }

    /// @brief test if ray and aabb box is intersected
    /// 盒子与射线
    /// @param ray DRay  of type T
    /// @param box DBox  of type T.
    /// @return a bool value that whether ray and aabb is intersected
    public static bool BoxAndRay(fp3 point, fp3 direct, BoxPrimitive boxPrimitive)
    {
        // direction of ray
        fp3 mDir = fpmath.normalize(direct);

        // make abs of direction
        fp3 mFDir = new fp3(fpmath.abs(mDir.x), fpmath.abs(mDir.y), fpmath.abs(mDir.z));

        // box center and extent
        fp3 center = boxPrimitive.GetAxis(3);
        fp3 extents = boxPrimitive.HalfSize;

        fp Dx = point.x - center.x;
        if (fpmath.abs(Dx) > extents.x && Dx * mDir.x >= 0) return false;

        fp Dy = point.y - center.y;
        if (fpmath.abs(Dy) > extents.y && Dy * mDir.y >= 0) return false;

        fp Dz = point.z - center.z;
        if (fpmath.abs(Dz) > extents.z && Dz * mDir.z >= 0) return false;

        fp f;
        f = mDir.y * Dz - mDir.z * Dy;
        if (fpmath.abs(f) > extents.y * mFDir.z + extents.z * mFDir.y) return false;

        f = mDir.z * Dx - mDir.x * Dz;
        if (fpmath.abs(f) > extents.x * mFDir.z + extents.z * mFDir.x) return false;

        f = mDir.x * Dy - mDir.y * Dx;
        if (fpmath.abs(f) > extents.x * mFDir.y + extents.y * mFDir.x) return false;

        return true;
    }

    #endregion

    #region SPHERE

    public static bool SphereAndSphere(SpherePrimitive one, SpherePrimitive two)
    {
        fp3 positionOne = one.GetAxis(3);
        fp3 positionTwo = two.GetAxis(3);
        //
        // Vector3 positionOne = one.SphereCenter;
        // Vector3 positionTwo = two.SphereCenter;

        fp3 midline = positionOne - positionTwo;
        fp size = fpmath1.magnitude(midline);

        if (size > one.Radius + two.Radius)
        {
            return false;
        }

        // TODO:碰撞点求解
        return true;
    }

    /// <summary>
    /// 球体表面距离盒体最近的点
    /// </summary>
    /// <param name="p"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static fp3 ClosestPointOnSphere(SpherePrimitive p, BoxPrimitive b)
    {
        ClosestPointBoxSphere(p, b, out fp3 vBox, out fp3 vSphere);
        return vSphere;
    }

    /// <summary>
    /// 球体与射线
    /// </summary>
    /// <param name="point"></param>
    /// <param name="direct"></param>
    /// <param name="spherePrimitive"></param>
    /// <returns></returns>
    public static bool SphereAndRay(fp3 point, fp3 direct, SpherePrimitive spherePrimitive)
    {
        fp3 nearestPointOnRay = NearestPointOnRay(point, direct, spherePrimitive.SphereCenter);

        // DrawDebugTools.DrawPoint(nearestPointOnRay,1,Color.green);
        return fpmath1.magnitude(nearestPointOnRay - spherePrimitive.SphereCenter) <= spherePrimitive.Radius;
    }

    #endregion

    #region SECTOR

    // 扇形与球形相交测试
    // a 扇形圆心
    // u 扇形方向（单位矢量）
    // theta 扇形扫掠半角 
    // l 扇形边长
    // c 圆盘圆心
    // r 圆盘半径
    public static bool SectorAndSphere(SectorPrimitive sectorPrimitive, SpherePrimitive spherePrimitive)
    {
        if (!sectorPrimitive.InternalCheckPrimitive() || !spherePrimitive.InternalCheckPrimitive())
        {
            return false;
        }

        // 1. 如果扇形圆心和圆盘圆心的方向能分离，两形状不相交
        fp3 d = spherePrimitive.SphereCenter - sectorPrimitive.SectorCenter;
        fp rsum = sectorPrimitive.Radius + spherePrimitive.Radius;

        if (fpmath1.sqrMagnitude(d) > (rsum * rsum))
            return false;

        // 2. 计算出扇形局部空间下的圆心坐标 p
        fp3 p = sectorPrimitive.Transform2Local(spherePrimitive.SphereCenter);

        // 3. 平面情况下 如果 p_x > ||p|| cos theta，两形状相交
        // 空间下表示圆心与扇形中心连线与扇形方向形成的夹角小于扇形扫掠角的一半
        if (p.x > fpmath1.magnitude(d) * fpmath.cos(sectorPrimitive.Angle / 2))
        {
            // 圆心在扇形空间下的x轴坐标小于扇形半径
            // 如果圆心和扇形所在的平面可以分离，两形状不相交
            if (p.x <= sectorPrimitive.Radius)
            {
                if (p.y <= spherePrimitive.Radius)
                {
                    return true;
                }

                return false;
            }

            // 扇形圆弧上距离圆心最近的点 sp
            fp3 sp = sectorPrimitive.Radius * fpmath.normalize(new fp3(p.x, 0, p.z)) - p;

            // 圆心在扇形空间下的x轴坐标大于扇形半径
            // 如果圆弧与圆心可以分离，两形状不相交 
            if (p.y <= spherePrimitive.Radius && fpmath1.magnitude(sp) <= spherePrimitive.Radius)
            {
                return true;
            }

            return false;
        }

        // 4. 求左边线段与圆盘是否相交
        // 扇形局部空间下第一象限的线段的终点坐标 q
        fp3 q = sectorPrimitive.Radius * new fp3(fpmath.cos(sectorPrimitive.Angle / 2), 0,
            fpmath.sin(sectorPrimitive.Angle / 2));

        // 利用扇形的对称性，把球心映射到第一象限再求球心到扇形边缘线段的距离
        p.y = fpmath.abs(p.y);
        p.z = fpmath.abs(p.z);
        return SegmentPointSqrDistance(fp3.zero, q, p) <= (spherePrimitive.Radius * spherePrimitive.Radius);
    }

    /// <summary>
    /// （已知）如果包含了扇形的三个顶点，会判断为不相交
    /// </summary>
    /// <param name="sectorPrimitive"></param>
    /// <param name="boxPrimitive"></param>
    /// <param name="polygonPrimitive"></param>
    /// <returns></returns>
    public static bool SectorAndBox(SectorPrimitive sectorPrimitive, BoxPrimitive boxPrimitive,
        out PolygonPrimitive polygonPrimitive)
    {
        // polygonPrimitive = new PolygonPrimitive(new Vector3[1], sectorPrimitive.SectorCenter, sectorPrimitive.Quaternion);
        polygonPrimitive = null;

        if (!sectorPrimitive.InternalCheckPrimitive() || !boxPrimitive.InternalCheckPrimitive())
        {
            return false;
        }

        // 球盒检测
        // 如果box与扇形圆心能分离，两形状不相交
        if (!BoxAndSphere(boxPrimitive, sectorPrimitive.SectorCenter, sectorPrimitive.Radius))
        {
            return false;
        }

        fp3 fp = fp3.zero;
        List<fp3> polygon = new List<fp3>();

        // x坐标最大的点
        int maxx = 0;

        // 检测box所有边与扇形所在平面是否相交，求出交点形成多边形再检测多边形与扇形的相交情况
        for (int i = 0; i < PrimitiveExtension.Lines.GetLength(0); i++)
        {
            int p1 = PrimitiveExtension.Lines[i, 0];
            int p2 = PrimitiveExtension.Lines[i, 1];

            if (!LineSegmentFootPointOnPlane(boxPrimitive.Vertices[p1], boxPrimitive.Vertices[p2],
                    sectorPrimitive.SectorCenter, sectorPrimitive.LocalUnitY, out fp)) continue;

            polygon.Add(fp);

            if (fp.x >= polygon[maxx].x)
            {
                maxx = polygon.Count - 1;
            }
        }

        // 没有交点，不相交
        if (polygon.Count <= 0)
        {
            return false;
        }

        polygonPrimitive =
            PolygonPrimitive.Create(polygon, sectorPrimitive.SectorCenter, sectorPrimitive.Quaternion);

        // 将多边形对称到扇形的第一二象限，利用扇形的对称性
        if (!GetGravityPoint(polygonPrimitive, out fp3 graPoint))
        {
            polygonPrimitive.XAxisSymmetric();
        }

        // 如果多边形所有顶点都在扇形对称轴反向上且扇形角度小于180度，不相交
        maxx = 1;

        foreach (var index in polygonPrimitive.verSort)
        {
            fp = polygonPrimitive.Vertices[index] - sectorPrimitive.SectorCenter;

            if (fpmath.dot(fp, sectorPrimitive.LocalUnitX) > 0)
            {
                maxx = 0;
            }
        }

        if (maxx != 0 && sectorPrimitive.Angle < 180 * (fp)Mathf.Deg2Rad)
        {
            return false;
        }

        //polygonPrimitive.PrimitiveDebug(Color.red);

        // 扇形角度大于180度，使用扇形第一二象限的半扇形做检测判定
        // 1. 取半扇形
        // 2. 将box对称到半扇形的一二象限
        if (sectorPrimitive.Angle > 180 * (fp)Mathf.Deg2Rad)
        {
            sectorPrimitive = SectorPrimitive.Create(sectorPrimitive.SectorCenter,
                sectorPrimitive.Quaternion * 
                fpmath1.EulerXYZ(new fp3(0, -sectorPrimitive.Angle * (fp)Mathf.Rad2Deg / 4, 0)),
                sectorPrimitive.Angle * (fp)Mathf.Rad2Deg / 2, sectorPrimitive.Radius);
        }

        if (!sectorPrimitive.InternalCheckPrimitive())
        {
            return false;
        }

        bool res = true;
        fp3 lineStart, lineEnd;

        // 检测1. 扇形与多边形每一条边做分离轴检测，如果有一条边上不存在分离轴，第一次分离轴检测结果就为false
        for (int i = 0; i < polygonPrimitive.verSort.Length; i++)
        {
            fp3 locali = polygonPrimitive.Vertices[polygonPrimitive.verSort[i]];
            fp3 locali1 =
                polygonPrimitive.Vertices[
                    polygonPrimitive.verSort[i + 1 >= polygonPrimitive.verSort.Length ? 0 : i + 1]];

            if (IsSectorIntersectLineSeg(sectorPrimitive, locali, locali1, out lineStart, out lineEnd))
            {
                res = false;
                break;
            }
        }

        if (res)
        {
            return false;
        }

        // 距离扇形圆心最近的多边形顶点
        int mindis = 0;
        fp3[] projectPoints = new fp3[polygonPrimitive.Vertices.Length];

        // 检测2. 扇形与多边形在扇形圆心与多边形距圆心最近点的连线上是否存在分离轴
        for (int i = 0; i < polygonPrimitive.Vertices.Length; i++)
        {
            if (fpmath1.magnitude((polygonPrimitive.Vertices[i] - sectorPrimitive.SectorCenter))
                <=fpmath1.magnitude(polygonPrimitive.Vertices[mindis] - sectorPrimitive.SectorCenter))
            {
                mindis = i;
            }
        }

        IsSectorIntersectLineSeg(sectorPrimitive, sectorPrimitive.SectorCenter, polygonPrimitive.Vertices[mindis],
            out lineStart, out lineEnd);

        // Vector3 projectAxis = polygonPrimitive.Vertices[mindis]+Vector3.Cross(sectorPrimitive.SectorCenter-polygonPrimitive.Vertices[mindis], sectorPrimitive.LocalUnitY);
        // Vector3 projectPedal = PointProjectionOnLineSeg(sectorPrimitive.SectorCenter, polygonPrimitive.Vertices[mindis], projectAxis);
        // Vector3 projectAxisInverse = projectPedal + (projectPedal - projectAxis);
        // Vector3 lineMidPoint = (sectorPrimitive.SectorCenter + polygonPrimitive.Vertices[mindis]) / 2;
        // Vector3 nor = lineMidPoint + projectAxis - projectPedal;
        // Vector3 inverseNor = lineMidPoint + projectAxisInverse - projectPedal;
        // Vector3 centerP = PointProjectionOnLineSeg(lineMidPoint, nor, sectorPrimitive.SectorCenter);
        // Vector3 leftP = PointProjectionOnLineSeg(lineMidPoint, nor, sectorPrimitive.EdgeVertices[0]);
        // Vector3 rightP = PointProjectionOnLineSeg(lineMidPoint, nor, sectorPrimitive.EdgeVertices[1]);
        // DrawDebugTools.DrawPoint(centerP,1,Color.magenta,3);
        // DrawDebugTools.DrawPoint(leftP,1,Color.magenta,3);
        // DrawDebugTools.DrawPoint(rightP,1,Color.magenta,3);
        // DrawDebugTools.DrawLine(lineMidPoint,nor,Color.red,3);
        // DrawDebugTools.DrawLine(lineMidPoint,inverseNor,Color.red,3);

        // DrawDebugTools.DrawLine(lineStart,lineEnd,Color.magenta,3);

        for (int i = 0; i < polygonPrimitive.Vertices.Length; i++)
        {
            projectPoints[i] = PointProjectionOnLineSeg(lineStart, lineEnd,
                polygonPrimitive.Vertices[polygonPrimitive.verSort[i]]);

            // GameObject ss = GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Sphere);
            // ss.transform.localScale = Vector3.one * 0.1f;
            // ss.transform.position = projectPoints[i];
            // ss.name = $"projectPoints[{i}]";
        }

        int startIdx = 0;
        int endIdx = 0;

        FarthestPoints(projectPoints, projectPoints.Length, ref startIdx, ref endIdx);

        fp3 lineProjectStart = ComparePosition(projectPoints[startIdx], projectPoints[endIdx])
            ? projectPoints[startIdx]
            : projectPoints[endIdx];

        fp3 lineProjectEnd = !ComparePosition(projectPoints[startIdx], projectPoints[endIdx])
            ? projectPoints[startIdx]
            : projectPoints[endIdx];

        res = IsLineSegIntersectLineSeg(lineStart, lineEnd, 
            lineProjectStart, lineProjectEnd, out fp3 cross);

        // DrawDebugTools.DrawLine(lineStart, lineEnd, Color.red, 3);
        // DrawDebugTools.DrawLine(lineProjectStart, lineProjectEnd, Color.blue, 3);

        if (!res)
        {
            // Debug.LogError("2false");
            return false;
        }

        // 检测3. 扇形两边的法线与多边形是否存在分离轴
        fp3 q = sectorPrimitive.Radius * new fp3(fpmath.cos(sectorPrimitive.Angle / 2), 0,
            fpmath.sin(sectorPrimitive.Angle / 2));
        q = sectorPrimitive.Transform2World(q);

        // 扇形在法线上投影线段的起点与终点
        IsSectorIntersectLineSeg(sectorPrimitive, q, 
            sectorPrimitive.SectorCenter, out lineStart, out lineEnd);

        // Vector3 projectAxis = sectorPrimitive.SectorCenter+Vector3.Cross(q-sectorPrimitive.SectorCenter, sectorPrimitive.LocalUnitY);
        // Vector3 projectPedal = PointProjectionOnLineSeg(sectorPrimitive.SectorCenter, q, projectAxis);
        // Vector3 projectAxisInverse = projectPedal + (projectPedal - projectAxis);
        // Vector3 lineMidPoint = (sectorPrimitive.SectorCenter + q) / 2;
        // Vector3 nor = lineMidPoint + projectAxis - projectPedal;
        // Vector3 inverseNor = lineMidPoint + projectAxisInverse - projectPedal;
        // Vector3 centerP = PointProjectionOnLineSeg(lineMidPoint, nor, sectorPrimitive.SectorCenter);
        // Vector3 leftP = PointProjectionOnLineSeg(lineMidPoint, nor, sectorPrimitive.EdgeVertices[0]);
        // Vector3 rightP = PointProjectionOnLineSeg(lineMidPoint, nor, sectorPrimitive.EdgeVertices[1]);
        // DrawDebugTools.DrawLine(lineMidPoint,nor,Color.magenta,3);
        // Vector3 norP = sectorPrimitive.SectorCenter + 
        //                ((projectAxis - projectPedal) / 
        //                 (projectAxis - projectPedal).magnitude) * sectorPrimitive.Radius;
        // Vector3 inverseNorP = sectorPrimitive.SectorCenter + 
        //                       ((projectAxisInverse-projectPedal)
        //                       / ( projectAxisInverse-projectPedal).magnitude) * sectorPrimitive.Radius;
        // norP = PointProjectionOnLineSeg(lineMidPoint,nor, norP);
        // inverseNorP = PointProjectionOnLineSeg(lineMidPoint,nor, inverseNorP);
        // DrawDebugTools.DrawPoint(norP,1,Color.magenta,3);
        // DrawDebugTools.DrawPoint(inverseNorP,1,Color.magenta,3);
        // DrawDebugTools.DrawPoint(leftP,1,Color.magenta,3);
        // DrawDebugTools.DrawPoint(rightP,1,Color.magenta,3);
        // DrawDebugTools.DrawLine(lineStart,lineEnd,Color.magenta,3);
        // 多边形在投影线段上的起点与终点
        for (int i = 0; i < polygonPrimitive.Vertices.Length; i++)
        {
            projectPoints[i] = PointProjectionOnLineSeg(lineStart, lineEnd,
                polygonPrimitive.Vertices[polygonPrimitive.verSort[i]]);
        }

        FarthestPoints(projectPoints, projectPoints.Length, ref startIdx, ref endIdx);
        lineProjectStart = ComparePosition(projectPoints[startIdx], projectPoints[endIdx])
            ? projectPoints[startIdx]
            : projectPoints[endIdx];
        lineProjectEnd = !ComparePosition(projectPoints[startIdx], projectPoints[endIdx])
            ? projectPoints[startIdx]
            : projectPoints[endIdx];

        res = IsLineSegIntersectLineSeg(lineStart, lineEnd, lineProjectStart, lineProjectEnd, out cross);

        return res;
    }

    /// <summary>
    /// TODO:
    /// </summary>
    /// <param name="one"></param>
    /// <param name="two"></param>
    /// <returns></returns>
    public static bool SectorAndSector(SectorPrimitive one, SectorPrimitive two)
    {
        fp3 c = one.Transform2Local(two.SectorCenter);

        // 点积接近0，扇形1法向量与两圆心组成的向量不垂直，两个扇形不在一个平面内，不相交
        if (fpmath.abs(fpmath.dot(two.SectorCenter - one.SectorCenter, one.LocalUnitY)) < (fp)0.0001)
        {
            return false;
        }

        // 两个扇形所在的圆可分离，不相交
        if (fpmath1.magnitude(c) > (one.Radius + two.Radius))
        {
            return false;
        }

        // 如果扇形1包含扇形2的3个点中任一个，相交
        fp3 q1 = two.Radius * new fp3(fpmath.cos(two.Angle / 2), 0, fpmath.sin(two.Angle / 2));
        q1 = two.Transform2World(q1);
        fp3 q2 = two.Radius * new fp3(fpmath.cos(two.Angle / 2), 0, -fpmath.sin(two.Angle / 2));
        q2 = two.Transform2World(q2);

        if (IsSectorHasPoint(one, two.SectorCenter) || IsSectorHasPoint(one, q1) || IsSectorHasPoint(one, q2))
        {
            return true;
        }

        // 扇形1与圆2相交
        if (c.x > fpmath1.magnitude(c) * fpmath.cos(one.Angle / 2))
        {
            // 扇形2的圆心在扇形1扫掠角内

            // 如果与弧相交
        }

        return false;
    }

    /// <summary>
    /// 点是否在扇形所在平面内且在扇形扫掠角区域内
    /// </summary>
    /// <param name="sector"></param>
    /// <param name="point"></param>
    /// <returns></returns>
    private static bool IsSectorHasPoint(SectorPrimitive sector, fp3 point)
    {
        fp3 dis = point - sector.SectorCenter;

        // 点积不为0，点与扇形不在一个平面内，点在扇形外
        if (!(fpmath.abs(fpmath.dot(dis, sector.LocalUnitY)) < (fp)0.0001))
        {
            return false;
        }

        // 点到圆心距离大于扇形半径，点在扇形外
        if (fpmath1.magnitude(dis) > sector.Radius)
        {
            return false;
        }

        return IsInsideSectorAngle(sector, point);
    }

    /// <summary>
    /// 在扇形平面上的某点与扇形圆心组成的射线是否在扇形扫掠角内
    /// </summary>
    /// <param name="sector"></param>
    /// <param name="point"></param>
    /// <returns></returns>
    private static bool IsInsideSectorAngle(SectorPrimitive sector, fp3 point)
    {
        fp3 p = sector.Transform2Local(point);
        fp pop = p.x / fpmath1.magnitude(p);
        fp degree = fpmath1.acos(pop);
        degree *= 2;

        if (fpmath.abs(degree - sector.Angle) <= (fp)0.01)
        {
            return true;
        }

        return degree <= sector.Angle;
    }

    /// <summary>
    /// 圆心为circleCenter，半径为radius的圆与线段是否碰撞
    /// </summary>
    /// <param name="circleCenter"></param>
    /// <param name="radius"></param>
    /// <param name="seg1"></param>
    /// <param name="seg2"></param>
    /// <returns></returns>
    private static bool IsCircleIntersectLineSeg(fp3 circleCenter, fp radius, fp3 seg1, fp3 seg2)
    {
        fp3 seg = seg2 - seg1;
        fp3 cseg1 = circleCenter - seg1;
        fp projection = fpmath.dot(seg, cseg1);
        fp3 p0;
        bool result;

        // 求出在线段上且距离圆心最近的点
        if (projection <= 0)
        {
            p0 = seg1;
        }
        else if (projection >= fpmath1.magnitude(seg))
        {
            p0 = seg2;
        }
        else
        {
            p0 = seg1 + cseg1 * projection;
        }

        result = fpmath1.magnitude((p0 - circleCenter)) <= radius;
        
        return result;
    }

    /// <summary>
    /// 扇形是否与线段在线段上存在分离轴
    /// </summary>
    /// <param name="sector"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="lineStart"></param>
    /// <param name="lineEnd"></param>
    /// <returns></returns>
    private static bool IsSectorIntersectLineSeg(SectorPrimitive sector, fp3 start, fp3 end,
        out fp3 lineStart, out fp3 lineEnd)
    {
        fp3 forwardDir = start - end;
        fp3 inverseDir = end - start;
        bool hasP1 = false;
        bool hasP2 = false;
        fp3 intersectPos1 = fp3.zero;
        fp3 intersectPos2 = fp3.zero;

        // 线段中垂线作为投影轴
        // 垂直与线段与扇形平面法向量的向量projectAxis
        fp3 projectAxis = end + fpmath.cross(forwardDir, sector.LocalUnitY);

        // 垂足
        fp3 projectPedal = PointProjectionOnLineSeg(start, end, projectAxis);

        // projectAxis的反方向
        fp3 projectAxisInverse = projectPedal + (projectPedal - projectAxis);

        // 线段中点
        fp3 lineMidPoint = (start + end) / 2;

        // 正方向
        fp3 nor = lineMidPoint + projectAxis - projectPedal;

        fp3 inverseNor = lineMidPoint + projectAxisInverse - projectPedal;

        // DrawDebugTools.DrawLine(start,end,Color.blue,3);
        // DrawDebugTools.DrawLine(projectPedal,projectAxis,Color.red,3);
        // DrawDebugTools.DrawLine(projectPedal,projectAxisInverse,Color.red,3);
        // DrawDebugTools.DrawLine(inverseNor,sector.SectorCenter,Color.blue,3);

        // 线段的正方向向量在扇形内
        if (IsInsideSectorAngle(sector, sector.SectorCenter + forwardDir))
        {
            intersectPos1 = sector.SectorCenter + (forwardDir / fpmath1.magnitude(forwardDir)) * sector.Radius;
            hasP1 = true;
        }

        // 线段的反方向向量在扇形内
        if (IsInsideSectorAngle(sector, sector.SectorCenter + inverseDir))
        {
            intersectPos2 = sector.SectorCenter + (inverseDir / fpmath1.magnitude(inverseDir)) * sector.Radius;
            hasP2 = true;
        }

        fp3 centerP = PointProjectionOnLineSeg(lineMidPoint, nor, sector.SectorCenter);
        fp3 leftP = PointProjectionOnLineSeg(lineMidPoint, nor, sector.EdgeVertices[0]);
        fp3 rightP = PointProjectionOnLineSeg(lineMidPoint, nor, sector.EdgeVertices[1]);
        lineStart = centerP;
        lineEnd = leftP;
        fp3[] vector3s;

        // DrawDebugTools.DrawPoint(centerP, 1, Color.blue, 3);
        // DrawDebugTools.DrawPoint(leftP, 1, Color.red, 3);
        // DrawDebugTools.DrawPoint(rightP, 1, Color.black, 3);

        // 扇形在投影轴方向上的坐标
        fp3 norP = centerP;
        fp3 inverseNorP = centerP;

        // DrawDebugTools.DrawPoint(sector.SectorCenter + (nor / nor.magnitude) * sector.Radius,1,Color.black,3);

        if (IsInsideSectorAngle(sector,
                sector.SectorCenter + fpmath1.magnitude((projectAxis - projectPedal) / (projectAxis - projectPedal)) *
                sector.Radius))
        {
            norP = sector.SectorCenter + fpmath1.magnitude((projectAxis - projectPedal) / (projectAxis - projectPedal)) *
                sector.Radius;
            norP = PointProjectionOnLineSeg(lineMidPoint, nor, norP);
        }

        if (IsInsideSectorAngle(sector,
                sector.SectorCenter +
                ((projectAxisInverse - projectPedal) / fpmath1.magnitude(projectAxisInverse - projectPedal)) *
                sector.Radius))
        {
            inverseNorP = sector.SectorCenter +
                          ((projectAxisInverse - projectPedal) / fpmath1.magnitude(projectAxisInverse - projectPedal) *
                          sector.Radius);

            inverseNorP = PointProjectionOnLineSeg(lineMidPoint, nor, inverseNorP);
        }

        // 1. 线段正反方向向量都在扇形区域内
        // 以pos1,pos2作为扇形左右顶点时组成的扇形在线段中垂线上的投影线段与线段相交，则相交
        if (hasP1 && hasP2)
        {
            fp3 sectorP1 = sector.SectorCenter + sector.Radius * (nor / fpmath1.magnitude(nor));
            sectorP1 = PointProjectionOnLineSeg(lineMidPoint, nor, sectorP1);
            fp3 p1Pro = PointProjectionOnLineSeg(lineMidPoint, nor, intersectPos1);
            fp3 p2Pro = PointProjectionOnLineSeg(lineMidPoint, nor, intersectPos2);
            vector3s = new[] { p1Pro, p2Pro, sectorP1, norP, inverseNorP };
        }

        // 线段的正方向向量与反方向向量都不在扇形区域内
        else if (!hasP1 && !hasP2)
        {
            vector3s = new[] { centerP, leftP, rightP, norP, inverseNorP };
        }

        // 线段的正方向向量在扇形区域内，反方向向量不在扇形区域内
        else if (hasP1)
        {
            fp3 pos1Project = PointProjectionOnLineSeg(lineMidPoint, nor, intersectPos1);

            vector3s = new[]
                { centerP, leftP, rightP, pos1Project, norP, inverseNorP };
        }

        // 线段的反方向向量在扇形区域内，正方向向量不在扇形区域内
        else
        {
            fp3 pos2Project = PointProjectionOnLineSeg(lineMidPoint, nor, intersectPos2);

            vector3s = new[]
                { centerP, leftP, rightP, pos2Project, norP, inverseNorP };
        }

        // line2 在投影轴上长度最长的线段的两个顶点构成的向量
        int startIdx = 0;
        int endIdx = 0;
        FarthestPoints(vector3s, vector3s.Length, ref startIdx, ref endIdx);
        lineStart = ComparePosition(vector3s[startIdx], vector3s[endIdx]) ? vector3s[startIdx] : vector3s[endIdx];
        lineEnd = !ComparePosition(vector3s[startIdx], vector3s[endIdx]) ? vector3s[startIdx] : vector3s[endIdx];

        bool res = IsLineSegIntersectLineSeg(lineStart, lineEnd, start, end, out fp3 crossPoint);

        return res;
    }

    public static bool SectorAndCapsule(SectorPrimitive sectorPrimitive, CapsulePrimitive capsulePrimitive,
        out fp3 bestA)
    {
        // 投影到xz平面下的扇形与胶囊体
        // 扇形法线方向只能与y轴方向平行
        // 视胶囊为线段，找到线段离球心的最近点，做球-扇形平面检测

        bestA = fp3.zero;
        fp3 bestB = fp3.zero;

        // if (sectorPrimitive.Angle * Mathf.Rad2Deg >= 180){
        //     sectorPrimitive = new SectorPrimitive(sectorPrimitive.SectorCenter,
        //         sectorPrimitive.Quaternion * Quaternion.Euler(new Vector3(0, -sectorPrimitive.Angle * Mathf.Rad2Deg / 4, 0)),
        //         sectorPrimitive.Angle * Mathf.Rad2Deg / 2, sectorPrimitive.Radius);
        // }

        GJKDetecotor.ClosestPoinsOnTwoLines(capsulePrimitive.CenterOne, capsulePrimitive.CenterTwo,
            sectorPrimitive.SectorCenter,
            sectorPrimitive.LocalUnitX * sectorPrimitive.Radius + sectorPrimitive.SectorCenter,
            ref bestA, ref bestB);

        return SectorAndSphere(sectorPrimitive,
            SpherePrimitive.Create(bestA, capsulePrimitive.Radius, capsulePrimitive.CapsuleQuaternion));
    }

    #endregion

    #region CAPSULE

    public static bool CapsuleAndCapsule(CapsulePrimitive capsule1, CapsulePrimitive capsule2)
    {
        // 要先把两个胶囊视为两条线段，找到两条线段的最近距离的两个点，然后以两个点做为球心，做球球检测

        fp3 bestA = fp3.zero;
        fp3 bestB = fp3.zero;
        GJKDetecotor.ClosestPoinsOnTwoLines(capsule1.CenterOne, capsule1.CenterTwo, capsule2.CenterOne,
            capsule2.CenterTwo, ref bestA, ref bestB);

        // 球球检测
        fp3 midline = bestA - bestB;
        fp size = fpmath1.magnitude(midline);

        if (size <= 0 || size >= capsule1.Radius + capsule2.Radius)
            return false;

        return true;
    }

    public static bool CapsuleAndSphere(CapsulePrimitive capsule, SpherePrimitive sphere)
    {
        // 视胶囊为线段，找到线段离球心的最近点，做球球检测

        fp3 bestB = sphere.GetAxis(3);

        fp3 bestA = GJKDetecotor.ClosestPointOnLineSegment(capsule.CenterOne, capsule.CenterTwo, bestB);

        fp3 midline = bestA - bestB;
        fp size = fpmath1.magnitude(midline);

        // size<=0 则球心与胶囊距球心最近点重合
        // if (size >= capsule.Radius + sphere.Radius)
        //     return false;
        //
        // return true;
        return size <= capsule.Radius + sphere.Radius;
    }

    public static bool CapsuleAndBox(CapsulePrimitive capsule, BoxPrimitive box)
    {
        // 先球盒检测，避免进行过多的分离轴检测而产生巨大耗时
        if (!BoxAndSphere(box, capsule.CapsuleCenter, capsule.HalfHeight.y))
        {
            return false;
        }

        // 用分离轴算法找出最浅相交的特征，然后确定是面碰撞，还是边碰撞，面碰撞做球面检测，边碰撞则找到胶囊离边的最近点，做球盒检测

        fp3 toCentre = box.GetAxis(3) - capsule.GetAxis(3);

        fp pen = fp.max_value;
        int best = 0xffffff;

        // box的3条对角斜轴
        fp3 edgeAxis3 = fpmath.normalize((box.GetAxis(0) + box.GetAxis(1)));
        fp3 edgeAxis4 = fpmath.normalize((box.GetAxis(0) + box.GetAxis(2)));
        fp3 edgeAxis5 = fpmath.normalize((box.GetAxis(1) + box.GetAxis(2)));

        if (!TryAxis(capsule, box, edgeAxis3, toCentre, 3, ref pen, ref best)) return false;
        if (!TryAxis(capsule, box, edgeAxis4, toCentre, 4, ref pen, ref best)) return false;
        if (!TryAxis(capsule, box, edgeAxis5, toCentre, 5, ref pen, ref best)) return false;

        int bestEdgeAxis = best;

        if (!TryAxis(capsule, box, box.GetAxis(0), toCentre, 0, ref pen, ref best)) return false;
        if (!TryAxis(capsule, box, box.GetAxis(1), toCentre, 1, ref pen, ref best)) return false;
        if (!TryAxis(capsule, box, box.GetAxis(2), toCentre, 2, ref pen, ref best)) return false;

        if (best < 3)
        {
            fp3 bestAxis = box.GetAxis(best);
            int sign = (int)fpmath1.sign(fpmath.dot(bestAxis, -toCentre));
            fp3 planeDirection = sign * bestAxis;

            // 找到离碰撞面较近的一端
            fp3 position = fp3.zero;

            if (fpmath.dot(capsule.CenterOneToTwo, planeDirection) > 0)
            {
                position = capsule.CenterOne;
            }
            else
            {
                position = capsule.CenterTwo;
            }

            fp3 realPosition = box.Transform.TransformInverse(position);

            // 如果该端点超出碰撞面，则找到最近边，视为与边的碰撞
            if (best == 0)
            {
                if (fpmath.abs(realPosition.y) > box.HalfSize.y || 
                    fpmath.abs(realPosition.z) > box.HalfSize.z)
                    best = bestEdgeAxis;
            }

            if (best == 1)
            {
                if (fpmath.abs(realPosition.x) > box.HalfSize.x || fpmath.abs(realPosition.z) > box.HalfSize.z)
                    best = bestEdgeAxis;
            }

            if (best == 2)
            {
                if (fpmath.abs(realPosition.x) > box.HalfSize.x || fpmath.abs(realPosition.y) > box.HalfSize.y)
                    best = bestEdgeAxis;
            }

            if (best < 3)
            {
                fp planeOffset = fpmath.dot(box.GetAxis(3), planeDirection) + box.HalfSize[best];

                fp dist = fpmath.dot(planeDirection, position) - capsule.Radius - planeOffset;

                if (dist >= 0) return false;

                return true;
            }
        }

        if (best >= 3)
        {
            // 找到最近边

            fp3 bestPoint = fp3.zero;
            fp3 linePointA = fp3.zero;
            fp3 linePointB = fp3.zero;

            if (best == 3)
            {
                int sign = (int)fpmath1.sign(fpmath.dot(edgeAxis3, -toCentre));
                linePointA = sign * box.HalfSize;
                linePointB = linePointA;
                linePointB.z *= -1;
            }

            if (best == 4)
            {
                int sign = (int)fpmath1.sign(fpmath.dot(edgeAxis4, -toCentre));
                linePointA = sign * box.HalfSize;
                linePointB = linePointA;
                linePointB.y *= -1;
            }

            if (best == 5)
            {
                int sign = (int)fpmath1.sign(fpmath.dot(edgeAxis5, -toCentre));
                linePointA = sign * box.HalfSize;
                linePointB = linePointA;
                linePointB.x *= -1;
            }

            linePointA = box.Transform.Transform(linePointA);
            linePointB = box.Transform.Transform(linePointB);
            bestPoint = GJKDetecotor.ClosestPointFromLineTwo(linePointA, linePointB, capsule.CenterOne,
                capsule.CenterTwo);

            return BoxAndSphere(box, bestPoint, capsule.Radius);

            // return BoxAndSphere(box, new SpherePrimitive(bestPoint, capsule.Radius, capsule.CapsuleQuaternion));
        }

        return false;
    }

    /// <summary>
    /// 胶囊体与盒体的分离轴测试
    /// </summary>
    /// <param name="one"></param>
    /// <param name="two"></param>
    /// <param name="axis"></param>
    /// <param name="toCentre"></param>
    /// <param name="index"></param>
    /// <param name="smallestPenetration"></param>
    /// <param name="smallestCase"></param>
    /// <returns></returns>
    private static bool TryAxis(CapsulePrimitive one, BoxPrimitive two, fp3 axis, fp3 toCentre, int index,
        ref fp smallestPenetration,
        ref int smallestCase)
    {
        // 两条边叉积接近0，为平行轴，不检测
        if (fpmath1.sqrMagnitude(axis) < (fp)0.0001) return true;

        axis = fpmath.normalize(axis);

        fp penetration = PenetrationOnAxis(one, two, axis, toCentre);

        if (penetration < 0) return false;

        if (penetration < smallestPenetration)
        {
            smallestPenetration = penetration;
            smallestCase = index;
        }

        return true;
    }

    /// <summary>
    /// 胶囊和盒体在一轴上的相交部分的投影
    /// </summary>
    /// <param name="one"></param>
    /// <param name="two"></param>
    /// <param name="axis"></param>
    /// <param name="toCentre"></param>
    /// <returns></returns>
    private static fp PenetrationOnAxis(CapsulePrimitive one, BoxPrimitive two,
        fp3 axis, fp3 toCentre)
    {
        fp oneProject = TransformToAxis(one, axis);
        fp twoProject = TransformToAxis(two, axis);

        fp distance = fpmath.abs(fpmath.dot(toCentre, axis));

        return oneProject + twoProject - distance;
    }

    /// <summary>
    /// 胶囊在一轴上的半投影
    /// </summary>
    /// <param name="capsule"></param>
    /// <param name="axis"></param>
    /// <returns></returns>
    private static fp TransformToAxis(CapsulePrimitive capsule, fp3 axis)
    {
        return
            capsule.Radius * fpmath.abs(fpmath.dot(axis, capsule.GetAxis(0))) +
            (capsule.HalfHeight.y + capsule.Radius) * fpmath.abs(fpmath.dot(axis, capsule.GetAxis(1))) +
            capsule.Radius * fpmath.abs(fpmath.dot(axis, capsule.GetAxis(2)));
    }

    /// <summary>
    /// 胶囊与射线
    /// </summary>
    /// <param name="point"></param>
    /// <param name="direct"></param>
    /// <param name="capsulePrimitive"></param>
    /// <returns></returns>
    public static bool CapsuleAndRay(fp3 point, fp3 direct, CapsulePrimitive capsulePrimitive)
    {
        direct = fpmath.normalize(direct);
        fp3 ab = capsulePrimitive.CenterOneToTwo; // 胶囊的方向向量
        fp3 ac = point - capsulePrimitive.CenterOne; // 胶囊起点到射线起点的向量

        fp abDot = fpmath.dot(ab, ab);
        fp acDot = fpmath.dot(ac, ab);

        // float t = Mathf.Clamp01(acDot / abDot); // 射线与胶囊方向向量的投影系数。如果clamp01，点会一直偏离射线
        fp t = acDot / abDot; // 射线与胶囊方向向量的投影系数

        fp3 closestPoint = capsulePrimitive.CenterOne + ab * t; // 射线与胶囊最近的点
        fp3 closestPointToRay = point + direct * fpmath.dot(closestPoint - point, direct); // 最近点到射线的垂直投影点

        // DrawDebugTools.DrawPoint(closestPoint, 1, Color.green);
        // DrawDebugTools.DrawPoint(closestPointToRay, 1, Color.yellow);
        fp distance = fpmath.distance(closestPoint, closestPointToRay);

        return distance <= capsulePrimitive.Radius;
    }

    #endregion

    #region Annulus

    public static bool AnnulusAndBox(AnnulusPrimitive annulusPrimitive, BoxPrimitive boxPrimitive)
    {
        SectorPrimitive outerSector = SectorPrimitive.Create(annulusPrimitive.AnnulusCenter,
            annulusPrimitive.Quaternion,
            annulusPrimitive.Angle * (fp)Mathf.Rad2Deg, annulusPrimitive.OuterDiameter);

        if (!outerSector.InternalCheckPrimitive() || !boxPrimitive.InternalCheckPrimitive())
        {
            return false;
        }

        // 1. 以环形外径为半径的完整扇形与盒体无碰撞，环形与盒体无碰撞
        if (!SectorAndBox(outerSector, boxPrimitive, out PolygonPrimitive polygonPrimitive))
        {
            return false;
        }

        bool isAllInside = true;

        for (int i = 0; i < boxPrimitive.Vertices.Length; i++)
        {
            if (fpmath.distance(boxPrimitive.Vertices[i], annulusPrimitive.AnnulusCenter) >
                annulusPrimitive.InternalDiameter)
            {
                isAllInside = false;
                break;
            }
        }

        // 2. 盒体的所有顶点在环形内径为半径的球体内，环形与盒体无碰撞
        if (isAllInside)
        {
            return false;
        }

        // 3. 判断矩形的边与扇形的左边和右边线段的交点，如果存在交点，且交点不在在扇形的镂空扇形区域内，则返回发生碰撞，反之，进入下一步
        // 4. 依次判断矩形的4边是否与扇形的两边所在射线相交，且交点不在镂空扇形范围内，存在任意一条，则返回发生碰撞，反之未发生碰撞

        SectorPrimitive interSector = SectorPrimitive.Create(annulusPrimitive.AnnulusCenter,
            annulusPrimitive.Quaternion,
            annulusPrimitive.Angle * (fp)Mathf.Rad2Deg, annulusPrimitive.InternalDiameter);

        List<int> pointOrders = new List<int>(polygonPrimitive.verSort);
        pointOrders.Add(pointOrders[0]);

        if (annulusPrimitive.Angle * (fp)Mathf.Rad2Deg >= 180)
        {
            annulusPrimitive = AnnulusPrimitive.Create(annulusPrimitive.AnnulusCenter,
                annulusPrimitive.Quaternion *
                fpmath1.EulerXYZ(new fp3(0, -annulusPrimitive.Angle * (fp)Mathf.Rad2Deg / 4, 0)),
                annulusPrimitive.Angle * (fp)Mathf.Rad2Deg / 2, annulusPrimitive.OuterDiameter,
                annulusPrimitive.InternalDiameter);

            outerSector = SectorPrimitive.Create(annulusPrimitive.AnnulusCenter,
                annulusPrimitive.Quaternion *
                fpmath1.EulerXYZ(new fp3(0, -annulusPrimitive.Angle * (fp)Mathf.Rad2Deg / 4, 0)),
                annulusPrimitive.Angle * (fp)Mathf.Rad2Deg / 2, annulusPrimitive.OuterDiameter);

            interSector = SectorPrimitive.Create(annulusPrimitive.AnnulusCenter,
                annulusPrimitive.Quaternion *
                fpmath1.EulerXYZ(new fp3(0, -annulusPrimitive.Angle * (fp)Mathf.Rad2Deg / 4, 0)),
                annulusPrimitive.Angle * (fp)Mathf.Rad2Deg / 2, annulusPrimitive.InternalDiameter);
        }

        if (!outerSector.InternalCheckPrimitive() || !interSector.InternalCheckPrimitive())
        {
            return false;
        }

        for (int i = 0; i < pointOrders.Count - 1; i++)
        {
            fp3 start = polygonPrimitive.Vertices[pointOrders[i]];
            fp3 end = polygonPrimitive.Vertices[pointOrders[i + 1]];

            // 判断边与扇形的左边和右边线段的交点，如果存在交点，且交点不在在扇形的镂空扇形区域内，则返回发生碰撞
            if (IsLineSegIntersectLineSeg(start, end, annulusPrimitive.AnnulusCenter, outerSector.EdgeVertices[0],
                    out fp3 leftIntersect))
            {
                if (!IsSectorHasPoint(interSector, leftIntersect))
                {
                    return true;
                }
            }

            if (IsLineSegIntersectLineSeg(start, end, annulusPrimitive.AnnulusCenter, outerSector.EdgeVertices[1],
                    out fp3 rightIntersect))
            {
                if (!IsSectorHasPoint(interSector, rightIntersect))
                {
                    return true;
                }
            }

            // 依次判断矩形的4边是否与扇形的两边所在射线相交，且交点不在镂空扇形范围内，存在任意一条，则返回发生碰撞，反之未发生碰撞；
            if (IsLineIntersectLine(start, end, annulusPrimitive.AnnulusCenter, outerSector.EdgeVertices[0],
                    out fp3 intersectPosLeft) &&
                fpmath.dot(intersectPosLeft - annulusPrimitive.AnnulusCenter,
                    interSector.EdgeVertices[0] - interSector.SectorCenter) >= 0)
            {
                // (Vector3.Dot(intersectPosLeft - start, end - start) >= 0 && Vector3.Dot(intersectPosLeft - end, start - end) >= 0)) {
                if (!IsSectorHasPoint(interSector, intersectPosLeft))
                {
                    return true;
                }
            }

            if (IsLineIntersectLine(start, end, annulusPrimitive.AnnulusCenter, outerSector.EdgeVertices[1],
                    out fp3 intersectPosRight) &&
                fpmath.dot(intersectPosRight - annulusPrimitive.AnnulusCenter,
                    interSector.EdgeVertices[1] - interSector.SectorCenter) >= 0)
            {
                // (Vector3.Dot(intersectPosRight - start, end - start) >= 0 && Vector3.Dot(intersectPosRight - end, end - start) >= 0)){
                if (!IsSectorHasPoint(interSector, intersectPosRight))
                {
                    return true;
                }
            }
        }

        outerSector.OnDispose();
        interSector.OnDispose();
        polygonPrimitive.OnDispose();
        return false;
    }

    public static bool AnnulusAndSphere(AnnulusPrimitive annulusPrimitive, SpherePrimitive spherePrimitive)
    {
        SectorPrimitive outerSector = SectorPrimitive.Create(annulusPrimitive.AnnulusCenter,
            annulusPrimitive.Quaternion,
            annulusPrimitive.Angle * (fp)Mathf.Rad2Deg,
            annulusPrimitive.OuterDiameter);

        // 1. 以环形外径为半径的完整扇形与球体无碰撞，环形与球体无碰撞
        if (!SectorAndSphere(outerSector, spherePrimitive) || !outerSector.InternalCheckPrimitive())
        {
            return false;
        }

        fp3 vec = annulusPrimitive.AnnulusCenter - spherePrimitive.SphereCenter;

        // 2. 以环形内径为半径的球体完全包含检测的球体，环形与球体无碰撞
        if (fpmath1.magnitude(vec) + spherePrimitive.Radius <= annulusPrimitive.InternalDiameter)
        {
            return false;
        }

        SectorPrimitive internalSector = SectorPrimitive.Create(annulusPrimitive.AnnulusCenter,
            annulusPrimitive.Quaternion,
            annulusPrimitive.Angle * (fp)Mathf.Rad2Deg,
            annulusPrimitive.InternalDiameter);

        // 环形角度大于180度，使用环形第一二象限的半环形做检测判定
        if (internalSector.Angle > 180 * (fp)Mathf.Deg2Rad)
        {
            internalSector = SectorPrimitive.Create(internalSector.SectorCenter,
                internalSector.Quaternion *
                fpmath1.EulerXYZ(new fp3(0, -internalSector.Angle * (fp)Mathf.Rad2Deg / 4, 0)),
                internalSector.Angle * (fp)Mathf.Rad2Deg / 2, internalSector.Radius);
        }

        if (!internalSector.InternalCheckPrimitive())
        {
            return false;
        }

        fp3 localSpCen = annulusPrimitive.Transform2Local(spherePrimitive.SphereCenter);
        localSpCen.z = fpmath.abs(localSpCen.z);

        fp3 symmetricSpCen = annulusPrimitive.Transform2World(localSpCen);

        fp pop = fpmath.dot(symmetricSpCen - internalSector.SectorCenter,
                        internalSector.LocalUnitX - internalSector.SectorCenter)
                    / (fpmath1.magnitude((symmetricSpCen - internalSector.SectorCenter)) *
                       fpmath1.magnitude((internalSector.LocalUnitX - internalSector.SectorCenter)));

        fp degree = fpmath1.acos(pop);
        
        degree *= 2;

        // 3. 球体投影球心与环形平面组成的扫掠角小于环形扫掠角,且球心到环形内径圆弧最短距离小于球体半径，环形与球体碰撞
        if (degree <= annulusPrimitive.Angle && fpmath.distance(symmetricSpCen, internalSector.SectorCenter) >=
            annulusPrimitive.OuterDiameter)
        {
            return true;
        }

        // 球心与环形中心连线与环形平面组成的扫掠角小于环形扫掠角，球心在环形内外径组成的两个球体之间时
        if (degree <= annulusPrimitive.Angle &&
            fpmath.distance(symmetricSpCen, internalSector.SectorCenter) >= annulusPrimitive.InternalDiameter &&
            fpmath.distance(symmetricSpCen, internalSector.SectorCenter) <= annulusPrimitive.OuterDiameter)
        {
            // if (Vector3.Distance(symmetricSpCen, internalSector.EdgeVertices[0]) <= spherePrimitive.Radius){
            //     return true;
            // }
            //
            // return false;
            // 前一个方法中，外扇形与胶囊必定相交才能进行到这里
            // 在这个前提下球心与环形在同一个平面且球心在环形中，那么必定相交
            return true;
        }

        // 4. 将球体对称到环形第一二象限，如果球心与环形内径第一象限顶点的距离小于球体半径，环形与球体碰撞
        // 4. 如果球心与球体球心在环形区域部分且球心到环形内径圆弧最短距离小于球体半径，环形与球体碰撞
        fp dis = fpmath.distance(symmetricSpCen, internalSector.EdgeVertices[0]);

        if (fpmath.distance(symmetricSpCen, internalSector.EdgeVertices[0]) <= spherePrimitive.Radius)
        {
            return true;
        }

        // 5. 球心与环形圆心组形成的夹角小于环形内径扇形的扫掠角，且球心与环形内径第一象限顶点的距离大于球体半径，环形与球体无碰撞

        if (degree <= annulusPrimitive.Angle &&
            fpmath.distance(symmetricSpCen, internalSector.SectorCenter) <= internalSector.Radius)
        {
            return false;
        }

        // 6. 球心在环形第一二象限的法线上的投影点在环形第一二象限的顶点外，环形与球体碰撞，反之无碰撞
        fp3 projectPoint = PointProjectionOnLineSeg(internalSector.SectorCenter, internalSector.EdgeVertices[0],
            symmetricSpCen);

        bool result =
            fpmath.dot(internalSector.EdgeVertices[0] - internalSector.SectorCenter,
                projectPoint - internalSector.SectorCenter) >= 0 &&
            fpmath.dot(projectPoint - internalSector.EdgeVertices[0],
                projectPoint - internalSector.SectorCenter) >= 0;

        outerSector.OnDispose();
        internalSector.OnDispose();

        return result;
    }

    public static bool AnnulusAndCapusle(AnnulusPrimitive annulusPrimitive, CapsulePrimitive capsulePrimitive)
    {
        SectorPrimitive outerSector = SectorPrimitive.Create(annulusPrimitive.AnnulusCenter,
            annulusPrimitive.Quaternion,
            annulusPrimitive.Angle * (fp)Mathf.Rad2Deg, annulusPrimitive.OuterDiameter);

        if (!annulusPrimitive.InternalCheckPrimitive())
        {
            return false;
        }

        if (!SectorAndCapsule(outerSector, capsulePrimitive, out fp3 bestA))
        {
            return false;
        }

        return AnnulusAndSphere(annulusPrimitive,
            SpherePrimitive.Create(bestA, capsulePrimitive.Radius, capsulePrimitive.CapsuleQuaternion));
    }

    #endregion

    #endregion

    #region Helpers

    // 二维
    // 计算线段与点的最短平方距离
    // x0 线段起点
    // u  线段方向至末端点
    // x  任意点
    static fp SegmentPointSqrDistance(fp3 x0, fp3 u, fp3 x)
    {
        fp t = fpmath.dot(x - x0, u) / fpmath1.sqrMagnitude(u);
        return fpmath1.sqrMagnitude(x - (x0 + fpmath.clamp(t, 0, 1) * u));
    }

    // 二维
    // 胶囊与圆盘相交测试
    // x0 胶囊线段起点
    // u  胶囊线段方向至末端点
    // cr 胶囊半径
    // c 圆盘圆心
    // r 圆盘半径
    static bool IsCapsuleDiskIntersect(
        fp3 x0, fp3 u, fp cr,
        fp3 c, fp r)
    {
        return SegmentPointSqrDistance(x0, u, c) <= (cr + r) * (cr + r);
    }

    private static void ClosestPointBoxSphere(SpherePrimitive p, BoxPrimitive b, out fp3 vBox,
        out fp3 vSphere)
    {
        // 限制球心与盒体中心点连线的向量的每个轴分量的大小
        // 得到盒体表面上距离球体最近的点
        // Vector3 d = p.SphereCenter - b.boxCenter;
        // vBox = b.boxCenter;
        // vBox += Vector3.Max(-b.HalfSize, Vector3.Min(b.HalfSize, d));

        fp3 d = b.Transform.TransformInverse(p.SphereCenter);
        vBox = fp3.zero;
        vBox += fpmath.max(-b.HalfSize, fpmath.min(b.HalfSize, d));
        vBox = b.Transform * vBox;

        // Vector3 v = p.Transform.TransformInverse(vBox).normalized * p.Radius;
        // vSphere = p.Transform * v;

        fp3 s = p.Transform.TransformInverse(b.boxCenter);
        vSphere = fp3.zero;
        vSphere += fpmath.max(-p.Radius * p.Scale, fpmath.min(p.Radius * p.Scale, s));
        vSphere = p.Transform * vSphere;
    }

    /// <summary>
    /// 判断线段与平面是否相交，返回交点
    /// </summary>
    /// <param name="pointA"></param>
    /// <param name="pointB"></param>
    /// <param name="planePoint"></param>
    /// <param name="planeNormal"></param>
    /// <param name="footPoint">线段与平面的交点</param>
    /// <returns>线段与平面是否相交</returns>
    public static bool LineSegmentFootPointOnPlane(fp3 pointA, fp3 pointB, fp3 planePoint,
        fp3 planeNormal,
        out fp3 footPoint)
    {
        fp3 ab = pointB - pointA;

        // 1. 判断线段与平面是否相交，不相交直接返回false
        // a点在平面上的投影点 fpA
        fp3 fpA = fp3.zero;
        PointProjectionOnPlane(planeNormal, planePoint, pointA, out fpA);

        // b点在平面上的投影点 fpB
        fp3 fpB = fp3.zero;
        PointProjectionOnPlane(planeNormal, planePoint, pointB, out fpB);

        fp3 pA = pointA - fpA;
        fp3 pB = pointB - fpB;

        // a、b点到各自垂足所形成的两个向量平行
        // 线段与平面不相交
        if (fpmath.dot(pA, pB) > 0)
        {
            footPoint = Vector3.negativeInfinity.ToFp3();
            return false;
        }

        fp ma = (fpmath1.magnitude(pA) * fpmath1.magnitude(ab)) / (fpmath1.magnitude(pB) + fpmath1.magnitude(pA));
        footPoint = pointA + ma * fpmath.normalize(ab);
        return true;
    }

    /// <summary>
    /// 点在线段上的投影点是否在线段内
    /// </summary>
    /// <param name="point"></param>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="isP1">点point是否在线段起点右侧</param>
    /// <param name="isP2">点point是否在线段终点左侧</param>
    /// <returns>是否在线段内</returns>
    private static bool IsPointProjectionInsideSegment(fp3 point, fp3 p1, fp3 p2, out bool isP1,
        out bool isP2)
    {
        isP1 = true;
        isP2 = true;

        // 求Cos∠PP1P2
        fp3 pp1 = point - p1;
        fp3 p12 = p2 - p1;

        if (fpmath.dot(pp1, p12) < 0)
        {
            isP1 = false;
        }

        // 求Cos∠PP2P1
        fp3 pp2 = point - p2;
        fp3 p21 = p1 - p2;

        if (fpmath.dot(pp2, p21) >= 0)
        {
            isP2 = false;
        }

        return isP1 && isP2;
    }

    /// <summary>
    /// 圆与直线的交点
    /// 需要保证直线与圆必定在一个二维平面内
    /// </summary>
    /// <param name="circleCenter"></param>
    /// <param name="radius"></param>
    /// <param name="lineP1"></param>
    /// <param name="lineP2"></param>
    /// <param name="point1"></param>
    /// <param name="point2"></param>
    private static void CircleIntersectLine(fp3 circleCenter, fp radius, fp3 lineP1, fp3 lineP2,
        out fp3 point1,
        out fp3 point2)
    {
        point1 = Vector3.negativeInfinity.ToFp3();
        point2 = Vector3.negativeInfinity.ToFp3();
        fp3 line = lineP2 - lineP1;

        // 利用叉乘的二维意义，通过平行四边形的面积求圆心点到直线的距离
        fp3 cross = fpmath.cross(line, circleCenter - lineP1);
        fp dis = fpmath.abs((cross.x + cross.y + cross.z) / fpmath1.magnitude(line));

        if (dis > radius)
        {
            return;
        }

        fp3 pointProject = PointProjectionOnLineSeg(lineP1, lineP2, circleCenter);
        fp3 nor = line / fpmath1.magnitude(line);
        fp len = fpmath.sqrt(radius * radius - fpmath1.magnitude((pointProject - circleCenter)));
        point1 = pointProject + nor * len;
        point2 = pointProject - nor * len;
    }

    /// <summary>
    /// 点在直线上的投影点坐标
    /// </summary>
    /// <param name="lineP1"></param>
    /// <param name="lineP2"></param>
    /// <param name="p"></param>
    /// <returns></returns>
    public static fp3 PointProjectionOnLineSeg(fp3 lineP1, fp3 lineP2, fp3 p)
    {
        //投影 对于给定的三个点p1、p2、p，从点p向通过
        //p1、p2的直线引一条垂线，求垂足x的坐标。（点p在直线p1p2上的投影） 
        fp3 line = lineP2 - lineP1;

        fp r = fpmath.dot(p - lineP1, line) / fpmath1.sqrMagnitude(line);
        
        return lineP1 + line * r;
    }

    /// <summary>
    /// 计算AB与CD两条线段的交点.
    /// </summary>
    /// <param name="line1P1">A点</param>
    /// <param name="line1P2">B点</param>
    /// <param name="line2P1">C点</param>
    /// <param name="line2P2">D点</param>
    /// <param name="intersectPos">AB与CD的交点</param>
    /// <returns>是否相交 true:相交 false:未相交</returns>
    private static bool IsLineSegIntersectLineSeg(fp3 line1P1, fp3 line1P2, fp3 line2P1,
        fp3 line2P2, out fp3 intersectPos)
    {
        intersectPos = fp3.zero;

        // 精度
        fp Epsilon = (fp)0.000001f;

        // 将线段两个端点排序，保证两条线段中p1是起点p2是终点
        fp3 l1P1 = ComparePosition(line1P1, line1P2) ? line1P1 : line1P2;
        fp3 l1P2 = !ComparePosition(line1P1, line1P2) ? line1P1 : line1P2;
        fp3 l2P1 = ComparePosition(line2P1, line2P2) ? line2P1 : line2P2;
        fp3 l2P2 = !ComparePosition(line2P1, line2P2) ? line2P1 : line2P2;
        line1P1 = l1P1;
        line1P2 = l1P2;
        line2P1 = l2P1;
        line2P2 = l2P2;

        fp3 ab = line1P2 - line1P1;
        fp3 ca = line1P1 - line2P1;
        fp3 cd = line2P2 - line2P1;
        fp3 ad = line2P2 - line1P1;
        fp3 cb = line1P2 - line2P1;

        fp3 v1 = fpmath.cross(ca, cd);

        if (fpmath.abs(fpmath.dot(v1, ab)) > (fp)0.001f)
        {
            // 不共面
            return false;
        }

        if (fpmath1.sqrMagnitude(fpmath.cross(ab, cd)) <= (fp)0.001f)
        {
            // 平行
            // 如果共线，有重合部分则相交
            if (fpmath1.magnitude(fpmath.cross(ca, cb))<= (fp)0.001f)
            {
                // 第一条起点小于第二条起点，第一条终点大于第二条起点
                if (ComparePosition(line1P1, line2P1) && !ComparePosition(line1P2, line2P1))
                {
                    return true;
                }

                // 第二条起点小于第一条起点，第二条终点大于第一条起点
                else if (ComparePosition(line2P1, line1P1) && !ComparePosition(line2P2, line1P1))
                {
                    return true;
                }
            }

            return false;
        }

        // 快速排斥
        if (fpmath.min(line1P1.x, line1P2.x) > fpmath.max(line2P1.x, line2P2.x)
            || fpmath.max(line1P1.x, line1P2.x) < fpmath.min(line2P1.x, line2P2.x)
            ||fpmath.min(line1P1.y, line1P2.y) > fpmath.max(line2P1.y, line2P2.y)
            || fpmath.max(line1P1.y, line1P2.y) < fpmath.min(line2P1.y, line2P2.y)
            || fpmath.min(line1P1.z, line1P2.z) > fpmath.max(line2P1.z, line2P2.z)
            || fpmath.max(line1P1.z, line1P2.z) < fpmath.min(line2P1.z, line2P2.z)
           )
            return false;

        // 跨立试验
        if (fpmath.dot(fpmath.cross(-ca, ab), fpmath.cross(ab, ad)) > 0
            && fpmath.dot(fpmath.cross(ca, cd), fpmath.cross(cd, cb)) > 0)
        {
            fp3 v2 = fpmath.cross(cd, ab);
            fp ratio = fpmath1.sqrMagnitude(fpmath.dot(v1, v2) / v2);
            intersectPos = line1P1 + ab * ratio;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 求空间中多边形的重心坐标
    /// </summary>
    /// <param name="polygon"></param>
    /// <param name="graPoint"></param>
    /// <returns>重心坐标是否在第一二象限</returns>
    private static bool GetGravityPoint(PolygonPrimitive polygon, out fp3 graPoint)
    {
        fp n, i;
        fp x1, y1, x2, y2, x3, y3;
        fp sum_x = 0, sum_y = 0, sum_s = 0;

        x1 = polygon.Transform.TransformInverse(polygon.Vertices[polygon.verSort[0]]).x;
        y1 = polygon.Transform.TransformInverse(polygon.Vertices[polygon.verSort[0]]).z;
        x2 = polygon.Transform.TransformInverse(polygon.Vertices[polygon.verSort[1]]).x;
        y2 = polygon.Transform.TransformInverse(polygon.Vertices[polygon.verSort[1]]).z;

        n = polygon.Vertices.Length;
        int k = 2;

        for (i = 1; i <= n - 2; i++)
        {
            x3 = polygon.Transform.TransformInverse(polygon.Vertices[polygon.verSort[k]]).x;
            y3 = polygon.Transform.TransformInverse(polygon.Vertices[polygon.verSort[k]]).z;

            fp s = ((x2 - x1) * (y3 - y1) - (x3 - x1) * (y2 - y1)) / (fp)2.0;
            sum_x += (x1 + x2 + x3) * s;
            sum_y += (y1 + y2 + y3) * s;
            sum_s += s;
            x2 = x3;
            y2 = y3;
            k++;
        }

        graPoint = new fp3((sum_x / sum_s / 3), 0, (sum_y / sum_s / 3));
        
        return graPoint.z > 0;
    }

    /// <summary>
    /// 比较坐标大小，返回p1是否小于p2
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns>p1是否小于p2</returns>
    private static bool ComparePosition(fp3 p1, fp3 p2)
    {
        // 精度
        double Epsilon = 1e-6;

        if (fpmath.abs(p1.x - p2.x) > (fp)0.001f)
        {
            return p1.x <= p2.x;
        }
        else if (fpmath.abs(p1.y - p2.y) > (fp)0.001f)
        {
            return p1.y <= p2.y;
        }
        else
        {
            return p1.z <= p2.z;
        }
    }

    /// <summary>
    /// 旋转卡壳算法，求一组点中距离最远的点对,（23.05.11 才知道只能用于凸包！无法处理共线的点集！
    /// </summary>
    /// <param name="points"></param>
    /// <param name="m"></param>
    /// <param name="maxindex1"></param>
    /// <param name="maxindex2"></param>
    /// <returns></returns>
    private static fp RotatingCalipers(fp3[] points, int m, ref int maxindex1, ref int maxindex2)
    {
        fp maxdist = 0, d1, d2;

        // 添加起点
        List<fp3> lists = new List<fp3>(points);
        lists.Add(points[0]);
        int i, j = 1; // i为慢指针,j为快指针

        for (i = 0; i < m; ++i)
        {
            while (fpmath.abs(PrimitiveExtension.Determinant(lists[i] - lists[i + 1], lists[j + 1] - lists[i + 1])) >
                   fpmath.abs(PrimitiveExtension.Determinant(lists[i] - lists[i + 1], lists[j] - lists[i + 1])))
                j = (j + 1) % m; // 以面积判断,面积大则说明离平行线远一些

            d1 = fpmath.distance(lists[i], lists[j]);

            if (d1 > maxdist)
            {
                maxdist = d1;
                maxindex1 = i;
                maxindex2 = j;
            }

            d2 = fpmath.distance(lists[i + 1], lists[j]);

            if (d2 > maxdist)
            {
                maxdist = d2;
                maxindex1 = i + 1;
                maxindex2 = j;
            }
        }

        maxindex1 = maxindex1 == lists.Count - 1 ? 0 : maxindex1;
        maxindex2 = maxindex2 == lists.Count - 1 ? 0 : maxindex2;

        return maxdist;
    }

    /// <summary>
    /// 求一组点中距离最远的点对
    /// </summary>
    /// <param name="points"></param>
    /// <param name="m"></param>
    /// <param name="maxindex1"></param>
    /// <param name="maxindex2"></param>
    /// <returns></returns>
    private static fp FarthestPoints(fp3[] points, int m, ref int maxindex1, ref int maxindex2)
    {
        fp d, maxdist = 0;

        //List<Vector3> lists = new List<Vector3>(points);

        for (int i = 0; i < m; ++i)
        {
            for (int j = i + 1; j < m; ++j)
            {
                d = fpmath.distance(points[i], points[j]);

                if (d > maxdist)
                {
                    maxdist = d;
                    maxindex1 = i;
                    maxindex2 = j;
                }
            }
        }

        return maxdist;
    }

    /// <summary>
    /// 点在平面上的投影点
    /// </summary>
    /// <param name="planeNormal"></param>
    /// <param name="planePoint"></param>
    /// <param name="oriPoint"></param>
    /// <param name="fpB"></param>
    private static void PointProjectionOnPlane(fp3 planeNormal, fp3 planePoint, fp3 oriPoint,
        out fp3 fpB)
    {
        // b点在平面上的投影点 fpB
        fpB = fp3.zero;

        fpB.x = (planeNormal.x * planeNormal.y * planePoint.y + planeNormal.y * planeNormal.y * oriPoint.x -
                 planeNormal.x * planeNormal.y * oriPoint.y + planeNormal.x * planeNormal.z * planePoint.z +
                 planeNormal.z * planeNormal.z * oriPoint.x - planeNormal.x * planeNormal.z * oriPoint.z +
                 planeNormal.x * planeNormal.x * planePoint.x) /
                (planeNormal.x * planeNormal.x + planeNormal.y * planeNormal.y + planeNormal.z * planeNormal.z);

        fpB.y = (planeNormal.y * planeNormal.z * planePoint.z + planeNormal.z * planeNormal.z * oriPoint.y -
                 planeNormal.y * planeNormal.z * oriPoint.z + planeNormal.y * planeNormal.x * planePoint.x +
                 planeNormal.x * planeNormal.x * oriPoint.y - planeNormal.x * planeNormal.y * oriPoint.x +
                 planeNormal.y * planeNormal.y * planePoint.y) /
                (planeNormal.x * planeNormal.x + planeNormal.y * planeNormal.y + planeNormal.z * planeNormal.z);

        fpB.z = (planeNormal.x * planeNormal.z * planePoint.x + planeNormal.x * planeNormal.x * oriPoint.z -
                 planeNormal.x * planeNormal.z * oriPoint.x + planeNormal.y * planeNormal.z * planePoint.y +
                 planeNormal.y * planeNormal.y * oriPoint.z - planeNormal.y * planeNormal.z * oriPoint.y +
                 planeNormal.z * planeNormal.z * planePoint.z) /
                (planeNormal.x * planeNormal.x + planeNormal.y * planeNormal.y + planeNormal.z * planeNormal.z);
    }

    /*/// <summary>
    /// 线段与直线是否相交
    /// </summary>
    /// <param name="segPt1"></param>
    /// <param name="segPt2"></param>
    /// <param name="linePt1"></param>
    /// <param name="linePt2"></param>
    /// <returns></returns>
    private static bool IsLineSegIntersectLine(Vector3 segPt1, Vector3 segPt2, Vector3 linePt1, Vector3 linePt2)
    {
        bool result = false;

        double temp1 = Vector3.Dot(linePt2 - linePt1, segPt1 - linePt1)
                       - Vector3.Dot(linePt2 - linePt1, segPt1 - linePt1);


        double temp2 = Vector3.Dot(linePt2 - linePt1, segPt2 - linePt1)
                       - Vector3.Dot(linePt2 - linePt1, segPt2 - linePt1);

        if ((temp1 >= 0 & temp2 <= 0) | (temp1 <= 0 & temp2 >= 0))
        {
            result = true;
        }

        return result;
    }*/

    /// <summary>
    /// 直线与直线是否相交，并求交点
    /// </summary>
    /// <param name="line1P1"></param>
    /// <param name="line1P2"></param>
    /// <param name="line2P1"></param>
    /// <param name="line2P2"></param>
    /// <param name="intersectPos"></param>
    /// <returns></returns>
    private static bool IsLineIntersectLine(fp3 line1P1, fp3 line1P2, fp3 line2P1, fp3 line2P2,
        out fp3 intersectPos)
    {
        intersectPos = fp3.zero;

        fp3 ab = line1P2 - line1P1;
        fp3 ca = line1P1 - line2P1;
        fp3 cd = line2P2 - line2P1;
        fp3 ad = line2P2 - line1P1;
        fp3 cb = line1P2 - line2P1;
        fp3 ac = line2P1 - line1P1;

        fp3 v1 = fpmath.cross(ca, cd);

        if (fpmath.abs(fpmath.dot(v1, ab)) > (fp)1e-6)
        {
            // 不共面
            return false;
        }

        if (fpmath1.sqrMagnitude(fpmath.cross(ab, cd)) <= (fp)1e-6)
        {
            // 平行
            // 如果共线，有重合部分则相交
            if (fpmath1.magnitude(fpmath.cross(ca, cb))<= (fp)1e-6)
            {
                // 第一条起点小于第二条起点，第一条终点大于第二条起点
                if (ComparePosition(line1P1, line2P1) && !ComparePosition(line1P2, line2P1))
                {
                    return true;
                }

                // 第二条起点小于第一条起点，第二条终点大于第一条起点
                else if (ComparePosition(line2P1, line1P1) && !ComparePosition(line2P2, line1P1))
                {
                    return true;
                }
            }

            return false;
        }

        fp3 vecS1 = fpmath.cross(ab, 
            
            
            cd); // 有向面积1
        fp3 vecS2 = fpmath.cross(ac, cd); // 有向面积2
        fp num = fpmath.dot(ac, vecS1);

        if (fpmath.abs(fpmath1.sqrMagnitude(vecS1)) <= (fp)1e-6)
        {
            return false;
        }

        fp num2 = fpmath.dot(vecS2, vecS1) / fpmath1.sqrMagnitude(vecS1);

        if (num2 > 1 || num < 0)
        {
            return false; //num2的大小还可以判断是延长线相交还是线段相交
        }

        intersectPos = line1P1 + v1 * num2;
        return true;
    }

    /// <summary>
    /// 点到射线的最近点坐标
    /// </summary>
    /// <param name="point"></param>
    /// <param name="direct"></param>
    /// <param name="targetPoint"></param>
    /// <returns></returns>
    public static fp3 NearestPointOnRay(fp3 point, fp3 direct, fp3 targetPoint)
    {
        fp3 vector = targetPoint - point;
        direct = fpmath.normalize(direct);
        fp dotValue = fpmath.dot(vector, direct);

        if (dotValue <= 0)
        {
            return point;
        }

        fp3 tarPoint = point + dotValue * direct;
        return tarPoint;
    }

    private static fp ABS(fp f)
    {
        return f < 0 ? -f : f;
    }

    #endregion
}