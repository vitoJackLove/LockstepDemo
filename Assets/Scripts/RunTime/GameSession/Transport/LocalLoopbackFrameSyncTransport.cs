using System.Collections.Generic;
using Rogue;

/// <summary>
/// 本地回环帧同步传输：在进程内模拟服务端权威帧广播，不依赖 TCP。
/// 发送帧命令后立即通过 Observer 投递权威帧，供 ServerCommandSystem 消费。
/// </summary>
public sealed class LocalLoopbackFrameSyncTransport : IFrameSyncTransport
{
    private const int DefaultPlayerIndex = 0;

    private int _localPlayerIndex = DefaultPlayerIndex;
    private bool _hasLocalPlayerIndex;

    /// <inheritdoc />
    public void Initialize()
    {
        _localPlayerIndex = DefaultPlayerIndex;
        _hasLocalPlayerIndex = true;

        // 模拟服务端分配玩家索引
        PlayerConnectMessage connectMessage = new PlayerConnectMessage
        {
            PlayerIndex = _localPlayerIndex,
        };
        GameEntry.Observer?.Notify(BattleObserverEventEnum.PlayerConnect, connectMessage);
    }

    /// <inheritdoc />
    public void Send(BattleObserverEventEnum messageType, IObserverParams payload)
    {
        if (payload == null || GameEntry.Observer == null)
        {
            return;
        }

        switch (messageType)
        {
            case BattleObserverEventEnum.FrameCommand:
                BroadcastAuthorityFrame((FrameCommandMessage)payload);
                break;

            default:
                // 选英雄、游戏开始等消息直接本地广播
                GameEntry.Observer.Notify(messageType, payload);
                break;
        }
    }

    /// <inheritdoc />
    public bool TryGetLocalPlayerIndex(out int playerIndex)
    {
        playerIndex = _localPlayerIndex;
        return _hasLocalPlayerIndex;
    }

    /// <inheritdoc />
    public void Shutdown()
    {
        _hasLocalPlayerIndex = false;
    }

    /// <summary>
    /// 将客户端帧输入立即转换为权威帧并投递，模拟服务端聚合广播。
    /// </summary>
    private static void BroadcastAuthorityFrame(FrameCommandMessage clientMessage)
    {
        if (clientMessage?.CommandData == null)
        {
            return;
        }

        FrameCommandMessage authorityMessage = new FrameCommandMessage
        {
            Tick = clientMessage.CommandData.Tick,
            CommandDataList = new List<CommandData> { clientMessage.CommandData },
        };

        GameEntry.Observer.Notify(BattleObserverEventEnum.FrameCommand, authorityMessage);
    }
}
