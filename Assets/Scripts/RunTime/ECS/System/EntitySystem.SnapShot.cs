using System.Collections.Generic;

/// <summary>
/// 实体系统快照（GGPO 单模拟态）。
/// </summary>
public partial class EntitySystem : BaseSystem
{
    private Dictionary<uint, BaseSnapShotData> _dynamicEntityLocalShotData = new();
    private Dictionary<uint, Dictionary<int, BaseSnapShotData>> _dynamicLocalEntityDic = new();

    public override void TakeLocalSnapShot(uint localTick)
    {
        for (int i = 0; i < _heroEntityList.Count; i++)
        {
            BaseSnapShotData hardWriter = new BaseSnapShotData();
            BaseSnapShotData softWriter = new BaseSnapShotData();
            _heroEntityList[i].TakeSnapShot(localTick, hardWriter, softWriter);
        }

        Dictionary<int, BaseSnapShotData> entityShotDic = new();
        _dynamicLocalEntityDic[localTick] = entityShotDic;
        TakeDynamicEntityShotData(localTick, _survivalLocalDynamic, _dynamicEntityLocalShotData, entityShotDic);
    }

    public override void TakeAuthoritySnapShot(uint authorityTick)
    {
        // GGPO 单模拟态不需要权威快照轨。
    }

    private void TakeDynamicEntityShotData(uint tick, List<BaseEntity> dynamicEntityList,
        Dictionary<uint, BaseSnapShotData> shotDataDic, Dictionary<int, BaseSnapShotData> entityShotData)
    {
        BaseSnapShotData dynamicSoftWriter = new BaseSnapShotData();
        dynamicSoftWriter.WriteInt32Data("动态实体数量：", dynamicEntityList.Count);

        for (int i = 0; i < dynamicEntityList.Count; i++)
        {
            BaseEntity dynamicEntity = dynamicEntityList[i];
            dynamicSoftWriter.WriteInt32Data("动态实体指纹：", dynamicEntity.EntityFingerprints);

            BaseSnapShotData dynamicEntityHardWriter = new BaseSnapShotData();
            BaseSnapShotData dynamicEntitySoftWriter = new BaseSnapShotData();
            dynamicEntity.TakeSnapShot(tick, dynamicEntityHardWriter, dynamicEntitySoftWriter);
            entityShotData[dynamicEntity.EntityFingerprints] = dynamicEntityHardWriter;
        }

        shotDataDic[tick] = dynamicSoftWriter;
    }

    public override void RemoveLocalSnapShot(uint startErrorTick, RollBackType rollbackType)
    {
        uint startError = startErrorTick;
        while (startError <= CurrentWorld.LocalTick)
        {
            for (int i = 0; i < _heroEntityList.Count; i++)
            {
                _heroEntityList[i].RemoveSnapShot(startError, rollbackType);
            }

            _dynamicEntityLocalShotData.Remove(startError);
            _dynamicLocalEntityDic.Remove(startError);
            startError++;
        }
    }
}
