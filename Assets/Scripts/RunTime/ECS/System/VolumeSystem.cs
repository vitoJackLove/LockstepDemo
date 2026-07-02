using System.Collections.Generic;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// 受击盒管理系统
/// </summary>
public class VolumeSystem : BaseSystem
{
    /// <summary>
    /// 实体本地id与对应受击盒列表
    /// </summary>
    private Dictionary<int, List<HitVolume>> _hitVolumes;
    
    /// <summary>
    /// 子弹受击盒
    /// </summary>
    private Dictionary<int, List<HitVolume>> _bulletEntityVolumes = new Dictionary<int, List<HitVolume>>();

    /// <summary>
    /// 等待销毁的受击盒的持有者id集合（已废弃批量释放，保留字段避免序列化引用）
    /// </summary>
    private List<int> _cacheRemoveId = new List<int>();

    /// <summary>
    /// 动态实体受击盒持有者，死亡时仅标记 Failed 以供回滚恢复
    /// </summary>
    private HashSet<int> _dynamicVolumeOwnerIds = new HashSet<int>();

    /// <summary>
    /// 碰撞检测前缓存子弹 id，避免命中销毁子弹时修改字典导致枚举异常
    /// </summary>
    private readonly List<int> _bulletCheckCache = new List<int>();

    /// <summary>
    /// 同一逻辑帧内 (attackerId, targetId) 只触发一次 TransferHit，保证帧同步确定性
    /// </summary>
    private readonly HashSet<long> _transferHitDedup = new HashSet<long>();

    public override void OnInit(object data = null)
    {
        base.OnInit(data);
        
        _hitVolumes = new Dictionary<int, List<HitVolume>>();
    }

    /// <summary>
    /// 注册实体的所有受击盒
    /// </summary>
    /// <param name="ownerEntity"></param>
    /// <param name="datas"></param>
    public void RegisterHitVolume(BaseEntity ownerEntity, Dictionary<string, VolumeData> datas)
    {
        if (datas.Count <= 0)
        {
            return;
        }

        List<HitVolume> volumes;

        if (_hitVolumes.TryGetValue(ownerEntity.EntityId, out volumes))
        {
            // 该实体的受击盒已经注册
            // 清掉重新注册
            _hitVolumes.Remove(ownerEntity.EntityId);

            _bulletEntityVolumes.Remove(ownerEntity.EntityId);
            _dynamicVolumeOwnerIds.Remove(ownerEntity.EntityId);
            _cacheRemoveId.Remove(ownerEntity.EntityId);

            for (int i = 0; i < volumes.Count; i++)
            {
                FPoolHelper.Release<HitVolume>(volumes[i]);
            }
        }

        volumes = new List<HitVolume>();

        foreach (var data in datas)
        {
            HitVolume hitVolume = HitVolume.Create(ownerEntity, data.Key, data.Value);
            
            volumes.Add(hitVolume);
        }

        _hitVolumes.Add(ownerEntity.EntityId, volumes);

        if (ownerEntity.ForecastEntityType == ForecastEntityType.DynamicEntity)
        {
            _dynamicVolumeOwnerIds.Add(ownerEntity.EntityId);
        }

        if (ownerEntity.EntityType == EntityType.BulletEntity)
        {
            _bulletEntityVolumes.Add(ownerEntity.EntityId, volumes);
        }
    }

    public override void OnUpdate(fp deltaTime)
    {
        base.OnUpdate(deltaTime);

        var dic = _hitVolumes.GetEnumerator();

        while (dic.MoveNext())
        {
            for (int i = 0; i < dic.Current.Value.Count; i++)
            {
                dic.Current.Value[i].PrimitiveDebug();
            }
        }

        dic.Dispose();
    }

    public override void OnFixedUpdate(fp deltaTime,WorldUpdateType worldUpdateType)
    {
        base.OnFixedUpdate(deltaTime, worldUpdateType);

        _transferHitDedup.Clear();
        
        var dic = _hitVolumes.GetEnumerator();

        while (dic.MoveNext())
        {
            for (int i = 0; i < dic.Current.Value.Count; i++)
            {
                dic.Current.Value[i].OnUpdate(deltaTime, worldUpdateType);
            }
        }

        dic.Dispose();

        //子弹和Target 实体
        CheckBulletAndTargetEntityIntersection(worldUpdateType);
    }


    private void CheckBulletAndTargetEntityIntersection(WorldUpdateType worldUpdateType)
    {
        if (_bulletEntityVolumes.Count <= 0)
        {
            return;
        }

        _bulletCheckCache.Clear();
        _bulletCheckCache.AddRange(_bulletEntityVolumes.Keys);

        for (int b = 0; b < _bulletCheckCache.Count; b++)
        {
            int bulletId = _bulletCheckCache[b];

            if (!IsActiveBulletVolume(bulletId))
            {
                continue;
            }

            foreach (var heroEntity in GetSystem<EntitySystem>().HeroEntityList)
            {
                IsIntersect(bulletId, heroEntity.EntityId, worldUpdateType);
            }
        }
    }

    /// <summary>
    /// 取消注册实体的所有受击盒（标记失活，供回滚恢复）
    /// </summary>
    /// <param name="id"></param>
    public void UnRegisterHitVolume(int id)
    {
        MarkHitVolumesFailed(id);
    }

    /// <summary>
    /// 释放并回收实体的受击盒（实体销毁时调用）
    /// </summary>
    /// <param name="id"></param>
    public void ReleaseHitVolume(int id)
    {
        if (!_hitVolumes.TryGetValue(id, out var volumes))
        {
            return;
        }

        if (volumes != null)
        {
            for (int i = 0; i < volumes.Count; i++)
            {
                FPoolHelper.Release<HitVolume>(volumes[i]);
            }
        }

        _hitVolumes.Remove(id);
        _bulletEntityVolumes.Remove(id);
        _dynamicVolumeOwnerIds.Remove(id);
        _cacheRemoveId.Remove(id);
    }

    private void MarkHitVolumesFailed(int id)
    {
        if (!_hitVolumes.TryGetValue(id, out var volumes) || volumes == null)
        {
            return;
        }

        for (int i = 0; i < volumes.Count; i++)
        {
            volumes[i].EntityState = EntityState.Failed;
        }
    }

    public void RestoreHitVolume(int id)
    {
        if (!_hitVolumes.TryGetValue(id, out var volumes) || volumes == null)
        {
            return;
        }

        for (int i = 0; i < volumes.Count; i++)
        {
            volumes[i].EntityState = EntityState.Survival;
        }

        BaseEntity entity = GetSystem<EntitySystem>().GetEntity(id);
        if (entity != null && entity.EntityType == EntityType.BulletEntity && entity.EntityState == EntityState.Survival)
        {
            _bulletEntityVolumes[id] = volumes;
        }
    }

    private bool IsActiveBulletVolume(int bulletId)
    {
        if (!_hitVolumes.TryGetValue(bulletId, out var volumes) || volumes == null)
        {
            return false;
        }

        for (int i = 0; i < volumes.Count; i++)
        {
            if (volumes[i].IsCollisionActive)
            {
                return true;
            }
        }

        return false;
    }

    private static long MakeTransferHitKey(int attackerId, int targetId)
    {
        return ((long)attackerId << 32) | (uint)targetId;
    }

    private void TransferHitOnce(int attackerId, int targetId, HitVolume attackerVolume, int fromEntityId)
    {
        if (!_transferHitDedup.Add(MakeTransferHitKey(attackerId, targetId)))
        {
            return;
        }

        attackerVolume.TransferHit(fromEntityId);
    }

    private bool IsIntersectHitVolumes(HitVolume attackerVolume, HitVolume targetVolume, int attackerId, int targetId)
    {
        if (!Primitive.IsIntersect(attackerVolume.Primitive, targetVolume.Primitive))
        {
            return false;
        }

        TransferHitOnce(attackerId, targetId, attackerVolume, targetVolume.OwnerId);
        return true;
    }

    /// <summary>
    /// 传入攻击者本地实体id与检测目标本地实体id，查看攻击是否命中
    /// </summary>
    /// <param name="attackerId"></param>
    /// <param name="targetId"></param>
    /// <returns></returns>
    private bool IsIntersect(int attackerId, int targetId, WorldUpdateType worldUpdateType)
    {
        _hitVolumes.TryGetValue(attackerId, out var v1);
        _hitVolumes.TryGetValue(targetId, out var v2);

        if (v1 == null || v2 == null)
        {
            // 有一方没有受击盒，不需要记录受击信息
            return false;
        }

        bool result = false;

        for (int i = 0; i < v1.Count; i++)
        {
            for (int j = 0; j < v2.Count; j++)
            {
                bool r1 = false;
                
                HitVolume attackerVolume = v1[i];
                HitVolume targetVolume = v2[j];
                
                if (!ShouldCheckVolumePair(attackerVolume, targetVolume, worldUpdateType))
                {
                    continue;
                }

                r1 = IsIntersectHitVolumes(attackerVolume, targetVolume, attackerId, targetId);
                
                if (r1 && !result)
                {
                    result = true;
                }
            }
        }

        return result;
    }

    private bool ShouldCheckVolumePair(HitVolume attackerVolume, HitVolume targetVolume, WorldUpdateType worldUpdateType)
    {
        if (attackerVolume == null || targetVolume == null)
        {
            return false;
        }

        if (!attackerVolume.IsCollisionActive || !targetVolume.IsCollisionActive)
        {
            return false;
        }

        if (attackerVolume.EntityUpdateType != targetVolume.EntityUpdateType)
        {
            return false;
        }

        if (worldUpdateType == WorldUpdateType.Authority)
        {
            return attackerVolume.EntityUpdateType == EntityUpdateType.AuthorityEntity;
        }

        if (worldUpdateType == WorldUpdateType.Local || worldUpdateType == WorldUpdateType.RollBack)
        {
            return attackerVolume.EntityUpdateType == EntityUpdateType.LocalEntity;
        }

        return false;
    }

    /// <summary>
    /// 传入攻击者受击盒与检测目标本地实体id，查看攻击是否命中
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="targetId"></param>
    /// <returns></returns>
    public bool IsIntersect(HitVolume v1, int targetId, WorldUpdateType worldUpdateType)
    {
        _hitVolumes.TryGetValue(targetId, out var v2);

        if (v1 == null || v2 == null)
        {
            // 有一方没有受击盒，不需要记录受击信息
            return false;
        }

        bool result = false;

        for (int j = 0; j < v2.Count; j++)
        {
            HitVolume targetVolume = v2[j];

            if (!ShouldCheckVolumePair(v1, targetVolume, worldUpdateType))
            {
                continue;
            }

            bool r1 = IsIntersectHitVolumes(v1, targetVolume, v1.OwnerId, targetId);

            if (r1 && !result)
            {
                result = true;
            }
        }

        return result;
    }

    public void AdjustHeroCapsuleVolume(int entityId, fp radius)
    {
        _hitVolumes.TryGetValue(entityId, out var v1);

        if (v1 == null)
        {
            return;
        }

        for (int i = 0; i < v1.Count; i++)
        {
            v1[i].AdjustCapsuleRadius(radius);
        }
    }

    /*/// <summary>
    /// 获取距离point点最近的受击盒,距离相等时取权重最高的一个
    /// </summary>
    /// <param name="point"></param>
    /// <param name="entityId"></param>
    /// <param name="variableDatas"></param>
    /// <returns></returns>
    public HitVolume GetClosestVolume(Vector3 point, int entityId,
        Dictionary<string, HitColliderVariableData> variableDatas)
    {
        if (!_hitVolumes.TryGetValue(entityId, out List<HitVolume> volumes))
        {
            return null;
        }

        int minidx = 0;

        for (int i = 0; i < volumes.Count; i++)
        {
            if (Vector3.Magnitude(volumes[minidx].PrimitiveInfo.Center - point) >
                Vector3.Magnitude(volumes[i].PrimitiveInfo.Center - point))
            {
                minidx = i;
                continue;
            }

            if (Mathf.Abs(Vector3.Magnitude(volumes[minidx].PrimitiveInfo.Center - point) -
                          Vector3.Magnitude(volumes[i].PrimitiveInfo.Center - point)) < 0.01)
            {
                minidx = variableDatas[volumes[i].Key].Weight > variableDatas[volumes[minidx].Key].Weight ? i : minidx;
            }
        }

        return volumes[minidx];
    }*/


    /// <summary>
    /// 获取direct方向的反方向上距离targetPoint最远的受击盒
    /// </summary>
    /// <param name="point"></param>
    /// <param name="direct"></param>
    /// <param name="volumes"></param>
    /// <param name="hitTargetPoint"></param>
    /// <returns></returns>
    public string GetFarthestVolume(fp3 point, fp3 direct, List<HitVolume> volumes, fp3 hitTargetPoint)
    {
        if (volumes == null || volumes.Count == 0)
        {
            return null;
        }

        int maxidx = -1;

        for (int i = 0; i < volumes.Count; i++)
        {
            // 受击盒没有在该方向上受击
            if (!Primitive.IsIntersect(point, direct, volumes[i].Primitive))
            {
                continue;
            }

            if (maxidx < 0)
            {
                maxidx = i;
                continue;
            }

            fp3 ipro = IntersectionDetection.PointProjectionOnLineSeg(point, point + direct,
                volumes[i].PrimitiveInfo.Center) - hitTargetPoint;

            fp3 maxpro = IntersectionDetection.PointProjectionOnLineSeg(point, point + direct,
                volumes[maxidx].PrimitiveInfo.Center) - hitTargetPoint;

            // ipro与maxpro同向
            if (fpmath.dot(ipro, maxpro) >= 0)
            {
                // 同向且都在正方向上
                if (fpmath.dot(ipro, direct) >= 0 && fpmath.dot(maxpro, direct) >= 0)
                {
                    if (fpmath1.magnitude(ipro - hitTargetPoint) <= fpmath1.magnitude(maxpro - hitTargetPoint))
                    {
                        maxidx = i;
                    }

                    continue;
                }

                // 同向且都在反方向上
                if (fpmath1.magnitude(ipro - hitTargetPoint) >= fpmath1.magnitude(maxpro - hitTargetPoint))
                {
                    maxidx = i;
                }

                continue;
            }

            // ipro与maxpro反向
            // ipro投影在正向上
            if (fpmath.dot(ipro, direct) >= 0)
            {
                continue;
            }

            // max投影在正向上
            maxidx = i;
        }

        return maxidx >= 0 ? volumes[maxidx].Key : null;
    }

    /*/// <summary>
    /// 获取权重最高的受击盒
    /// </summary>
    /// <param name="variableData"></param>
    /// <returns></returns>
    public int GetHighestWeightVolume(List<HitColliderVariableData> variableData)
    {
        int maxidx = 0;

        for (int i = 0; i < variableData.Count; i++)
        {
            if (variableData[i].Weight >= variableData[maxidx].Weight)
            {
                maxidx = i;
            }
        }

        return maxidx;
    }*/

    /*public override void OnReset()
    {
        base.OnReset();
        var dic = _hitVolumes.GetEnumerator();

        while (dic.MoveNext())
        {
            foreach (var volume in dic.Current.Value)
            {
                if (volume != null)
                {
                    ReferencePool.Release(volume);
                }
            }

            dic.Current.Value.Clear();
        }

        dic.Dispose();

        _hitVolumes.Clear();
    }*/

    public override void OnDispose()
    {
        if (_hitVolumes != null)
        {
            foreach (var pair in _hitVolumes)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                for (int i = 0; i < pair.Value.Count; i++)
                {
                    FPoolHelper.Release<HitVolume>(pair.Value[i]);
                }
            }

            _hitVolumes.Clear();
        }

        _bulletEntityVolumes.Clear();
        _dynamicVolumeOwnerIds.Clear();
        _cacheRemoveId.Clear();
        _bulletCheckCache.Clear();
        _transferHitDedup.Clear();

        base.OnDispose();
    }
}
