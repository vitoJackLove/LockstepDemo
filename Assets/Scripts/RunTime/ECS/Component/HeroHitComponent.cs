using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// 英雄攻击组件
/// </summary>
public class HeroHitComponent : HitComponent
{
    protected override fp DamageProgress()
    {
        uint tick = DefenseEntity.BaseWorld.LocalTick;

        fp attack = Entity.GetProperty(PropertyKey.Attack);

        DefenseEntity.ChangeProperty(PropertyKey.Hp, -attack);

        fp hp = DefenseEntity.GetProperty(PropertyKey.Hp);

        if (hp <= 0)
        {
            DefenseEntity.GetTypeOfComponent<StateComponent>().ChangeState(2, true,false);
        }
        
        Debug.Log($"实体 ID : {DefenseEntity.EntityId} 受击... 世界时间 ：{tick} 本地伤害 ： {attack}");

        return attack;
    }
}
