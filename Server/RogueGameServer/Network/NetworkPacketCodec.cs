using Google.Protobuf;
using Rogue.Network.Proto;

namespace Rogue.GameServer.Network;

public static class NetworkPacketCodec
{
    private const int MessageTypeWireOffset = 1;

    public static byte[] SerializePlayerConnect(int playerIndex)
    {
        PlayerConnectMessage payload = new()
        {
            PlayerIndex = playerIndex,
        };

        NetworkPacket packet = new()
        {
            MessageType = ToWireMessageType(BattleObserverEventMessage.PlayerConnect),
            Payload = ByteString.CopyFrom(payload.ToByteArray()),
        };

        return packet.ToByteArray();
    }

    public static bool TryGetMessageType(byte[] packetData, out BattleObserverEventMessage messageType)
    {
        messageType = BattleObserverEventMessage.None;

        try
        {
            NetworkPacket packet = NetworkPacket.Parser.ParseFrom(packetData);
            int runtimeMessageType = packet.MessageType - MessageTypeWireOffset;
            messageType = (BattleObserverEventMessage)runtimeMessageType;
            return packet.MessageType > 0
                && Enum.IsDefined(typeof(BattleObserverEventMessage), runtimeMessageType)
                && messageType != BattleObserverEventMessage.None;
        }
        catch (InvalidProtocolBufferException)
        {
            return false;
        }
    }

    public static bool TryDeserializeFrameCommand(byte[] packetData, out CommandData commandData)
    {
        commandData = new CommandData();

        try
        {
            NetworkPacket packet = NetworkPacket.Parser.ParseFrom(packetData);
            int runtimeMessageType = packet.MessageType - MessageTypeWireOffset;
            if ((BattleObserverEventMessage)runtimeMessageType != BattleObserverEventMessage.FrameCommand)
            {
                return false;
            }

            FrameCommandMessage message = FrameCommandMessage.Parser.ParseFrom(packet.Payload);
            if (message.CommandData == null)
            {
                return false;
            }

            commandData = message.CommandData;
            return true;
        }
        catch (InvalidProtocolBufferException)
        {
            return false;
        }
    }

    public static bool TryDeserializeSelectHero(byte[] packetData, out SelectHeroMessage selectHeroMessage)
    {
        selectHeroMessage = new SelectHeroMessage();

        try
        {
            NetworkPacket packet = NetworkPacket.Parser.ParseFrom(packetData);
            int runtimeMessageType = packet.MessageType - MessageTypeWireOffset;
            if ((BattleObserverEventMessage)runtimeMessageType != BattleObserverEventMessage.SelectHero)
            {
                return false;
            }

            selectHeroMessage = SelectHeroMessage.Parser.ParseFrom(packet.Payload);
            return true;
        }
        catch (InvalidProtocolBufferException)
        {
            return false;
        }
    }

    public static byte[] SerializeSelectHero(int playerIndex, int selectHeroId)
    {
        SelectHeroMessage payload = new()
        {
            PlayerIndex = playerIndex,
            SelectHeroId = selectHeroId,
        };

        return SerializePayload(BattleObserverEventMessage.SelectHero, payload);
    }

    public static byte[] SerializeGameStart(bool isStart, uint startTick)
    {
        GameStartMessage payload = new()
        {
            IsStart = isStart,
            StartTick = startTick,
        };

        return SerializePayload(BattleObserverEventMessage.GameStart, payload);
    }

    public static byte[] SerializeFrameCommand(uint serverTick, IReadOnlyList<(int PlayerIndex, CommandData? Command)> frameInputs)
    {
        FrameCommandMessage payload = new()
        {
            Tick = serverTick,
        };

        for (int i = 0; i < frameInputs.Count; i++)
        {
            CommandData commandData = CloneCommand(frameInputs[i].Command);
            commandData.EntityId = frameInputs[i].PlayerIndex;
            commandData.Tick = serverTick;
            payload.CommandDataList.Add(commandData);
        }

        NetworkPacket packet = new()
        {
            MessageType = ToWireMessageType(BattleObserverEventMessage.FrameCommand),
            Payload = ByteString.CopyFrom(payload.ToByteArray()),
        };

        return packet.ToByteArray();
    }

    private static byte[] SerializePayload(BattleObserverEventMessage messageType, IMessage payload)
    {
        NetworkPacket packet = new()
        {
            MessageType = ToWireMessageType(messageType),
            Payload = ByteString.CopyFrom(payload.ToByteArray()),
        };

        return packet.ToByteArray();
    }

    public static byte[] SerializeFrameCommand(int playerIndex, uint serverTick, CommandData? sourceCommand)
    {
        CommandData commandData = CloneCommand(sourceCommand);

        commandData.EntityId = playerIndex;
        commandData.Tick = serverTick;

        FrameCommandMessage payload = new()
        {
            Tick = serverTick,
        };
        payload.CommandDataList.Add(commandData);

        NetworkPacket packet = new()
        {
            MessageType = ToWireMessageType(BattleObserverEventMessage.FrameCommand),
            Payload = ByteString.CopyFrom(payload.ToByteArray()),
        };

        return packet.ToByteArray();
    }

    private static CommandData CloneCommand(CommandData? sourceCommand)
    {
        CommandData commandData = new();

        if (sourceCommand == null)
        {
            return commandData;
        }

        commandData.EntityId = sourceCommand.EntityId;
        commandData.Tick = sourceCommand.Tick;
        commandData.MoveDir = sourceCommand.MoveDir;
        commandData.SkillDir = sourceCommand.SkillDir;
        commandData.CommandType = sourceCommand.CommandType;
        commandData.CommandState = sourceCommand.CommandState;
        commandData.ClientSeq = sourceCommand.ClientSeq;
        return commandData;
    }

    private static int ToWireMessageType(BattleObserverEventMessage messageType)
    {
        if (messageType == BattleObserverEventMessage.None)
        {
            throw new NotSupportedException($"Unsupported protobuf message type: {messageType}");
        }

        return (int)messageType + MessageTypeWireOffset;
    }
}
