using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Mathematics.FixedPoint;

public class BaseSystem : ISystem
{
    /// <summary>
    /// 当前世界
    /// </summary>
    protected BaseWorld CurrentWorld;

    public virtual void OnInit(object data = null)
    {
        CurrentWorld = (BaseWorld)data;
    }

    /// <summary>
    /// 进入主要处理异步
    /// </summary>
    public virtual Task<bool> OnEnter()
    {
        return Task.FromResult(true);
    }

    public virtual void OnStart(object data = null) { }

    /// <summary>
    /// 执行服务器指令
    /// </summary>
    /// <param name="data"></param>
    public virtual void OnExecuteServerCommand(List<CommandData> data) { }
    
    /// <summary>
    /// 执行客户端指令
    /// </summary>
    /// <param name="data"></param>
    public virtual void OnExecuteLocalCommand(CommandData data){}
    
    public virtual void OnUpdate(fp deltaTime) { }

    /// <summary>
    /// 更新
    /// </summary>
    /// <param name="deltaTime"></param>
    /// <param name="worldUpdateType">更新的类型</param>
    public virtual void OnFixedUpdate(fp deltaTime,WorldUpdateType worldUpdateType) { }

    /// <summary>
    /// 回滚
    /// </summary>
    /// <param name="tick"></param>
    /// <param name="rollBackType">回滚类型</param>
    public virtual void RollBack(uint tick,RollBackType rollBackType) { }
    
    public virtual void TakeLocalSnapShot(uint localTick) { }
    
    public virtual void TakeAuthoritySnapShot(uint authorityTick){}

    /// <summary>
    /// 移除本地错误的快照
    /// </summary>
    /// <param name="startErrorTick"></param>
    /// <param name="rollbackType"></param>
    public virtual void RemoveLocalSnapShot(uint startErrorTick,RollBackType rollbackType){}

    /// <summary>
    /// 校验预测
    /// </summary>
    public virtual RollBackType VerifyForecast(uint tick)
    {
        return RollBackType.NoRollBack;
    }

    public virtual void OnPause(bool isPause) { }

    public virtual void OnDispose() { }

    public T GetSystem<T>() where T : BaseSystem
    {
        return CurrentWorld.GetSystem<T>();
    }
}