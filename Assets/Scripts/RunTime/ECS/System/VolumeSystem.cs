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
    /// 等待销毁的受击盒的持有者id集合
    /// </summary>
    private List<int> _cacheRemoveId = new List<int>();

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

        ReleaseDeadEntity();
        
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

    /*/// <summary>
    /// 相交检测
    /// </summary>
    private void CheckTargetEntityIntersection()
    {
        foreach (var monsterEntity in GetSystem<EntitySystem>().MonsterEntityList)
        {
            foreach (var heroEntity in GetSystem<EntitySystem>().HeroEntityList)
            {
                IsIntersect(monsterEntity.EntityId, heroEntity.EntityId);
            }
        }
    }*/

    private void CheckBulletAndTargetEntityIntersection(WorldUpdateType worldUpdateType)
    {
        foreach (var bulletId in _bulletEntityVolumes.Keys)
        {
            foreach (var heroEntity in GetSystem<EntitySystem>().HeroEntityList)
            {
                IsIntersect(bulletId, heroEntity.EntityId, worldUpdateType);
            }
            
            foreach (var monsterEntity in GetSystem<EntitySystem>().MonsterEntityList)
            {
                IsIntersect(bulletId, monsterEntity.EntityId, worldUpdateType);
            }
        }
    }

    /// <summary>
    /// 取消注册实体的所有受击盒
    /// </summary>
    /// <param name="id"></param>
    public void UnRegisterHitVolume(int id)
    {
        lock (this._cacheRemoveId)
        {
            if (_hitVolumes.TryGetValue(id, out var  volumes))
            {
                if (volumes != null)
                {
                    for (int i = 0; i < volumes.Count; i++)
                    {
                        volumes[i].EntityState = EntityState.Failed;
                    }
                }
            }
        }
    }

    public void RestoreHitVolume(int id)
    {
        if (_hitVolumes.TryGetValue(id, out var volumes))
        {
            if (volumes == null)
            {
                return;
            }

            for (int i = 0; i < volumes.Count; i++)
            {
                volumes[i].EntityState = EntityState.Survival;
            }
        }
    }

    private bool IsIntersect(BaseVolume v1, BaseVolume v2)
    {
        bool result = Primitive.IsIntersect(v1.Primitive, v2.Primitive);

        if (result)
        {
            // 将结果传递给两个volume
            // 实体通过volume的结果处理攻击信息
            v1.TransferHit(v2.OwnerId);
        }

        return result;
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
                
                if (attackerVolume.EntityState == EntityState.Survival && targetVolume.EntityState == EntityState.Survival)
                {
                    if (ShouldCheckVolumePair(attackerVolume, targetVolume, worldUpdateType))
                    {
                        r1 = IsIntersect(attackerVolume, targetVolume);
                    }
                }
                
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
    public bool IsIntersect(HitVolume v1, int targetId)
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
            bool r1 = IsIntersect(v1, v2[j]);

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
        int maxidx = 0;

        fp3 negDirect = -fpmath.normalize(direct);
        fp3 negPoint = hitTargetPoint + negDirect;
        fp3 posPoint = hitTargetPoint + direct;

        for (int i = 0; i < volumes.Count; i++)
        {
            // 受击盒没有在该方向上受击
            if (!Primitive.IsIntersect(point, direct, volumes[i].Primitive))
            {
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

        return volumes[maxidx].Key;
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

    /// <summary>
    /// 释放死亡的实体的受击盒
    /// </summary>
    private void ReleaseDeadEntity()
    {
        lock (this._cacheRemoveId)
        {
            if (this._cacheRemoveId.Count <= 0)
            {
                return;
            }

            foreach (var entityId in _cacheRemoveId)
            {
                List<HitVolume> volumes;

                if (!_hitVolumes.TryGetValue(entityId, out volumes))
                {
                    continue;
                }

                if (volumes != null)
                {
                    for (int i = 0; i < volumes.Count; i++)
                    {
                        FPoolHelper.Release<HitVolume>(volumes[i]);
                    }
                }
                
                _hitVolumes.Remove(entityId);
            }

            this._cacheRemoveId.Clear();
        }
    }

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
        base.OnDispose();
    }
}
