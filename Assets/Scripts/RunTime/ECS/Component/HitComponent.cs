using Unity.Mathematics.FixedPoint;

/// <summary>
/// 受击组件
/// </summary>
public abstract class HitComponent : BaseComponent
{
    /// <summary>
    /// 防御的实体
    /// </summary>
    protected BaseEntity DefenseEntity;

    /// <summary>
    /// 开始攻击
    /// </summary>
    /// <param name="fromId"></param>
    public void AttackEntity(int fromId)
    {
        DefenseEntity = Entity.GetSystem<EntitySystem>().GetEntity(fromId);

        fp damage = DamageProgress();

        ExecuteBattleEvent(damage);
    }

    /// <summary>
    /// 伤害流程
    /// </summary>
    /// <returns></returns>
    protected abstract fp DamageProgress();

    /// <summary>
    /// 发送战斗事件
    /// </summary>
    /// <param name="damage"></param>
    private void ExecuteBattleEvent(fp damage)
    {
        BattleAttackEventParams attackEventParams =
            BattleAttackEventParams.Create(Entity, DefenseEntity, damage);

        Entity.GetSystem<BattleObserverSystem>().Notify(BattleExecuteTiming.BulletHit, attackEventParams);
    }
}
