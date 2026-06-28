/// <summary>
/// 观察者接口
/// </summary>
public interface IBattleObserverHandle 
{
    /// <summary>
    /// 通知
    /// </summary>
    void OnNotify(IBattleObserverParams param);
}
