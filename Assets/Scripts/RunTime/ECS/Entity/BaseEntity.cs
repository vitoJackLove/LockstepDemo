using System;
using System.Collections.Generic;
using Rogue.ECS;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// 预测实体类型
/// </summary>
public enum ForecastEntityType
{
    /// <summary>
    /// 从游戏开始贯穿到游戏结束的实体
    /// </summary>
    StaticEntity,
    
    /// <summary>
    /// 游戏过程中生成的逻辑实体
    /// </summary>
    DynamicEntity,
}

public enum EntityState 
{
    Null,
    
    /// <summary>
    /// 存活
    /// </summary>
    Survival,
    
    /// <summary>
    /// 失效中 实体存活到死亡 有一定的是失效时间
    /// </summary>
    Failed,
    
    /// <summary>
    /// 死亡
    /// </summary>
    Dead,
}

/// <summary>
/// 实体更新类型
/// </summary>
public enum EntityUpdateType
{
    /// <summary>
    /// 本地实体
    /// </summary>
    LocalEntity,
    
    /// <summary>
    /// 权威实体
    /// </summary>
    AuthorityEntity,
}

public abstract partial class BaseEntity : ILifeCycle
{
    /// <summary>
    /// 实体ID 唯一
    /// </summary>
    private int _entityId;

    /// <summary>
    /// 实体指纹
    /// </summary>
    private int _entityFingerprints;
    
    /// <summary>
    /// 当前世界
    /// </summary>
    private BaseWorld _baseWorld;

    /// <summary>
    /// 配置数据
    /// </summary>
    protected EntityAssetsConfig Config;

    /// <summary>
    /// 父实体
    /// </summary>
    private BaseEntity _parentEntity;
    
    /// <summary>
    /// 父实体
    /// </summary>
    public BaseEntity ParentEntity => _parentEntity;

    /// <summary>
    /// 实体属性数据
    /// </summary>
    private BattleEntityData _battleEntityData;
    
    /// <summary>
    /// 是否需要执行表现
    /// </summary>
    private bool _isNeedExecuteView;

    public bool IsNeedExecuteView => _isNeedExecuteView;

    private EntityType _entityType;

    /// <summary>
    /// 实体状态
    /// </summary>
    private EntityState _entityState;

    public EntityState EntityState => _entityState;

    /// <summary>
    /// 实体刷新类型
    /// </summary>
    private EntityUpdateType _entityUpdateType;

    public EntityUpdateType EntityUpdateType => _entityUpdateType;

    private Dictionary<Type, BaseComponent> _entityComponent;

    /// <summary>
    /// 是否是主角
    /// </summary>
    public bool IsActor => GetSystem<EntitySystem>()?.IsActorEntity(this) == true;

    /// <summary>
    /// 实体逻辑位置信息
    /// </summary>
    private FTransform _fTransform;

    private object _customData;

    public object CustomData => _customData;

    protected abstract Type[] GetComponentTypes();

    protected abstract Type EntityView();

    public abstract EntityType EntityType { get; }

    /// <summary>
    /// 预测实体的类型
    /// </summary>
    public abstract ForecastEntityType ForecastEntityType { get; }

    public virtual void OnInit(object data = null)
    {
        EntityCreateData entityCreateData = data as EntityCreateData;

        if (entityCreateData == null)
        {
            Debug.Log($"实体初始化错误：实体的创建信息为空...");
            
            return;
        }

        _entityState = EntityState.Survival;

        Config = entityCreateData.Config;

        this._parentEntity = entityCreateData.ParentEntity;
        
        _fTransform = FTransform.Create(entityCreateData);

        UnityGameObject = entityCreateData.GameObject;

        _isNeedExecuteView = entityCreateData.IsNeedExecuteView;

        _customData = entityCreateData.EntityData;

        FPoolHelper.Release<EntityCreateData>(entityCreateData);

        InitEntityComponentData();

        InitComponent();

        BindUnityComponent();
    }

    private void InitView()
    {
        if (UnityGameObject == null)
        {
            return;
        }

        Type viewType = EntityView();

        if (viewType != null)
        {
            _entityView = (EntityView)UnityGameObject.GetComponent(EntityView());
        
            if (_entityView == null)
            {
                _entityView = (EntityView)UnityGameObject.AddComponent(EntityView());
            }
        
            _entityView.OnInit(this);
        }
    }

    private void InitComponent()
    {
        _entityComponent = new Dictionary<Type, BaseComponent>();
        
        for (int i = 0; i < GetComponentTypes().Length; i++)
        {
            var component = (BaseComponent)Activator.CreateInstance(GetComponentTypes()[i]);

            _entityComponent.Add(GetComponentTypes()[i], component);

            component.OnInit(this);
        }
    }

    public virtual void OnStart(object data = null)
    {
        var dic = _entityComponent.GetEnumerator();

        while (dic.MoveNext())
        {
            dic.Current.Value.OnStart(data);
        }

        dic.Dispose();
        
        InitView();
    }

    /// <summary>
    /// 执行服务器指令
    /// </summary>
    /// <param name="data"></param>
    public virtual void OnExecuteServerCommand(CommandData data)
    {
        var dic = _entityComponent.GetEnumerator();

        while (dic.MoveNext())
        {
            dic.Current.Value.OnExecuteServerCommand(data);
        }

        dic.Dispose();

        //RecodeCommandResult(data.Tick, data.MoveCommandExecuteIsSuccess);
    }

    /// <summary>
    /// 执行本地指令
    /// </summary>
    public virtual void OnExecuteLocalCommand(CommandData data)
    {
        var dic = _entityComponent.GetEnumerator();

        while (dic.MoveNext())
        {
            dic.Current.Value.OnExecuteLocalCommand(data);
        }

        dic.Dispose();
    }
    
    public virtual void OnUpdate(fp deltaTime)
    {
        var dic = _entityComponent.GetEnumerator();

        while (dic.MoveNext())
        {
            dic.Current.Value.OnUpdate(deltaTime);
        }

        dic.Dispose();
    }
    
    public virtual void OnFixedUpdate(fp deltaTime, WorldUpdateType worldUpdateType)
    {
        var dic = _entityComponent.GetEnumerator();

        while (dic.MoveNext())
        {
            dic.Current.Value.OnFixedUpdate(deltaTime, worldUpdateType);
        }

        dic.Dispose();
    }
    
    
    public virtual void OnPause(bool isPause)
    {
        var dic = _entityComponent.GetEnumerator();

        while (dic.MoveNext())
        {
            dic.Current.Value.OnPause(isPause);
        }

        dic.Dispose();
    }
    
    public virtual void DoEntityDead(object data = null)
    {
        if (_entityState == EntityState.Dead || _entityState == EntityState.Null)
        {
            return;
        }
        
        _entityState = EntityState.Dead;

        OnEntityDead(data);

        GetSystem<EntitySystem>().DoEntityDestroy(this);
    }

    protected virtual void OnEntityDead(object data = null)
    {
        var dic = _entityComponent.GetEnumerator();

        while (dic.MoveNext())
        {
            dic.Current.Value.OnEntityDead(data);
        }

        dic.Dispose();

        if (_entityView != null)
        {
            _entityView.OnEntityDead();
        }
    }
    
    public virtual void OnDispose()
    {
        var dic = _entityComponent.GetEnumerator();

        while (dic.MoveNext())
        {
            dic.Current.Value.OnDispose();
        }

        dic.Dispose();
        
        GetSystem<VolumeSystem>().UnRegisterHitVolume(_entityId);
        
        GetSystem<EntityViewSystem>().ReleaseGameObject(UnityGameObject);
        
        _entityComponent.Clear();

        _entityComponent = null;

        _entityState = EntityState.Null;

        Config = null;

        _parentEntity = null;

        FPoolHelper.Release<FTransform>(_fTransform);
        
        _entityView = null;
        
        _baseWorld = null;
        
        _entityId = 0;

        _entityUpdateType = EntityUpdateType.AuthorityEntity;
    }

    /// <summary>
    /// 根据类型获取组件
    /// </summary>
    /// <returns></returns>
    public T GetComponent<T>() where T : BaseComponent
    {
        Type type = typeof(T);

        if (_entityComponent.TryGetValue(type, out var value))
        {
            return (T)value;
        }

        return null;
    }
    
    /// <summary>
    /// 根据类型获取组件
    /// </summary>
    /// <returns></returns>
    public T GetSystem<T>() where T : BaseSystem
    {
        T system = _baseWorld.GetSystem<T>();
        
        return system;
    }

    /// <summary>
    /// 获取某个类型的组件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T GetTypeOfComponent<T>() where T : BaseComponent
    {
        foreach (var compType in this._entityComponent.Keys)
        {
            if (typeof(T).IsAssignableFrom(compType))
            {
                return (T)_entityComponent[compType];
            }
        }

        return null;
    }

    /// <summary>
    /// 设置实体ID
    /// </summary>
    /// <param name="entityId"></param>
    public void SetEntityId(int entityId)
    {
        this._entityId = entityId;
    }

    /// <summary>
    /// 设置实体指纹
    /// </summary>
    /// <param name="entityFingerprints">实体指纹</param>
    public void SetEntityFingerprints(int entityFingerprints)
    {
        this._entityFingerprints = entityFingerprints;
    }

    /// <summary>
    /// 实体更新类型
    /// </summary>
    /// <param name="updateType"></param>
    public void SetUpdateType(EntityUpdateType updateType)
    {
        this._entityUpdateType = updateType;
    }

    public void SetWorld(BaseWorld baseWorld)
    {
        this._baseWorld = baseWorld;
    }

    public BaseWorld BaseWorld => _baseWorld;
    
    public FTransform transform => _fTransform;

    public int EntityId => _entityId;

    /// <summary>
    /// 配置ID
    /// </summary>
    public int ConfigId => Config?.assetsId ?? 0;

    /// <summary>
    /// 实体指纹 只有动态实体会生成指纹
    /// </summary>
    public int EntityFingerprints => _entityFingerprints;

    protected Dictionary<Type, BaseComponent> EntityComponent => _entityComponent;
}
