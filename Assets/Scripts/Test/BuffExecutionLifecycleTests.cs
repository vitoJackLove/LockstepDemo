#if UNITY_EDITOR
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Mathematics.FixedPoint;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Buff 执行生命周期测试，覆盖即时属性效果和临时属性修正的释放回滚。
/// </summary>
public class BuffExecutionLifecycleTests
{
    [Test]
    public void BuffClear_ReleasesPropertyEffectWithoutRecursivePoolRelease()
    {
        TestBattleData battleData = new TestBattleData();
        TestBuffOwnerEntity owner = new TestBuffOwnerEntity(battleData);

        BuffEffectAssets effectAssets = new BuffEffectAssets
        {
            buffExecuteTarget = BuffExecuteTarget.Self,
            executeProperty = PropertyKey.Hp,
            isCommonValue = true,
            normalValue = -10,
            influenceProperty = PropertyKey.Null,
            rate = 0
        };

        Buff buff = Buff.CreateBuff(owner, null, effectAssets);

        Assert.NotNull(buff);
        Assert.AreEqual(100, owner.GetProperty(PropertyKey.Hp));

        BattleAttackEventParams attackParams = BattleAttackEventParams.Create(owner, owner, 10);

        Assert.IsTrue(buff.ExecuteBuffAction(attackParams));
        Assert.AreEqual(90, owner.GetProperty(PropertyKey.Hp));

        FPoolHelper.Release<Buff>(buff);
        FPoolHelper.Release<BattleAttackEventParams>(attackParams);
    }

    [Test]
    public void PropertyEffectClear_OnlyClearsState_AndCanBeReleasedThroughPool()
    {
        TestBattleData battleData = new TestBattleData();
        TestBuffOwnerEntity owner = new TestBuffOwnerEntity(battleData);
        PropertyEffect effect = PropertyEffect.Create(owner, PropertyKey.Hp, true, -10,
            PropertyKey.Null, 0);

        Assert.IsTrue(effect.ExecuteEffect(owner, owner));
        Assert.AreEqual(90, owner.GetProperty(PropertyKey.Hp));

        Assert.DoesNotThrow(effect.Clear);

        Assert.DoesNotThrow(() => FPoolHelper.Release<PropertyEffect>(effect));
    }

    /// <summary>
    /// 验证临时属性加成会同时提升 Attack 的 Current 和 Max，重复执行不叠加，释放后回滚。
    /// </summary>
    [Test]
    public void TemporaryPropertyModifier_IncreasesCurrentAndMax_ThenRollsBackOnRelease()
    {
        TestBattleData battleData = new TestBattleData();
        TestBuffOwnerEntity owner = new TestBuffOwnerEntity(battleData);
        BuffEffectAssets effectAssets = CreateTemporaryModifierAssets(PropertyKey.Attack, 20);
        Buff buff = Buff.CreateBuff(owner, null, effectAssets);

        Assert.NotNull(buff);
        Assert.IsTrue(buff.ExecuteBuffAction(null));
        Assert.AreEqual(70, owner.GetProperty(PropertyKey.Attack));
        Assert.AreEqual(70, GetPropertyValue(owner, PropertyKey.Attack, PropertyValueType.Max));

        Assert.IsTrue(buff.ExecuteBuffAction(null));
        Assert.AreEqual(70, owner.GetProperty(PropertyKey.Attack));
        Assert.AreEqual(70, GetPropertyValue(owner, PropertyKey.Attack, PropertyValueType.Max));

        FPoolHelper.Release<Buff>(buff);

        Assert.AreEqual(50, owner.GetProperty(PropertyKey.Attack));
        Assert.AreEqual(50, GetPropertyValue(owner, PropertyKey.Attack, PropertyValueType.Max));
    }

    /// <summary>
    /// 验证临时属性减益会同时降低 Speed 和 Defence 的 Current 与 Max，释放后按记录量回滚。
    /// </summary>
    [Test]
    public void TemporaryPropertyModifier_DecreasesSpeedAndDefence_ThenRollsBackOnRelease()
    {
        TestBattleData battleData = new TestBattleData();
        TestBuffOwnerEntity owner = new TestBuffOwnerEntity(battleData);
        Buff speedBuff = Buff.CreateBuff(owner, null, CreateTemporaryModifierAssets(PropertyKey.Speed, -3));
        Buff defenceBuff = Buff.CreateBuff(owner, null, CreateTemporaryModifierAssets(PropertyKey.Defence, -5));

        Assert.NotNull(speedBuff);
        Assert.NotNull(defenceBuff);

        Assert.IsTrue(speedBuff.ExecuteBuffAction(null));
        Assert.IsTrue(defenceBuff.ExecuteBuffAction(null));

        Assert.AreEqual(7, owner.GetProperty(PropertyKey.Speed));
        Assert.AreEqual(7, GetPropertyValue(owner, PropertyKey.Speed, PropertyValueType.Max));
        Assert.AreEqual(25, owner.GetProperty(PropertyKey.Defence));
        Assert.AreEqual(25, GetPropertyValue(owner, PropertyKey.Defence, PropertyValueType.Max));

        FPoolHelper.Release<Buff>(speedBuff);
        FPoolHelper.Release<Buff>(defenceBuff);

        Assert.AreEqual(10, owner.GetProperty(PropertyKey.Speed));
        Assert.AreEqual(10, GetPropertyValue(owner, PropertyKey.Speed, PropertyValueType.Max));
        Assert.AreEqual(30, owner.GetProperty(PropertyKey.Defence));
        Assert.AreEqual(30, GetPropertyValue(owner, PropertyKey.Defence, PropertyValueType.Max));
    }

    /// <summary>
    /// 验证 PerSecond Buff 在 30 次固定帧更新后只触发一次属性效果。
    /// </summary>
    [Test]
    public void PerSecondBuff_ExecutesOnceAfterThirtyFixedUpdates()
    {
        TestBattleData battleData = new TestBattleData();
        TestBuffOwnerEntity owner = new TestBuffOwnerEntity(battleData);
        TestBuffEntity buffEntity = CreateTestBuffEntity(owner, new BuffAssetsConfig
        {
            assetsId = 9001,
            lifeTime = -1,
            gameEventType = BattleExecuteTiming.PerSecond,
            buffEffectAssets = CreateInstantPropertyAssets(BuffExecuteTarget.Self, PropertyKey.Hp, -5),
            buffConditionAssets = CreateConditionAssets(),
            executeValue = 0
        });

        for (int i = 0; i < 29; i++)
        {
            buffEntity.OnFixedUpdate((fp)0.033f, WorldUpdateType.Local);
        }

        Assert.AreEqual(100, owner.GetProperty(PropertyKey.Hp));
        Assert.AreEqual(29, buffEntity.PerSecondElapsedFrameCount);

        buffEntity.OnFixedUpdate((fp)0.033f, WorldUpdateType.Local);

        Assert.AreEqual(95, owner.GetProperty(PropertyKey.Hp));
        Assert.AreEqual(0, buffEntity.PerSecondElapsedFrameCount);
        Assert.IsFalse(buffEntity.IsDeadRequested);
    }

    /// <summary>
    /// 验证 OwnerAsDefender 只在 Buff 持有者作为受击者时满足触发条件。
    /// </summary>
    [Test]
    public void OwnerAsDefender_OnlyPassesWhenOwnerIsDefender()
    {
        TestBuffOwnerEntity owner = new TestBuffOwnerEntity(new TestBattleData());
        TestBuffOwnerEntity opponent = new TestBuffOwnerEntity(new TestBattleData());
        Buff buff = Buff.CreateBuff(owner, CreateConditionAssets(BuffTriggerRole.OwnerAsDefender),
            CreateInstantPropertyAssets(BuffExecuteTarget.Other, PropertyKey.Hp, -10));
        BattleAttackEventParams ownerDefendedParams = BattleAttackEventParams.Create(opponent, owner, 10);
        BattleAttackEventParams ownerAttackedParams = BattleAttackEventParams.Create(owner, opponent, 10);

        Assert.NotNull(buff);
        Assert.IsTrue(buff.CheckCondition(ownerDefendedParams));
        Assert.IsTrue(buff.ExecuteBuffAction(ownerDefendedParams));
        Assert.AreEqual(90, opponent.GetProperty(PropertyKey.Hp));

        Assert.IsFalse(buff.CheckCondition(ownerAttackedParams));
        Assert.IsFalse(buff.ExecuteBuffAction(ownerAttackedParams));
        Assert.AreEqual(100, owner.GetProperty(PropertyKey.Hp));

        FPoolHelper.Release<Buff>(buff);
        FPoolHelper.Release<BattleAttackEventParams>(ownerDefendedParams);
        FPoolHelper.Release<BattleAttackEventParams>(ownerAttackedParams);
    }

    /// <summary>
    /// 验证 OwnerAsAttacker 只在 Buff 持有者作为攻击者时满足触发条件。
    /// </summary>
    [Test]
    public void OwnerAsAttacker_OnlyPassesWhenOwnerIsAttacker()
    {
        TestBuffOwnerEntity owner = new TestBuffOwnerEntity(new TestBattleData());
        TestBuffOwnerEntity opponent = new TestBuffOwnerEntity(new TestBattleData());
        Buff buff = Buff.CreateBuff(owner, CreateConditionAssets(BuffTriggerRole.OwnerAsAttacker),
            CreateInstantPropertyAssets(BuffExecuteTarget.Other, PropertyKey.Hp, -10));
        BattleAttackEventParams ownerAttackedParams = BattleAttackEventParams.Create(owner, opponent, 10);
        BattleAttackEventParams ownerDefendedParams = BattleAttackEventParams.Create(opponent, owner, 10);

        Assert.NotNull(buff);
        Assert.IsTrue(buff.CheckCondition(ownerAttackedParams));
        Assert.IsTrue(buff.ExecuteBuffAction(ownerAttackedParams));
        Assert.AreEqual(90, opponent.GetProperty(PropertyKey.Hp));

        Assert.IsFalse(buff.CheckCondition(ownerDefendedParams));
        Assert.IsFalse(buff.ExecuteBuffAction(ownerDefendedParams));
        Assert.AreEqual(100, owner.GetProperty(PropertyKey.Hp));

        FPoolHelper.Release<Buff>(buff);
        FPoolHelper.Release<BattleAttackEventParams>(ownerAttackedParams);
        FPoolHelper.Release<BattleAttackEventParams>(ownerDefendedParams);
    }

    /// <summary>
    /// 验证 executeValue=3 的 Buff 成功执行三次后请求移除。
    /// </summary>
    [Test]
    public void ExecuteValueThree_RemovesBuffAfterThreeSuccessfulExecutions()
    {
        TestBuffOwnerEntity owner = new TestBuffOwnerEntity(new TestBattleData());
        TestBuffEntity buffEntity = CreateTestBuffEntity(owner, new BuffAssetsConfig
        {
            assetsId = 9010,
            lifeTime = -1,
            gameEventType = BattleExecuteTiming.PerSecond,
            buffEffectAssets = CreateInstantPropertyAssets(BuffExecuteTarget.Self, PropertyKey.Hp, -10),
            buffConditionAssets = CreateConditionAssets(),
            executeValue = 3
        });

        for (int i = 0; i < 90; i++)
        {
            buffEntity.OnFixedUpdate((fp)0.033f, WorldUpdateType.Local);
        }

        Assert.AreEqual(70, owner.GetProperty(PropertyKey.Hp));
        Assert.AreEqual(0, buffEntity.RemainingExecuteCount);
        Assert.IsTrue(buffEntity.IsDeadRequested);

        for (int i = 0; i < 30; i++)
        {
            buffEntity.OnFixedUpdate((fp)0.033f, WorldUpdateType.Local);
        }

        Assert.AreEqual(70, owner.GetProperty(PropertyKey.Hp));
    }

    /// <summary>
    /// 验证生产 Buff 配置资源可以查到 4001-4010，且每条配置具备基础运行参数。
    /// </summary>
    [Test]
    public void ProductionBuffAssets_CanLookupCommonBuffConfigs4001To4010()
    {
        BuffAssets buffAssets = AssetDatabase.LoadAssetAtPath<BuffAssets>("Assets/GameAssetConfig/BuffAssets.asset");

        Assert.NotNull(buffAssets);

        HashSet<int> foundIds = new HashSet<int>();

        for (int id = 4001; id <= 4010; id++)
        {
            BuffAssetsConfig config = buffAssets.GetDataTable(id) as BuffAssetsConfig;

            Assert.NotNull(config, $"BuffAssets.asset 缺少 Buff 配置 {id}");
            Assert.NotNull(config.buffEffectAssets, $"Buff 配置 {id} 缺少效果配置");
            Assert.NotNull(config.buffConditionAssets, $"Buff 配置 {id} 缺少条件配置");
            Assert.IsFalse(foundIds.Contains(id), $"Buff 配置 {id} 重复");
            foundIds.Add(id);
        }

        Assert.AreEqual(10, foundIds.Count);
    }

    /// <summary>
    /// 创建即时属性效果配置，用于事件触发和周期触发测试。
    /// </summary>
    /// <param name="target">Buff 执行目标集合。</param>
    /// <param name="propertyKey">需要被修改的属性键。</param>
    /// <param name="normalValue">直接应用到属性 Current 的固定变化量。</param>
    /// <returns>可传入 Buff.CreateBuff 的即时效果配置。</returns>
    private static BuffEffectAssets CreateInstantPropertyAssets(BuffExecuteTarget target, PropertyKey propertyKey,
        int normalValue)
    {
        return new BuffEffectAssets
        {
            effectMode = BuffEffectMode.Instant,
            buffExecuteTarget = target,
            executeProperty = propertyKey,
            isCommonValue = true,
            normalValue = normalValue,
            influenceProperty = PropertyKey.Null,
            rate = 0
        };
    }

    /// <summary>
    /// 创建 Buff 条件配置，默认不检查属性条件，只限制触发角色。
    /// </summary>
    /// <param name="triggerRole">Buff 持有者在攻击事件中需要扮演的角色。</param>
    /// <returns>可传入 Buff.CreateBuff 的条件配置。</returns>
    private static BuffConditionAssets CreateConditionAssets(BuffTriggerRole triggerRole = BuffTriggerRole.Any)
    {
        return new BuffConditionAssets
        {
            triggerRole = triggerRole,
            buffConditionTarget = BuffExecuteTarget.Self,
            propertyKey = PropertyKey.Null,
            propertyValueType = PropertyValueType.Current,
            compareMethod = CompareMethod.EqualTo,
            value = 0
        };
    }

    /// <summary>
    /// 创建测试用 BuffEntity，并用最小实体创建数据初始化基础运行状态。
    /// </summary>
    /// <param name="owner">Buff 持有者实体，会作为 BuffEntity 的 CustomData。</param>
    /// <param name="config">本次测试使用的 Buff 配置。</param>
    /// <returns>已初始化的测试 BuffEntity。</returns>
    private static TestBuffEntity CreateTestBuffEntity(TestBuffOwnerEntity owner, BuffAssetsConfig config)
    {
        TestBuffEntity buffEntity = new TestBuffEntity();
        EntityCreateData createData = EntityCreateData.Create(config, fp3.zero, fp3.zero, fp3.zero, false,
            null, null, owner);

        buffEntity.OnInit(createData);
        buffEntity.SetWorld(TestBuffWorld.Create());

        return buffEntity;
    }

    /// <summary>
    /// 创建测试用临时属性修正配置。
    /// </summary>
    /// <param name="propertyKey">需要被临时修正的属性键。</param>
    /// <param name="normalValue">直接应用到 Current 和 Max 的固定修正量。</param>
    /// <returns>可传入 Buff.CreateBuff 的效果配置。</returns>
    protected static BuffEffectAssets CreateTemporaryModifierAssets(PropertyKey propertyKey, int normalValue)
    {
        return new BuffEffectAssets
        {
            effectMode = BuffEffectMode.TemporaryPropertyModifier,
            buffExecuteTarget = BuffExecuteTarget.Self,
            executeProperty = propertyKey,
            isCommonValue = true,
            normalValue = normalValue,
            influenceProperty = PropertyKey.Null,
            rate = 0
        };
    }

    /// <summary>
    /// 读取测试实体指定属性值类型的数值，并断言该属性存在。
    /// </summary>
    /// <param name="entity">需要读取属性的测试实体。</param>
    /// <param name="propertyKey">需要读取的属性键。</param>
    /// <param name="valueType">需要读取的属性值类型。</param>
    /// <returns>指定属性值类型的当前数值。</returns>
    protected static fp GetPropertyValue(BaseEntity entity, PropertyKey propertyKey, PropertyValueType valueType)
    {
        Assert.IsTrue(entity.TryGetPropertyValue(propertyKey, valueType, out fp value));

        return value;
    }

    /// <summary>
    /// 测试用 Buff 持有者实体，只提供属性数据并屏蔽组件和视图依赖。
    /// </summary>
    protected sealed class TestBuffOwnerEntity : BaseEntity
    {
        /// <summary>
        /// 测试实体持有的战斗属性数据。
        /// </summary>
        private readonly BattleEntityData _entityPropertyData;

        /// <summary>
        /// 创建测试用 Buff 持有者实体。
        /// </summary>
        /// <param name="entityPropertyData">注入给实体读取的战斗属性数据。</param>
        public TestBuffOwnerEntity(BattleEntityData entityPropertyData)
        {
            _entityPropertyData = entityPropertyData;

            EntityCreateData createData = EntityCreateData.Create(new EntityAssetsConfig(), fp3.zero, fp3.zero,
                fp3.zero, false);

            OnInit(createData);
        }

        /// <summary>
        /// 测试实体使用的战斗属性数据。
        /// </summary>
        protected override BattleEntityData EntityPropertyData => _entityPropertyData;

        /// <summary>
        /// 测试实体不需要运行时组件。
        /// </summary>
        /// <returns>空组件类型数组。</returns>
        protected override System.Type[] GetComponentTypes()
        {
            return System.Array.Empty<System.Type>();
        }

        /// <summary>
        /// 测试实体不需要 Unity 视图。
        /// </summary>
        /// <returns>始终返回 null。</returns>
        protected override System.Type EntityView()
        {
            return null;
        }

        /// <summary>
        /// 测试实体类型，复用 BuffEntity 类型避免新增枚举。
        /// </summary>
        public override EntityType EntityType => EntityType.BuffEntity;

        /// <summary>
        /// 测试实体预测类型，按动态实体处理。
        /// </summary>
        public override ForecastEntityType ForecastEntityType => ForecastEntityType.DynamicEntity;
    }

    /// <summary>
    /// 测试用 BuffEntity，屏蔽完整 World 销毁流程，只记录 Buff 是否请求移除。
    /// </summary>
    private sealed class TestBuffEntity : BuffEntity
    {
        /// <summary>
        /// BuffEntity 是否已因为执行次数耗尽或生命周期结束请求死亡。
        /// </summary>
        public bool IsDeadRequested { get; private set; }

        /// <summary>
        /// 记录死亡请求，避免 EditMode 单元测试依赖 EntitySystem 和完整 World。
        /// </summary>
        /// <param name="data">运行时传入的死亡原因数据，当前测试不读取。</param>
        public override void DoEntityDead(object data = null)
        {
            IsDeadRequested = true;
        }
    }

    /// <summary>
    /// 测试用最小 World，只为 BuffEntity 提供 Tick 读取所需的 BaseWorld 实例。
    /// </summary>
    private sealed class TestBuffWorld : BaseWorld
    {
        /// <summary>
        /// 使用临时根节点创建测试 World，避免依赖真实场景和系统初始化。
        /// </summary>
        private TestBuffWorld(CreateWorldData createWorldData) : base(createWorldData)
        {
        }

        /// <summary>
        /// 创建测试 World，并把用于构造的创建数据释放回对象池。
        /// </summary>
        /// <returns>可挂接到测试实体上的最小 World。</returns>
        public static TestBuffWorld Create()
        {
            CreateWorldData createWorldData = FPoolHelper.Get<CreateWorldData>();
            GameObject worldRoot = new GameObject("BuffTestWorldRoot");

            createWorldData.WorldId = 1;
            createWorldData.SceneName = "BuffTest";
            createWorldData.WorldRoot = worldRoot.transform;

            TestBuffWorld world = new TestBuffWorld(createWorldData);

            FPoolHelper.Release<CreateWorldData>(createWorldData);

            return world;
        }

        /// <summary>
        /// 测试 World 不注册任何系统，避免 EditMode 单元测试拉起完整运行时。
        /// </summary>
        /// <returns>空系统类型数组。</returns>
        protected override System.Type[] GetSystemTypes()
        {
            return System.Array.Empty<System.Type>();
        }

        /// <summary>
        /// 测试 World 不执行游戏准备流程，保留抽象方法的最小实现。
        /// </summary>
        /// <param name="createWorldData">创建 World 时传入的数据，当前测试不读取。</param>
        /// <returns>固定返回 true 的已完成任务。</returns>
        protected override Task<bool> GamePreparation(CreateWorldData createWorldData)
        {
            return Task.FromResult(true);
        }
    }

    /// <summary>
    /// 测试用战斗属性数据，提供 HP、Attack、Speed 和 Defence 的初始值。
    /// </summary>
    protected sealed class TestBattleData : BattleEntityData
    {
        /// <summary>
        /// 创建测试属性数据并立即初始化属性表。
        /// </summary>
        public TestBattleData()
        {
            InitProperty();
        }

        /// <summary>
        /// 初始化测试需要的属性键和值域。
        /// </summary>
        public override void InitProperty()
        {
            EntityPropertyDataDic.Clear();
            EntityPropertyDataDic.Add(PropertyKey.Hp.ToString(), PropertyData.Create(100, 0, 100));
            EntityPropertyDataDic.Add(PropertyKey.Attack.ToString(), PropertyData.Create(50, 0, 50));
            EntityPropertyDataDic.Add(PropertyKey.Speed.ToString(), PropertyData.Create(10, 0, 10));
            EntityPropertyDataDic.Add(PropertyKey.Defence.ToString(), PropertyData.Create(30, 0, 30));
        }
    }
}
#endif
