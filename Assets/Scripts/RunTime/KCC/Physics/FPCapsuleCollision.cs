using Unity.Mathematics.FixedPoint;

/// <summary>
/// 胶囊碰撞检测工具。支持胶囊与盒体/胶囊/平面的重叠、cast、穿透及射线检测。
/// </summary>
public static class FPCapsuleCollision
{
    /// <summary>
    /// 检测胶囊是否与指定碰撞体重叠。
    /// </summary>
    /// <param name="capsule">胶囊几何体。</param>
    /// <param name="collider">目标碰撞体。</param>
    /// <returns>若重叠则返回 true。</returns>
    public static bool OverlapCapsule(FPCapsuleGeometry capsule, IFPCollider collider)
    {
        switch (collider.ShapeType)
        {
            case FPShapeType.Box:
                return CapsuleIntersectsBox(capsule, collider.GetBoxShape());
            case FPShapeType.Capsule:
                return CapsuleIntersectsCapsule(capsule, collider.GetCapsuleShape(), collider.Position, collider.Rotation);
            case FPShapeType.Plane:
                return CapsuleIntersectsPlane(capsule, collider.GetPlaneShape());
            case FPShapeType.Sphere:
                return CapsuleIntersectsSphere(capsule, collider.GetSphereShape());
            default:
                return false;
        }
    }

    /// <summary>
    /// 沿方向对胶囊做离散步进 cast，检测与碰撞体的首次接触。
    /// </summary>
    /// <param name="capsule">起始胶囊几何体。</param>
    /// <param name="direction">cast 方向。</param>
    /// <param name="distance">cast 最大距离。</param>
    /// <param name="collider">目标碰撞体。</param>
    /// <param name="hit">输出命中结果。</param>
    /// <returns>若 cast 命中则返回 true。</returns>
    public static bool CapsuleCast(FPCapsuleGeometry capsule, fp3 direction, fp distance, IFPCollider collider, out FPRaycastHit hit)
    {
        hit = default;
        if (distance <= (fp)0)
        {
            return false;
        }

        fp3 dir = fpmath.normalize(direction);
        fp bestDistance = distance + (fp)1;
        fp3 bestNormal = fp3.zero;
        fp3 bestPoint = fp3.zero;
        bool found = false;

        // 沿 cast 方向分步检测穿透
        int steps = 8;
        for (int i = 0; i <= steps; i++)
        {
            fp t = (fp)i / (fp)steps * distance;
            FPCapsuleGeometry swept = capsule;
            swept.BottomHemiCenter += dir * t;
            swept.TopHemiCenter += dir * t;

            fp3 normal;
            fp penetration;
            if (TryComputePenetration(swept, collider, out normal, out penetration))
            {
                fp hitDistance = t;
                if (hitDistance < bestDistance)
                {
                    bestDistance = hitDistance;
                    bestNormal = normal;
                    bestPoint = swept.Center;
                    found = true;
                }
            }
        }

        if (!found)
        {
            return false;
        }

        hit = new FPRaycastHit
        {
            Collider = collider,
            Distance = bestDistance,
            Normal = fpmath.normalize(bestNormal),
            Point = bestPoint,
        };
        return true;
    }

    /// <summary>
    /// 计算胶囊与碰撞体之间的穿透分离方向与深度。
    /// </summary>
    /// <param name="capsule">胶囊几何体。</param>
    /// <param name="collider">目标碰撞体。</param>
    /// <param name="direction">输出分离方向。</param>
    /// <param name="distance">输出穿透深度。</param>
    /// <returns>若存在穿透则返回 true。</returns>
    public static bool TryComputePenetration(FPCapsuleGeometry capsule, IFPCollider collider, out fp3 direction, out fp distance)
    {
        direction = fp3.zero;
        distance = (fp)0;

        switch (collider.ShapeType)
        {
            case FPShapeType.Box:
                return CapsulePenetratesBox(capsule, collider.GetBoxShape(), out direction, out distance);
            case FPShapeType.Capsule:
                return CapsulePenetratesCapsule(capsule, collider.GetCapsuleShape(), collider.Position, collider.Rotation, out direction, out distance);
            case FPShapeType.Plane:
                return CapsulePenetratesPlane(capsule, collider.GetPlaneShape(), out direction, out distance);
            case FPShapeType.Sphere:
                return CapsulePenetratesSphere(capsule, collider.GetSphereShape(), out direction, out distance);
            default:
                return false;
        }
    }

    /// <summary>
    /// 射线与碰撞体相交检测。
    /// </summary>
    /// <param name="origin">射线起点。</param>
    /// <param name="direction">射线方向。</param>
    /// <param name="distance">射线最大距离。</param>
    /// <param name="collider">目标碰撞体。</param>
    /// <param name="hit">输出命中结果。</param>
    /// <returns>若射线命中则返回 true。</returns>
    public static bool Raycast(fp3 origin, fp3 direction, fp distance, IFPCollider collider, out FPRaycastHit hit)
    {
        hit = default;
        fp3 dir = fpmath.normalize(direction);

        switch (collider.ShapeType)
        {
            case FPShapeType.Plane:
            {
                FPPlaneShape plane = collider.GetPlaneShape();
                fp denom = fpmath.dot(dir, plane.Normal);
                if (fpmath.abs(denom) <= (fp)0.0000001f)
                {
                    return false;
                }

                fp t = fpmath.dot(plane.Point - origin, plane.Normal) / denom;
                if (t < (fp)0 || t > distance)
                {
                    return false;
                }

                hit = new FPRaycastHit
                {
                    Collider = collider,
                    Distance = t,
                    Normal = denom < (fp)0 ? plane.Normal : -plane.Normal,
                    Point = origin + dir * t,
                };
                return true;
            }
            case FPShapeType.Box:
            {
                FPBoxShape box = collider.GetBoxShape();
                fp3 localOrigin = FPMathKCC.InverseTransformPoint(origin, box.Center, box.Rotation);
                fp3 localDir = fpquaternionKCCExtensions.Inverse(box.Rotation) * dir;
                fp tMin = (fp)0;
                fp tMax = distance;
                // Slab 法 AABB 射线相交
                for (int axis = 0; axis < 3; axis++)
                {
                    fp originAxis = axis == 0 ? localOrigin.x : axis == 1 ? localOrigin.y : localOrigin.z;
                    fp dirAxis = axis == 0 ? localDir.x : axis == 1 ? localDir.y : localDir.z;
                    fp minB = -(axis == 0 ? box.HalfExtents.x : axis == 1 ? box.HalfExtents.y : box.HalfExtents.z);
                    fp maxB = -minB;

                    if (fpmath.abs(dirAxis) <= (fp)0.0000001f)
                    {
                        if (originAxis < minB || originAxis > maxB)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        fp t1 = (minB - originAxis) / dirAxis;
                        fp t2 = (maxB - originAxis) / dirAxis;
                        if (t1 > t2)
                        {
                            (t1, t2) = (t2, t1);
                        }

                        tMin = fpmath.max(tMin, t1);
                        tMax = fpmath.min(tMax, t2);
                        if (tMin > tMax)
                        {
                            return false;
                        }
                    }
                }

                hit = new FPRaycastHit
                {
                    Collider = collider,
                    Distance = tMin,
                    Normal = fpmath.normalize(dir),
                    Point = origin + dir * tMin,
                };
                return true;
            }
            case FPShapeType.Sphere:
            {
                FPSphereShape sphere = collider.GetSphereShape();
                fp3 oc = origin - sphere.Center;
                fp b = (fp)2 * fpmath.dot(oc, dir);
                fp c = fpmath1.sqrMagnitude(oc) - sphere.Radius * sphere.Radius;
                fp discriminant = b * b - (fp)4 * c;
                if (discriminant < (fp)0)
                {
                    return false;
                }

                fp sqrtD = fpmath.sqrt(discriminant);
                fp invDenom = (fp)0.5f;
                fp t0 = (-b - sqrtD) * invDenom;
                fp t1 = (-b + sqrtD) * invDenom;
                fp t = t0 >= (fp)0 ? t0 : t1;
                if (t < (fp)0 || t > distance)
                {
                    return false;
                }

                fp3 point = origin + dir * t;
                hit = new FPRaycastHit
                {
                    Collider = collider,
                    Distance = t,
                    Normal = fpmath.normalize(point - sphere.Center),
                    Point = point,
                };
                return true;
            }
            default:
                return false;
        }
    }

    /// <summary>
    /// 检测胶囊与球体是否相交。
    /// </summary>
    /// <param name="capsule">胶囊几何体。</param>
    /// <param name="sphere">球体形状。</param>
    /// <returns>若相交则返回 true。</returns>
    private static bool CapsuleIntersectsSphere(FPCapsuleGeometry capsule, FPSphereShape sphere)
    {
        fp3 closest;
        fp dist = FPMathKCC.ClosestPointOnSegmentToPoint(
            capsule.BottomHemiCenter,
            capsule.TopHemiCenter,
            sphere.Center,
            out closest);
        return dist <= capsule.Radius + sphere.Radius;
    }

    /// <summary>
    /// 计算胶囊与球体之间的穿透分离方向与深度。
    /// </summary>
    /// <param name="capsule">胶囊几何体。</param>
    /// <param name="sphere">球体形状。</param>
    /// <param name="direction">输出分离方向。</param>
    /// <param name="distance">输出穿透深度。</param>
    /// <returns>若存在穿透则返回 true。</returns>
    private static bool CapsulePenetratesSphere(
        FPCapsuleGeometry capsule,
        FPSphereShape sphere,
        out fp3 direction,
        out fp distance)
    {
        direction = fp3.zero;
        distance = (fp)0;
        fp3 closest;
        fp dist = FPMathKCC.ClosestPointOnSegmentToPoint(
            capsule.BottomHemiCenter,
            capsule.TopHemiCenter,
            sphere.Center,
            out closest);
        fp combined = capsule.Radius + sphere.Radius;
        if (dist >= combined)
        {
            return false;
        }

        direction = dist > (fp)0.0000001f
            ? fpmath.normalize(closest - sphere.Center)
            : FPMathKCC.WorldUp;
        distance = combined - dist;
        return true;
    }

    /// <summary>
    /// 检测胶囊与平面是否相交。
    /// </summary>
    /// <param name="capsule">胶囊几何体。</param>
    /// <param name="plane">平面形状。</param>
    /// <returns>若相交则返回 true。</returns>
    private static bool CapsuleIntersectsPlane(FPCapsuleGeometry capsule, FPPlaneShape plane)
    {
        fp3 n = fpmath.normalize(plane.Normal);
        fp distBottom = fpmath.dot(capsule.BottomHemiCenter - plane.Point, n);
        fp distTop = fpmath.dot(capsule.TopHemiCenter - plane.Point, n);
        fp minDist = fpmath.min(distBottom, distTop);
        return minDist <= capsule.Radius;
    }

    /// <summary>
    /// 检测胶囊与盒体是否相交。
    /// </summary>
    /// <param name="capsule">胶囊几何体。</param>
    /// <param name="box">盒体形状。</param>
    /// <returns>若相交则返回 true。</returns>
    private static bool CapsuleIntersectsBox(FPCapsuleGeometry capsule, FPBoxShape box)
    {
        fp penetration;
        fp3 direction;
        return CapsulePenetratesBox(capsule, box, out direction, out penetration);
    }

    /// <summary>
    /// 检测两胶囊是否相交。
    /// </summary>
    /// <param name="a">胶囊 A 几何体。</param>
    /// <param name="shape">胶囊 B 形状定义。</param>
    /// <param name="position">胶囊 B 世界位置。</param>
    /// <param name="rotation">胶囊 B 世界旋转。</param>
    /// <returns>若相交则返回 true。</returns>
    private static bool CapsuleIntersectsCapsule(FPCapsuleGeometry a, FPCapsuleShape shape, fp3 position, fpquaternion rotation)
    {
        fp penetration;
        fp3 direction;
        return CapsulePenetratesCapsule(a, shape, position, rotation, out direction, out penetration);
    }

    /// <summary>
    /// 由胶囊形状定义构建世界空间胶囊几何体。
    /// </summary>
    /// <param name="shape">胶囊形状定义。</param>
    /// <param name="position">世界位置。</param>
    /// <param name="rotation">世界旋转。</param>
    /// <returns>胶囊几何体。</returns>
    private static FPCapsuleGeometry BuildShapeCapsule(FPCapsuleShape shape, fp3 position, fpquaternion rotation)
    {
        fp halfHeight = fpmath.max((fp)0, shape.Height * (fp)0.5f - shape.Radius);
        fp3 up = rotation * FPMathKCC.WorldUp;
        fp3 center = position + rotation * new fp3((fp)0, shape.Center.y, (fp)0);
        return new FPCapsuleGeometry
        {
            BottomHemiCenter = center - up * halfHeight,
            TopHemiCenter = center + up * halfHeight,
            Radius = shape.Radius,
        };
    }

    /// <summary>
    /// 计算两胶囊之间的穿透分离方向与深度。
    /// </summary>
    /// <param name="a">胶囊 A 几何体。</param>
    /// <param name="shape">胶囊 B 形状定义。</param>
    /// <param name="position">胶囊 B 世界位置。</param>
    /// <param name="rotation">胶囊 B 世界旋转。</param>
    /// <param name="direction">输出分离方向。</param>
    /// <param name="distance">输出穿透深度。</param>
    /// <returns>若存在穿透则返回 true。</returns>
    private static bool CapsulePenetratesCapsule(FPCapsuleGeometry a, FPCapsuleShape shape, fp3 position, fpquaternion rotation, out fp3 direction, out fp distance)
    {
        direction = fp3.zero;
        distance = (fp)0;

        FPCapsuleGeometry b = BuildShapeCapsule(shape, position, rotation);
        fp3 c1;
        fp3 c2;
        // 求两胶囊轴线段之间的最近距离
        fp dist = FPMathKCC.SegmentSegmentDistance(a.BottomHemiCenter, a.TopHemiCenter, b.BottomHemiCenter, b.TopHemiCenter, out c1, out c2);
        fp combinedRadius = a.Radius + b.Radius;
        if (dist >= combinedRadius)
        {
            return false;
        }

        direction = dist > (fp)0.0000001f ? fpmath.normalize(c1 - c2) : FPMathKCC.WorldUp;
        distance = combinedRadius - dist;
        return true;
    }

    /// <summary>
    /// 计算胶囊与平面的穿透分离方向与深度。
    /// </summary>
    /// <param name="capsule">胶囊几何体。</param>
    /// <param name="plane">平面形状。</param>
    /// <param name="direction">输出分离方向。</param>
    /// <param name="distance">输出穿透深度。</param>
    /// <returns>若存在穿透则返回 true。</returns>
    private static bool CapsulePenetratesPlane(FPCapsuleGeometry capsule, FPPlaneShape plane, out fp3 direction, out fp distance)
    {
        direction = fpmath.normalize(plane.Normal);
        fp distBottom = fpmath.dot(capsule.BottomHemiCenter - plane.Point, direction);
        fp distTop = fpmath.dot(capsule.TopHemiCenter - plane.Point, direction);
        fp minDist = fpmath.min(distBottom, distTop);
        if (minDist > capsule.Radius)
        {
            distance = (fp)0;
            return false;
        }

        distance = capsule.Radius - minDist;
        return true;
    }

    /// <summary>
    /// 计算胶囊与盒体的穿透分离方向与深度。
    /// </summary>
    /// <param name="capsule">胶囊几何体。</param>
    /// <param name="box">盒体形状。</param>
    /// <param name="direction">输出分离方向。</param>
    /// <param name="distance">输出穿透深度。</param>
    /// <returns>若存在穿透则返回 true。</returns>
    private static bool CapsulePenetratesBox(FPCapsuleGeometry capsule, FPBoxShape box, out fp3 direction, out fp distance)
    {
        direction = fp3.zero;
        distance = (fp)0;

        fp3 localA = FPMathKCC.InverseTransformPoint(capsule.BottomHemiCenter, box.Center, box.Rotation);
        fp3 localB = FPMathKCC.InverseTransformPoint(capsule.TopHemiCenter, box.Center, box.Rotation);
        fp3 localCenter = (localA + localB) * (fp)0.5f;

        // 在盒体局部空间求最近点
        fp3 closest = new fp3(
            fpmath.clamp(localCenter.x, -box.HalfExtents.x, box.HalfExtents.x),
            fpmath.clamp(localCenter.y, -box.HalfExtents.y, box.HalfExtents.y),
            fpmath.clamp(localCenter.z, -box.HalfExtents.z, box.HalfExtents.z));

        fp3 delta = localCenter - closest;
        fp distSq = fpmath1.sqrMagnitude(delta);
        if (distSq > capsule.Radius * capsule.Radius)
        {
            // 中心距过远时，改用胶囊轴线段与最近点的距离判定
            fp3 c1;
            fp3 c2;
            fp segmentDist = FPMathKCC.SegmentSegmentDistance(localA, localB, closest, closest, out c1, out c2);
            if (segmentDist > capsule.Radius)
            {
                return false;
            }

            direction = box.Rotation * fpmath.normalize(c1 - c2);
            distance = capsule.Radius - segmentDist;
            return true;
        }

        fp dist = fpmath.sqrt(fpmath.max(distSq, (fp)0.0000001f));
        fp3 localDir = dist > (fp)0.0000001f ? delta / dist : FPMathKCC.WorldUp;
        direction = box.Rotation * localDir;
        distance = capsule.Radius - dist;
        return true;
    }
}
