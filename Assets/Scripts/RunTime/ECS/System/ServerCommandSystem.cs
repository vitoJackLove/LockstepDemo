using System.Collections.Generic;
using Rogue;

/// <summary>
/// 帧同步指令系统：接收服务端转发的远端输入并写入 GGPO 缓冲。
/// </summary>
public class ServerCommandSystem : BaseSystem, IObserverHandler
{
    private int _teamNumber;
    private uint _clientSeq;

    public override void OnInit(object data = null)
    {
        base.OnInit(data);
        GameEntry.Observer.Attach(BattleObserverEventEnum.FrameCommand, this);
    }

    public void InitNumber(int number)
    {
        _teamNumber = number;
    }

    /// <summary>
    /// 发送本地玩家输入到服务端（由 GGPO 主循环调用）。
    /// </summary>
    public void SendPlayerInput(CommandData commandData)
    {
        if (commandData == null)
        {
            return;
        }

        commandData.ClientSeq = ++_clientSeq;

        FrameCommandMessage commandMessage = new FrameCommandMessage
        {
            CommandData = commandData,
            Tick = commandData.Tick,
        };

        GameEntry.FrameSyncTransport?.Send(BattleObserverEventEnum.FrameCommand, commandMessage);
    }

    public void OnNotify(IObserverParams param)
    {
        FrameCommandMessage commandMessage = (FrameCommandMessage)param;
        ProcessRelayCommands(commandMessage);
    }

    /// <summary>
    /// 将远端输入立即写入 GGPO 输入窗口（不等待全员到齐）。
    /// </summary>
    private void ProcessRelayCommands(FrameCommandMessage commandMessage)
    {
        if (CurrentWorld == null)
        {
            return;
        }

        EntitySystem entitySystem = GetSystem<EntitySystem>();
        int localEntityId = entitySystem.ActorLocalEntity?.EntityId ?? 0;

        if (commandMessage.CommandDataList != null && commandMessage.CommandDataList.Count > 0)
        {
            for (int i = 0; i < commandMessage.CommandDataList.Count; i++)
            {
                PushRemoteCommand(commandMessage.CommandDataList[i], commandMessage.Tick, localEntityId);
            }
        }
        else if (commandMessage.CommandData != null)
        {
            PushRemoteCommand(commandMessage.CommandData, commandMessage.Tick, localEntityId);
        }
    }

    private void PushRemoteCommand(CommandData commandData, uint messageTick, int localEntityId)
    {
        if (commandData == null)
        {
            return;
        }

        if (commandData.EntityId == localEntityId)
        {
            return;
        }

        uint tick = commandData.Tick > 0 ? commandData.Tick : messageTick;
        commandData.Tick = tick;
        CurrentWorld.PushGgpoRemoteInput(commandData.EntityId, tick, commandData);
    }

    public override void OnDispose()
    {
        base.OnDispose();
    }
}
