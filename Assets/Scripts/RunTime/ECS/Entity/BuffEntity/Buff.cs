using System.Collections.Generic;
using Unity.Mathematics.FixedPoint;

/// <summary>
/// Buff
/// </summary>
public class Buff : IPool
{
    /// <summary>
    /// Buff持有者
    /// </summary>
    private BaseEntity _ownerEntity;

    private BuffConditionAssets _conditionAssets;
    
    /// <summary>
    /// 属性效果
    /// </summary>
    private PropertyEffect _propertyEffect;

    private BuffEffectAssets _buffEffectAssets;

    private readonly List<BaseEntity> _targetCache = new List<BaseEntity>();
    
    /// <summary>
    /// 创建 Buff 运行时对象并缓存配置引用，ownerEntity 为空时返回 null。
    /// </summary>
    /// <param name="ownerEntity">Buff 持有者，决定角色过滤和默认执行来源。</param>
    /// <param name="conditionAssets">Buff 条件配置，旧资源可为空并按无条件处理。</param>
    /// <param name="buffEffectAssets">Buff 效果配置，旧资源可为空并跳过效果执行。</param>
    /// <returns>可复用的 Buff 运行时对象，创建失败时返回 null。</returns>
    public static Buff CreateBuff(BaseEntity ownerEntity,BuffConditionAssets 
        conditionAssets,BuffEffectAssets buffEffectAssets)
    {
        if (ownerEntity == null)
        {
            return null;
        }

        Buff buff = FPoolHelper.Get<Buff>();

        buff._ownerEntity = ownerEntity;
        buff._conditionAssets = conditionAssets;
        buff._buffEffectAssets = buffEffectAssets;

        if (buffEffectAssets != null)
        {
            buff._propertyEffect = PropertyEffect.Create(ownerEntity, buffEffectAssets.executeProperty,
                buffEffectAssets.isCommonValue, buffEffectAssets.normalValue, buffEffectAssets.influenceProperty,
                buffEffectAssets.rate);
        }

        return buff;
    }

    /// <summary>
    /// 检查 Buff 触发角色与属性条件是否满足。
    /// </summary>
    /// <param name="observerParams">战斗观察者事件参数，用于解析攻击者、受击者和条件目标。</param>
    /// <returns>角色过滤与属性条件均通过时返回 true，否则返回 false。</returns>
    public bool CheckCondition(IBattleObserverParams observerParams)
    {
        if (!CheckTriggerRole(observerParams))
        {
            return false;
        }

        if (_conditionAssets == null || _conditionAssets.propertyKey == PropertyKey.Null)
        {
            return true;
        }

        _targetCache.Clear();

        ResolveTargets(_conditionAssets.buffConditionTarget, observerParams, _targetCache);

        if (_targetCache.Count == 0)
        {
            return false;
        }

        fp compareValue = _conditionAssets.value;

        for (int i = 0; i < _targetCache.Count; i++)
        {
            BaseEntity target = _targetCache[i];

            if (target == null ||
                !target.TryGetPropertyValue(_conditionAssets.propertyKey, _conditionAssets.propertyValueType,
                    out fp propertyValue))
            {
                return false;
            }

            if (!Compare(propertyValue, compareValue, _conditionAssets.compareMethod))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 执行 Buff 效果，Instant 立即修改 Current，TemporaryPropertyModifier 临时修改 Current 和 Max。
    /// </summary>
    /// <param name="observerParams">战斗观察者事件参数，用于解析效果执行目标。</param>
    /// <returns>至少一个目标成功执行属性效果时返回 true，否则返回 false。</returns>
    public bool ExecuteBuffAction(IBattleObserverParams observerParams)
    {
        if (_propertyEffect == null || _buffEffectAssets == null)
        {
            return false;
        }

        _targetCache.Clear();

        ResolveTargets(_buffEffectAssets.buffExecuteTarget, observerParams, _targetCache);

        if (_targetCache.Count == 0)
        {
            return false;
        }

        bool executed = false;

        for (int i = 0; i < _targetCache.Count; i++)
        {
            if (ExecutePropertyEffectByMode(_targetCache[i]))
            {
                executed = true;
            }
        }

        return executed;
    }

    /// <summary>
    /// Buff 持有者实体，用于外部测试或调试确认运行时归属。
    /// </summary>
    public BaseEntity OwnerEntity => _ownerEntity;

    /// <summary>
    /// Buff 条件配置引用，保留原始配置便于运行时判断触发角色和属性条件。
    /// </summary>
    public BuffConditionAssets ConditionAssets => _conditionAssets;

    /// <summary>
    /// Buff 效果配置引用，保留原始配置便于运行时判断效果目标和执行模式。
    /// </summary>
    public BuffEffectAssets BuffEffectAssets => _buffEffectAssets;

    /// <summary>
    /// Buff 每个固定逻辑帧的扩展更新入口，基础 Buff 当前不执行额外逻辑。
    /// </summary>
    public virtual void OnFixUpdate() { }

    /// <summary>
    /// 清理 Buff 运行时状态，释放属性效果时会回滚尚未清理的临时属性修正。
    /// </summary>
    public void Clear()
    {
        _ownerEntity = null;
        _conditionAssets = null;
        _buffEffectAssets = null;

        if (_propertyEffect != null)
        {
            FPoolHelper.Release<PropertyEffect>(_propertyEffect);
            _propertyEffect = null;
        }

        _targetCache.Clear();
    }

    /// <summary>
    /// 按 Buff 效果模式执行属性效果。
    /// </summary>
    /// <param name="target">本次效果解析出的目标实体。</param>
    /// <returns>目标成功执行对应模式属性效果时返回 true，否则返回 false。</returns>
    private bool ExecutePropertyEffectByMode(BaseEntity target)
    {
        switch (_buffEffectAssets.effectMode)
        {
            case BuffEffectMode.Instant:
                return _propertyEffect.ExecuteEffect(target, _ownerEntity);
            case BuffEffectMode.TemporaryPropertyModifier:
                return _propertyEffect.ApplyTemporaryModifier(target, _ownerEntity);
            default:
                return false;
        }
    }

    /// <summary>
    /// 根据配置的执行目标解析实际实体列表，Self 与 AllEnemy 支持无事件周期触发，Other 仍依赖攻击事件。
    /// </summary>
    /// <param name="targetType">Buff 配置中的目标标记，可组合自己、指向目标和所有敌人。</param>
    /// <param name="observerParams">战斗观察者事件参数；周期触发时可为空。</param>
    /// <param name="targets">输出的目标缓存列表，调用方负责在传入前清空。</param>
    private void ResolveTargets(BuffExecuteTarget targetType, IBattleObserverParams observerParams,
        List<BaseEntity> targets)
    {
        if (targetType == 0 || _ownerEntity == null)
        {
            return;
        }

        if ((targetType & BuffExecuteTarget.Self) != 0)
        {
            AddTarget(_ownerEntity, targets);
        }

        if ((targetType & BuffExecuteTarget.Other) != 0)
        {
            AddTarget(ResolveOtherTarget(observerParams), targets);
        }

        if ((targetType & BuffExecuteTarget.AllEnemy) != 0)
        {
            AddAllEnemyTargets(targets);
        }
    }

    /// <summary>
    /// 从攻击事件中解析与 Buff 持有者相对的另一方实体。
    /// </summary>
    /// <param name="observerParams">战斗观察者事件参数，非攻击事件会返回 null。</param>
    /// <returns>持有者为攻击者时返回受击者，持有者为受击者时返回攻击者，否则返回 null。</returns>
    private BaseEntity ResolveOtherTarget(IBattleObserverParams observerParams)
    {
        if (!CanResolveTargets(observerParams))
        {
            return null;
        }

        BattleAttackEventParams attackParams = observerParams as BattleAttackEventParams;

        if (attackParams.Attacker == _ownerEntity)
        {
            return attackParams.Defender;
        }

        if (attackParams.Defender == _ownerEntity)
        {
            return attackParams.Attacker;
        }

        return null;
    }

    /// <summary>
    /// 判断当前事件是否足以解析攻击事件中的相对目标。
    /// </summary>
    /// <param name="observerParams">战斗观察者事件参数，当前仅攻击事件可解析 Other。</param>
    /// <returns>事件中包含 Buff 持有者作为攻击者或受击者时返回 true。</returns>
    private bool CanResolveTargets(IBattleObserverParams observerParams)
    {
        BattleAttackEventParams attackParams = observerParams as BattleAttackEventParams;

        if (attackParams == null || _ownerEntity == null)
        {
            return false;
        }

        return attackParams.Attacker == _ownerEntity || attackParams.Defender == _ownerEntity;
    }

    /// <summary>
    /// 按 Buff 条件中的触发角色过滤战斗事件，缺省配置按 Any 兼容旧资源。
    /// </summary>
    /// <param name="observerParams">战斗观察者事件参数，目前支持攻击事件中的攻击者与受击者判断。</param>
    /// <returns>持有者角色满足配置时返回 true，否则返回 false。</returns>
    private bool CheckTriggerRole(IBattleObserverParams observerParams)
    {
        BuffTriggerRole triggerRole = _conditionAssets?.triggerRole ?? BuffTriggerRole.Any;

        if (triggerRole == BuffTriggerRole.Any)
        {
            return true;
        }

        BattleAttackEventParams attackParams = observerParams as BattleAttackEventParams;

        if (attackParams == null || _ownerEntity == null)
        {
            return false;
        }

        switch (triggerRole)
        {
            case BuffTriggerRole.OwnerAsAttacker:
                return attackParams.Attacker == _ownerEntity;
            case BuffTriggerRole.OwnerAsDefender:
                return attackParams.Defender == _ownerEntity;
            default:
                return true;
        }
    }

    /// <summary>
    /// 收集与 Buff 持有者敌对且处于同一更新域的所有可用敌方实体。
    /// </summary>
    /// <param name="targets">输出的敌方目标缓存列表。</param>
    private void AddAllEnemyTargets(List<BaseEntity> targets)
    {
        if (_ownerEntity == null)
        {
            return;
        }

        if (_ownerEntity.BaseWorld == null)
        {
            return;
        }

        EntitySystem entitySystem = _ownerEntity.GetSystem<EntitySystem>();

        if (entitySystem == null)
        {
            return;
        }

        WorldUpdateType worldUpdateType = WorldUpdateType.Local;

        AddEnemyTargets(entitySystem.GetLockTargetCandidates(worldUpdateType), targets);
    }

    /// <summary>
    /// 从候选实体列表中筛选敌对存活目标并加入目标缓存。
    /// </summary>
    /// <param name="candidates">候选实体只读列表，可为空。</param>
    /// <param name="targets">输出的目标缓存列表，会自动跳过重复实体。</param>
    private void AddEnemyTargets(IReadOnlyList<BaseEntity> candidates, List<BaseEntity> targets)
    {
        if (candidates == null)
        {
            return;
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            BaseEntity candidate = candidates[i];

            if (candidate == null || candidate == _ownerEntity || candidate.EntityState != EntityState.Survival)
            {
                continue;
            }

            if (candidate.EntityUpdateType != _ownerEntity.EntityUpdateType)
            {
                continue;
            }

            if (_ownerEntity.CheckIsAdversarial(candidate))
            {
                AddTarget(candidate, targets);
            }
        }
    }

    /// <summary>
    /// 将单个有效目标加入缓存，过滤空实体、死亡实体、重复实体和不同更新域实体。
    /// </summary>
    /// <param name="target">准备加入的目标实体。</param>
    /// <param name="targets">输出的目标缓存列表。</param>
    private void AddTarget(BaseEntity target, List<BaseEntity> targets)
    {
        if (target == null || target.EntityState != EntityState.Survival || targets.Contains(target))
        {
            return;
        }

        if (_ownerEntity != null && target != _ownerEntity &&
            target.EntityUpdateType != _ownerEntity.EntityUpdateType)
        {
            return;
        }

        targets.Add(target);
    }

    /// <summary>
    /// 使用配置的比较方式判断属性值是否满足条件。
    /// </summary>
    /// <param name="propertyValue">实体当前读取到的属性值。</param>
    /// <param name="compareValue">配置转换后的比较值。</param>
    /// <param name="compareMethod">条件配置中的比较方式。</param>
    /// <returns>比较成立时返回 true，否则返回 false。</returns>
    private bool Compare(fp propertyValue, fp compareValue, CompareMethod compareMethod)
    {
        switch (compareMethod)
        {
            case CompareMethod.EqualTo:
                return propertyValue == compareValue;
            case CompareMethod.GreaterThan:
                return propertyValue > compareValue;
            case CompareMethod.LessThan:
                return propertyValue < compareValue;
            case CompareMethod.GreaterOrEqualTo:
                return propertyValue >= compareValue;
            case CompareMethod.LessOrEqualTo:
                return propertyValue <= compareValue;
            default:
                return false;
        }
    }
}
