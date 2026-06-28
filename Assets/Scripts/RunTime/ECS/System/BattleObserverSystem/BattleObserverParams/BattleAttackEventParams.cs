using Unity.Mathematics.FixedPoint;

/// <summary>
/// 攻击事件参数
/// </summary>
public class BattleAttackEventParams : IBattleObserverParams
{
    public BattleExecuteTiming BattleExecuteTiming => BattleExecuteTiming.BulletHit;

    private BaseEntity _attacker;
    private BaseEntity _defender;
    private fp _damage;

    /// <summary>
    /// 攻击者
    /// </summary>
    public BaseEntity Attacker => _attacker;

    /// <summary>
    /// 防御者
    /// </summary>
    public BaseEntity Defender => _defender;

    /// <summary>
    /// 本次事件伤害
    /// </summary>
    public fp Damage => _damage;
    
    /// <summary>
    /// 战斗攻击事件
    /// </summary>
    /// <param name="attacker">攻击者</param>
    /// <param name="defender">防御者</param>
    /// <param name="damage"></param>
    /// <returns></returns>
    public static BattleAttackEventParams Create(BaseEntity attacker, BaseEntity defender, fp damage)
    {
        BattleAttackEventParams attackEventParams = FPoolHelper.Get<BattleAttackEventParams>();

        attackEventParams._attacker = attacker;
        attackEventParams._defender = defender;
        attackEventParams._damage = damage;
        
        return attackEventParams;
    }

    public void Clear()
    {
        _attacker = null;
        _defender = null;
        _damage = 0;
    }
}
