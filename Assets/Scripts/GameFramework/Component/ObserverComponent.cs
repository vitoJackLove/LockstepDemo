using System.Collections.Generic;

public enum BattleObserverEventEnum : int
{
    /// <summary>
    /// 无
    /// </summary>
    None = -1,

    /// <summary>
    /// 玩家连接
    /// </summary>
    PlayerConnect = 0,
    
    /// <summary>
    /// 帧数据
    /// </summary>
    FrameCommand = 1,
    
    /// <summary>
    /// 选择角色
    /// </summary>
    SelectHero = 2,
    
    /// <summary>
    /// 开始游戏
    /// </summary>
    GameStart = 3,
}

public class ObserverComponent : RunTimeComponent
{
    private Dictionary<BattleObserverEventEnum, List<IObserverHandler>> _observers = new ();

    /// <summary>
    /// 附加观察者
    /// </summary>
    public void Attach(BattleObserverEventEnum eventType, IObserverHandler observer)
    {
        if (!_observers.TryGetValue(eventType, out List<IObserverHandler> observerHandlers))
        {
            observerHandlers = new List<IObserverHandler>();
            _observers.Add(eventType, observerHandlers);
        }

        observerHandlers.Add(observer);
    }
    
    /// <summary>
    /// 通知观察系统
    /// </summary>
    public void Notify(BattleObserverEventEnum eventType, IObserverParams param)
    {
        if (_observers.TryGetValue(eventType, out List<IObserverHandler> observerHandlers))
        {
            foreach (var observer in observerHandlers)
            {
                observer.OnNotify(param);
            }
        }
    }
}
