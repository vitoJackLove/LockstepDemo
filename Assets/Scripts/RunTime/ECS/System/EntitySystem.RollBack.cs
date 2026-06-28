using System.Collections.Generic;
using Ase.Serializing;

/// <summary>
/// 实体回滚
/// </summary>
public partial class EntitySystem
{
    public override void RollBack(uint tick, RollBackType rollBackType)
    {
        //如果是硬回滚
        if ((rollBackType & RollBackType.HardRollBack) != 0)
        {
            //静态实体回滚
            foreach (var localEntity in _entityMap.Keys)
            {
                var authorityEntity = _entityMap[localEntity];
                
                BaseSnapShotData authorityHardSnapShot = authorityEntity.GetHardSnapShot(tick);
                
                localEntity.HardRollBack(
                    ReaderPool.GetReader(authorityHardSnapShot.PooledWriter.GetArraySegment()));
            }
        }

        //软回滚
        foreach (var localEntity in _entityMap.Keys)
        {
            var authorityEntity = _entityMap[localEntity];
                
            BaseSnapShotData authoritySoftSnapShot = authorityEntity.GetSoftSnapShot(tick);
            
            localEntity.SoftRollBack(
                ReaderPool.GetReader(authoritySoftSnapShot.PooledWriter.GetArraySegment()));
        }

        DynamicEntityRollBack(tick);
    }

    /// <summary>
    /// 动态实体回滚
    /// </summary>
    /// <param name="rollBackTick"></param>
    private void DynamicEntityRollBack(uint rollBackTick)
    {
        //动态权威快照
        BaseSnapShotData dynamicAuthoritySnapShot = _dynamicEntityAuthorityShotData[rollBackTick];

        var dynamicAuthorityData = ReaderPool.GetReader(dynamicAuthoritySnapShot.PooledWriter.GetArraySegment());

        //权威实体数量
        int dynamicAuthorityCount = dynamicAuthorityData.ReadInt32();

        //权威实体指纹
        List<int> dynamicAuthorityEntityFingerprintsList = new List<int>();
        
        for (int i = 0; i < dynamicAuthorityCount; i++)
        {
            int fingerprints = dynamicAuthorityData.ReadInt32();
            
            dynamicAuthorityEntityFingerprintsList.Add(fingerprints);
        }
        
        //拿到当前的所有动态实体指纹
        List<int> currentDynamicEntityId = new List<int>();

        for (int i = 0; i < _survivalLocalDynamic.Count; i++)
        {
            currentDynamicEntityId.Add(_survivalLocalDynamic[i].EntityFingerprints);
        }
        
        Dictionary<int, BaseSnapShotData> dynamicAuthorityShotData = _dynamicAuthorityEntityDic[rollBackTick];

        //找出多余的动态实体或者指纹匹配不上的
        for (int i = 0; i <  _survivalLocalDynamic.Count; i++)
        {
            var localDynamicEntity = _survivalLocalDynamic[i];

            //权威有 预测也有 覆盖数据
            if (dynamicAuthorityEntityFingerprintsList.Contains(localDynamicEntity.EntityFingerprints))
            {
                BaseSnapShotData authorityHardSnapShot = dynamicAuthorityShotData[localDynamicEntity.EntityFingerprints];
                
                localDynamicEntity.HardRollBack(ReaderPool.GetReader(authorityHardSnapShot.PooledWriter.GetArraySegment()));
            }
            //权威有 预测没有 销毁实体
            else
            {
                //销毁指纹对应不上的预测实体
                localDynamicEntity.DoEntityDead();
            }
        }
        
        //找出少的恢复
        for (int i = 0; i < dynamicAuthorityEntityFingerprintsList.Count; i++)
        {
            var id = dynamicAuthorityEntityFingerprintsList[i];
                
            if (!currentDynamicEntityId.Contains(id))
            {
                BaseSnapShotData authorityHardSnapShot = dynamicAuthorityShotData[id];

                if (_cacheDynamicLocalEntity.TryGetValue(id, out BaseEntity cacheDynamicEntity))
                {
                    cacheDynamicEntity.HardRollBack(
                        ReaderPool.GetReader(authorityHardSnapShot.PooledWriter.GetArraySegment()));

                    RestoreDynamicEntity(cacheDynamicEntity);
                }
                else
                {
                    GameLog.Error(GameLogChannel.Rollback, $"动态实体回滚错误：本地缓存中找不到指纹 {id} 对应的实体...");
                }
            }
        }
    }

    /// <summary>
    /// 校验预测
    /// </summary>
    /// <param name="tick"></param>
    /// <returns></returns>
    public override RollBackType VerifyForecast(uint tick)
    {
        RollBackType rollBackType = RollBackType.NoRollBack;

        //静态对比
        foreach (var localEntity in _entityMap.Keys)
        {
            var authorityEntity = _entityMap[localEntity];
            
            rollBackType |= VerifyEntitySnapShot(localEntity, authorityEntity, tick);
        }
        
        //动态实体对比
        if (_dynamicEntityAuthorityShotData.ContainsKey(tick) && _dynamicEntityLocalShotData.ContainsKey(tick))
        {
            BaseSnapShotData dynamicAuthoritySnapShot = _dynamicEntityAuthorityShotData[tick];
            BaseSnapShotData dynamicLocalSnapShot = _dynamicEntityLocalShotData[tick];

            if (dynamicAuthoritySnapShot != null && dynamicLocalSnapShot != null)
            {
                if (!dynamicAuthoritySnapShot.IsSameData(dynamicLocalSnapShot))
                {
#if UNITY_EDITOR
                    GameLog.Debug(GameLogChannel.Rollback, $"本地Log : {dynamicLocalSnapShot.DevelSnapShotData}");
                    GameLog.Debug(GameLogChannel.Rollback, $"权威Log : {dynamicAuthoritySnapShot.DevelSnapShotData}");
#endif
                    rollBackType |= RollBackType.HardRollBack;
                }
            }
        }

        return rollBackType;
    }

    /// <summary>
    /// 比对实体快照
    /// </summary>
    /// <param name="localEntity"></param>
    /// <param name="authorityEntity"></param>
    /// <param name="tick"></param>
    /// <returns></returns>
    private RollBackType VerifyEntitySnapShot(BaseEntity localEntity, BaseEntity authorityEntity, uint tick)
    {
        RollBackType rollBackType = RollBackType.NoRollBack;
        
        //1. 对比实体的硬快照
        BaseSnapShotData localHandWriter = localEntity.GetHardSnapShot(tick);
        BaseSnapShotData authorityHandWriter = authorityEntity.GetHardSnapShot(tick);

        if (localHandWriter != null && authorityHandWriter != null)
        {
            if (!localHandWriter.IsSameData(authorityHandWriter))
            {
#if UNITY_EDITOR
                GameLog.Debug(GameLogChannel.Rollback, $"本地Log 第 {tick} 帧: {localHandWriter.DevelSnapShotData}");
                GameLog.Debug(GameLogChannel.Rollback, $"权威Log 第 {tick} 帧: {authorityHandWriter.DevelSnapShotData}");
#endif
                rollBackType |= RollBackType.HardRollBack;
            }
        }

        //2. 对比实体的软快照
        BaseSnapShotData localSoftWriter = localEntity.GetSoftSnapShot(tick);
        BaseSnapShotData authoritySoftWriter = authorityEntity.GetSoftSnapShot(tick);

        if (localSoftWriter != null && authoritySoftWriter != null)
        {
            if (!localSoftWriter.IsSameData(authoritySoftWriter))
            {
#if UNITY_EDITOR
                GameLog.Debug(GameLogChannel.Rollback, $"本地Log : {localSoftWriter.DevelSnapShotData}");
                GameLog.Debug(GameLogChannel.Rollback, $"权威Log : {authoritySoftWriter.DevelSnapShotData}");
#endif
                rollBackType |= RollBackType.SoftRollBack;
            }
        }

        return rollBackType;
    }
}
