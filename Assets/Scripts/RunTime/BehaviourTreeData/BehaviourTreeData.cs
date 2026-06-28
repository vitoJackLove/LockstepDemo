using Ase.Serializing;
using TheKiwiCoder;
using Unity.Mathematics.FixedPoint;

public class BehaviourTreeData : IPool
{
    /// <summary>
    /// 行为树执行器
    /// </summary>
    private BehaviourTreeInstance _runner;

    /// <summary>
    /// 
    /// </summary>
    private BaseEntity _entity;

    public static BehaviourTreeData Create(BehaviourTreeInstance runner,BaseEntity entity)
    {
        BehaviourTreeData data = FPoolHelper.Get<BehaviourTreeData>();

        data._runner = runner;

        data._runner.StartBehaviour(entity, entity.GameObject);

        return data;
    }

    public void FixUpdate(fp deltaTime, uint worldTick, WorldUpdateType worldUpdateType)
    {
        _runner.ManualTick(worldTick,worldUpdateType);
    }

    public void Pause()
    {
        _runner.DoPause();
    }
    
    public void Recover()
    {
        _runner.DoRecover();
    }
    
    public void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
    {
        _runner.TakeSnapShot(hardWriter, softWriter);
    }

    public void RollBackTo(PooledReader authoritySnapShot)
    {
        _runner.RollBackTo(authoritySnapShot);
    }
    
    public void Clear()
    {
        _runner = null;
    }
}
