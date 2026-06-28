using System.Collections.Generic;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

public struct SupportInfo
{
    public fp3 vertice1;
    public fp3 vertice2;

    public SupportInfo(fp3 one, fp3 two)
    {
        vertice1 = one;
        vertice2 = two;
    }
}

public class Simplex
{
    public struct SimplexPlane
    {
        public SimplexPlane(fp3 normal, fp distance, fp3 A, fp3 B, fp3 C)
        {
            this.normal = normal;
            this.sqrDistance = distance;
            this.A = A;
            this.B = B;
            this.C = C;
            this.originInPlane = OriginInPlane(normal, A, B, C);
        }

        public fp3 normal;
        public fp sqrDistance;
        public fp3 A;
        public fp3 B;
        public fp3 C;
        public bool originInPlane;

        public static bool OriginInPlane(fp3 normal, fp3 A, fp3 B, fp3 C)
        {
            fp3 AF = normal - A;
            fp signPAB = fpmath1.sign(fpmath.dot(normal, fpmath.cross(B - A, AF)));
            fp signPAC = fpmath1.sign(fpmath.dot(normal, fpmath.cross(AF, C - A)));
            if ((signPAB * signPAC) == -1) return false;

            fp signPBC = fpmath1.sign(fpmath.dot(normal, fpmath.cross(C - B, normal - B)));
            if ((signPBC * signPAB) == -1 || (signPBC * signPAC) == -1) return false;

            return true;
        }
    }

    public List<fp3> points;
    public List<SimplexPlane> planes;


    public fp3 A
    {
        get => points[0];
    }

    public fp3 B
    {
        get => points[1];
    }

    public fp3 C
    {
        get => points[2];
    }

    public fp3 D
    {
        get => points[3];
    }

    public int Count
    {
        get => points.Count;
    }

    public Simplex()
    {
        points = new List<fp3>();
        planes = new List<SimplexPlane>();
    }

    public void Clear()
    {
        points.Clear();
        planes.Clear();
    }

    public void Add(fp3 point)
    {
        points.Add(point);
    }

    public void RemoveAt(int index)
    {
        points.RemoveAt(index);
    }

    public bool ContainsPoint(fp3 point)
    {
        if (Count < 4) return false;

        fp3 AB = B - A;
        fp3 BC = C - B;
        fp3 AC = C - A;
        fp3 CD = D - C;
        fp3 AD = D - A;
        fp3 DB = B - D;
        fp3 BD = D - B;
        fp3 DC = C - D;

        fp3 NormalABC = fpmath.cross(AB, BC);
        fp3 NormalACD = fpmath.cross(AC, CD);
        fp3 NormalADB = fpmath.cross(AD, DB);
        fp3 NormalBDC = fpmath.cross(BD, DC);

        fp3 AP = point - A;
        fp3 BP = point - B;

        //可能为0
        int signABC = (int)fpmath1.sign(fpmath.dot(NormalABC, AP));
        if (signABC == 0) return false;

        int signACD = (int)fpmath1.sign(fpmath.dot(NormalACD, AP));
        if (signACD == 0 || signACD * signABC == -1) return false;

        int signADB = (int)fpmath1.sign(fpmath.dot(NormalADB, AP));
        if (signADB == 0 || signADB * signABC == -1) return false;

        int signBDC = (int)fpmath1.sign(fpmath.dot(NormalBDC, BP));
        if (signBDC == 0 || signBDC * signABC == -1) return false;

        return true;
    }

    public SimplexPlane? FindClosestPlane()
    {
        if (planes.Count == 0) return null;

        int index = 0;

        for (int i = 1; i < planes.Count; i++)
        {
            index = planes[index].sqrDistance < planes[i].sqrDistance ? index : i;
        }

        return planes[index];
    }

    public void InitPlanes()
    {
        if (points.Count < 4) return;

        planes.Clear();
        fp3 NormalABC = GJKDetecotor.FootPointOnPlane(A, B, C, fp3.zero);
        fp3 NormalACD = GJKDetecotor.FootPointOnPlane(A, C, D, fp3.zero);
        fp3 NormalADB = GJKDetecotor.FootPointOnPlane(A, D, B, fp3.zero);
        fp3 NormalBDC = GJKDetecotor.FootPointOnPlane(B, D, C, fp3.zero);

        SimplexPlane planeABC = new SimplexPlane(NormalABC, fpmath1.sqrMagnitude(NormalABC), A, B, C);
        SimplexPlane planeACD = new SimplexPlane(NormalACD, fpmath1.sqrMagnitude(NormalACD), A, C, D);
        SimplexPlane planeADB = new SimplexPlane(NormalADB, fpmath1.sqrMagnitude(NormalADB), A, D, B);
        SimplexPlane planeBDC = new SimplexPlane(NormalBDC, fpmath1.sqrMagnitude(NormalBDC), B, D, C);

        planes.Add(planeABC);
        planes.Add(planeACD);
        planes.Add(planeADB);
        planes.Add(planeBDC);
    }

    public void GeneratePlanes()
    {
        if (points.Count < 3) return;

        planes.Clear();

        if (points.Count == 3)
        {
            fp3 NormalABC = GJKDetecotor.FootPointOnPlane(A, B, C, fp3.zero);
            SimplexPlane planeABC = new SimplexPlane(NormalABC, fpmath1.sqrMagnitude(NormalABC), A, B, C);
            planes.Add(planeABC);
        }
        else
        {
            fp3 NormalABC = GJKDetecotor.FootPointOnPlane(A, B, C, fp3.zero);
            fp3 NormalACD = GJKDetecotor.FootPointOnPlane(A, C, D, fp3.zero);
            fp3 NormalADB = GJKDetecotor.FootPointOnPlane(A, D, B, fp3.zero);
            fp3 NormalBDC = GJKDetecotor.FootPointOnPlane(B, D, C, fp3.zero);

            SimplexPlane planeABC = new SimplexPlane(NormalABC, fpmath1.sqrMagnitude(NormalABC), A, B, C);
            SimplexPlane planeACD = new SimplexPlane(NormalACD, fpmath1.sqrMagnitude(NormalACD), A, C, D);
            SimplexPlane planeADB = new SimplexPlane(NormalADB, fpmath1.sqrMagnitude(NormalADB), A, D, B);
            SimplexPlane planeBDC = new SimplexPlane(NormalBDC, fpmath1.sqrMagnitude(NormalBDC), B, D, C);

            planes.Add(planeABC);
            planes.Add(planeACD);
            planes.Add(planeADB);
            planes.Add(planeBDC);
        }
    }


    public void InsertPlanePoint(fp3 point, SimplexPlane plane)
    {
        fp3 NormalPAB = GJKDetecotor.FootPointOnPlane(point, plane.A, plane.B, fp3.zero);
        fp3 NormalPAC = GJKDetecotor.FootPointOnPlane(point, plane.A, plane.C, fp3.zero);
        fp3 NormalPBC = GJKDetecotor.FootPointOnPlane(point, plane.B, plane.C, fp3.zero);

        SimplexPlane planePAB = new SimplexPlane(NormalPAB, fpmath1.sqrMagnitude(NormalPAB), point, plane.A, plane.B);
        SimplexPlane planePAC = new SimplexPlane(NormalPAC, fpmath1.sqrMagnitude(NormalPAC), point, plane.A, plane.C);
        SimplexPlane planePBC = new SimplexPlane(NormalPBC, fpmath1.sqrMagnitude(NormalPBC), point, plane.B, plane.C);

        RemovePlane(plane);


        if (planePAB.originInPlane)
            planes.Add(planePAB);

        if (planePAC.originInPlane)
            planes.Add(planePAC);

        if (planePBC.originInPlane)
            planes.Add(planePBC);

        Add(point);
    }

    void RemovePlane(SimplexPlane plane)
    {
        for (int i = planes.Count - 1; i >= 0; i--)
        {
            var p = planes[i];

            if ((p.normal == plane.normal).Bool3ToBool() && (p.sqrDistance == plane.sqrDistance)
                                           && (p.A == plane.A).Bool3ToBool()
                                           && (p.B == plane.B).Bool3ToBool()
                                           && (p.C == plane.C).Bool3ToBool())
                planes.Remove(plane);
        }
    }
}

public class GJKDetecotor
{
    public Simplex simplex;
    private Dictionary<fp3, SupportInfo> supports;


    public GJKDetecotor()
    {
        simplex = new Simplex();
        supports = new Dictionary<fp3, SupportInfo>();
    }

    public void Clear()
    {
        simplex.Clear();
        supports.Clear();
    }

    public bool GJKTest(fp3[] vertices1, fp3[] vertices2)
    {
        simplex.Clear();
        supports.Clear();

        // 得到初始的方向
        fp3 direction = FindFirstDirection(vertices1, vertices2);

        // 得到首个support点
        simplex.Add(Support(vertices1, vertices2, direction));

        // 得到第二个方向
        direction = -direction;

        var maxIterations = vertices1.Length + vertices2.Length;

        for (int i = 0; i < maxIterations; i++)
        {
            fp3 p = Support(vertices1, vertices2, direction);

            // 沿着dir的方向，已经找不到能够跨越原点的support点了。
            if (fpmath.dot(p, direction) < 0)
                return false;

            simplex.Add(p);

            // 单形体包含原点了
            if (simplex.ContainsPoint(fp3.zero))
            {
                return true;
            }

            direction = FindNextDirection();
        }

        Debug.Log("MaxIterations----" + "GJK");
        return false;
    }

    public bool GJK(fp3[] vertices1, fp3[] vertices2, ref fp3 normal, ref fp3 contactPoint,
        ref fp penetration)
    {
        simplex.Clear();
        supports.Clear();

        // 得到初始的方向
        fp3 direction = FindFirstDirection(vertices1, vertices2);

        // 得到首个support点
        simplex.Add(Support(vertices1, vertices2, direction));

        // 得到第二个方向
        direction = -direction;

        var maxIterations = vertices1.Length + vertices2.Length;

        for (int i = 0; i < maxIterations; i++)
        {
            fp3 p = Support(vertices1, vertices2, direction);

            // 沿着dir的方向，已经找不到能够跨越原点的support点了。
            if (fpmath.dot(p, direction) < 0)
                return false;

            simplex.Add(p);

            // 单形体包含原点了
            if (simplex.ContainsPoint(fp3.zero))
            {
                if (EPA(vertices1, vertices2, ref normal, ref contactPoint, ref penetration))
                {
                    return true;
                }

                return false;
            }

            direction = FindNextDirection();
        }

        Debug.Log("MaxIterations----" + "GJK");
        return false;
    }

    public bool GJKDist(fp3[] vertices1, fp3[] vertices2, ref fp3 normal, ref fp3 contactPoint,
        ref fp penetration)
    {
        simplex.Clear();
        supports.Clear();

        // 得到初始的方向
        fp3 direction = FindFirstDirection(vertices1, vertices2);

        // 得到首个support点
        simplex.Add(Support(vertices1, vertices2, direction));

        // 得到第二个方向
        direction = -direction;

        var maxIterations = vertices1.Length + vertices2.Length;

        for (int i = 0; i < maxIterations; i++)
        {
            fp3 p = Support(vertices1, vertices2, direction);

            // 沿着dir的方向，已经找不到能够跨越原点的support点了。
            if (fpmath.dot(p, direction) < 0)
            {
                simplex.GeneratePlanes();
                var plane = simplex.FindClosestPlane();
                if (plane == null) return false;

                Simplex.SimplexPlane simplexPlane = (Simplex.SimplexPlane)plane;
                if (!simplexPlane.originInPlane) return false;

                normal = fpmath.normalize(simplexPlane.normal);
                penetration = fpmath.sqrt(simplexPlane.sqrDistance);
                contactPoint = DistContactPoint(simplexPlane.A, simplexPlane.B, simplexPlane.C, simplexPlane.normal);
                return true;
            }

            simplex.Add(p);
            direction = FindNextDirection();
        }

        Debug.Log("MaxIterations----" + "GJK");
        return false;
    }

    public bool EPA(fp3[] vertices1, fp3[] vertices2, ref fp3 normal, ref fp3 contactPoint,
        ref fp penetration)
    {
        int maxIterations = vertices1.Length + vertices2.Length;
        simplex.InitPlanes();

        for (int i = 0; i < maxIterations; i++)
        {
            // 找到距离原点最近的边
            Simplex.SimplexPlane? p = simplex.FindClosestPlane();
            if (p == null) break;

            Simplex.SimplexPlane plane = (Simplex.SimplexPlane)p;

            // 沿着边的法线方向，尝试找一个新的support点
            fp3 point = Support(vertices1, vertices2, plane.normal);

            // 无法找到能够跨越该边的support点了。也就是说，该边就是差集最近边
            fp distance = fpmath.dot(point, plane.normal);

            if (distance - plane.sqrDistance <= (fp)0.001)
            {
                // 返回碰撞信息
                normal = -fpmath.normalize(plane.normal);
                penetration = fpmath.sqrt(plane.sqrDistance);
                contactPoint = ContactPoint(plane.A, plane.B, plane.C, plane.normal);
                return true;
            }

            simplex.InsertPlanePoint(point, plane);
        }

        Debug.Log("MaxIterations----" + "EPA");
        return false;
    }

    fp3 ContactPoint(fp3 sp1, fp3 sp2, fp3 sp3, fp3 normal)
    {
        var si1 = supports[sp1];
        var si2 = supports[sp2];
        var si3 = supports[sp3];

        if ((si1.vertice1 == si2.vertice1).Bool3ToBool() && (si2.vertice1 == si3.vertice1).Bool3ToBool())
        {
            return si1.vertice1;
        }

        if ((si1.vertice2 == si2.vertice2).Bool3ToBool() && (si2.vertice2 == si3.vertice2).Bool3ToBool())
        {
            return si1.vertice2;
        }

        fp3 one1 = si1.vertice1;
        fp3 one2 = (si2.vertice1 == one1).Bool3ToBool() ? si3.vertice1 : si2.vertice1;
        fp3 two1 = si1.vertice2;
        fp3 two2 = (si2.vertice2 == two1).Bool3ToBool() ? si3.vertice2 : si2.vertice2;

        return ClosestPointOnTwoLines(one1, 
            one2, two1, two2);
    }

    fp3 DistContactPoint(fp3 sp1, fp3 sp2, fp3 sp3, fp3 normal)
    {
        var si1 = supports[sp1];
        var si2 = supports[sp2];
        var si3 = supports[sp3];

        if ((si1.vertice1 == si2.vertice1).Bool3ToBool() && (si2.vertice1 == si3.vertice1).Bool3ToBool())
        {
            return si1.vertice1 + normal;
        }

        if ((si1.vertice2 == si2.vertice2).Bool3ToBool() && (si2.vertice2 == si3.vertice2).Bool3ToBool())
        {
            return si1.vertice2 + normal;
        }

        fp3 one1 = si1.vertice1;
        fp3 one2 = (si2.vertice1 == one1).Bool3ToBool(true) ? si3.vertice1 : si2.vertice1;
        fp3 two1 = si1.vertice2;
        fp3 two2 = (si2.vertice2 == two1).Bool3ToBool(true) ? si3.vertice2 : si2.vertice2;

        return ClosestPointOnTwoLines(one1, one2, two1, two2);
    }

    fp3 FindFirstDirection(fp3[] vertices1, fp3[] vertices2, int startIndex = 0)
    {
        if ((vertices1.Length <= startIndex) || (vertices2.Length <= startIndex)) return new fp3(1,1,1);

        fp3 direction = vertices1[startIndex] - vertices2[startIndex];

        if ((direction == fp3.zero).Bool3ToBool())
        {
            int index = startIndex;
            index++;
            return FindFirstDirection(vertices1, vertices2, index);
        }

        return direction;
    }

    fp3 Support(fp3[] vertices1, fp3[] vertices2, fp3 dir)
    {
        fp3 a = GetFarthestPointInDirection(vertices1, dir);
        fp3 b = GetFarthestPointInDirection(vertices2, -dir);
        fp3 support = a - b;
        CacheSupport(support, a, b);
        return support;
    }

    void CacheSupport(fp3 support, fp3 vertice1, fp3 vertice2)
    {
        if (supports.ContainsKey(support)) return;

        var si = new SupportInfo(vertice1, vertice2);
        supports.Add(support, si);
    }

    fp3 GetFarthestPointInDirection(fp3[] vertices, fp3 direction)
    {
        fp maxDistance = fp.min_value;
        int maxIndex = 0;

        for (int i = 0; i < vertices.Length; ++i)
        {
            fp distance = fpmath.dot(vertices[i], direction);

            if (distance > maxDistance)
            {
                maxDistance = distance;
                maxIndex = i;
            }
        }

        return vertices[maxIndex];
    }

    fp3 FindNextDirection()
    {
        int count = simplex.Count;

        if (count == 2)
        {
            // 计算原点到直线01的垂足
            fp3 crossPoint = ClosestPointOnLine(simplex.A, simplex.B, fp3.zero);

            // 取靠近原点方向的向量
            return fp3.zero - crossPoint;
        }
        else if (count == 3)
        {
            // 计算原点到面012的垂足
            fp3 crossPoint = FootPointOnPlane(simplex.A, simplex.B, simplex.C, fp3.zero);
            return fp3.zero - crossPoint;
        }
        else if (count == 4)
        {
            // 计算原点到面301的垂足
            fp3 crossOnDAB = FootPointOnPlane(simplex.D, simplex.A, simplex.B, fp3.zero);

            // 计算原点到面302的垂足
            fp3 crossOnDAC = FootPointOnPlane(simplex.D, simplex.A, simplex.C, fp3.zero);

            // 计算原点到面312的垂足
            fp3 crossOnDBC = FootPointOnPlane(simplex.D, simplex.B, simplex.C, fp3.zero);

            fp originToDAB = fpmath1.sqrMagnitude(crossOnDAB);
            fp originToDAC = fpmath1.sqrMagnitude(crossOnDAC);
            fp originToDBC = fpmath1.sqrMagnitude(crossOnDBC);

            int minIndex = MinIndex(originToDAB, originToDAC, originToDBC);

            // 保留距离原点最近的一个面
            if (minIndex == 1)
            {
                simplex.RemoveAt(2);
                return fp3.zero - crossOnDAB;
            }

            if (minIndex == 2)
            {
                simplex.RemoveAt(1);
                return fp3.zero - crossOnDAC;
            }
            else
            {
                simplex.RemoveAt(0);
                return fp3.zero - crossOnDBC;
            }
        }
        else
        {
            // 不应该执行到这里
            return fp3.zero;
        }
    }

    public static fp3 ClosestPointOnLine(fp3 linePointA, fp3 linePointB, fp3 point)
    {
        fp3 AB = linePointB - linePointA;
        fp t = fpmath.dot(point - linePointA, AB) / fpmath.dot(AB, AB);
        return linePointA + t * AB;
    }

    /// <summary>
    /// 第二条线段离第一条线段的最近点
    /// </summary>
    /// <param name="lineOnePointA"></param>
    /// <param name="lineOnePointB"></param>
    /// <param name="lineTwoPointA"></param>
    /// <param name="lineTwoPointB"></param>
    /// <returns></returns>
    public static fp3 ClosestPointFromLineTwo(fp3 lineOnePointA, fp3 lineOnePointB, fp3 lineTwoPointA,
        fp3 lineTwoPointB)
    {
        fp3 bestA = fp3.zero;
        fp3 bestB = fp3.zero;
        ClosestPoinsOnTwoLines(lineOnePointA, lineOnePointB, lineTwoPointA, lineTwoPointB, ref bestA, ref bestB);
        return bestB;
    }

    /// <summary>
    /// 两条线段的两个最近点
    /// </summary>
    /// <param name="lineOnePointA"></param>
    /// <param name="lineOnePointB"></param>
    /// <param name="lineTwoPointA"></param>
    /// <param name="lineTwoPointB"></param>
    /// <param name="bestA"></param>
    /// <param name="bestB"></param>
    public static void ClosestPoinsOnTwoLines(fp3 lineOnePointA, fp3 lineOnePointB, fp3 lineTwoPointA,
        fp3 lineTwoPointB,
        ref fp3 bestA, ref fp3 bestB)
    {
        fp episolon = (fp)0.0001f;
        fp3 d1 = lineOnePointB - lineOnePointA;
        fp3 d2 = lineTwoPointB - lineTwoPointA;
        fp3 r = lineOnePointA - lineTwoPointA;
        fp a = fpmath.dot(d1, d1);
        fp e = fpmath.dot(d2, d2);
        fp f = fpmath.dot(d2, r);

        fp s = 0;
        fp t = 0;

        if (a <= episolon && e <= episolon)
        {
            s = t = 0;
            bestA = lineOnePointA;
            bestB = lineTwoPointA;
            return;
        }

        if (a <= episolon)
        {
            s = 0;
            t = f / e;
            t = fpmath.clamp(t, 0, 1);
        }
        else
        {
            fp c = fpmath.dot(d1, r);

            if (e <= episolon)
            {
                t = 0;
                s = fpmath.clamp(-c / a, 0, 1);
            }
            else
            {
                fp b = fpmath.dot(d1, d2);
                fp denom = a * e - b * b;
                s = denom == 0 ? 0 : fpmath.clamp((b * f - c * e) / denom, 0, 1);

                fp tnom = b * s + f;

                if (tnom < 0)
                {
                    t = 0;
                    s = fpmath.clamp(-c / a, 0, 1);
                }
                else if (tnom > e)
                {
                    t = 1;
                    s = fpmath.clamp((b - c) / a, 0, 1);
                }
                else
                {
                    t = tnom / e;
                }
            }
        }

        bestA = lineOnePointA + d1 * s;
        bestB = lineTwoPointA + d2 * t;
    }

    /// <summary>
    /// 两条线段最近距离点
    /// </summary>
    /// <param name="lineOnePointA"></param>
    /// <param name="lineOnePointB"></param>
    /// <param name="lineTwoPointA"></param>
    /// <param name="lineTwoPointB"></param>
    /// <returns></returns>
    public static fp3 ClosestPointOnTwoLines(fp3 lineOnePointA, fp3 lineOnePointB, fp3 lineTwoPointA,
        fp3 lineTwoPointB)
    {
        fp3 bestA = fp3.zero;
        fp3 bestB = fp3.zero;
        ClosestPoinsOnTwoLines(lineOnePointA, lineOnePointB, lineTwoPointA, lineTwoPointB, ref bestA, ref bestB);
        return bestA + (bestB - bestA) * (fp)0.5f;
    }

    /// <summary>
    /// 线段到点的最近点
    /// </summary>
    /// <param name="linePointA"></param>
    /// <param name="linePointB"></param>
    /// <param name="Point"></param>
    /// <returns></returns>
    public static fp3 ClosestPointOnLineSegment(fp3 linePointA, fp3 linePointB, fp3 Point)
    {
        fp3 AB = linePointB - linePointA;
        fp t = fpmath.dot(Point - linePointA, AB) / fpmath.dot(AB, AB);
        return linePointA + fpmath.clamp(t, 0, 1) * AB;
    }

    public static fp3 FootPointOnPlane(fp3 planePointA, fp3 planePointB, fp3 planePointC, fp3 point)
    {
        fp3 normal = fpmath.cross(planePointB - planePointA, planePointC - planePointA); // not normalized
        if ((normal == fp3.zero).Bool3ToBool()) return fp3.zero;

        fp3 PA = planePointA - point;
        fp t = fpmath.dot(PA, normal) / fpmath.dot(normal, normal);
        return point + t * normal;
    }

    public static fp PlaneCenterDistance(fp3 planePointA, fp3 planePointB, fp3 planePointC)
    {
        fp x = (planePointA.x + planePointB.x + planePointC.x) * (fp)0.33333f;
        fp y = (planePointA.y + planePointB.y + planePointC.y) * (fp)0.33333f;
        fp z = (planePointA.z + planePointB.z + planePointC.z) * (fp)0.33333f;
        return x * x + y * y + z * z;
    }


    public static int MinIndex(fp one, fp two, fp three)
    {
        int index = one < two ? 1 : 2;

        if (index == 1)
        {
            index = one < three ? 1 : 3;
        }
        else
        {
            index = two < three ? 2 : 3;
        }

        return index;
    }

    public static int MaxIndex(float one, float two, float three)
    {
        int index = one > two ? 1 : 2;

        if (index == 1)
        {
            index = one > three ? 1 : 3;
        }
        else
        {
            index = two > three ? 2 : 3;
        }

        return index;
    }
}