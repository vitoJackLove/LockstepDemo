namespace Rogue.GameServer.Network;

public enum BattleObserverEventMessage
{
    None = -1,
    PlayerConnect = 0,
    FrameCommand = 1,
    SelectHero = 2,
    GameStart = 3,
}
