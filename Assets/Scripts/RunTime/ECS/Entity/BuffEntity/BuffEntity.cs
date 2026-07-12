using System;
using Ase.Serializing;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// Buff实体
/// </summary>
public class BuffEntity : BaseEntity , IBattleObserverHandle
{
    /// <summary>
    /// PerSecond Buff 使用的逻辑帧间隔，等于 1 秒内的逻辑帧数。
    /// </summary>
    private int PerSecondFrameInterval => fpmath1.LogicFrameRate;

    /// <summary>
    /// Buff的层数
    /// </summary>
    private int _layer;
    
    /// <summary>
    /// 监听的游戏事件
    /// </summary>
    private BattleExecuteTiming _monitorGameEvent;

    /// <summary>
    /// 效果剩余可执行次数；仅在 _isExecuteCountLimited 为 true 时递减到 0。
    /// </summary>
    private int _remainingExecuteCount;

    /// <summary>
    /// executeValue 大于 0 时限制执行次数；小于等于 0 时不限制。
    /// </summary>
    private bool _isExecuteCountLimited;

    /// <summary>
    /// 每层 Buff 可提供的执行次数，用于叠层时累加剩余次数。
    /// </summary>
    private int _executeCountPerLayer;

    /// <summary>
    /// 当前 Buff 是否已注册到战斗观察者系统，避免重复注册和释放。
    /// </summary>
    private bool _isObserverAttached;

    /// <summary>
    /// Buff 生命周期配置值，写入组件数据并参与快照恢复。
    /// </summary>
    private int _lifeTime;

    /// <summary>
    /// Buff 效果配置引用，用于回滚恢复运行时 Buff 对象。
    /// </summary>
    private BuffEffectAssets _buffEffectAssets;

    /// <summary>
    /// Buff 条件配置引用，用于触发角色和属性条件判断。
    /// </summary>
    private BuffConditionAssets _buffConditionAssets;

    /// <summary>
    /// Buff 运行时执行对象，负责条件判断、目标解析和效果执行。
    /// </summary>
    private Buff _buff;

    /// <summary>
    /// PerSecond 已累计的逻辑帧数，达到 30 帧后触发一次并清零。
    /// </summary>
    private int _perSecondElapsedFrameCount;

    /// <summary>
    /// PerSecond 最近一次执行所在的世界帧，用于防止同一帧重复触发。
    /// </summary>
    private int _lastPerSecondExecuteTick;

    /// <summary>
    /// PerSecond 最近一次执行所在的更新域，区分本地、权威和回放帧。
    /// </summary>
    private WorldUpdateType _lastPerSecondExecuteWorldUpdateType;

    /// <summary>
    /// PerSecond 是否已经记录过执行帧，避免初始 Tick 与默认值 0 冲突。
    /// </summary>
    private bool _hasPerSecondExecuteTick;
    
    public override void OnInit(object data = null)
    {
        base.OnInit(data);

        BuffAssetsConfig config = Config as BuffAssetsConfig;

        if (config == null)
        {
            Debug.Log($"Buff 初始化失败 : 没有配置...");

            return;
        }

        BaseEntity ownerEntity = CustomData as BaseEntity;

        if (ownerEntity == null)
        {
            Debug.Log($"Buff 初始化失败 : 目标实体为空...");

            return;
        }

        _layer = 1;
        _lifeTime = config.lifeTime;
        _monitorGameEvent = config.gameEventType;
        _isExecuteCountLimited = config.executeValue > 0;
        _executeCountPerLayer = _isExecuteCountLimited ? config.executeValue : 0;
        _remainingExecuteCount = _executeCountPerLayer;
        _buffEffectAssets = config.buffEffectAssets;
        _buffConditionAssets = config.buffConditionAssets;

        _buff = Buff.CreateBuff(ownerEntity, _buffConditionAssets, _buffEffectAssets);

        if (_buff == null)
        {
            Debug.Log($"Buff 初始化失败 : Buff 运行时创建失败...");

            return;
        }

        InitComponentData(config);

        if (ShouldAttachObserver(_monitorGameEvent))
        {
            GetSystem<BattleObserverSystem>().Attach(_monitorGameEvent, this);
            _isObserverAttached = true;
        }
    }

    /// <summary>
    /// 增加层数
    /// </summary>
    public void IncreaseLayer()
    {
        _layer++;

        if (_isExecuteCountLimited)
        {
            _remainingExecuteCount += _executeCountPerLayer;
        }
    }

    /// <summary>
    /// 接收战斗观察者事件通知，并在事件时机匹配时执行 Buff。
    /// </summary>
    /// <param name="param">战斗事件参数，事件驱动 Buff 必须提供有效参数。</param>
    public void OnNotify(IBattleObserverParams param)
    {
        if (param == null || _buff == null)
        {
            return;
        }

        if (_monitorGameEvent != param.BattleExecuteTiming)
        {
            return;
        }

        TryExecuteBuff(param);
    }

    /// <summary>
    /// Buff 固定帧更新入口，驱动运行时对象更新并处理 PerSecond 周期触发。
    /// </summary>
    /// <param name="deltaTime">当前逻辑帧间隔。</param>
    /// <param name="worldUpdateType">当前世界更新域。</param>
    public override void OnFixedUpdate(fp deltaTime, WorldUpdateType worldUpdateType)
    {
        base.OnFixedUpdate(deltaTime, worldUpdateType);

        if (_buff != null)
        {
            _buff.OnFixUpdate();
        }

        TryExecutePerSecond(worldUpdateType);
    }

    protected override void OnEntityDead(object data = null)
    {
        ReleaseRuntimeReferences();

        base.OnEntityDead(data);
    }

    public override void OnDispose()
    {
        ReleaseRuntimeReferences();

        base.OnDispose();
        
        _layer = 0;
        _monitorGameEvent = BattleExecuteTiming.Null;
        _remainingExecuteCount = 0;
        _isExecuteCountLimited = false;
        _executeCountPerLayer = 0;
        _isObserverAttached = false;
        _lifeTime = 0;
        _buffEffectAssets = null;
        _buffConditionAssets = null;
        _perSecondElapsedFrameCount = 0;
        _lastPerSecondExecuteTick = 0;
        _lastPerSecondExecuteWorldUpdateType = WorldUpdateType.Local;
        _hasPerSecondExecuteTick = false;
    }

    private void ReleaseRuntimeReferences()
    {
        ReleaseObserver();

        ParentEntity?.GetComponent<BuffComponent>()?.RemoveBuff(ConfigId, EntityFingerprints);

        if (_buff != null)
        {
            FPoolHelper.Release<Buff>(_buff);
            _buff = null;
        }
    }

    private void ReleaseObserver()
    {
        if (_isObserverAttached)
        {
            GetSystem<BattleObserverSystem>()?.Detach(_monitorGameEvent, this);
            _isObserverAttached = false;
        }
    }


    private void EnsureRuntimeReferences()
    {
        BaseEntity ownerEntity = CustomData as BaseEntity;

        if (_buff == null && ownerEntity != null)
        {
            _buff = Buff.CreateBuff(ownerEntity, _buffConditionAssets, _buffEffectAssets);
        }

        if (!_isObserverAttached && ShouldAttachObserver(_monitorGameEvent))
        {
            GetSystem<BattleObserverSystem>()?.Attach(_monitorGameEvent, this);
            _isObserverAttached = true;
        }
    }

    /// <summary>
    /// 恢复 Buff 执行次数相关运行时状态，用于测试或回滚流程在重建引用前写回层数和剩余次数。
    /// </summary>
    /// <param name="layer">需要恢复的 Buff 当前层数。</param>
    /// <param name="isExecuteCountLimited">是否启用执行次数限制。</param>
    /// <param name="executeCountPerLayer">每层 Buff 对应的可执行次数。</param>
    /// <param name="remainingExecuteCount">当前剩余可执行次数。</param>
    public void RestoreExecutionState(int layer, bool isExecuteCountLimited, int executeCountPerLayer,
        int remainingExecuteCount)
    {
        _layer = layer;
        _isExecuteCountLimited = isExecuteCountLimited;
        _executeCountPerLayer = executeCountPerLayer;
        _remainingExecuteCount = remainingExecuteCount;

        if (EntityState == EntityState.Survival)
        {
            EnsureRuntimeReferences();
        }
    }

    public override void TakeSnapShot(uint tick, BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
    {
        base.TakeSnapShot(tick, hardWriter, softWriter);

        hardWriter.WriteInt32Data($"Buff的层数", _layer);
        hardWriter.WriteInt32Data($"Buff监听时机", (int)_monitorGameEvent);
        hardWriter.WriteInt32Data($"Buff生命周期", _lifeTime);
        hardWriter.WriterBoolData($"Buff是否限制执行次数", _isExecuteCountLimited);
        hardWriter.WriteInt32Data($"Buff每层执行次数", _executeCountPerLayer);
        hardWriter.WriteInt32Data($"Buff剩余执行次数", _remainingExecuteCount);
        hardWriter.WriteInt32Data($"Buff每秒触发累计帧", _perSecondElapsedFrameCount);
        hardWriter.WriteInt32Data($"Buff每秒触发最近帧", _lastPerSecondExecuteTick);
        hardWriter.WriteInt32Data($"Buff每秒触发最近更新域", (int)_lastPerSecondExecuteWorldUpdateType);
        hardWriter.WriterBoolData($"Buff每秒触发是否已有最近帧", _hasPerSecondExecuteTick);

        softWriter.WriteInt32Data($"Buff的层数", _layer);
        softWriter.WriteInt32Data($"Buff监听时机", (int)_monitorGameEvent);
        softWriter.WriteInt32Data($"Buff生命周期", _lifeTime);
        softWriter.WriterBoolData($"Buff是否限制执行次数", _isExecuteCountLimited);
        softWriter.WriteInt32Data($"Buff每层执行次数", _executeCountPerLayer);
        softWriter.WriteInt32Data($"Buff剩余执行次数", _remainingExecuteCount);
        softWriter.WriteInt32Data($"Buff每秒触发累计帧", _perSecondElapsedFrameCount);
        softWriter.WriteInt32Data($"Buff每秒触发最近帧", _lastPerSecondExecuteTick);
        softWriter.WriteInt32Data($"Buff每秒触发最近更新域", (int)_lastPerSecondExecuteWorldUpdateType);
        softWriter.WriterBoolData($"Buff每秒触发是否已有最近帧", _hasPerSecondExecuteTick);
    }

    public override void HardRollBack(PooledReader authoritySnapShot)
    {
        base.HardRollBack(authoritySnapShot);

        _layer = authoritySnapShot.ReadInt32();
        BattleExecuteTiming restoredMonitorGameEvent = (BattleExecuteTiming)authoritySnapShot.ReadInt32();
        _lifeTime = authoritySnapShot.ReadInt32();
        _isExecuteCountLimited = authoritySnapShot.ReadBoolean();
        _executeCountPerLayer = authoritySnapShot.ReadInt32();
        _remainingExecuteCount = authoritySnapShot.ReadInt32();
        _perSecondElapsedFrameCount = authoritySnapShot.ReadInt32();
        _lastPerSecondExecuteTick = authoritySnapShot.ReadInt32();
        _lastPerSecondExecuteWorldUpdateType = (WorldUpdateType)authoritySnapShot.ReadInt32();
        _hasPerSecondExecuteTick = authoritySnapShot.ReadBoolean();

        if (_monitorGameEvent != restoredMonitorGameEvent)
        {
            ReleaseObserver();
            _monitorGameEvent = restoredMonitorGameEvent;
        }

        if (EntityState == EntityState.Survival)
        {
            EnsureRuntimeReferences();
        }
        else
        {
            ReleaseRuntimeReferences();
        }
    }

    public override void SoftRollBack(PooledReader authoritySnapShot)
    {
        base.SoftRollBack(authoritySnapShot);

        _layer = authoritySnapShot.ReadInt32();
        BattleExecuteTiming restoredMonitorGameEvent = (BattleExecuteTiming)authoritySnapShot.ReadInt32();
        _lifeTime = authoritySnapShot.ReadInt32();
        _isExecuteCountLimited = authoritySnapShot.ReadBoolean();
        _executeCountPerLayer = authoritySnapShot.ReadInt32();
        _remainingExecuteCount = authoritySnapShot.ReadInt32();
        _perSecondElapsedFrameCount = authoritySnapShot.ReadInt32();
        _lastPerSecondExecuteTick = authoritySnapShot.ReadInt32();
        _lastPerSecondExecuteWorldUpdateType = (WorldUpdateType)authoritySnapShot.ReadInt32();
        _hasPerSecondExecuteTick = authoritySnapShot.ReadBoolean();

        if (_monitorGameEvent != restoredMonitorGameEvent)
        {
            ReleaseObserver();
            _monitorGameEvent = restoredMonitorGameEvent;
        }

        if (EntityState == EntityState.Survival)
        {
            EnsureRuntimeReferences();
        }
    }

    private void InitComponentData(BuffAssetsConfig config)
    {
        this.SetData(ComponentDataKey.LifeTime, _lifeTime);
    }

    /// <summary>
    /// 判断指定 Buff 时机是否需要注册到战斗观察者系统；周期 Buff 由固定帧更新驱动。
    /// </summary>
    /// <param name="timing">Buff 配置的触发时机。</param>
    /// <returns>事件驱动时机返回 true，Null 和 PerSecond 返回 false。</returns>
    private bool ShouldAttachObserver(BattleExecuteTiming timing)
    {
        return timing != BattleExecuteTiming.Null && timing != BattleExecuteTiming.PerSecond;
    }

    /// <summary>
    /// 在固定帧更新中处理 PerSecond Buff，累计 30 逻辑帧后执行一次。
    /// </summary>
    /// <param name="worldUpdateType">当前世界更新域，用于读取对应 Tick 并防止同帧重复触发。</param>
    private void TryExecutePerSecond(WorldUpdateType worldUpdateType)
    {
        if (_monitorGameEvent != BattleExecuteTiming.PerSecond || _buff == null)
        {
            return;
        }

        _perSecondElapsedFrameCount++;

        if (_perSecondElapsedFrameCount < PerSecondFrameInterval)
        {
            return;
        }

        _perSecondElapsedFrameCount = 0;

        int currentTick = GetCurrentTick(worldUpdateType);

        if (_hasPerSecondExecuteTick &&
            _lastPerSecondExecuteTick == currentTick &&
            _lastPerSecondExecuteWorldUpdateType == worldUpdateType)
        {
            return;
        }

        _lastPerSecondExecuteTick = currentTick;
        _lastPerSecondExecuteWorldUpdateType = worldUpdateType;
        _hasPerSecondExecuteTick = true;

        TryExecuteBuff(null);
    }

    /// <summary>
    /// 读取当前更新域对应的世界 Tick，回放流程沿用本地 Tick。
    /// </summary>
    /// <param name="worldUpdateType">当前世界更新域。</param>
    /// <returns>权威域返回 AuthorityTick，其余返回 LocalTick。</returns>
    private int GetCurrentTick(WorldUpdateType worldUpdateType)
    {
        if (BaseWorld == null)
        {
            return 0;
        }

        uint currentTick = worldUpdateType == WorldUpdateType.Authority
            ? BaseWorld.AuthorityTick
            : BaseWorld.LocalTick;

        return currentTick > int.MaxValue ? int.MaxValue : (int)currentTick;
    }

    /// <summary>
    /// 统一执行 Buff 条件判断、效果触发和执行次数扣减。
    /// </summary>
    /// <param name="param">战斗事件参数；周期触发时传入 null 并走无事件目标解析。</param>
    /// <returns>本次 Buff 成功执行至少一个效果时返回 true，否则返回 false。</returns>
    private bool TryExecuteBuff(IBattleObserverParams param)
    {
        if (_buff == null)
        {
            return false;
        }

        if (_isExecuteCountLimited && _remainingExecuteCount <= 0)
        {
            DoEntityDead();

            return false;
        }

        if (!_buff.CheckCondition(param))
        {
            return false;
        }

        bool executed = _buff.ExecuteBuffAction(param);

        if (executed && _isExecuteCountLimited)
        {
            _remainingExecuteCount--;

            if (_remainingExecuteCount <= 0)
            {
                DoEntityDead();
            }
        }

        return executed;
    }

    /// <summary>
    /// PerSecond 已累计的逻辑帧数，用于测试和回滚调试。
    /// </summary>
    public int PerSecondElapsedFrameCount => _perSecondElapsedFrameCount;

    /// <summary>
    /// PerSecond 最近执行的世界 Tick，用于测试同帧去重。
    /// </summary>
    public int LastPerSecondExecuteTick => _lastPerSecondExecuteTick;

    /// <summary>
    /// 当前 Buff 叠加层数，用于叠层效果和回滚状态检查。
    /// </summary>
    public int Layer => _layer;

    /// <summary>
    /// 当前 Buff 监听或周期驱动的战斗时机。
    /// </summary>
    public BattleExecuteTiming MonitorGameEvent => _monitorGameEvent;

    /// <summary>
    /// 当前 Buff 剩余可执行次数，未限制次数时该值不参与扣减。
    /// </summary>
    public int RemainingExecuteCount => _remainingExecuteCount;

    /// <summary>
    /// 当前 Buff 是否启用执行次数限制。
    /// </summary>
    public bool IsExecuteCountLimited => _isExecuteCountLimited;

    /// <summary>
    /// 每层 Buff 提供的可执行次数，用于叠层时累加。
    /// </summary>
    public int ExecuteCountPerLayer => _executeCountPerLayer;

    /// <summary>
    /// Buff 生命周期配置值，供组件数据和调试读取。
    /// </summary>
    public int LifeTime => _lifeTime;

    /// <summary>
    /// 当前 Buff 使用的效果配置引用。
    /// </summary>
    public BuffEffectAssets BuffEffectAssets => _buffEffectAssets;

    /// <summary>
    /// 当前 Buff 使用的条件配置引用。
    /// </summary>
    public BuffConditionAssets BuffConditionAssets => _buffConditionAssets;

    protected override BattleEntityData EntityPropertyData => null;
    
    protected override Type[] GetComponentTypes()
    {
        return new []
        {
            typeof(EntityLifeTimeComponent),
            typeof(ActionComponent),
        };
    }

    protected override Type EntityView()
    {
        return null;
    }

    public override EntityType EntityType => EntityType.BuffEntity;
    public override ForecastEntityType ForecastEntityType => ForecastEntityType.DynamicEntity;
}
