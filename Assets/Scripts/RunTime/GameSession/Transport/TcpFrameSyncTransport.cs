using Rogue;

/// <summary>
/// TCP 帧同步传输：包装 FrameSyncClientComponent，供联机模式使用。
/// </summary>
public sealed class TcpFrameSyncTransport : IFrameSyncTransport
{
    private readonly FrameSyncClientComponent _client;

    public TcpFrameSyncTransport(FrameSyncClientComponent client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public void Initialize()
    {
        _client?.Init();
    }

    /// <inheritdoc />
    public void Send(BattleObserverEventEnum messageType, IObserverParams payload)
    {
        _client?.Send(payload, messageType);
    }

    /// <inheritdoc />
    public bool TryGetLocalPlayerIndex(out int playerIndex)
    {
        if (_client != null && _client.TryGetLocalPlayerIndex(out playerIndex))
        {
            return true;
        }

        playerIndex = 0;
        return false;
    }

    /// <inheritdoc />
    public void Shutdown()
    {
        _client?.Shutdown();
    }
}
