using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// 怪物攻击组件
/// </summary>
public class MonsterHitComponent : HitComponent
{
    protected override fp DamageProgress()
    {
        uint tick = DefenseEntity.EntityUpdateType == EntityUpdateType.AuthorityEntity
            ? DefenseEntity.BaseWorld.AuthorityTick
            : DefenseEntity.BaseWorld.LocalTick;

        Debug.Log($"实体 ID : {DefenseEntity.EntityId} 受击... 世界时间 ：{tick}");
        
        fp attack = Entity.GetProperty(PropertyKey.Attack);
        
        DefenseEntity.ChangeProperty(PropertyKey.Hp, -attack);
        
        DefenseEntity.GetTypeOfComponent<StateComponent>().ChangeState(2, true, false);

        return attack;
    }
}
