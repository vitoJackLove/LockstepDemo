/// <summary>
/// 战斗事件参数
/// </summary>
public interface IBattleObserverParams : IPool 
{
    BattleExecuteTiming BattleExecuteTiming { get; }
}
