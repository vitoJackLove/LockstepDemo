using Rogue;

/// <summary>
/// 帧同步传输层：负责客户端与服务端之间的消息收发。
/// 联机走 TCP，单机走进程内回环，上层 Observer 分发路径保持一致。
/// </summary>
public interface IFrameSyncTransport
{
    /// <summary>初始化传输层（分配玩家索引、建立连接等）。</summary>
    void Initialize();

    /// <summary>发送帧同步消息。</summary>
    void Send(BattleObserverEventEnum messageType, IObserverParams payload);

    /// <summary>获取本地玩家索引。</summary>
    bool TryGetLocalPlayerIndex(out int playerIndex);

    /// <summary>关闭传输层并释放资源。</summary>
    void Shutdown();
}
