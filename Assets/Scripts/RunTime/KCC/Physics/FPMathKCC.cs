using Unity.Mathematics.FixedPoint;

/// <summary>
/// KCC 定点数数学工具集。提供向量投影、插值、胶囊构建等常用运算。
/// </summary>
public static class FPMathKCC
{
    /// <summary>世界空间上方向。</summary>
    public static readonly fp3 WorldUp = new fp3((fp)0, (fp)1, (fp)0);

    /// <summary>世界空间前方向。</summary>
    public static readonly fp3 WorldForward = new fp3((fp)0, (fp)0, (fp)1);

    /// <summary>世界空间右方向。</summary>
    public static readonly fp3 WorldRight = new fp3((fp)1, (fp)0, (fp)0);

    /// <summary>默认重力加速度（世界 Y，米/秒²）。</summary>
    public static readonly fp DefaultGravity = (fp)(-9.81f);

    /// <summary>刚体贴地探测皮肤厚度。</summary>
    public static readonly fp GroundProbeSkin = (fp)0.02f;

    /// <summary>
    /// 安全归一化：零向量或近零向量时返回 zero（对齐 Unity Vector3.normalized）。
    /// fixed-point 库的 normalizesafe 会 eager 求值 rsqrt，零向量时仍会除零。
    /// </summary>
    public static fp3 NormalizeSafe(fp3 vector)
    {
        fp lenSq = fpmath1.sqrMagnitude(vector);
        if (lenSq <= (fp)0.00000001f)
        {
            return fp3.zero;
        }

        return vector * fpmath.rsqrt(lenSq);
    }

    /// <summary>
    /// 将向量投影到指定平面上。
    /// </summary>
    /// <param name="vector">待投影向量。</param>
    /// <param name="planeNormal">平面法线。</param>
    /// <returns>投影后的向量。</returns>
    public static fp3 ProjectOnPlane(fp3 vector, fp3 planeNormal)
    {
        fp dot = fpmath.dot(vector, planeNormal);
        return vector - planeNormal * dot;
    }

    /// <summary>
    /// 计算两向量夹角（度）。
    /// </summary>
    /// <param name="from">起始向量。</param>
    /// <param name="to">目标向量。</param>
    /// <returns>夹角（度），零向量时返回 0。</returns>
    public static fp Angle(fp3 from, fp3 to)
    {
        fp denom = fpmath.sqrt(fpmath1.sqrMagnitude(from) * fpmath1.sqrMagnitude(to));
        if (denom <= (fp)0.0000001f)
        {
            return (fp)0;
        }

        fp cos = fpmath.clamp(fpmath.dot(from, to) / denom, (fp)(-1), (fp)1);
        return fpmath.degrees(fpmath1.acos(cos));
    }

    /// <summary>
    /// 两向量球面线性插值。
    /// </summary>
    /// <param name="from">起始向量。</param>
    /// <param name="to">目标向量。</param>
    /// <param name="t">插值因子 [0, 1]。</param>
    /// <returns>插值结果向量。</returns>
    public static fp3 Slerp(fp3 from, fp3 to, fp t)
    {
        t = fpmath.clamp(t, (fp)0, (fp)1);
        if (fpmath1.sqrMagnitude(from) <= (fp)0 || fpmath1.sqrMagnitude(to) <= (fp)0)
        {
            return fpmath1.sqrMagnitude(to) > (fp)0 ? to : from;
        }

        fp3 fromNorm = fpmath.normalize(from);
        fp3 toNorm = fpmath.normalize(to);
        fp dot = fpmath.clamp(fpmath.dot(fromNorm, toNorm), (fp)(-1), (fp)1);
        fp theta = fpmath1.acos(dot) * t;
        fp3 rel = toNorm - fromNorm * dot;
        if (fpmath1.sqrMagnitude(rel) <= (fp)0.0000001f)
        {
            return fromNorm;
        }

        rel = fpmath.normalize(rel);
        return fromNorm * fpmath.cos(theta) + rel * fpmath.sin(theta);
    }

    /// <summary>
    /// 指数衰减插值因子。用于平滑阻尼类插值。
    /// </summary>
    /// <param name="sharpness">锐度系数。</param>
    /// <param name="deltaTime">帧间隔时间。</param>
    /// <returns>插值因子。</returns>
    public static fp ExpDecayLerpFactor(fp sharpness, fp deltaTime)
    {
        fp x = sharpness * deltaTime;
        return x / ((fp)1 + x);
    }

    /// <summary>
    /// 限制向量长度不超过指定最大值。
    /// </summary>
    /// <param name="vector">待限制向量。</param>
    /// <param name="maxLength">最大长度。</param>
    /// <returns>限制后的向量。</returns>
    public static fp3 ClampMagnitude(fp3 vector, fp maxLength)
    {
        fp sqr = fpmath1.sqrMagnitude(vector);
        if (sqr <= maxLength * maxLength)
        {
            return vector;
        }

        return fpmath.normalize(vector) * maxLength;
    }

    /// <summary>
    /// 求点到线段上的最近点。
    /// </summary>
    /// <param name="point">查询点。</param>
    /// <param name="a">线段端点 A。</param>
    /// <param name="b">线段端点 B。</param>
    /// <returns>线段上距查询点最近的点。</returns>
    public static fp3 ClosestPointOnSegment(fp3 point, fp3 a, fp3 b)
    {
        fp3 ab = b - a;
        fp t = fpmath.dot(point - a, ab) / fpmath.max(fpmath1.sqrMagnitude(ab), (fp)0.0000001f);
        t = fpmath.clamp(t, (fp)0, (fp)1);
        return a + ab * t;
    }

    /// <summary>
    /// 求点到线段上的最近点，并返回点到该最近点的距离。
    /// </summary>
    /// <param name="a">线段端点 A。</param>
    /// <param name="b">线段端点 B。</param>
    /// <param name="point">查询点。</param>
    /// <param name="closest">线段上距查询点最近的点。</param>
    /// <returns>查询点到最近点的距离。</returns>
    public static fp ClosestPointOnSegmentToPoint(fp3 a, fp3 b, fp3 point, out fp3 closest)
    {
        closest = ClosestPointOnSegment(point, a, b);
        return fpmath.length(closest - point);
    }

    /// <summary>
    /// 计算两线段之间的最短距离及各自最近点。
    /// </summary>
    /// <param name="p1">线段 1 端点 A。</param>
    /// <param name="q1">线段 1 端点 B。</param>
    /// <param name="p2">线段 2 端点 A。</param>
    /// <param name="q2">线段 2 端点 B。</param>
    /// <param name="c1">线段 1 上的最近点。</param>
    /// <param name="c2">线段 2 上的最近点。</param>
    /// <returns>两最近点之间的距离。</returns>
    public static fp SegmentSegmentDistance(fp3 p1, fp3 q1, fp3 p2, fp3 q2, out fp3 c1, out fp3 c2)
    {
        fp3 d1 = q1 - p1;
        fp3 d2 = q2 - p2;
        fp3 r = p1 - p2;
        fp a = fpmath1.sqrMagnitude(d1);
        fp e = fpmath1.sqrMagnitude(d2);
        fp f = fpmath.dot(d2, r);

        fp s;
        fp t;

        // 两线段均退化为点
        if (a <= (fp)0.0000001f && e <= (fp)0.0000001f)
        {
            c1 = p1;
            c2 = p2;
            return fpmath.length(p1 - p2);
        }

        if (a <= (fp)0.0000001f)
        {
            s = (fp)0;
            t = fpmath.clamp(f / e, (fp)0, (fp)1);
        }
        else
        {
            fp c = fpmath.dot(d1, r);
            if (e <= (fp)0.0000001f)
            {
                t = (fp)0;
                s = fpmath.clamp(-c / a, (fp)0, (fp)1);
            }
            else
            {
                // 一般情况：解两线段最近点参数
                fp b = fpmath.dot(d1, d2);
                fp denom = a * e - b * b;
                s = denom != (fp)0 ? fpmath.clamp((b * f - c * e) / denom, (fp)0, (fp)1) : (fp)0;
                t = (b * s + f) / e;
                if (t < (fp)0)
                {
                    t = (fp)0;
                    s = fpmath.clamp(-c / a, (fp)0, (fp)1);
                }
                else if (t > (fp)1)
                {
                    t = (fp)1;
                    s = fpmath.clamp((b - c) / a, (fp)0, (fp)1);
                }
            }
        }

        c1 = p1 + d1 * s;
        c2 = p2 + d2 * t;
        return fpmath.length(c1 - c2);
    }

    /// <summary>
    /// 由增量四元数计算角速度向量。
    /// </summary>
    /// <param name="deltaRotation">帧间旋转增量。</param>
    /// <param name="deltaTime">帧间隔时间。</param>
    /// <returns>角速度向量（度/秒）。</returns>
    public static fp3 QuaternionToAngularVelocity(fpquaternion deltaRotation, fp deltaTime)
    {
        if (deltaTime <= (fp)0)
        {
            return fp3.zero;
        }

        fp angle;
        fp3 axis;
        ToAngleAxis(deltaRotation, out angle, out axis);
        return axis * (angle / deltaTime);
    }

    /// <summary>
    /// 将四元数转换为轴角表示。
    /// </summary>
    /// <param name="rotation">输入四元数。</param>
    /// <param name="angle">输出旋转角（度）。</param>
    /// <param name="axis">输出旋转轴。</param>
    public static void ToAngleAxis(fpquaternion rotation, out fp angle, out fp3 axis)
    {
        rotation = fpmath1.normalize(rotation);
        fp w = fpmath.clamp(rotation.w, (fp)(-1), (fp)1);
        angle = (fp)2 * fpmath.degrees(fpmath1.acos(w));
        fp s = fpmath.sqrt((fp)1 - w * w);
        if (s <= (fp)0.0001f)
        {
            axis = WorldUp;
        }
        else
        {
            axis = new fp3(rotation.x / s, rotation.y / s, rotation.z / s);
        }
    }

    /// <summary>
    /// 根据位姿与尺寸构建胶囊几何体。
    /// </summary>
    /// <param name="position">世界位置。</param>
    /// <param name="rotation">世界旋转。</param>
    /// <param name="radius">胶囊半径。</param>
    /// <param name="height">胶囊总高度。</param>
    /// <param name="yOffset">局部 Y 轴偏移。</param>
    /// <returns>胶囊几何体。</returns>
    public static FPCapsuleGeometry BuildCapsuleGeometry(fp3 position, fpquaternion rotation, fp radius, fp height, fp yOffset)
    {
        fp halfHeight = fpmath.max((fp)0, height * (fp)0.5f - radius);
        fp3 center = position + rotation * new fp3((fp)0, yOffset, (fp)0);
        fp3 up = rotation * WorldUp;
        return new FPCapsuleGeometry
        {
            BottomHemiCenter = center - up * halfHeight,
            TopHemiCenter = center + up * halfHeight,
            Radius = radius,
        };
    }

    /// <summary>
    /// 将世界坐标点变换到指定局部空间。
    /// </summary>
    /// <param name="worldPoint">世界坐标点。</param>
    /// <param name="position">局部原点世界位置。</param>
    /// <param name="rotation">局部原点世界旋转。</param>
    /// <returns>局部坐标点。</returns>
    public static fp3 InverseTransformPoint(fp3 worldPoint, fp3 position, fpquaternion rotation)
    {
        return fpquaternionKCCExtensions.Inverse(rotation) * (worldPoint - position);
    }

    /// <summary>
    /// 将局部坐标点变换到世界空间。
    /// </summary>
    /// <param name="localPoint">局部坐标点。</param>
    /// <param name="position">局部原点世界位置。</param>
    /// <param name="rotation">局部原点世界旋转。</param>
    /// <returns>世界坐标点。</returns>
    public static fp3 TransformPoint(fp3 localPoint, fp3 position, fpquaternion rotation)
    {
        return position + rotation * localPoint;
    }

    /// <summary>
    /// 判断点是否位于以原点为中心的 AABB 内部（含边界）。
    /// </summary>
    public static bool IsInsideLocalAabb(fp3 localPoint, fp3 halfExtents)
    {
        return fpmath.abs(localPoint.x) <= halfExtents.x
            && fpmath.abs(localPoint.y) <= halfExtents.y
            && fpmath.abs(localPoint.z) <= halfExtents.z;
    }

    /// <summary>
    /// 求点到以原点为中心的 AABB 的最近点。
    /// </summary>
    public static fp3 ClosestPointOnLocalAabb(fp3 localPoint, fp3 halfExtents)
    {
        return new fp3(
            fpmath.clamp(localPoint.x, -halfExtents.x, halfExtents.x),
            fpmath.clamp(localPoint.y, -halfExtents.y, halfExtents.y),
            fpmath.clamp(localPoint.z, -halfExtents.z, halfExtents.z));
    }

    /// <summary>
    /// 求线段与以原点为中心的 AABB 的最近点对（OBB 局部空间）。
    /// 通过端点 + 12 条棱边 segment-segment 检测，保证精确性。
    /// </summary>
    public static fp ClosestPointsSegmentLocalAabb(
        fp3 segA,
        fp3 segB,
        fp3 halfExtents,
        out fp3 pointOnSegment,
        out fp3 pointOnBox)
    {
        fp bestDistSq = fp.max_value;
        fp3 bestPointOnSegment = segA;
        fp3 bestPointOnBox = fp3.zero;

        void Consider(fp3 ps, fp3 pb)
        {
            fp distSq = fpmath1.sqrMagnitude(ps - pb);
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                bestPointOnSegment = ps;
                bestPointOnBox = pb;
            }
        }

        Consider(segA, ClosestPointOnLocalAabb(segA, halfExtents));
        Consider(segB, ClosestPointOnLocalAabb(segB, halfExtents));

        fp hx = halfExtents.x;
        fp hy = halfExtents.y;
        fp hz = halfExtents.z;

        // X 轴平行棱边（4 条）
        ConsiderSegmentAabbEdge(segA, segB, new fp3(-hx, -hy, -hz), new fp3(hx, -hy, -hz), Consider);
        ConsiderSegmentAabbEdge(segA, segB, new fp3(-hx, -hy, hz), new fp3(hx, -hy, hz), Consider);
        ConsiderSegmentAabbEdge(segA, segB, new fp3(-hx, hy, -hz), new fp3(hx, hy, -hz), Consider);
        ConsiderSegmentAabbEdge(segA, segB, new fp3(-hx, hy, hz), new fp3(hx, hy, hz), Consider);

        // Y 轴平行棱边（4 条）
        ConsiderSegmentAabbEdge(segA, segB, new fp3(-hx, -hy, -hz), new fp3(-hx, hy, -hz), Consider);
        ConsiderSegmentAabbEdge(segA, segB, new fp3(-hx, -hy, hz), new fp3(-hx, hy, hz), Consider);
        ConsiderSegmentAabbEdge(segA, segB, new fp3(hx, -hy, -hz), new fp3(hx, hy, -hz), Consider);
        ConsiderSegmentAabbEdge(segA, segB, new fp3(hx, -hy, hz), new fp3(hx, hy, hz), Consider);

        // Z 轴平行棱边（4 条）
        ConsiderSegmentAabbEdge(segA, segB, new fp3(-hx, -hy, -hz), new fp3(-hx, -hy, hz), Consider);
        ConsiderSegmentAabbEdge(segA, segB, new fp3(-hx, hy, -hz), new fp3(-hx, hy, hz), Consider);
        ConsiderSegmentAabbEdge(segA, segB, new fp3(hx, -hy, -hz), new fp3(hx, -hy, hz), Consider);
        ConsiderSegmentAabbEdge(segA, segB, new fp3(hx, hy, -hz), new fp3(hx, hy, hz), Consider);

        pointOnSegment = bestPointOnSegment;
        pointOnBox = bestPointOnBox;
        return fpmath.sqrt(bestDistSq);
    }

    /// <summary>
    /// 求 AABB 内点到最近面的推出方向与距离（局部空间，盒心在原点）。
    /// </summary>
    public static void GetNearestLocalAabbFaceDepenetration(
        fp3 localPoint,
        fp3 halfExtents,
        out fp3 localDirection,
        out fp exitDistance)
    {
        fp distanceToPositiveX = halfExtents.x - localPoint.x;
        fp distanceToNegativeX = halfExtents.x + localPoint.x;
        fp distanceToPositiveY = halfExtents.y - localPoint.y;
        fp distanceToNegativeY = halfExtents.y + localPoint.y;
        fp distanceToPositiveZ = halfExtents.z - localPoint.z;
        fp distanceToNegativeZ = halfExtents.z + localPoint.z;

        exitDistance = distanceToPositiveX;
        localDirection = new fp3((fp)1, (fp)0, (fp)0);

        if (distanceToNegativeX < exitDistance)
        {
            exitDistance = distanceToNegativeX;
            localDirection = new fp3((fp)(-1), (fp)0, (fp)0);
        }

        if (distanceToPositiveY < exitDistance)
        {
            exitDistance = distanceToPositiveY;
            localDirection = new fp3((fp)0, (fp)1, (fp)0);
        }

        if (distanceToNegativeY < exitDistance)
        {
            exitDistance = distanceToNegativeY;
            localDirection = new fp3((fp)0, (fp)(-1), (fp)0);
        }

        if (distanceToPositiveZ < exitDistance)
        {
            exitDistance = distanceToPositiveZ;
            localDirection = new fp3((fp)0, (fp)0, (fp)1);
        }

        if (distanceToNegativeZ < exitDistance)
        {
            exitDistance = distanceToNegativeZ;
            localDirection = new fp3((fp)0, (fp)0, (fp)(-1));
        }
    }

    /// <summary>
    /// 由两最近点及各自半径计算 MTD（最小平移分离）。
    /// </summary>
    public static bool TryComputeRadiusMtd(
        fp3 pointA,
        fp3 pointB,
        fp radiusA,
        fp radiusB,
        fp3 fallbackDirection,
        out fp3 direction,
        out fp penetration)
    {
        direction = fp3.zero;
        penetration = (fp)0;

        fp combined = radiusA + radiusB;
        fp dist = fpmath.length(pointA - pointB);
        if (dist >= combined)
        {
            return false;
        }

        if (dist > (fp)0.0000001f)
        {
            direction = fpmath.normalize(pointA - pointB);
        }
        else if (fpmath1.sqrMagnitude(fallbackDirection) > (fp)0.0000001f)
        {
            direction = fpmath.normalize(fallbackDirection);
        }
        else
        {
            direction = WorldUp;
        }

        penetration = combined - dist;
        return true;
    }

    private static void ConsiderSegmentAabbEdge(
        fp3 segA,
        fp3 segB,
        fp3 edgeA,
        fp3 edgeB,
        System.Action<fp3, fp3> consider)
    {
        fp3 c1;
        fp3 c2;
        SegmentSegmentDistance(segA, segB, edgeA, edgeB, out c1, out c2);
        consider(c1, c2);
    }
}

/// <summary>
/// 定点四元数 KCC 扩展方法。
/// </summary>
public static class fpquaternionKCCExtensions
{
    /// <summary>
    /// 计算四元数的逆（共轭除以模长平方）。
    /// </summary>
    /// <param name="q">输入四元数。</param>
    /// <returns>逆四元数；模长为零时返回单位四元数。</returns>
    public static fpquaternion Inverse(fpquaternion q)
    {
        fp dot = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
        if (dot <= (fp)0)
        {
            return fpquaternion.identity;
        }

        fp inv = (fp)1 / dot;
        return new fpquaternion(-q.x * inv, -q.y * inv, -q.z * inv, q.w * inv);
    }
}
