/// <summary>
/// 游戏会话工厂：创建联机会话上下文。
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
    /// 创建联机会话 Profile。
    /// </summary>
    public static IGameSessionProfile CreateProfile(GameSessionModeType mode)
    {
        return new OnlineGameSessionProfile();
    }
}
