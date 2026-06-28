/// <summary>
/// 游戏会话上下文：聚合 Profile 与 Transport，供 GameEntry 与世界创建流程使用。
/// </summary>
public sealed class GameSessionContext
{
    public GameSessionContext(IGameSessionProfile profile, IFrameSyncTransport transport)
    {
        Profile = profile;
        Transport = transport;
    }

    /// <summary>当前会话配置。</summary>
    public IGameSessionProfile Profile { get; }

    /// <summary>当前帧同步传输层。</summary>
    public IFrameSyncTransport Transport { get; }
}
