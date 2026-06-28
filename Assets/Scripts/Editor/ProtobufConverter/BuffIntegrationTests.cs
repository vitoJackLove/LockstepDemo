using System;
using System.Reflection;
using System.Threading.Tasks;
using Ase.Serializing;
using NUnit.Framework;
using Unity.Mathematics.FixedPoint;
using UnityEditor;
using UnityEngine;

namespace Rogue.Editor.Tests
{
    public class BuffIntegrationTests
    {
        [Test]
        public void ExecuteBuffAction_WithCommonValueSelfTarget_ChangesOwnerProperty()
        {
            TestEntity owner = CreateStandaloneEntity(hp: 100, attack: 20);
            TestEntity other = CreateStandaloneEntity(hp: 80, attack: 10);
            Buff buff = Buff.CreateBuff(owner, null, CommonHpEffect(BuffExecuteTarget.Self, -15));
            BattleAttackEventParams attackParams = BattleAttackEventParams.Create(owner, other, 10);

            Assert.IsTrue(buff.CheckCondition(attackParams));
            Assert.IsTrue(buff.ExecuteBuffAction(attackParams));
            Assert.AreEqual((fp)85, owner.GetProperty(PropertyKey.Hp));
            Assert.AreEqual((fp)80, other.GetProperty(PropertyKey.Hp));

            Release(buff, attackParams);
        }

        [Test]
        public void ExecuteBuffAction_WithRateValueOtherTarget_UsesOwnerInfluenceProperty()
        {
            TestEntity owner = CreateStandaloneEntity(hp: 100, attack: 40);
            TestEntity other = CreateStandaloneEntity(hp: 100, attack: 10);
            Buff buff = Buff.CreateBuff(owner, null, new BuffEffectAssets
            {
                buffExecuteTarget = BuffExecuteTarget.Other,
                executeProperty = PropertyKey.Hp,
                isCommonValue = false,
                influenceProperty = PropertyKey.Attack,
                rate = -500
            });
            BattleAttackEventParams attackParams = BattleAttackEventParams.Create(owner, other, 10);

            Assert.IsTrue(buff.ExecuteBuffAction(attackParams));
            Assert.AreEqual((fp)100, owner.GetProperty(PropertyKey.Hp));
            Assert.AreEqual((fp)80, other.GetProperty(PropertyKey.Hp));

            Release(buff, attackParams);
        }

        [Test]
        public void CheckCondition_WhenConditionPassesAndFails_ControlsEffectExecution()
        {
            TestEntity owner = CreateStandaloneEntity(hp: 100, attack: 20);
            TestEntity other = CreateStandaloneEntity(hp: 80, attack: 10);
            BattleAttackEventParams attackParams = BattleAttackEventParams.Create(owner, other, 10);

            Buff passingBuff = Buff.CreateBuff(owner,
                HpCondition(BuffExecuteTarget.Self, CompareMethod.GreaterOrEqualTo, 100),
                CommonHpEffect(BuffExecuteTarget.Other, -10));
            Assert.IsTrue(passingBuff.CheckCondition(attackParams));
            Assert.IsTrue(passingBuff.ExecuteBuffAction(attackParams));
            Assert.AreEqual((fp)70, other.GetProperty(PropertyKey.Hp));

            Buff failingBuff = Buff.CreateBuff(owner,
                HpCondition(BuffExecuteTarget.Self, CompareMethod.LessThan, 50),
                CommonHpEffect(BuffExecuteTarget.Other, -10));
            Assert.IsFalse(failingBuff.CheckCondition(attackParams));
            Assert.AreEqual((fp)70, other.GetProperty(PropertyKey.Hp));

            FPoolHelper.Release<Buff>(passingBuff);
            FPoolHelper.Release<Buff>(failingBuff);
            FPoolHelper.Release<BattleAttackEventParams>(attackParams);
        }

        [Test]
        public void ExecuteBuffAction_WithOtherTarget_ResolvesOppositeAttackParticipant()
        {
            TestEntity ownerAsDefender = CreateStandaloneEntity(hp: 100, attack: 20);
            TestEntity attacker = CreateStandaloneEntity(hp: 100, attack: 10);
            Buff buff = Buff.CreateBuff(ownerAsDefender, null, CommonHpEffect(BuffExecuteTarget.Other, -25));
            BattleAttackEventParams attackParams = BattleAttackEventParams.Create(attacker, ownerAsDefender, 10);

            Assert.IsTrue(buff.ExecuteBuffAction(attackParams));
            Assert.AreEqual((fp)75, attacker.GetProperty(PropertyKey.Hp));
            Assert.AreEqual((fp)100, ownerAsDefender.GetProperty(PropertyKey.Hp));

            Release(buff, attackParams);
        }

        [Test]
        public void BuffEntity_WhenExecuteCountIsConsumed_DiesAndStopsReceivingNotifications()
        {
            using TestBuffWorldScope scope = TestBuffWorldScope.Create();
            TestEntity owner = scope.CreateOwner(hp: 100, attack: 20);
            TestEntity other = scope.CreateOwner(hp: 100, attack: 10);
            BuffEntity buffEntity = scope.CreateBuffEntity(owner, new BuffAssetsConfig
            {
                assetsId = 61001,
                lifeTime = -1,
                gameEventType = BattleExecuteTiming.BulletHit,
                executeValue = 1,
                buffEffectAssets = CommonHpEffect(BuffExecuteTarget.Other, -10)
            });
            BattleAttackEventParams attackParams = BattleAttackEventParams.Create(owner, other, 10);

            scope.Observer.Notify(BattleExecuteTiming.BulletHit, attackParams);

            Assert.AreEqual((fp)90, other.GetProperty(PropertyKey.Hp));
            Assert.AreEqual(0, buffEntity.RemainingExecuteCount);
            Assert.AreEqual(EntityState.Dead, buffEntity.EntityState);

            scope.Observer.Notify(BattleExecuteTiming.BulletHit, attackParams);
            Assert.AreEqual((fp)90, other.GetProperty(PropertyKey.Hp));

            FPoolHelper.Release<BattleAttackEventParams>(attackParams);
        }

        [Test]
        public void BuffEntity_IncreaseLayer_AddsRemainingExecuteCount()
        {
            using TestBuffWorldScope scope = TestBuffWorldScope.Create();
            TestEntity owner = scope.CreateOwner(hp: 100, attack: 20);
            BuffEntity buffEntity = scope.CreateBuffEntity(owner, new BuffAssetsConfig
            {
                assetsId = 61002,
                lifeTime = -1,
                gameEventType = BattleExecuteTiming.BulletHit,
                executeValue = 2,
                buffEffectAssets = CommonHpEffect(BuffExecuteTarget.Self, -1)
            });

            buffEntity.IncreaseLayer();

            Assert.AreEqual(2, buffEntity.Layer);
            Assert.IsTrue(buffEntity.IsExecuteCountLimited);
            Assert.AreEqual(2, buffEntity.ExecuteCountPerLayer);
            Assert.AreEqual(4, buffEntity.RemainingExecuteCount);
        }

        [Test]
        public void BuffEntity_LifeTimeComponent_WhenLifeTimeEnds_ReleasesObserver()
        {
            using TestBuffWorldScope scope = TestBuffWorldScope.Create();
            TestEntity owner = scope.CreateOwner(hp: 100, attack: 20);
            TestEntity other = scope.CreateOwner(hp: 100, attack: 10);
            BuffEntity buffEntity = scope.CreateBuffEntity(owner, new BuffAssetsConfig
            {
                assetsId = 61003,
                lifeTime = 1,
                gameEventType = BattleExecuteTiming.BulletHit,
                executeValue = 0,
                buffEffectAssets = CommonHpEffect(BuffExecuteTarget.Other, -10)
            });
            BattleAttackEventParams attackParams = BattleAttackEventParams.Create(owner, other, 10);

            buffEntity.OnFixedUpdate((fp)1, WorldUpdateType.Authority);

            Assert.AreEqual(EntityState.Dead, buffEntity.EntityState);

            scope.Observer.Notify(BattleExecuteTiming.BulletHit, attackParams);
            Assert.AreEqual((fp)100, other.GetProperty(PropertyKey.Hp));

            FPoolHelper.Release<BattleAttackEventParams>(attackParams);
        }

        [Test]
        public void BuffEntity_HardRollbackRestoresExecutionStateConsistently()
        {
            using TestBuffWorldScope scope = TestBuffWorldScope.Create();
            TestEntity owner = scope.CreateOwner(hp: 100, attack: 20);
            BuffEntity buffEntity = scope.CreateBuffEntity(owner, new BuffAssetsConfig
            {
                assetsId = 61004,
                lifeTime = -1,
                gameEventType = BattleExecuteTiming.BulletHit,
                executeValue = 2,
                buffEffectAssets = CommonHpEffect(BuffExecuteTarget.Self, -1)
            });
            BaseSnapShotData hardWriter = new BaseSnapShotData();
            BaseSnapShotData softWriter = new BaseSnapShotData();

            buffEntity.IncreaseLayer();
            buffEntity.TakeSnapShot(1, hardWriter, softWriter);
            buffEntity.RestoreExecutionState(1, true, 2, 1);

            buffEntity.HardRollBack(ReaderPool.GetReader(hardWriter.PooledWriter.GetArraySegment()));

            Assert.AreEqual(2, buffEntity.Layer);
            Assert.IsTrue(buffEntity.IsExecuteCountLimited);
            Assert.AreEqual(2, buffEntity.ExecuteCountPerLayer);
            Assert.AreEqual(4, buffEntity.RemainingExecuteCount);
            Assert.AreEqual(BattleExecuteTiming.BulletHit, buffEntity.MonitorGameEvent);
        }

        /// <summary>
        /// 验证 PerSecond Buff 不依赖 BulletHit，累计 30 次固定帧后只执行一次。
        /// </summary>
        [Test]
        public void BuffEntity_PerSecondExecutesOnceAfterThirtyFixedUpdatesWithoutBulletHit()
        {
            using TestBuffWorldScope scope = TestBuffWorldScope.Create();
            TestEntity owner = scope.CreateOwner(hp: 100, attack: 20, CampEnum.CharacterCamp);
            BuffEntity buffEntity = scope.CreateBuffEntity(owner, new BuffAssetsConfig
            {
                assetsId = 61005,
                lifeTime = -1,
                gameEventType = BattleExecuteTiming.PerSecond,
                executeValue = 0,
                buffEffectAssets = CommonHpEffect(BuffExecuteTarget.Self, -5)
            });

            for (int i = 0; i < 29; i++)
            {
                scope.SetAuthorityTick((uint)i);
                buffEntity.OnFixedUpdate((fp)1, WorldUpdateType.Authority);
            }

            Assert.AreEqual((fp)100, owner.GetProperty(PropertyKey.Hp));
            Assert.AreEqual(29, buffEntity.PerSecondElapsedFrameCount);

            scope.SetAuthorityTick(30);
            buffEntity.OnFixedUpdate((fp)1, WorldUpdateType.Authority);

            Assert.AreEqual((fp)95, owner.GetProperty(PropertyKey.Hp));
            Assert.AreEqual(0, buffEntity.PerSecondElapsedFrameCount);
            Assert.AreEqual(30, buffEntity.LastPerSecondExecuteTick);

            BattleAttackEventParams attackParams = BattleAttackEventParams.Create(owner, owner, 0);
            scope.Observer.Notify(BattleExecuteTiming.BulletHit, attackParams);

            Assert.AreEqual((fp)95, owner.GetProperty(PropertyKey.Hp));

            FPoolHelper.Release<BattleAttackEventParams>(attackParams);
        }

        /// <summary>
        /// 验证同一世界 Tick 内重复累计到 30 帧时，PerSecond Buff 不会重复执行。
        /// </summary>
        [Test]
        public void BuffEntity_PerSecondDoesNotExecuteTwiceInSameTick()
        {
            using TestBuffWorldScope scope = TestBuffWorldScope.Create();
            TestEntity owner = scope.CreateOwner(hp: 100, attack: 20, CampEnum.CharacterCamp);
            BuffEntity buffEntity = scope.CreateBuffEntity(owner, new BuffAssetsConfig
            {
                assetsId = 61006,
                lifeTime = -1,
                gameEventType = BattleExecuteTiming.PerSecond,
                executeValue = 0,
                buffEffectAssets = CommonHpEffect(BuffExecuteTarget.Self, -5)
            });

            scope.SetAuthorityTick(7);

            for (int i = 0; i < 60; i++)
            {
                buffEntity.OnFixedUpdate((fp)1, WorldUpdateType.Authority);
            }

            Assert.AreEqual((fp)95, owner.GetProperty(PropertyKey.Hp));
            Assert.AreEqual(7, buffEntity.LastPerSecondExecuteTick);
        }

        /// <summary>
        /// 验证 triggerRole 能区分 Buff 持有者在攻击事件中的攻击者和受击者身份。
        /// </summary>
        [Test]
        public void CheckCondition_WithTriggerRoleFiltersAttackerAndDefender()
        {
            TestEntity owner = CreateStandaloneEntity(hp: 100, attack: 20);
            TestEntity other = CreateStandaloneEntity(hp: 100, attack: 10);
            BattleAttackEventParams ownerAttackParams = BattleAttackEventParams.Create(owner, other, 10);
            BattleAttackEventParams ownerDefendParams = BattleAttackEventParams.Create(other, owner, 10);
            Buff attackerBuff = Buff.CreateBuff(owner,
                TriggerRoleCondition(BuffTriggerRole.OwnerAsAttacker),
                CommonHpEffect(BuffExecuteTarget.Self, -1));
            Buff defenderBuff = Buff.CreateBuff(owner,
                TriggerRoleCondition(BuffTriggerRole.OwnerAsDefender),
                CommonHpEffect(BuffExecuteTarget.Self, -1));

            Assert.IsTrue(attackerBuff.CheckCondition(ownerAttackParams));
            Assert.IsFalse(attackerBuff.CheckCondition(ownerDefendParams));
            Assert.IsFalse(defenderBuff.CheckCondition(ownerAttackParams));
            Assert.IsTrue(defenderBuff.CheckCondition(ownerDefendParams));

            FPoolHelper.Release<Buff>(attackerBuff);
            FPoolHelper.Release<Buff>(defenderBuff);
            FPoolHelper.Release<BattleAttackEventParams>(ownerAttackParams);
            FPoolHelper.Release<BattleAttackEventParams>(ownerDefendParams);
        }

        /// <summary>
        /// 验证无事件参数时 Self 目标能解析为 Buff 持有者。
        /// </summary>
        [Test]
        public void ExecuteBuffAction_WithoutEventParams_ResolvesSelfTarget()
        {
            TestEntity owner = CreateStandaloneEntity(hp: 100, attack: 20);
            Buff buff = Buff.CreateBuff(owner, null, CommonHpEffect(BuffExecuteTarget.Self, -12));

            Assert.IsTrue(buff.ExecuteBuffAction(null));
            Assert.AreEqual((fp)88, owner.GetProperty(PropertyKey.Hp));

            FPoolHelper.Release<Buff>(buff);
        }

        /// <summary>
        /// 验证无事件参数时 AllEnemy 只影响同一更新域内的敌对存活实体。
        /// </summary>
        [Test]
        public void ExecuteBuffAction_WithoutEventParams_AllEnemyAffectsSameUpdateDomainEnemiesOnly()
        {
            using TestBuffWorldScope scope = TestBuffWorldScope.Create();
            TestEntity owner = scope.CreateRegisteredEntity(hp: 100, attack: 20, CampEnum.CharacterCamp,
                EntityUpdateType.AuthorityEntity);
            TestEntity authorityEnemy = scope.CreateRegisteredEntity(hp: 100, attack: 10, CampEnum.MonsterCamp,
                EntityUpdateType.AuthorityEntity);
            TestEntity localEnemy = scope.CreateRegisteredEntity(hp: 100, attack: 10, CampEnum.MonsterCamp,
                EntityUpdateType.LocalEntity);
            TestEntity authorityAlly = scope.CreateRegisteredEntity(hp: 100, attack: 10, CampEnum.CharacterCamp,
                EntityUpdateType.AuthorityEntity);
            Buff buff = Buff.CreateBuff(owner, null, CommonHpEffect(BuffExecuteTarget.AllEnemy, -10));

            Assert.IsTrue(buff.ExecuteBuffAction(null));

            Assert.AreEqual((fp)100, owner.GetProperty(PropertyKey.Hp));
            Assert.AreEqual((fp)90, authorityEnemy.GetProperty(PropertyKey.Hp));
            Assert.AreEqual((fp)100, localEnemy.GetProperty(PropertyKey.Hp));
            Assert.AreEqual((fp)100, authorityAlly.GetProperty(PropertyKey.Hp));

            FPoolHelper.Release<Buff>(buff);
        }

        /// <summary>
        /// 验证临时属性加成会同时提升 Attack 的 Current 和 Max，重复执行不叠加，释放后回滚。
        /// </summary>
        [Test]
        public void TemporaryPropertyModifier_IncreasesCurrentAndMax_ThenRollsBackOnRelease()
        {
            TestEntity owner = CreateStandaloneEntity(hp: 100, attack: 50, speed: 10, defence: 30);
            Buff buff = Buff.CreateBuff(owner, null,
                TemporaryPropertyModifierEffect(BuffExecuteTarget.Self, PropertyKey.Attack, 20));

            Assert.IsTrue(buff.ExecuteBuffAction(null));
            Assert.AreEqual((fp)70, owner.GetProperty(PropertyKey.Attack));
            Assert.AreEqual((fp)70, GetPropertyValue(owner, PropertyKey.Attack, PropertyValueType.Max));

            Assert.IsTrue(buff.ExecuteBuffAction(null));
            Assert.AreEqual((fp)70, owner.GetProperty(PropertyKey.Attack));
            Assert.AreEqual((fp)70, GetPropertyValue(owner, PropertyKey.Attack, PropertyValueType.Max));

            FPoolHelper.Release<Buff>(buff);

            Assert.AreEqual((fp)50, owner.GetProperty(PropertyKey.Attack));
            Assert.AreEqual((fp)50, GetPropertyValue(owner, PropertyKey.Attack, PropertyValueType.Max));
        }

        /// <summary>
        /// 验证临时属性减益会同时降低 Speed 和 Defence 的 Current 与 Max，释放后按记录量回滚。
        /// </summary>
        [Test]
        public void TemporaryPropertyModifier_DecreasesSpeedAndDefence_ThenRollsBackOnRelease()
        {
            TestEntity owner = CreateStandaloneEntity(hp: 100, attack: 50, speed: 10, defence: 30);
            Buff speedBuff = Buff.CreateBuff(owner, null,
                TemporaryPropertyModifierEffect(BuffExecuteTarget.Self, PropertyKey.Speed, -3));
            Buff defenceBuff = Buff.CreateBuff(owner, null,
                TemporaryPropertyModifierEffect(BuffExecuteTarget.Self, PropertyKey.Defence, -5));

            Assert.IsTrue(speedBuff.ExecuteBuffAction(null));
            Assert.IsTrue(defenceBuff.ExecuteBuffAction(null));

            Assert.AreEqual((fp)7, owner.GetProperty(PropertyKey.Speed));
            Assert.AreEqual((fp)7, GetPropertyValue(owner, PropertyKey.Speed, PropertyValueType.Max));
            Assert.AreEqual((fp)25, owner.GetProperty(PropertyKey.Defence));
            Assert.AreEqual((fp)25, GetPropertyValue(owner, PropertyKey.Defence, PropertyValueType.Max));

            FPoolHelper.Release<Buff>(speedBuff);
            FPoolHelper.Release<Buff>(defenceBuff);

            Assert.AreEqual((fp)10, owner.GetProperty(PropertyKey.Speed));
            Assert.AreEqual((fp)10, GetPropertyValue(owner, PropertyKey.Speed, PropertyValueType.Max));
            Assert.AreEqual((fp)30, owner.GetProperty(PropertyKey.Defence));
            Assert.AreEqual((fp)30, GetPropertyValue(owner, PropertyKey.Defence, PropertyValueType.Max));
        }

        /// <summary>
        /// 验证生产 BuffAssets 资源可以查到 4001-4010 十个常用 Buff 配置，并且基础运行配置完整。
        /// </summary>
        [Test]
        public void ProductionBuffAssets_CanLookupCommonBuffConfigs4001To4010()
        {
            BuffAssets buffAssets = AssetDatabase.LoadAssetAtPath<BuffAssets>("Assets/GameAssetConfig/BuffAssets.asset");

            Assert.NotNull(buffAssets);

            for (int id = 4001; id <= 4010; id++)
            {
                BuffAssetsConfig config = buffAssets.GetDataTable(id) as BuffAssetsConfig;

                Assert.NotNull(config, $"BuffAssets.asset 缺少 Buff 配置 {id}");
                Assert.NotNull(config.buffEffectAssets, $"Buff 配置 {id} 缺少效果配置");
                Assert.NotNull(config.buffConditionAssets, $"Buff 配置 {id} 缺少条件配置");
                Assert.AreNotEqual(PropertyKey.Null, config.buffEffectAssets.executeProperty,
                    $"Buff 配置 {id} 未指定执行属性");
            }
        }

        private static BuffEffectAssets CommonHpEffect(BuffExecuteTarget target, int value)
        {
            return new BuffEffectAssets
            {
                buffExecuteTarget = target,
                executeProperty = PropertyKey.Hp,
                isCommonValue = true,
                normalValue = value,
                influenceProperty = PropertyKey.Null,
                rate = 0
            };
        }

        /// <summary>
        /// 创建临时属性修正效果配置。
        /// </summary>
        /// <param name="target">Buff 效果目标。</param>
        /// <param name="propertyKey">需要临时修正的属性键。</param>
        /// <param name="value">直接应用到 Current 和 Max 的固定修正量。</param>
        /// <returns>用于测试临时属性修正的 Buff 效果配置。</returns>
        private static BuffEffectAssets TemporaryPropertyModifierEffect(BuffExecuteTarget target,
            PropertyKey propertyKey, int value)
        {
            return new BuffEffectAssets
            {
                effectMode = BuffEffectMode.TemporaryPropertyModifier,
                buffExecuteTarget = target,
                executeProperty = propertyKey,
                isCommonValue = true,
                normalValue = value,
                influenceProperty = PropertyKey.Null,
                rate = 0
            };
        }

        /// <summary>
        /// 读取测试实体指定属性值类型的数值，并断言属性存在。
        /// </summary>
        /// <param name="entity">需要读取属性的测试实体。</param>
        /// <param name="propertyKey">需要读取的属性键。</param>
        /// <param name="valueType">需要读取的属性值类型。</param>
        /// <returns>指定属性值类型的当前数值。</returns>
        private static fp GetPropertyValue(BaseEntity entity, PropertyKey propertyKey, PropertyValueType valueType)
        {
            Assert.IsTrue(entity.TryGetPropertyValue(propertyKey, valueType, out fp value));

            return value;
        }

        private static BuffConditionAssets HpCondition(BuffExecuteTarget target, CompareMethod compareMethod,
            int value)
        {
            return new BuffConditionAssets
            {
                buffConditionTarget = target,
                propertyKey = PropertyKey.Hp,
                propertyValueType = PropertyValueType.Current,
                compareMethod = compareMethod,
                value = value
            };
        }

        /// <summary>
        /// 创建只检查触发角色、不检查属性数值的 Buff 条件配置。
        /// </summary>
        /// <param name="triggerRole">Buff 持有者在攻击事件中必须满足的角色。</param>
        /// <returns>用于测试 triggerRole 的 Buff 条件配置。</returns>
        private static BuffConditionAssets TriggerRoleCondition(BuffTriggerRole triggerRole)
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

        private static void Release(Buff buff, BattleAttackEventParams attackParams)
        {
            FPoolHelper.Release<Buff>(buff);
            FPoolHelper.Release<BattleAttackEventParams>(attackParams);
        }

        /// <summary>
        /// 创建默认角色阵营的脱离世界测试实体。
        /// </summary>
        /// <param name="hp">测试实体初始生命值。</param>
        /// <param name="attack">测试实体初始攻击力。</param>
        /// <returns>已完成 OnInit 的测试实体。</returns>
        private static TestEntity CreateStandaloneEntity(int hp, int attack)
        {
            return CreateStandaloneEntity(hp, attack, CampEnum.CharacterCamp);
        }

        /// <summary>
        /// 创建脱离测试世界的测试实体，并指定属性与阵营。
        /// </summary>
        /// <param name="hp">测试实体初始生命值。</param>
        /// <param name="attack">测试实体初始攻击力。</param>
        /// <param name="campEnum">测试实体所属阵营。</param>
        /// <returns>已完成 OnInit 的测试实体。</returns>
        private static TestEntity CreateStandaloneEntity(int hp, int attack, CampEnum campEnum)
        {
            TestEntity entity = new TestEntity(new TestBattleData(hp, attack));
            EntityCreateData createData = EntityCreateData.Create(new EntityAssetsConfig { assetsId = hp + attack },
                fp3.zero, fp3.zero, new fp3(1, 1, 1), false);
            entity.OnInit(createData);
            entity.SetCamp(campEnum);
            return entity;
        }

        /// <summary>
        /// 创建脱离测试世界的测试实体，并指定临时属性修正测试所需的属性值。
        /// </summary>
        /// <param name="hp">测试实体初始生命值。</param>
        /// <param name="attack">测试实体初始攻击力。</param>
        /// <param name="speed">测试实体初始速度。</param>
        /// <param name="defence">测试实体初始防御力。</param>
        /// <returns>已完成 OnInit 的测试实体。</returns>
        private static TestEntity CreateStandaloneEntity(int hp, int attack, int speed, int defence)
        {
            TestEntity entity = new TestEntity(new TestBattleData(hp, attack, speed, defence));
            EntityCreateData createData = EntityCreateData.Create(new EntityAssetsConfig { assetsId = hp + attack },
                fp3.zero, fp3.zero, new fp3(1, 1, 1), false);
            entity.OnInit(createData);
            entity.SetCamp(CampEnum.CharacterCamp);
            return entity;
        }

        private sealed class TestBuffWorldScope : IDisposable
        {
            private readonly TestWorld _world;
            private readonly GameObject _worldRoot;

            private TestBuffWorldScope(TestWorld world, GameObject worldRoot)
            {
                _world = world;
                _worldRoot = worldRoot;
            }

            public BattleObserverSystem Observer => _world.GetSystem<BattleObserverSystem>();

            public static TestBuffWorldScope Create()
            {
                GameObject worldRoot = new GameObject("BuffIntegrationTestWorld");
                CreateWorldData worldData = FPoolHelper.Get<CreateWorldData>();
                worldData.WorldId = 1;
                worldData.SceneName = "BuffIntegrationTests";
                worldData.WorldRoot = worldRoot.transform;

                TestWorld world = new TestWorld(worldData);
                world.InitializeForTests();

                return new TestBuffWorldScope(world, worldRoot);
            }

            /// <summary>
            /// 创建默认角色阵营的 Buff 持有者测试实体，但不注册到实体系统候选列表。
            /// </summary>
            /// <param name="hp">测试实体初始生命值。</param>
            /// <param name="attack">测试实体初始攻击力。</param>
            /// <returns>已绑定测试世界的 Buff 持有者实体。</returns>
            public TestEntity CreateOwner(int hp, int attack)
            {
                return CreateOwner(hp, attack, CampEnum.CharacterCamp);
            }

            /// <summary>
            /// 创建指定阵营的 Buff 持有者测试实体，但不注册到实体系统候选列表。
            /// </summary>
            /// <param name="hp">测试实体初始生命值。</param>
            /// <param name="attack">测试实体初始攻击力。</param>
            /// <param name="campEnum">测试实体所属阵营。</param>
            /// <returns>已绑定测试世界的 Buff 持有者实体。</returns>
            public TestEntity CreateOwner(int hp, int attack, CampEnum campEnum)
            {
                TestEntity entity = CreateStandaloneEntity(hp, attack, campEnum);
                entity.SetWorld(_world);
                entity.SetEntityId(100 + hp + attack);
                entity.SetEntityFingerprints(200 + hp + attack);
                entity.SetUpdateType(EntityUpdateType.AuthorityEntity);
                return entity;
            }

            /// <summary>
            /// 创建并注册到 EntitySystem 的测试实体，用于验证 AllEnemy 真实候选目标收集。
            /// </summary>
            /// <param name="hp">测试实体初始生命值。</param>
            /// <param name="attack">测试实体初始攻击力。</param>
            /// <param name="campEnum">测试实体所属阵营。</param>
            /// <param name="updateType">测试实体所属更新域。</param>
            /// <returns>已注册到测试世界的测试实体。</returns>
            public TestEntity CreateRegisteredEntity(int hp, int attack, CampEnum campEnum,
                EntityUpdateType updateType)
            {
                EntityCreateData createData = EntityCreateData.Create(new EntityAssetsConfig { assetsId = hp + attack },
                    fp3.zero, fp3.zero, new fp3(1, 1, 1), false);
                createData.EntityData = new TestBattleData(hp, attack);
                TestEntity entity = _world.GetSystem<EntitySystem>().CreateStaticEntity<TestEntity>(createData,
                    updateType);
                entity.SetCamp(campEnum);

                return entity;
            }

            public BuffEntity CreateBuffEntity(TestEntity owner, BuffAssetsConfig config)
            {
                EntityCreateData createData = EntityCreateData.Create(config, fp3.zero, fp3.zero,
                    new fp3(1, 1, 1), false, null, owner, owner);

                return _world.GetSystem<EntitySystem>().CreateDynamicEntity<BuffEntity>(createData,
                    EntityUpdateType.AuthorityEntity, config.assetsId + 100000);
            }

            public void Dispose()
            {
                _world.Shutdown();
                UnityEngine.Object.DestroyImmediate(_worldRoot);
            }

            /// <summary>
            /// 设置测试世界权威 Tick，用于验证 PerSecond 同帧去重。
            /// </summary>
            /// <param name="tick">要写入测试世界的权威 Tick。</param>
            public void SetAuthorityTick(uint tick)
            {
                _world.SetAuthorityTickForTests(tick);
            }
        }

        private sealed class TestWorld : BaseWorld
        {
            public TestWorld(CreateWorldData createWorldData)
                : base(createWorldData)
            {
            }

            protected override Type[] GetSystemTypes()
            {
                return new[]
                {
                    typeof(EntitySystem),
                    typeof(BattleObserverSystem)
                };
            }

            protected override Task<bool> GamePreparation(CreateWorldData createWorldData)
            {
                return Task.FromResult(true);
            }

            /// <summary>
            /// 初始化测试世界所需系统、回滚参数和阵营配置。
            /// </summary>
            public void InitializeForTests()
            {
                InitRollBackData(2, 0);
                SetCampConfigForTests();
                GetSystem<EntitySystem>().OnInit(this);
                GetSystem<BattleObserverSystem>().OnInit(this);
            }

            /// <summary>
            /// 通过反射设置 BaseWorld 私有权威 Tick，仅用于测试 PerSecond 去重。
            /// </summary>
            /// <param name="tick">要写入测试世界的权威 Tick。</param>
            public void SetAuthorityTickForTests(uint tick)
            {
                FieldInfo field = typeof(BaseWorld).GetField("_authorityTick",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                field?.SetValue(this, tick);
            }

            /// <summary>
            /// 为测试世界注入临时阵营矩阵，使 Buff AllEnemy 走真实敌对关系判断。
            /// </summary>
            private void SetCampConfigForTests()
            {
                EntityCampConfig campConfig = ScriptableObject.CreateInstance<EntityCampConfig>();
                FieldInfo field = typeof(BaseWorld).GetField("_entityCampConfig",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                field?.SetValue(this, campConfig);
            }
        }

        private sealed class TestEntity : BaseEntity
        {
            private readonly BattleEntityData _entityPropertyData;
            /// <summary>
            /// 测试实体阵营，默认使用角色阵营。
            /// </summary>
            private CampEnum _campEnum = CampEnum.CharacterCamp;

            /// <summary>
            /// 创建默认属性的测试实体，供 EntitySystem 反射构造使用。
            /// </summary>
            public TestEntity()
                : this(new TestBattleData(100, 10))
            {
            }

            public TestEntity(BattleEntityData entityPropertyData)
            {
                _entityPropertyData = entityPropertyData;
                SetUpdateType(EntityUpdateType.AuthorityEntity);
            }

            protected override BattleEntityData EntityPropertyData => _entityPropertyData;

            protected override Type[] GetComponentTypes()
            {
                return Array.Empty<Type>();
            }

            protected override Type EntityView()
            {
                return null;
            }

            public override EntityType EntityType => EntityType.HeroEntity;

            public override ForecastEntityType ForecastEntityType => ForecastEntityType.StaticEntity;

            public override CampEnum CampEnum => _campEnum;

            /// <summary>
            /// 设置测试实体阵营，用于 AllEnemy 敌对关系验证。
            /// </summary>
            /// <param name="campEnum">测试实体所属阵营。</param>
            public void SetCamp(CampEnum campEnum)
            {
                _campEnum = campEnum;
            }
        }

        private sealed class TestBattleData : BattleEntityData
        {
            private readonly int _hp;
            private readonly int _attack;
            private readonly int _speed;
            private readonly int _defence;

            public TestBattleData(int hp, int attack)
                : this(hp, attack, 0, 0)
            {
            }

            /// <summary>
            /// 创建测试用战斗属性数据。
            /// </summary>
            /// <param name="hp">测试实体初始生命值。</param>
            /// <param name="attack">测试实体初始攻击力。</param>
            /// <param name="speed">测试实体初始速度。</param>
            /// <param name="defence">测试实体初始防御力。</param>
            public TestBattleData(int hp, int attack, int speed, int defence)
            {
                _hp = hp;
                _attack = attack;
                _speed = speed;
                _defence = defence;
                InitProperty();
            }

            public override void InitProperty()
            {
                EntityPropertyDataDic.Clear();
                EntityPropertyDataDic.Add(PropertyKey.Hp.ToString(), PropertyData.Create(_hp, 0, 1000));
                EntityPropertyDataDic.Add(PropertyKey.Attack.ToString(), PropertyData.Create(_attack, 0, _attack));
                EntityPropertyDataDic.Add(PropertyKey.Speed.ToString(), PropertyData.Create(_speed, 0, _speed));
                EntityPropertyDataDic.Add(PropertyKey.Defence.ToString(), PropertyData.Create(_defence, 0, _defence));
            }
        }
    }
}
