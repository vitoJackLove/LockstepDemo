using Ase.Serializing;

/// <summary>
/// 子弹触发组件
/// </summary>
public class BulletTriggerComponent : BaseComponent
{
    /// <summary>
    /// 子弹的攻击次数
    /// </summary>
    private int _attackNumber;
    
    public override void OnStart(object data = null)
    {
        base.OnStart(data);
        
        _attackNumber = Entity.GetData<int>(ComponentDataKey.AttackNumber);
    }

    public void OnBulletTrigger(int entityId)
    {
        if (Entity.ParentEntity == null)
        {
            return;
        } 
        
        BaseEntity triggerEntity = Entity.GetSystem<EntitySystem>().GetEntity(entityId);

        if (Entity.CheckIsAdversarial(triggerEntity))
        {
            Entity.ParentEntity.GetTypeOfComponent<HitComponent>().AttackEntity(entityId);

            _attackNumber--;
        }

        if (_attackNumber <= 0)
        {
            Entity.DoEntityDead();
        }
    }

    public override void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
    {
        base.TakeSnapShot(hardWriter, softWriter);

        hardWriter.WriteInt32Data($"子弹的攻击次数", _attackNumber);
    }

    public override void HardRollBackTo(PooledReader authoritySnapShot)
    {
        base.HardRollBackTo(authoritySnapShot);
        
        _attackNumber = authoritySnapShot.ReadInt32();
    }
}
