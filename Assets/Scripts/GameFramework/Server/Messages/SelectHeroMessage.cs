public class SelectHeroMessage : IObserverParams
{
    public BattleObserverEventEnum ObserverEventType => BattleObserverEventEnum.SelectHero;

    /// <summary>
    /// 选择的角色ID
    /// </summary>
    public int SelectHeroId;

    /// <summary>
    /// 角色Index
    /// </summary>
    public int PlayerIndex;
}
