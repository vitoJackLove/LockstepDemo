using System;
using Google.Protobuf;
using Unity.Mathematics.FixedPoint;
using UnityEngine;
using Proto = Rogue.Network.Proto;

namespace Rogue.Network
{
    public static class NetworkProtobufCodec
    {
        public const int MaxPacketBytes = 1024 * 1024;

        private const int MessageTypeWireOffset = 1;
        private const int FixedVectorScale = 10000;

        public static byte[] SerializePacket(BattleObserverEventEnum messageType, IObserverParams observerParams)
        {
            if (observerParams == null)
            {
                throw new ArgumentNullException(nameof(observerParams));
            }

            IMessage payload = CreatePayload(messageType, observerParams);
            Proto.NetworkPacket packet = new Proto.NetworkPacket
            {
                MessageType = ToWireMessageType(messageType),
                Payload = ByteString.CopyFrom(payload.ToByteArray()),
            };

            return packet.ToByteArray();
        }

        public static bool TryDeserializePacket(byte[] packetData, out BattleObserverEventEnum messageType, out IObserverParams observerParams)
        {
            messageType = BattleObserverEventEnum.None;
            observerParams = null;

            if (packetData == null || packetData.Length == 0)
            {
                return false;
            }

            try
            {
                Proto.NetworkPacket packet = Proto.NetworkPacket.Parser.ParseFrom(packetData);
                if (!TryFromWireMessageType(packet.MessageType, out messageType))
                {
                    GameLog.Error(GameLogChannel.Network, $"Protobuf unsupported message wire type: {packet.MessageType}");
                    return false;
                }

                observerParams = ParsePayload(messageType, packet.Payload ?? ByteString.Empty);
                return observerParams != null;
            }
            catch (Exception e) when (e is InvalidProtocolBufferException || e is InvalidCastException || e is NotSupportedException)
            {
                GameLog.Error(GameLogChannel.Network, $"Protobuf failed to decode packet: {e.Message}");
                messageType = BattleObserverEventEnum.None;
                observerParams = null;
                return false;
            }
        }

        public static bool TryGetMessageType(byte[] packetData, out BattleObserverEventEnum messageType)
        {
            messageType = BattleObserverEventEnum.None;

            if (packetData == null || packetData.Length == 0)
            {
                return false;
            }

            try
            {
                Proto.NetworkPacket packet = Proto.NetworkPacket.Parser.ParseFrom(packetData);
                return TryFromWireMessageType(packet.MessageType, out messageType);
            }
            catch (InvalidProtocolBufferException)
            {
                return false;
            }
        }

        private static IMessage CreatePayload(BattleObserverEventEnum messageType, IObserverParams observerParams)
        {
            switch (messageType)
            {
                case BattleObserverEventEnum.PlayerConnect:
                    return ToProto((global::PlayerConnectMessage)observerParams);
                case BattleObserverEventEnum.FrameCommand:
                    return ToProto((global::FrameCommandMessage)observerParams);
                case BattleObserverEventEnum.SelectHero:
                    return ToProto((global::SelectHeroMessage)observerParams);
                case BattleObserverEventEnum.GameStart:
                    return ToProto((global::GameStartMessage)observerParams);
                default:
                    throw new NotSupportedException($"Unsupported protobuf message type: {messageType}");
            }
        }

        private static IObserverParams ParsePayload(BattleObserverEventEnum messageType, ByteString payload)
        {
            switch (messageType)
            {
                case BattleObserverEventEnum.PlayerConnect:
                    return ToRuntime(Proto.PlayerConnectMessage.Parser.ParseFrom(payload));
                case BattleObserverEventEnum.FrameCommand:
                    return ToRuntime(Proto.FrameCommandMessage.Parser.ParseFrom(payload));
                case BattleObserverEventEnum.SelectHero:
                    return ToRuntime(Proto.SelectHeroMessage.Parser.ParseFrom(payload));
                case BattleObserverEventEnum.GameStart:
                    return ToRuntime(Proto.GameStartMessage.Parser.ParseFrom(payload));
                default:
                    throw new NotSupportedException($"Unsupported protobuf message type: {messageType}");
            }
        }

        private static Proto.PlayerConnectMessage ToProto(global::PlayerConnectMessage message)
        {
            return new Proto.PlayerConnectMessage
            {
                PlayerIndex = message.PlayerIndex,
            };
        }

        private static global::PlayerConnectMessage ToRuntime(Proto.PlayerConnectMessage message)
        {
            return new global::PlayerConnectMessage
            {
                PlayerIndex = message.PlayerIndex,
            };
        }

        private static Proto.SelectHeroMessage ToProto(global::SelectHeroMessage message)
        {
            return new Proto.SelectHeroMessage
            {
                PlayerIndex = message.PlayerIndex,
                SelectHeroId = message.SelectHeroId,
            };
        }

        private static global::SelectHeroMessage ToRuntime(Proto.SelectHeroMessage message)
        {
            return new global::SelectHeroMessage
            {
                PlayerIndex = message.PlayerIndex,
                SelectHeroId = message.SelectHeroId,
            };
        }

        private static Proto.GameStartMessage ToProto(global::GameStartMessage message)
        {
            return new Proto.GameStartMessage
            {
                IsStart = message.isStart,
                StartTick = message.StartTick,
            };
        }

        private static global::GameStartMessage ToRuntime(Proto.GameStartMessage message)
        {
            return new global::GameStartMessage
            {
                isStart = message.IsStart,
                StartTick = message.StartTick,
            };
        }

        private static Proto.FrameCommandMessage ToProto(global::FrameCommandMessage message)
        {
            Proto.FrameCommandMessage protoMessage = new Proto.FrameCommandMessage
            {
                Tick = message.Tick,
            };

            if (message.CommandData != null)
            {
                protoMessage.CommandData = ToProto(message.CommandData);
            }

            if (message.CommandDataList != null)
            {
                for (int i = 0; i < message.CommandDataList.Count; i++)
                {
                    if (message.CommandDataList[i] != null)
                    {
                        protoMessage.CommandDataList.Add(ToProto(message.CommandDataList[i]));
                    }
                }
            }

            return protoMessage;
        }

        private static global::FrameCommandMessage ToRuntime(Proto.FrameCommandMessage message)
        {
            global::FrameCommandMessage runtimeMessage = new global::FrameCommandMessage
            {
                Tick = message.Tick,
                CommandData = message.CommandData == null ? global::CommandData.Create() : ToRuntime(message.CommandData),
            };

            for (int i = 0; i < message.CommandDataList.Count; i++)
            {
                runtimeMessage.CommandDataList.Add(ToRuntime(message.CommandDataList[i]));
            }

            return runtimeMessage;
        }

        private static Proto.CommandData ToProto(global::CommandData commandData)
        {
            return new Proto.CommandData
            {
                EntityId = commandData.EntityId,
                Tick = commandData.Tick,
                MoveDir = ToProto(commandData.MoveDir),
                SkillDir = ToProto(commandData.SkillDir),
                CommandType = (Proto.CommandType)(int)commandData.CommandType,
                CommandState = (Proto.CommandExecuteState)(int)commandData.CommandState,
                ClientSeq = commandData.ClientSeq,
            };
        }

        private static global::CommandData ToRuntime(Proto.CommandData message)
        {
            global::CommandData commandData = global::CommandData.Create();
            commandData.EntityId = message.EntityId;
            commandData.Tick = message.Tick;
            commandData.MoveDir = ToRuntime(message.MoveDir);
            commandData.SkillDir = ToRuntime(message.SkillDir);
            commandData.CommandType = (global::CommandType)(int)message.CommandType;
            commandData.CommandState = (global::WorldContent.CommandExecuteState)(int)message.CommandState;
            commandData.ClientSeq = message.ClientSeq;
            return commandData;
        }

        private static Proto.FixedPointVector3 ToProto(fp3 value)
        {
            fp3 quantizedValue = QuantizeVector(value);
            return new Proto.FixedPointVector3
            {
                X = ToScaledInt(quantizedValue.x),
                Y = ToScaledInt(quantizedValue.y),
                Z = ToScaledInt(quantizedValue.z),
            };
        }

        private static fp3 ToRuntime(Proto.FixedPointVector3 value)
        {
            if (value == null)
            {
                return fp3.zero;
            }

            return new fp3(ToFixedPoint(value.X), ToFixedPoint(value.Y), ToFixedPoint(value.Z));
        }

        public static fp3 QuantizeVector(fp3 value)
        {
            return new fp3(
                ToFixedPoint(ToScaledInt(value.x)),
                ToFixedPoint(ToScaledInt(value.y)),
                ToFixedPoint(ToScaledInt(value.z)));
        }

        private static int ToScaledInt(fp value)
        {
            return Mathf.RoundToInt((float)value * FixedVectorScale);
        }

        private static fp ToFixedPoint(int value)
        {
            return (fp)((float)value / FixedVectorScale);
        }

        private static int ToWireMessageType(BattleObserverEventEnum messageType)
        {
            if (messageType == BattleObserverEventEnum.None)
            {
                throw new NotSupportedException($"Unsupported protobuf message type: {messageType}");
            }

            return (int)messageType + MessageTypeWireOffset;
        }

        private static bool TryFromWireMessageType(int wireMessageType, out BattleObserverEventEnum messageType)
        {
            int runtimeMessageType = wireMessageType - MessageTypeWireOffset;
            messageType = (BattleObserverEventEnum)runtimeMessageType;
            return wireMessageType > 0 && Enum.IsDefined(typeof(BattleObserverEventEnum), runtimeMessageType) && messageType != BattleObserverEventEnum.None;
        }
    }
}
