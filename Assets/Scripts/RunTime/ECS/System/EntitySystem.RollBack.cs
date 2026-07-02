using System.Collections.Generic;
using Ase.Serializing;

/// <summary>
/// 实体回滚（GGPO 单模拟态：从本地快照恢复）。
/// </summary>
public partial class EntitySystem
{
    public override void RollBack(uint tick, RollBackType rollBackType)
    {
        for (int i = 0; i < _heroEntityList.Count; i++)
        {
            BaseEntity entity = _heroEntityList[i];
            if ((rollBackType & RollBackType.HardRollBack) != 0)
            {
                BaseSnapShotData hardSnapShot = entity.GetHardSnapShot(tick);
                if (hardSnapShot != null)
                {
                    entity.HardRollBack(ReaderPool.GetReader(hardSnapShot.PooledWriter.GetArraySegment()));
                }
            }

            if ((rollBackType & RollBackType.SoftRollBack) != 0)
            {
                BaseSnapShotData softSnapShot = entity.GetSoftSnapShot(tick);
                if (softSnapShot != null)
                {
                    entity.SoftRollBack(ReaderPool.GetReader(softSnapShot.PooledWriter.GetArraySegment()));
                }
            }
        }

        DynamicEntityRollBackSingleSim(tick, rollBackType);
    }

    private void DynamicEntityRollBackSingleSim(uint rollBackTick, RollBackType rollBackType)
    {
        if (!_dynamicLocalEntityDic.TryGetValue(rollBackTick, out Dictionary<int, BaseSnapShotData> snapshotByFingerprint))
        {
            return;
        }

        for (int i = 0; i < _survivalLocalDynamic.Count; i++)
        {
            BaseEntity dynamicEntity = _survivalLocalDynamic[i];
            if (snapshotByFingerprint.TryGetValue(dynamicEntity.EntityFingerprints, out BaseSnapShotData hardSnapShot)
                && (rollBackType & RollBackType.HardRollBack) != 0)
            {
                dynamicEntity.HardRollBack(ReaderPool.GetReader(hardSnapShot.PooledWriter.GetArraySegment()));
            }
        }
    }

    public override RollBackType VerifyForecast(uint tick)
    {
        return RollBackType.NoRollBack;
    }
}
