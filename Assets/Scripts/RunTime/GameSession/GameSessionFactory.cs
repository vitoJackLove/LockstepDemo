/// <summary>
/// 游戏会话工厂：根据模式创建 Profile、Transport 及完整会话上下文。
/// </summary>
public static class GameSessionFactory
{
    /// <summary>
    /// 创建联机会话上下文，使用 TCP 传输。
    /// </summary>
    public static GameSessionContext CreateOnline(FrameSyncClientComponent tcpClient)
    {
        return new GameSessionContext(
            new OnlineGameSessionProfile(),
            new TcpFrameSyncTransport(tcpClient));
    }

    /// <summary>
    /// 创建单机会话上下文，使用本地回环传输。
    /// </summary>
    public static GameSessionContext CreateSinglePlayer()
    {
        return new GameSessionContext(
            new SinglePlayerGameSessionProfile(),
            new LocalLoopbackFrameSyncTransport());
    }

    /// <summary>
    /// 仅创建会话 Profile，供世界实例持有。
    /// </summary>
    public static IGameSessionProfile CreateProfile(GameSessionModeType mode)
    {
        return mode switch
        {
            GameSessionModeType.SinglePlayer => new SinglePlayerGameSessionProfile(),
            GameSessionModeType.Online => new OnlineGameSessionProfile(),
            _ => new OnlineGameSessionProfile(),
        };
    }
}
