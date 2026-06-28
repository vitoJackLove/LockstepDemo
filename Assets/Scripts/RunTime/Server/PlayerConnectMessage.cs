public class PlayerConnectMessage : IObserverParams
{
    public BattleObserverEventEnum ObserverEventType => BattleObserverEventEnum.PlayerConnect;

    public int PlayerIndex;
}

