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
