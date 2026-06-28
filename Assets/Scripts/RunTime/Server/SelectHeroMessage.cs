public class SelectHeroMessage : IObserverParams
{
    public BattleObserverEventEnum ObserverEventType => BattleObserverEventEnum.SelectHero;

    /// <summary>
    /// 閫夋嫨鐨勮鑹睮D
    /// </summary>
    public int SelectHeroId;

    /// <summary>
    /// 瑙掕壊Index
    /// </summary>
    public int PlayerIndex;
}

