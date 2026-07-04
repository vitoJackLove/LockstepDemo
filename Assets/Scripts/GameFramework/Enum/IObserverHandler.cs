/// <summary>
/// 战斗观察者操作接口
/// </summary>
public interface IObserverHandler
{
    /// <summary>
    /// 通知
    /// </summary>
    void OnNotify(IObserverParams param);
}
