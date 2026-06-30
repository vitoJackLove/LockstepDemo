using System.Collections.Generic;
using Ase.Serializing;
using UnityEngine;

/// <summary>
/// 实体快照
/// </summary>
public partial class BaseEntity
{
    /// <summary>
    /// 实体缓存的硬回滚帧快照  key = 帧 value = 快照 key = 组件的标签
    /// </summary>
    private Dictionary<uint, BaseSnapShotData> _cacheHardSnapShots = new ();
    
    /// <summary>
    /// 实体缓存的软回滚帧快照
    /// </summary>
    private Dictionary<uint, BaseSnapShotData> _cacheSoftSnapShots = new ();
    
    public virtual void TakeSnapShot(uint tick, BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
    {
        hardWriter.WriteInt32Data($"实体状态", (int)EntityState);
        
        var dic = EntityComponent.GetEnumerator();

        while (dic.MoveNext())
        {
            dic.Current.Value.TakeSnapShot(hardWriter, softWriter);
        }

        dic.Dispose();
        
                
        _cacheHardSnapShots.Add(tick, hardWriter);
        _cacheSoftSnapShots.Add(tick, softWriter);
    }
    
    /// <summary>
    /// 硬回滚
    /// </summary>
    public virtual void HardRollBack(PooledReader authoritySnapShot)
    {
        _entityState = (EntityState)authoritySnapShot.ReadInt32();
        
        var dic = _entityComponent.GetEnumerator();

        while (dic.MoveNext())
        {
            dic.Current.Value.HardRollBackTo(authoritySnapShot);
        }

        dic.Dispose();

        SyncHitVolumesAfterRollBack();
    }

    private void SyncHitVolumesAfterRollBack()
    {
        VolumeSystem volumeSystem = GetSystem<VolumeSystem>();
        if (volumeSystem == null)
        {
            return;
        }

        if (_entityState == EntityState.Survival)
        {
            volumeSystem.RestoreHitVolume(_entityId);
        }
        else
        {
            volumeSystem.UnRegisterHitVolume(_entityId);
        }
    }
    
        
    /// <summary>
    /// 软回滚
    /// </summary>
    public virtual void SoftRollBack(PooledReader authoritySnapShot)
    {
        var dic = _entityComponent.GetEnumerator();

        while (dic.MoveNext())
        {
            dic.Current.Value.SoftRollBackTo(authoritySnapShot);
        }

        dic.Dispose();
    }

    /// <summary>
    /// 移除快照
    /// </summary>
    /// <param name="tick"></param>
    /// <param name="rollbackType"></param>
    public void RemoveSnapShot(uint tick,RollBackType rollbackType)
    {
        //硬回滚 需要移除俩种快照
        if ((rollbackType & RollBackType.HardRollBack) != 0)
        {
            if (_cacheHardSnapShots.ContainsKey(tick))
            {
                Debug.Log($"<color=yellow> EntitySystem ： 移除硬回滚快照 {tick} Tick </color>");
            
                _cacheHardSnapShots.Remove(tick);
            }
            
            if (_cacheSoftSnapShots.ContainsKey(tick))
            {
                Debug.Log($"<color=yellow> EntitySystem ： 移除软回滚快照 {tick} Tick </color>");

                _cacheSoftSnapShots.Remove(tick);
            }
        }

        if ((rollbackType & RollBackType.SoftRollBack) != 0)
        {
            if (_cacheSoftSnapShots.ContainsKey(tick))
            {
                Debug.Log($"<color=yellow> EntitySystem ： 移除软回滚快照 {tick} Tick </color>");

                _cacheSoftSnapShots.Remove(tick);
            }
        }
    }
    
    /// <summary>
    /// 获取硬快照
    /// </summary>
    /// <param name="tick"></param>
    /// <returns></returns>
    public BaseSnapShotData GetHardSnapShot(uint tick)
    {
        if (_cacheHardSnapShots.TryGetValue(tick, out var snapShotData))
        {
            if (snapShotData != null)
            {
                return snapShotData;
            }
        }

        return null;
    }
    
    /// <summary>
    /// 获取软快照
    /// </summary>
    /// <param name="tick"></param>
    /// <returns></returns>
    public BaseSnapShotData GetSoftSnapShot(uint tick)
    {
        if (_cacheSoftSnapShots.TryGetValue(tick, out var snapShotData))
        {
            if (snapShotData != null)
            {
                return snapShotData;
            }
        }

        return null;
    }
    
    /// <summary>
    /// debug
    /// </summary>
    /// <param name="content"></param>
    public void EntityDebug(string content)
    {
        if (EntityUpdateType == EntityUpdateType.AuthorityEntity)
        {
            GameLog.Debug(GameLogChannel.Rollback,$"权威实体 ID : {EntityId} 在世界帧 ：{BaseWorld.AuthorityTick} 打印 ： {content}");
        }

        if (EntityUpdateType == EntityUpdateType.LocalEntity)
        {
            GameLog.Debug(GameLogChannel.Rollback,$"本地实体 ID : {EntityId} 在世界帧 ：{BaseWorld.LocalTick} 打印 ： {content}");
        }
    }
}