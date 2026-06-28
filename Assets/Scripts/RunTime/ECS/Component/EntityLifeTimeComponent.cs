using Ase.Serializing;
using Unity.Mathematics.FixedPoint;

/// <summary>
/// 实体生命周期组件
/// </summary>
public class EntityLifeTimeComponent : BaseComponent
{
    /// <summary>
    /// 存活时间 (帧)
    /// </summary>
    private int _lifeTime;

    public override void OnStart(object data = null)
    {
        base.OnStart(data);
        
        _lifeTime = Entity.GetData<int>(ComponentDataKey.LifeTime);
    }

    public override void OnFixedUpdate(fp deltaTime, WorldUpdateType worldUpdateType)
    {
        base.OnFixedUpdate(deltaTime, worldUpdateType);
        
        if (Entity.EntityState != EntityState.Survival)
        {
            return;
        }

        if (_lifeTime == -1)
        {
            return;
        }

        _lifeTime--;

        if (_lifeTime == 0)
        {
            DoEntityDestroy();
        }
    }

    public override void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
    {
        base.TakeSnapShot(hardWriter, softWriter);

        hardWriter.WriteInt32Data($"实体生命周期", _lifeTime);
    }

    public override void HardRollBackTo(PooledReader authoritySnapShot)
    {
        base.HardRollBackTo(authoritySnapShot);
        
        _lifeTime = authoritySnapShot.ReadInt32();
    }

    private void DoEntityDestroy()
    {
        Entity.DoEntityDead();
    }
}
