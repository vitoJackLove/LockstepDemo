[System.Serializable]
public class GameStartMessage : IObserverParams
{
    public BattleObserverEventEnum ObserverEventType => BattleObserverEventEnum.GameStart;

    public bool isStart;

    public uint StartTick;
}
