using System.Collections.Generic;
using Ase.Serializing;
using Rogue;
using Unity.Mathematics.FixedPoint;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Buff组件
/// </summary>
public class BuffComponent : BaseComponent
{
    /// <summary>
    /// key = Buff的配置ID value = Buff的指纹
    /// </summary>
    private Dictionary<int, int> _buffDic;

    public override void OnInit(object data = null)
    {
        base.OnInit(data);

        _buffDic = DictionaryPool<int, int>.Get();
    }

    /// <summary>
    /// 创建Buff
    /// </summary>
    /// <param name="buffConfigId"></param>
    public void CreateBuff(int buffConfigId)
    {
        if (Entity == null || _buffDic == null)
        {
            return;
        }

        if (!_buffDic.TryGetValue(buffConfigId, out int buffEntityFingerprints))
        {
            if (GameEntry.DataTable == null)
            {
                return;
            }

            BuffAssetsConfig config = GameEntry.DataTable.GetDataTable<BuffAssetsConfig>(buffConfigId);

            if (config == null)
            {
                return;
            }

            if (config.gameEventType == BattleExecuteTiming.Null)
            {
                return;
            }

            EntityCreateData createData = EntityCreateData.Create(config, fp3.zero, fp3.zero,
                new fp3(1, 1, 1), Entity.IsNeedExecuteView,
                null, Entity, Entity);
        
            uint frame = Entity.EntityUpdateType == EntityUpdateType.AuthorityEntity
                ? Entity.BaseWorld.AuthorityTick
                : Entity.BaseWorld.LocalTick;
        
            //指纹
            EntitySystem entitySystem = Entity.GetSystem<EntitySystem>();
            int stableEntityIdentity = entitySystem.GetStableEntityIdentity(Entity);
            int fingerprints =
                FingerprintsGenerate.GenerateFingerprint((int)frame, Entity.ConfigId,
                    buffConfigId, new fp3(stableEntityIdentity, 0, 0), fp3.zero);

            CreateBuffEntity(buffConfigId, createData, entitySystem, fingerprints);
        }
        else
        {
            BuffEntity buffEntity = GetBuffEntity(buffEntityFingerprints);

            if (buffEntity == null)
            {
                _buffDic.Remove(buffConfigId);
                CreateBuff(buffConfigId);
            }
            else
            {
                buffEntity.IncreaseLayer();
            }
        }
    }

    public void CreateBuffWithFingerprints(int buffConfigId, int fingerprints)
    {
        if (Entity == null || _buffDic == null)
        {
            return;
        }

        if (_buffDic.TryGetValue(buffConfigId, out int buffEntityFingerprints))
        {
            BuffEntity existingBuffEntity = GetBuffEntity(buffEntityFingerprints);

            if (existingBuffEntity != null)
            {
                existingBuffEntity.IncreaseLayer();
                return;
            }

            _buffDic.Remove(buffConfigId);
        }

        if (GameEntry.DataTable == null)
        {
            return;
        }

        BuffAssetsConfig config = GameEntry.DataTable.GetDataTable<BuffAssetsConfig>(buffConfigId);

        if (config == null)
        {
            return;
        }

        if (config.gameEventType == BattleExecuteTiming.Null)
        {
            return;
        }

        EntityCreateData createData = EntityCreateData.Create(config, fp3.zero, fp3.zero,
            new fp3(1, 1, 1), Entity.IsNeedExecuteView,
            null, Entity, Entity);

        CreateBuffEntity(buffConfigId, createData, Entity.GetSystem<EntitySystem>(), fingerprints);
    }

    private void CreateBuffEntity(int buffConfigId, EntityCreateData createData, EntitySystem entitySystem, int fingerprints)
    {
        // 指纹已存在时优先叠层，避免金手指或重复创建导致动态实体字典键冲突
        BuffEntity existingBuffEntity = Entity.EntityUpdateType == EntityUpdateType.AuthorityEntity
            ? entitySystem.GetDynamicAuthorityEntity<BuffEntity>(fingerprints)
            : entitySystem.GetDynamicLocalEntity<BuffEntity>(fingerprints);

        if (existingBuffEntity != null && existingBuffEntity.ParentEntity == Entity)
        {
            if (_buffDic.TryGetValue(buffConfigId, out int mappedFingerprints) &&
                mappedFingerprints == fingerprints)
            {
                existingBuffEntity.IncreaseLayer();
                return;
            }

            GameLog.Warn(GameLogChannel.Battle,
                $"Buff 指纹冲突，跳过重复创建：ConfigId={buffConfigId}, Fingerprints={fingerprints}");
            return;
        }

        BuffEntity buffEntity = entitySystem.CreateDynamicEntity<BuffEntity>(createData,
            Entity.EntityUpdateType, fingerprints);

        if (buffEntity == null)
        {
            return;
        }

        if (_buffDic.ContainsKey(buffConfigId))
        {
            _buffDic[buffConfigId] = buffEntity.EntityFingerprints;
        }
        else
        {
            _buffDic.Add(buffConfigId, buffEntity.EntityFingerprints);
        }
    }

    public void RemoveBuff(int buffConfigId, int buffEntityFingerprints)
    {
        if (_buffDic == null)
        {
            return;
        }

        if (_buffDic.TryGetValue(buffConfigId, out int currentFingerprints) &&
            currentFingerprints == buffEntityFingerprints)
        {
            _buffDic.Remove(buffConfigId);
        }
    }

    public override void OnEntityDead(object data)
    {
        base.OnEntityDead(data);

        DestroyAllBuffs();
    }

    private BuffEntity GetBuffEntity(int buffEntityFingerprints)
    {
        if (Entity == null)
        {
            return null;
        }

        if (Entity.EntityUpdateType == EntityUpdateType.AuthorityEntity)
        {
            return Entity.GetSystem<EntitySystem>().GetDynamicAuthorityEntity<BuffEntity>(buffEntityFingerprints);
        }

        return Entity.GetSystem<EntitySystem>().GetDynamicLocalEntity<BuffEntity>(buffEntityFingerprints);
    }

    private BuffEntity GetBuffEntityForRollback(int buffEntityFingerprints)
    {
        if (Entity == null)
        {
            return null;
        }

        EntitySystem entitySystem = Entity.GetSystem<EntitySystem>();

        if (Entity.EntityUpdateType == EntityUpdateType.AuthorityEntity)
        {
            return entitySystem.GetDynamicAuthorityEntity<BuffEntity>(buffEntityFingerprints);
        }

        return entitySystem.GetDynamicLocalOrCachedEntity<BuffEntity>(buffEntityFingerprints);
    }

    private void DestroyAllBuffs()
    {
        if (_buffDic == null || _buffDic.Count == 0)
        {
            return;
        }

        List<int> buffFingerprintsList = new List<int>(_buffDic.Values);

        for (int i = 0; i < buffFingerprintsList.Count; i++)
        {
            BuffEntity buffEntity = GetBuffEntity(buffFingerprintsList[i]);

            if (buffEntity != null)
            {
                buffEntity.DoEntityDead();
            }
        }

        _buffDic.Clear();
    }

    public override void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
    {
        base.TakeSnapShot(hardWriter, softWriter);

        softWriter.WriteInt32Data($"Buff列表长度", _buffDic.Count);

        foreach (var buffConfigId in _buffDic.Keys)
        {
            softWriter.WriteInt32Data($"BuffConfigId", buffConfigId);

            int buffEntityFingerprints = _buffDic[buffConfigId];
            BuffEntity buffEntity = GetBuffEntity(buffEntityFingerprints);

            softWriter.WriteInt32Data($"Buff指纹", buffEntityFingerprints);
            softWriter.WriteInt32Data($"Buff层数", buffEntity?.Layer ?? 0);
            softWriter.WriterBoolData($"Buff是否限制执行次数", buffEntity?.IsExecuteCountLimited ?? false);
            softWriter.WriteInt32Data($"Buff每层执行次数", buffEntity?.ExecuteCountPerLayer ?? 0);
            softWriter.WriteInt32Data($"Buff剩余执行次数", buffEntity?.RemainingExecuteCount ?? 0);
        }
    }

    public override void SoftRollBackTo(PooledReader authoritySnapShot)
    {
        base.SoftRollBackTo(authoritySnapShot);

        _buffDic.Clear();
        
        int buffCount = authoritySnapShot.ReadInt32();

        for (int i = 0; i < buffCount; i++)
        {
            int key = authoritySnapShot.ReadInt32();
            int value = authoritySnapShot.ReadInt32();
            int layer = authoritySnapShot.ReadInt32();
            bool isExecuteCountLimited = authoritySnapShot.ReadBoolean();
            int executeCountPerLayer = authoritySnapShot.ReadInt32();
            int remainingExecuteCount = authoritySnapShot.ReadInt32();

            BuffEntity buffEntity = GetBuffEntityForRollback(value);

            if (buffEntity == null)
            {
                GameLog.Warn(GameLogChannel.Rollback, $"Buff软回滚清理失效指纹：ConfigId={key}, Fingerprints={value}");

                continue;
            }

            _buffDic[key] = value;
            buffEntity.RestoreExecutionState(layer, isExecuteCountLimited, executeCountPerLayer,
                remainingExecuteCount);
        }
    }

    public override void OnDispose()
    {
        base.OnDispose();

        DestroyAllBuffs();

        if (_buffDic != null)
        {
            DictionaryPool<int, int>.Release(_buffDic);

            _buffDic = null;
        }
    }
}
