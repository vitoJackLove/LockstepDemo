using System;
using Unity.Mathematics.FixedPoint;

/// <summary>
/// KCC 定点数物理查询工具。提供胶囊重叠、cast、射线检测及穿透计算。
/// </summary>
public static class FPPhysicsQuery
{
    /// <summary>
    /// 胶囊与非分配式重叠检测，结果写入调用方提供的数组。
    /// </summary>
    /// <param name="position">胶囊世界位置。</param>
    /// <param name="rotation">胶囊世界旋转。</param>
    /// <param name="radius">胶囊半径。</param>
    /// <param name="height">胶囊总高度。</param>
    /// <param name="yOffset">局部 Y 轴偏移。</param>
    /// <param name="results">结果缓冲区。</param>
    /// <param name="layerMask">层掩码过滤。</param>
    /// <param name="queryTriggers">是否包含触发器。</param>
    /// <returns>命中的碰撞体数量（可能超过缓冲区长度）。</returns>
    public static int OverlapCapsuleNonAlloc(
        fp3 position,
        fpquaternion rotation,
        fp radius,
        fp height,
        fp yOffset,
        IFPCollider[] results,
        FPLayerMask layerMask,
        bool queryTriggers = false)
    {
        FPCollisionWorld world = FPCollisionWorld.Instance;
        FPCapsuleGeometry capsule = FPMathKCC.BuildCapsuleGeometry(position, rotation, radius, height, yOffset);
        int count = 0;

        for (int i = 0; i < world.Colliders.Count; i++)
        {
            IFPCollider collider = world.Colliders[i];
            if (collider.IsTrigger && !queryTriggers)
            {
                continue;
            }

            if (!layerMask.Contains(collider.Layer))
            {
                continue;
            }

            if (!FPCapsuleCollision.OverlapCapsule(capsule, collider))
            {
                continue;
            }

            if (count < results.Length)
            {
                results[count++] = collider;
            }
        }

        return count;
    }

    /// <summary>
    /// 胶囊 cast 非分配式检测。
    /// </summary>
    /// <param name="position">胶囊起始世界位置。</param>
    /// <param name="rotation">胶囊世界旋转。</param>
    /// <param name="radius">胶囊半径。</param>
    /// <param name="height">胶囊总高度。</param>
    /// <param name="yOffset">局部 Y 轴偏移。</param>
    /// <param name="direction">cast 方向。</param>
    /// <param name="distance">cast 最大距离。</param>
    /// <param name="hits">命中结果缓冲区。</param>
    /// <param name="layerMask">层掩码过滤。</param>
    /// <param name="queryTriggers">是否包含触发器。</param>
    /// <returns>写入缓冲区的命中数量。</returns>
    public static int CapsuleCastNonAlloc(
        fp3 position,
        fpquaternion rotation,
        fp radius,
        fp height,
        fp yOffset,
        fp3 direction,
        fp distance,
        FPRaycastHit[] hits,
        FPLayerMask layerMask,
        bool queryTriggers = false)
    {
        FPCollisionWorld world = FPCollisionWorld.Instance;
        FPCapsuleGeometry capsule = FPMathKCC.BuildCapsuleGeometry(position, rotation, radius, height, yOffset);
        int count = 0;
        fp closestDistance = distance + (fp)1;

        for (int i = 0; i < world.Colliders.Count; i++)
        {
            IFPCollider collider = world.Colliders[i];
            if (collider.IsTrigger && !queryTriggers)
            {
                continue;
            }

            if (!layerMask.Contains(collider.Layer))
            {
                continue;
            }

            FPRaycastHit hit;
            if (!FPCapsuleCollision.CapsuleCast(capsule, direction, distance, collider, out hit))
            {
                continue;
            }

            if (hit.Distance < closestDistance)
            {
                if (count < hits.Length)
                {
                    hits[count++] = hit;
                }
            }
        }

        return count;
    }

    /// <summary>
    /// 胶囊 cast 单次检测，返回最近命中。
    /// </summary>
    /// <param name="position">胶囊起始世界位置。</param>
    /// <param name="rotation">胶囊世界旋转。</param>
    /// <param name="radius">胶囊半径。</param>
    /// <param name="height">胶囊总高度。</param>
    /// <param name="yOffset">局部 Y 轴偏移。</param>
    /// <param name="direction">cast 方向。</param>
    /// <param name="distance">cast 最大距离。</param>
    /// <param name="layerMask">层掩码过滤。</param>
    /// <param name="closestHit">输出最近命中结果。</param>
    /// <param name="queryTriggers">是否包含触发器。</param>
    /// <returns>若命中则返回 true。</returns>
    public static bool CapsuleCast(
        fp3 position,
        fpquaternion rotation,
        fp radius,
        fp height,
        fp yOffset,
        fp3 direction,
        fp distance,
        FPLayerMask layerMask,
        out FPRaycastHit closestHit,
        bool queryTriggers = false)
    {
        closestHit = default;
        FPRaycastHit[] buffer = FPPhysicsScratch.RaycastHits;
        int count = CapsuleCastNonAlloc(position, rotation, radius, height, yOffset, direction, distance, buffer, layerMask, queryTriggers);
        if (count <= 0)
        {
            return false;
        }

        // 从缓冲区中选取最近命中
        fp best = distance + (fp)1;
        for (int i = 0; i < count; i++)
        {
            if (buffer[i].Distance < best)
            {
                best = buffer[i].Distance;
                closestHit = buffer[i];
            }
        }

        return true;
    }

    /// <summary>
    /// 射线非分配式检测，返回最近命中（最多 1 条）。
    /// </summary>
    /// <param name="origin">射线起点。</param>
    /// <param name="direction">射线方向。</param>
    /// <param name="distance">射线最大距离。</param>
    /// <param name="hits">命中结果缓冲区。</param>
    /// <param name="layerMask">层掩码过滤。</param>
    /// <param name="queryTriggers">是否包含触发器。</param>
    /// <returns>命中数量（0 或 1）。</returns>
    public static int RaycastNonAlloc(
        fp3 origin,
        fp3 direction,
        fp distance,
        FPRaycastHit[] hits,
        FPLayerMask layerMask,
        bool queryTriggers = false)
    {
        FPCollisionWorld world = FPCollisionWorld.Instance;
        int count = 0;
        fp bestDistance = distance + (fp)1;
        int bestIndex = -1;

        for (int i = 0; i < world.Colliders.Count; i++)
        {
            IFPCollider collider = world.Colliders[i];
            if (collider.IsTrigger && !queryTriggers)
            {
                continue;
            }

            if (!layerMask.Contains(collider.Layer))
            {
                continue;
            }

            FPRaycastHit hit;
            if (!FPCapsuleCollision.Raycast(origin, direction, distance, collider, out hit))
            {
                continue;
            }

            if (hit.Distance < bestDistance)
            {
                bestDistance = hit.Distance;
                bestIndex = count;
                if (count < hits.Length)
                {
                    hits[count] = hit;
                }

                count = Math.Min(count + 1, hits.Length);
            }
        }

        // 将最近命中交换到缓冲区首位
        if (bestIndex > 0 && bestIndex < hits.Length)
        {
            FPRaycastHit temp = hits[0];
            hits[0] = hits[bestIndex];
            hits[bestIndex] = temp;
        }

        return bestIndex >= 0 ? 1 : 0;
    }

    /// <summary>
    /// 计算胶囊与碰撞体之间的穿透深度与分离方向。
    /// </summary>
    /// <param name="capsulePosition">胶囊世界位置。</param>
    /// <param name="capsuleRotation">胶囊世界旋转。</param>
    /// <param name="capsuleRadius">胶囊半径。</param>
    /// <param name="capsuleHeight">胶囊总高度。</param>
    /// <param name="capsuleYOffset">局部 Y 轴偏移。</param>
    /// <param name="collider">目标碰撞体。</param>
    /// <param name="direction">输出分离方向。</param>
    /// <param name="distance">输出穿透深度。</param>
    /// <returns>若存在穿透则返回 true。</returns>
    public static bool ComputePenetration(
        fp3 capsulePosition,
        fpquaternion capsuleRotation,
        fp capsuleRadius,
        fp capsuleHeight,
        fp capsuleYOffset,
        IFPCollider collider,
        out fp3 direction,
        out fp distance)
    {
        FPCapsuleGeometry capsule = FPMathKCC.BuildCapsuleGeometry(capsulePosition, capsuleRotation, capsuleRadius, capsuleHeight, capsuleYOffset);
        return FPCapsuleCollision.TryComputePenetration(capsule, collider, out direction, out distance);
    }

    /// <summary>
    /// 查询两层之间是否发生碰撞。
    /// </summary>
    /// <param name="a">碰撞层 A。</param>
    /// <param name="b">碰撞层 B。</param>
    /// <returns>若应碰撞则返回 true。</returns>
    public static bool GetLayerCollision(FPCollisionLayer a, FPCollisionLayer b)
    {
        return FPCollisionWorld.Instance.GetLayersCollide(a, b);
    }
}

/// <summary>
/// 物理查询临时缓冲区，避免运行时分配。
/// </summary>
internal static class FPPhysicsScratch
{
    /// <summary>射线/cast 命中结果临时数组。</summary>
    public static readonly FPRaycastHit[] RaycastHits = new FPRaycastHit[16];
}
