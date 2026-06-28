using Unity.Mathematics.FixedPoint;

namespace Rogue.ECS
{
    /// <summary>
    /// 初始化接口
    /// </summary>
    public interface IInit
    {
        void OnInit(object data = null);
    }

    /// <summary>
    /// Awake接口
    /// </summary>
    public interface IAwake
    {
        void OnAwake(object data = null);
    }

    /// <summary>
    /// 开始接口
    /// </summary>
    public interface IStart
    {
        void OnStart(object data = null);
    }

    /// <summary>
    /// 更新接口
    /// </summary>
    public interface IUpdate
    {
        void OnUpdate(fp deltaTime);
    }

    /// <summary>
    /// 固定更新接口
    /// </summary>
    public interface IFixedUpdate
    {
        void OnFixedUpdate(fp deltaTime, WorldUpdateType worldUpdateType);
    }

    /// <summary>
    /// 重置接口
    /// </summary>
    public interface IReset
    {
        void OnReset();
    }

    /// <summary>
    /// 销毁接口
    /// </summary>
    public interface IDispose
    {
        void OnDispose();
    }

    /// <summary>
    /// 暂停接口
    /// </summary>
    public interface IPause
    {
        void OnPause(bool isPause);
    }
    /// <summary>
    /// 一般生命周期接口
    /// </summary>
    public interface ILifeCycle : IInit, IStart, IUpdate, IFixedUpdate, IPause, IDispose { }

    /// <summary>
    /// 生命周期基类
    /// </summary>
    public class BaseLifeCycle : ILifeCycle
    {
        public virtual void OnInit(object data = null) { }

        public virtual void OnStart(object data = null) { }

        public virtual void OnUpdate(fp deltaTime) { }

        public virtual void OnFixedUpdate(fp deltaTime,WorldUpdateType worldUpdateType) { }

        public virtual void OnPause(bool isPause) { }
        public virtual void OnDispose() { }

    }
}