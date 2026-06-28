using Ase.Serializing;
using Rogue.ECS;
using Unity.Mathematics.FixedPoint;

public abstract class BaseComponent : ILifeCycle
{
    /// <summary>
    /// 当前实体
    /// </summary>
    protected BaseEntity Entity;

    public virtual void OnInit(object data = null)
    {
        if (data is BaseEntity baseEntity)
        {
            Entity = baseEntity;
        }
    }

    public virtual void OnStart(object data = null) {}
    
    /// <summary>
    /// 执行服务器指令
    /// </summary>
    /// <param name="commandData"></param>
    public virtual void OnExecuteServerCommand(CommandData commandData) {}
    
    /// <summary>
    /// 执行客户端指令
    /// </summary>
    /// <param name="commandData"></param>
    public virtual void OnExecuteLocalCommand(CommandData commandData){}

    public virtual void OnUpdate(fp deltaTime) {}

    public virtual void OnFixedUpdate(fp deltaTime, WorldUpdateType worldUpdateType) {}

    public virtual void OnPause(bool isPause) {}

    public virtual void OnDispose() {}

    public virtual void OnEntityDead(object data) { }

    /// <summary>
    /// 截取快照
    /// </summary>
    /// <param name="hardWriter"></param>
    /// <param name="softWriter"></param>
    public virtual void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter) { }

    /// <summary>
    /// 硬回滚
    /// </summary>
    /// <param name="authoritySnapShot"></param>
    public virtual void HardRollBackTo(PooledReader authoritySnapShot) { }
    
    /// <summary>
    /// 软回滚
    /// </summary>
    /// <param name="authoritySnapShot"></param>
    public virtual void SoftRollBackTo(PooledReader authoritySnapShot) { }
}
