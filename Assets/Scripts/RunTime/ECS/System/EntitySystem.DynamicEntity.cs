using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 动态实体管理
/// </summary>
public partial class EntitySystem 
{
    /// <summary>
    /// 存活的动态预测实体
    /// </summary>
    private List<BaseEntity> _survivalLocalDynamic = new ();

    /// <summary>
    /// 存活的动态预测实体 （用于查询）
    /// </summary>
    private Dictionary<int, BaseEntity> _survivalLocalDynamicDic = new ();

    /// <summary>
    /// 存活的动态预测实体
    /// </summary>
    private List<BaseEntity> _survivalAuthorityDynamic = new List<BaseEntity>();
        
    /// <summary>
    /// 存活的动态权威实体 （用于查询）
    /// </summary>
    private Dictionary<int, BaseEntity> _survivalAuthorityDynamicDic = new ();
    
    /// <summary>
    ///  Key = 实体的指纹 缓存的动态本地实体(已经失活的动态实体)
    /// </summary>
    private Dictionary<int, BaseEntity> _cacheDynamicLocalEntity = new();
    
    /// <summary>
    /// 注册动态的实体 （动态实体包括：子弹 Buff 等游戏过程中生成的逻辑实体）
    /// </summary>
    private void RegisterDynamicEntity(BaseEntity dynamicEntity)
    {
        if (dynamicEntity.ForecastEntityType == ForecastEntityType.DynamicEntity)
        {
            if (dynamicEntity.EntityUpdateType == EntityUpdateType.AuthorityEntity)
            {
                _survivalAuthorityDynamic.Add(dynamicEntity);
                _survivalAuthorityDynamicDic.Add(dynamicEntity.EntityFingerprints, dynamicEntity);
            }
            else
            {
                _survivalLocalDynamic.Add(dynamicEntity);
                _survivalLocalDynamicDic.Add(dynamicEntity.EntityFingerprints, dynamicEntity);
            }
        }
        else
        {
            GameLog.Error(GameLogChannel.Battle, "注册动态实体关联错误：实体不是动态类型...");
        }
    }
    
    /// <summary>
    /// 移除动态实体
    /// </summary>
    /// <param name="dynamicEntity"></param>
    private void RemoveDynamicEntity(BaseEntity dynamicEntity)
    {
        if (dynamicEntity.ForecastEntityType == ForecastEntityType.DynamicEntity)
        {
            if (dynamicEntity.EntityUpdateType == EntityUpdateType.LocalEntity)
            {
                _survivalLocalDynamic.Remove(dynamicEntity);
                _survivalLocalDynamicDic.Remove(dynamicEntity.EntityFingerprints);
                _cacheDynamicLocalEntity.TryAdd(dynamicEntity.EntityFingerprints, dynamicEntity);
            }
            else
            {
                _survivalAuthorityDynamic.Remove(dynamicEntity);
                _survivalAuthorityDynamicDic.Remove(dynamicEntity.EntityFingerprints);
            }
        }
    }

    private void RestoreDynamicEntity(BaseEntity dynamicEntity)
    {
        if (dynamicEntity == null)
        {
            return;
        }

        if (dynamicEntity.ForecastEntityType != ForecastEntityType.DynamicEntity)
        {
            GameLog.Error(GameLogChannel.Battle, "恢复动态实体错误：实体不是动态类型...");
            return;
        }

        if (!_executeEntityDic.ContainsKey(dynamicEntity.EntityId))
        {
            _executeEntityDic.Add(dynamicEntity.EntityId, dynamicEntity);
        }

        if (!_executeEntityList.Contains(dynamicEntity))
        {
            _executeEntityList.Add(dynamicEntity);
        }

        if (dynamicEntity.EntityUpdateType == EntityUpdateType.LocalEntity)
        {
            if (!_survivalLocalDynamic.Contains(dynamicEntity))
            {
                _survivalLocalDynamic.Add(dynamicEntity);
            }

            _survivalLocalDynamicDic[dynamicEntity.EntityFingerprints] = dynamicEntity;
            _cacheDynamicLocalEntity.Remove(dynamicEntity.EntityFingerprints);
        }
        else
        {
            if (!_survivalAuthorityDynamic.Contains(dynamicEntity))
            {
                _survivalAuthorityDynamic.Add(dynamicEntity);
            }

            _survivalAuthorityDynamicDic[dynamicEntity.EntityFingerprints] = dynamicEntity;
        }

        GetSystem<VolumeSystem>()?.RestoreHitVolume(dynamicEntity.EntityId);
    }
    
    /// <summary>
    /// 通过指纹 获取预测的动态实体
    /// </summary>
    /// <param name="entityFingerprints">实体指纹</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T GetDynamicLocalEntity<T>(int entityFingerprints) where T : BaseEntity
    {
        _survivalLocalDynamicDic.TryGetValue(entityFingerprints, out var baseEntity);

        return (T)baseEntity;
    }

    /// <summary>
    /// 閫氳繃鎸囩汗 鑾峰彇瀛樻椿鎴栫紦瀛樼殑鏈湴鍔ㄦ€佸疄浣?
    /// </summary>
    /// <param name="entityFingerprints">瀹炰綋鎸囩汗</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T GetDynamicLocalOrCachedEntity<T>(int entityFingerprints) where T : BaseEntity
    {
        _survivalLocalDynamicDic.TryGetValue(entityFingerprints, out var baseEntity);

        if (baseEntity == null)
        {
            _cacheDynamicLocalEntity.TryGetValue(entityFingerprints, out baseEntity);
        }

        return baseEntity as T;
    }
    
    /// <summary>
    /// 通过指纹 获取权威的动态实体
    /// </summary>
    /// <param name="entityFingerprints">实体指纹</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T GetDynamicAuthorityEntity<T>(int entityFingerprints) where T : BaseEntity
    {
        _survivalAuthorityDynamicDic.TryGetValue(entityFingerprints, out var baseEntity);

        return (T)baseEntity;
    }
}
