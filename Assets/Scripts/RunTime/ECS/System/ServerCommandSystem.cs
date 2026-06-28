using System.Collections.Concurrent;
using System.Collections.Generic;
using Rogue;

/// <summary>
/// Caches server-authoritative frame inputs on the client.
/// </summary>
public class ServerCommandSystem : BaseSystem, IObserverHandler
{
    private ConcurrentDictionary<uint, List<CommandData>> _serverCommands =
        new ConcurrentDictionary<uint, List<CommandData>>();

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
        };

        GameEntry.FrameSyncTransport?.Send(BattleObserverEventEnum.FrameCommand, commandMessage);
    }

    public void OnNotify(IObserverParams param)
    {
        FrameCommandMessage commandMessage = (FrameCommandMessage)param;

        if (commandMessage.CommandDataList == null || commandMessage.CommandDataList.Count == 0)
        {
            return;
        }

        ProcessFrameData(commandMessage);
    }

    private void ProcessFrameData(FrameCommandMessage commandMessage)
    {
        if (_serverCommands == null)
        {
            return;
        }

        List<CommandData> commandList = new List<CommandData>(commandMessage.CommandDataList.Count);
        HashSet<int> entityIds = new HashSet<int>();

        for (int i = 0; i < commandMessage.CommandDataList.Count; i++)
        {
            CommandData commandData = commandMessage.CommandDataList[i];
            if (commandData == null || !entityIds.Add(commandData.EntityId))
            {
                continue;
            }

            commandData.Tick = commandMessage.Tick;
            commandList.Add(commandData);
        }

        if (_teamNumber > 0 && commandList.Count < _teamNumber)
        {
            return;
        }

        _serverCommands[commandMessage.Tick] = commandList;
    }

    public List<CommandData> GetServerCommand(uint tick)
    {
        if (_serverCommands.TryGetValue(tick, out var commandDataList))
        {
            return commandDataList;
        }

        return null;
    }

    public CommandData GetServerCommand(uint tick, uint entityId)
    {
        List<CommandData> commandList = GetServerCommand(tick);

        if (commandList == null)
        {
            return null;
        }

        for (int i = 0; i < commandList.Count; i++)
        {
            if (commandList[i].EntityId == entityId)
            {
                return commandList[i];
            }
        }

        return null;
    }

    public override void OnDispose()
    {
        base.OnDispose();

        _serverCommands?.Clear();
        _serverCommands = null;
    }
}
