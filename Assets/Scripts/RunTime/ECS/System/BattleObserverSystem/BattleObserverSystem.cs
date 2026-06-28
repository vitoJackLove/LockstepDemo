using System.Collections.Generic;
using UnityEngine.Pool;

/// <summary>
/// 战斗观察者系统
/// </summary>
public class BattleObserverSystem : BaseSystem
{
    private Dictionary<BattleExecuteTiming, List<IBattleObserverHandle>> _observers = new ();
    
    /// <summary>
    /// 附加观察者
    /// </summary>
    public void Attach(BattleExecuteTiming eventType, IBattleObserverHandle observer)
    {
        if (eventType == BattleExecuteTiming.Null || observer == null)
        {
            return;
        }

        if (!_observers.TryGetValue(eventType, out List<IBattleObserverHandle> observerHandlers))
        {
            observerHandlers = new List<IBattleObserverHandle>();
            
            _observers.Add(eventType, observerHandlers);
        }

        if (observerHandlers.Contains(observer))
        {
            return;
        }

        observerHandlers.Add(observer);
    }

    /// <summary>
    /// 移除观察者
    /// </summary>
    public void Detach(BattleExecuteTiming eventType, IBattleObserverHandle observer)
    {
        if (eventType == BattleExecuteTiming.Null || observer == null)
        {
            return;
        }

        if (!_observers.TryGetValue(eventType, out List<IBattleObserverHandle> observerHandlers))
        {
            return;
        }

        observerHandlers.Remove(observer);

        if (observerHandlers.Count == 0)
        {
            _observers.Remove(eventType);
        }
    }
    
    /// <summary>
    /// 通知观察系统
    /// </summary>
    public void Notify(BattleExecuteTiming eventType, IBattleObserverParams param)
    {
        if (_observers.TryGetValue(eventType, out List<IBattleObserverHandle> observerHandlers))
        {
            List<IBattleObserverHandle> notifyHandlers = ListPool<IBattleObserverHandle>.Get();
            notifyHandlers.AddRange(observerHandlers);

            try
            {
                for (int i = notifyHandlers.Count - 1; i >= 0; i--)
                {
                    IBattleObserverHandle observer = notifyHandlers[i];

                    if (!_observers.TryGetValue(eventType, out observerHandlers) ||
                        !observerHandlers.Contains(observer))
                    {
                        continue;
                    }

                    observer.OnNotify(param);
                }
            }
            finally
            {
                ListPool<IBattleObserverHandle>.Release(notifyHandlers);
            }
        }
    }
}
