using System;
using System.Collections.Generic;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// 实体系统
/// </summary>
public partial class EntitySystem 
{
    /// <summary>
    /// 实体序列
    /// </summary>
    private int _serialId;

    /// <summary>
    /// 所有的执行中的静态实体(包括权威实体和预测实体) 用于查询
    /// </summary>
    private Dictionary<int, BaseEntity> _executeEntityDic;

    /// <summary>
    /// 所有的执行中的静态实体(包括权威实体和预测实体) 用于循环
    /// </summary>
    private List<BaseEntity> _executeEntityList;

    /// <summary>
    /// 主角权威实体
    /// </summary>
    private BaseEntity _actorAuthorityEntity;
    
    /// <summary>
    /// 主角本地实体
    /// </summary>
    private BaseEntity _actorLocalEntity;
    
    private List<BaseEntity> _monsterEntityList = new List<BaseEntity>();

    /// <summary>
    /// 怪物实体集合
    /// </summary>
    public List<BaseEntity> MonsterEntityList => _monsterEntityList;

    /// <summary>
    /// 英雄实体集合
    /// </summary>
    private List<BaseEntity> _heroEntityList = new();

    public List<BaseEntity> HeroEntityList => _heroEntityList;

    /// <summary>
    /// 角色权威实体集合
    /// </summary>
    private List<BaseEntity> _heroAuthorityEntityList = new();

    /// <summary>
    /// key = 本地表现实体  Value = 本地表现实体对应的权威实体 （有可能实体不存在）
    /// </summary>
    private Dictionary<BaseEntity, BaseEntity> _entityMap = new ();

    private List<BaseEntity> _lockTargetCandidateCache = new();

    /// <summary>
    /// 注册实体关联
    /// </summary>
    /// <param name="localEntityId"></param>
    /// <param name="authorityEntityId"></param>
    public void RegisterStaticEntityMap(BaseEntity localEntityId, BaseEntity authorityEntityId)
    {
        _entityMap[localEntityId] = authorityEntityId;
    }

    /// <summary>
    /// 获取实体的稳定身份 ID，本地实体会优先映射到对应权威实体 ID。
    /// </summary>
    /// <param name="entity">需要生成稳定身份的实体。</param>
    /// <returns>可用于运行时指纹生成的实体身份；实体为空时返回 0。</returns>
    public int GetStableEntityIdentity(BaseEntity entity)
    {
        if (entity == null)
        {
            return 0;
        }

        if (entity.EntityUpdateType == EntityUpdateType.LocalEntity &&
            _entityMap.TryGetValue(entity, out BaseEntity authorityEntity) &&
            authorityEntity != null)
        {
            return authorityEntity.EntityId;
        }

        return entity.EntityId;
    }

    public IReadOnlyList<BaseEntity> GetLockTargetCandidates(WorldUpdateType worldUpdateType)
    {
        if (worldUpdateType == WorldUpdateType.Authority)
        {
            return _heroAuthorityEntityList;
        }

        _lockTargetCandidateCache.Clear();
        _lockTargetCandidateCache.AddRange(_heroEntityList);

        for (int i = 0; i < _heroAuthorityEntityList.Count; i++)
        {
            BaseEntity authorityHero = _heroAuthorityEntityList[i];

            if (HasLocalMappedEntity(authorityHero))
            {
                continue;
            }

            _lockTargetCandidateCache.Add(authorityHero);
        }

        return _lockTargetCandidateCache;
    }

    private bool HasLocalMappedEntity(BaseEntity authorityEntity)
    {
        foreach (KeyValuePair<BaseEntity, BaseEntity> entityPair in _entityMap)
        {
            if (entityPair.Value == authorityEntity)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 获取权威角色
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public BaseEntity GetAuthorityHeroEntity(int index)
    {
        if (_heroAuthorityEntityList.Count > index)
        {
            return _heroAuthorityEntityList[index];
        }

        return null;
    }
    
    /// <summary>
    /// 注册主角
    /// </summary>
    /// <param name="actorAuthorityEntity">权威</param>
    /// <param name="actorLocalEntity">本地</param>
    public void RegisterActor(BaseEntity actorAuthorityEntity,BaseEntity actorLocalEntity)
    {
        this._actorAuthorityEntity = actorAuthorityEntity;
        this._actorLocalEntity = actorLocalEntity;
    }

    public BaseEntity GetActorCommandEntity(bool isSinglePlayer)
    {
        if (CurrentWorld?.SessionProfile != null)
        {
            return CurrentWorld.SessionProfile.EntitySyncPolicy.GetCommandEntity(this);
        }

        return isSinglePlayer ? _actorLocalEntity : _actorAuthorityEntity;
    }

    /// <summary>
    /// 判断实体是否仍在当前执行列表中。
    /// </summary>
    public bool ContainsExecutingEntity(BaseEntity entity)
    {
        return entity != null && _executeEntityDic != null && _executeEntityDic.ContainsKey(entity.EntityId);
    }

    /// <summary>
    /// 通过会话实体同步策略解析金手指操作目标。
    /// </summary>
    public bool TryResolveCheatOperationTargets(
        BaseEntity selectedEntity,
        out IReadOnlyList<BaseEntity> targets,
        out string failureMessage)
    {
        if (CurrentWorld?.SessionProfile == null)
        {
            targets = null;
            failureMessage = "当前世界未配置会话 Profile";
            return false;
        }

        return CurrentWorld.SessionProfile.EntitySyncPolicy.TryResolveOperationTargets(
            selectedEntity,
            this,
            out targets,
            out failureMessage);
    }

    public bool IsActorEntity(BaseEntity entity)
    {
        if (entity == null)
        {
            return false;
        }

        return entity == _actorLocalEntity || entity == _actorAuthorityEntity;
    }
    
    private void RemoveMonster(BaseEntity entity)
    {
        if (_monsterEntityList.Contains(entity))
        {
            _monsterEntityList.Remove(entity);
        }
    }

    /// <summary>
    /// 创建静态实体
    /// </summary>
    /// <param name="entityCreateData"></param>
    /// <param name="entityUpdateType"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T CreateStaticEntity<T>(EntityCreateData entityCreateData,EntityUpdateType entityUpdateType) where T : BaseEntity
    {
        var baseEntity = (BaseEntity)Activator.CreateInstance(typeof(T));

        if (baseEntity.ForecastEntityType != ForecastEntityType.StaticEntity)
        {
            GameLog.Error(GameLogChannel.Battle, $"创建静态实体错误：创建的实体类型是 ： [{baseEntity.ForecastEntityType}]...");
            
            return null;
        }
        
        _serialId++;
        baseEntity.SetUpdateType(entityUpdateType);
        baseEntity.SetEntityId(_serialId);
        baseEntity.SetWorld(CurrentWorld);
        baseEntity.OnInit(entityCreateData);
        baseEntity.OnStart();
        
        _executeEntityDic.Add(_serialId,baseEntity);
        _executeEntityList.Add(baseEntity);
        
        if (baseEntity.EntityType == EntityType.HeroEntity)
        {
            if (entityUpdateType == EntityUpdateType.AuthorityEntity)
            {
                _heroAuthorityEntityList.Add(baseEntity);
            }
            else
            {
                HeroEntityList.Add(baseEntity);
            }
        }
        
        if (baseEntity.EntityType == EntityType.MonsterEntity)
        {
            _monsterEntityList.Add(baseEntity);
        }

        return (T)baseEntity;
    }

    /// <summary>
    /// 创建动态实体
    /// </summary>
    /// <param name="entityCreateData"></param>
    /// <param name="entityUpdateType"></param>
    /// <param name="entityFingerprints">实体指纹用于关联预测和权威实体的ID</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T CreateDynamicEntity<T>(EntityCreateData entityCreateData,
        EntityUpdateType entityUpdateType, int entityFingerprints) where T : BaseEntity
    {
        var baseEntity = (BaseEntity)Activator.CreateInstance(typeof(T));

        if (baseEntity.ForecastEntityType != ForecastEntityType.DynamicEntity)
        {
            GameLog.Error(GameLogChannel.Battle, $"创建动态实体错误：创建的实体类型是 ： [{baseEntity.ForecastEntityType}]...");
            
            return null;
        }
        
        _serialId++;
        baseEntity.SetUpdateType(entityUpdateType);
        baseEntity.SetEntityFingerprints(entityFingerprints); 
        baseEntity.SetEntityId(_serialId);
        baseEntity.SetWorld(CurrentWorld);
        baseEntity.OnInit(entityCreateData);
        baseEntity.OnStart();
        
        _executeEntityDic.Add(_serialId,baseEntity);
        _executeEntityList.Add(baseEntity);
        RegisterDynamicEntity(baseEntity);
        
        return (T)baseEntity;
    }
    
    /// <summary>
    /// 创建服务器实体
    /// </summary>
    /// <param name="serverEntityId"></param>
    /// <param name="entityCreateData"></param>
    /// <param name="entityUpdateType"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T CreateServerEntity<T>(int serverEntityId, EntityCreateData entityCreateData,EntityUpdateType entityUpdateType) where T : BaseEntity
    {
        var baseEntity = (BaseEntity)Activator.CreateInstance(typeof(T));
        baseEntity.SetUpdateType(entityUpdateType);
        baseEntity.SetEntityId(serverEntityId);
        baseEntity.SetWorld(CurrentWorld);
        baseEntity.OnInit(entityCreateData);
        baseEntity.OnStart();

        _executeEntityDic.Add(serverEntityId, baseEntity);
        _executeEntityList.Add(baseEntity);
        
        if (baseEntity.EntityType == EntityType.HeroEntity)
        {
            if (entityUpdateType == EntityUpdateType.AuthorityEntity)
            {
                _heroAuthorityEntityList.Add(baseEntity);
            }
            else
            {
                HeroEntityList.Add(baseEntity);
            }
        }
        
        return (T)baseEntity;
    }
    
    /// <summary>
    /// 实体销毁
    /// </summary>
    /// <param name="entity"></param>
    public void DoEntityDestroy(BaseEntity entity)
    {
        if (entity.EntityType == EntityType.MonsterEntity)
        {
            RemoveMonster(entity);
        }

        if (entity.EntityType == EntityType.HeroEntity)
        {
            HeroEntityList.Remove(entity);
            _heroAuthorityEntityList.Remove(entity);
        }

        _executeEntityDic.Remove(entity.EntityId);

        _executeEntityList.Remove(entity);

        //动态实体不销毁
        if (entity.ForecastEntityType == ForecastEntityType.DynamicEntity)
        {
            RemoveDynamicEntity(entity);
        }
        else
        {
            entity.OnDispose();
        }
    }
    
    public override void OnInit(object data = null)
    {
        base.OnInit(data);

        _serialId = 0;
        _executeEntityDic = new Dictionary<int, BaseEntity>();
        _executeEntityList = new List<BaseEntity>();
    }

    public override void OnExecuteServerCommand(List<CommandData> dataList)
    {
        for (int i = 0; i < dataList.Count; i++)
        {
            CommandData commandData = dataList[i];

            if (commandData != null)
            {
                BaseEntity entity = GetEntity(dataList[i].EntityId);
            
                entity?.OnExecuteServerCommand(dataList[i]);
            }
        }
    }

    public override void OnExecuteLocalCommand(CommandData data)
    {
        base.OnExecuteLocalCommand(data);
        
        _actorLocalEntity?.OnExecuteLocalCommand(data);
    }

    public override void OnUpdate(fp deltaTime)
    {
        for (int i = 0; i < _executeEntityList.Count; i++)
        {
            if (_executeEntityList[i].EntityState == EntityState.Survival)
            {
                _executeEntityList[i].OnUpdate(deltaTime);
            }
        }
    }

    public override void OnFixedUpdate(fp deltaTime, WorldUpdateType worldUpdateType)
    {
        base.OnFixedUpdate(deltaTime, worldUpdateType);
        
        for (int i = 0; i < _executeEntityList.Count; i++)
        {
            if ( _executeEntityList[i].EntityUpdateType == EntityUpdateType.AuthorityEntity &&
                 worldUpdateType == WorldUpdateType.Authority)
            {
                if (_executeEntityList[i].EntityState == EntityState.Survival)
                {
                    _executeEntityList[i].OnFixedUpdate(deltaTime, worldUpdateType);
                }
            }
            else if(_executeEntityList[i].EntityUpdateType == EntityUpdateType.LocalEntity 
                    && (worldUpdateType == WorldUpdateType.Local || worldUpdateType == WorldUpdateType.RollBack))
            {
                if (_executeEntityList[i].EntityState == EntityState.Survival)
                {
                    _executeEntityList[i].OnFixedUpdate(deltaTime, worldUpdateType);
                }
            }
        }

        if (worldUpdateType == WorldUpdateType.Authority)
        {
            SyncAuthorityMonsterLockState();
        }
    }

    private void SyncAuthorityMonsterLockState()
    {
        foreach (KeyValuePair<BaseEntity, BaseEntity> entityPair in _entityMap)
        {
            BaseEntity localEntity = entityPair.Key;
            BaseEntity authorityEntity = entityPair.Value;

            if (localEntity == null || authorityEntity == null)
            {
                continue;
            }

            if (localEntity.EntityType != EntityType.MonsterEntity ||
                authorityEntity.EntityType != EntityType.MonsterEntity)
            {
                continue;
            }

            EnemyDetectionComponent localDetection = localEntity.GetComponent<EnemyDetectionComponent>();
            EnemyDetectionComponent authorityDetection = authorityEntity.GetComponent<EnemyDetectionComponent>();

            if (localDetection == null || authorityDetection == null)
            {
                continue;
            }

            // 锁定位置/旋转是旁路辅助状态，不进入快照系统；权威更新后直接覆盖预测怪物的锁定数据。
            localDetection.ApplyAuthorityLockFrom(authorityDetection);
        }
    }

    /// <summary>
    /// 通过ID 获取实体
    /// </summary>
    /// <param name="entityId"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T GetEntity<T>(int entityId) where T : BaseEntity
    {
        _executeEntityDic.TryGetValue(entityId, out var baseEntity);

        return (T)baseEntity;
    }
    
    /// <summary>
    /// 通过ID 获取实体
    /// </summary>
    /// <param name="entityId"></param>
    /// <returns></returns>
    public BaseEntity GetEntity(int entityId)
    {
        _executeEntityDic.TryGetValue(entityId, out var baseEntity);

        return baseEntity;
    }

    /// <summary>
    /// 主角权威实体
    /// </summary>
    public BaseEntity ActorAuthorityEntity => _actorAuthorityEntity;

#if UNITY_EDITOR
    /// <summary>
    /// 获取当前执行中的实体列表，仅供 Unity 编辑器金手指和调试工具浏览。
    /// </summary>
    /// <returns>当前执行实体的只读列表；系统尚未初始化时返回空列表。</returns>
    public IReadOnlyList<BaseEntity> GetExecutingEntitiesForDebug()
    {
        if (_executeEntityList == null)
        {
            return Array.Empty<BaseEntity>();
        }

        return _executeEntityList;
    }
#endif

    /// <summary>
    /// 主角本地实体
    /// </summary>
    public BaseEntity ActorLocalEntity => _actorLocalEntity;
}
