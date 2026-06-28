using System.Collections.Generic;

/// <summary>
/// 实体系统快照
/// </summary>
public partial class EntitySystem : BaseSystem
{
    /// <summary>
    /// 本地动态实体快照 主要记录数量 和 指纹ID
    /// </summary>
    private Dictionary<uint, BaseSnapShotData> _dynamicEntityLocalShotData = new();
    
    /// <summary>
    /// 权威动态实体快照
    /// </summary>
    private Dictionary<uint, BaseSnapShotData> _dynamicEntityAuthorityShotData = new();

    /// <summary>
    /// 预测动态实体 key = 帧号 value = 预测动态实体列表
    /// </summary>
    private Dictionary<uint, Dictionary<int,BaseSnapShotData>> _dynamicLocalEntityDic = new ();
    
    /// <summary>
    /// 权威动态实体 key = 帧号 value = 权威动态实体列表
    /// </summary>
    private Dictionary<uint, Dictionary<int,BaseSnapShotData>> _dynamicAuthorityEntityDic = new ();

    /// <summary>
    /// 生成本地实体快照
    /// </summary>
    /// <param name="localTick"></param>
    public override void TakeLocalSnapShot(uint localTick)
    {
        //静态实体快照
        foreach (var localEntity in _entityMap.Keys)
        {
            BaseSnapShotData hardWriter = new BaseSnapShotData();
            BaseSnapShotData softWriter = new BaseSnapShotData();

            localEntity.TakeSnapShot(localTick, hardWriter, softWriter);
        }
        
        //动态实体快照
        Dictionary<int, BaseSnapShotData> entityShotDic = new();
        
        _dynamicLocalEntityDic.Add(localTick, entityShotDic);
        
        TakeDynamicEntityShotData(localTick, _survivalLocalDynamic, _dynamicEntityLocalShotData, entityShotDic);
    }

    public override void TakeAuthoritySnapShot(uint authorityTick)
    {
        foreach (var authorityEntity in _entityMap.Values)
        {
            BaseSnapShotData hardWriter = new BaseSnapShotData();
            BaseSnapShotData softWriter = new BaseSnapShotData();
            
            authorityEntity.TakeSnapShot(authorityTick, hardWriter, softWriter);
        }
        
        //动态实体快照
        Dictionary<int, BaseSnapShotData> entityShotDic = new();
        
        _dynamicAuthorityEntityDic.Add(authorityTick, entityShotDic);
        
        TakeDynamicEntityShotData(authorityTick, _survivalAuthorityDynamic, _dynamicEntityAuthorityShotData,entityShotDic);
    }

    /// <summary>
    /// 拍摄动态实体Tick
    /// </summary>
    /// <param name="tick"></param>
    /// <param name="dynamicEntityList"></param>
    /// <param name="shotDataDic"></param>
    /// <param name="entityShotData"></param>
    private void TakeDynamicEntityShotData(uint tick, List<BaseEntity> dynamicEntityList,
        Dictionary<uint, BaseSnapShotData> shotDataDic,Dictionary<int,BaseSnapShotData> entityShotData)
    {
        //动态实体快照
         BaseSnapShotData dynamicSoftWriter = new BaseSnapShotData();

        dynamicSoftWriter.WriteInt32Data($"动态实体数量：", dynamicEntityList.Count);
        
        for (int i = 0; i < dynamicEntityList.Count; i++)
        {
            var dynamicEntity = dynamicEntityList[i];

            dynamicSoftWriter.WriteInt32Data($"动态实体指纹：", dynamicEntity.EntityFingerprints);
            
            //动态实体快照
            BaseSnapShotData dynamicEntityHardWriter = new BaseSnapShotData();
            BaseSnapShotData dynamicEntitySoftWriter = new BaseSnapShotData();
            
            dynamicEntity.TakeSnapShot(tick, dynamicEntityHardWriter, dynamicEntitySoftWriter);

            entityShotData.Add(dynamicEntity.EntityFingerprints, dynamicEntityHardWriter);
        }
        
        shotDataDic.Add(tick, dynamicSoftWriter);
    }
    
    /// <summary>
    /// 移除错误的
    /// </summary>
    /// <param name="startErrorTick"></param>
    /// <param name="rollbackType"></param>
    public override void RemoveLocalSnapShot(uint startErrorTick, RollBackType rollbackType)
    {
        uint startError = startErrorTick;

        while (startError <= CurrentWorld.LocalTick)
        {
            foreach (var localEntity in _entityMap.Keys)
            {
                localEntity.RemoveSnapShot(startError, rollbackType);
            }

            _dynamicEntityLocalShotData.Remove(startError);

            //移除动态实体快照
            _dynamicLocalEntityDic.Remove(startError);
            
            startError++;
        }
    }
}