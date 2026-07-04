using System;
using System.Net;
using System.Net.Sockets;
using NUnit.Framework;
using Rogue.Network;
using Unity.Mathematics.FixedPoint;

namespace Rogue.Editor.Tests
{
    public class NetworkProtobufRoundTripTests
    {
        [TestCase(BattleObserverEventEnum.PlayerConnect)]
        [TestCase(BattleObserverEventEnum.FrameCommand)]
        [TestCase(BattleObserverEventEnum.SelectHero)]
        [TestCase(BattleObserverEventEnum.GameStart)]
        public void TcpPacket_RoundTrip_PreservesMessage(BattleObserverEventEnum messageType)
        {
            IObserverParams source = CreateMessage(messageType);

            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                listener.Start();
                int port = ((IPEndPoint)listener.LocalEndpoint).Port;

                using (TcpClient client = new TcpClient())
                {
                    client.Connect(IPAddress.Loopback, port);

                    using (TcpClient accepted = listener.AcceptTcpClient())
                    using (NetworkStream clientStream = client.GetStream())
                    using (NetworkStream serverStream = accepted.GetStream())
                    {
                        NetworkProtobufCodec codec = new NetworkProtobufCodec();
                        byte[] expectedPacket = codec.SerializePacket(messageType, source);
                        NetworkPacketStreamUtility.WritePacket(clientStream, expectedPacket);

                        Assert.IsTrue(NetworkPacketStreamUtility.TryReadPacket(serverStream, out byte[] receivedPacket));
                        CollectionAssert.AreEqual(expectedPacket, receivedPacket);

                        Assert.IsTrue(codec.TryDeserializePacket(receivedPacket, out BattleObserverEventEnum decodedType, out IObserverParams decodedMessage));
                        Assert.AreEqual(messageType, decodedType);
                        AssertMessageEqual(source, decodedMessage);
                    }
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        private static IObserverParams CreateMessage(BattleObserverEventEnum messageType)
        {
            switch (messageType)
            {
                case BattleObserverEventEnum.PlayerConnect:
                    return new PlayerConnectMessage
                    {
                        PlayerIndex = 12,
                    };

                case BattleObserverEventEnum.SelectHero:
                    return new SelectHeroMessage
                    {
                        PlayerIndex = 3,
                        SelectHeroId = 7,
                    };

                case BattleObserverEventEnum.GameStart:
                    return new GameStartMessage
                    {
                        isStart = true,
                        StartTick = 33,
                    };

                case BattleObserverEventEnum.FrameCommand:
                    FrameCommandMessage frameCommandMessage = new FrameCommandMessage
                    {
                        Tick = 1234,
                        CommandData = CreateCommandData(),
                    };
                    frameCommandMessage.CommandDataList.Add(CreateCommandData());
                    frameCommandMessage.CommandDataList.Add(new CommandData
                    {
                        EntityId = 100,
                        Tick = 1234,
                        ClientSeq = 8,
                        MoveDir = new fp3((fp)2, (fp)0, (fp)1),
                        SkillDir = new fp3((fp)0, (fp)0, (fp)1),
                        CommandType = CommandType.Attack,
                        CommandState = WorldContent.CommandExecuteState.OnlyDown,
                    });
                    return frameCommandMessage;

                default:
                    throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
            }
        }

        private static CommandData CreateCommandData()
        {
            return new CommandData
            {
                EntityId = 99,
                Tick = 1234,
                ClientSeq = 7,
                MoveDir = new fp3((fp)1, (fp)0, (fp)2),
                SkillDir = new fp3((fp)(-3), (fp)0, (fp)4),
                CommandType = CommandType.Roll,
                CommandState = WorldContent.CommandExecuteState.DownUp,
            };
        }

        private static void AssertMessageEqual(IObserverParams expected, IObserverParams actual)
        {
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.ObserverEventType, actual.ObserverEventType);

            switch (expected)
            {
                case PlayerConnectMessage expectedPlayerConnect:
                    Assert.That(actual, Is.TypeOf<PlayerConnectMessage>());
                    Assert.AreEqual(expectedPlayerConnect.PlayerIndex, ((PlayerConnectMessage)actual).PlayerIndex);
                    break;

                case SelectHeroMessage expectedSelectHero:
                    Assert.That(actual, Is.TypeOf<SelectHeroMessage>());
                    SelectHeroMessage actualSelectHero = (SelectHeroMessage)actual;
                    Assert.AreEqual(expectedSelectHero.PlayerIndex, actualSelectHero.PlayerIndex);
                    Assert.AreEqual(expectedSelectHero.SelectHeroId, actualSelectHero.SelectHeroId);
                    break;

                case GameStartMessage expectedGameStart:
                    Assert.That(actual, Is.TypeOf<GameStartMessage>());
                    GameStartMessage actualGameStart = (GameStartMessage)actual;
                    Assert.AreEqual(expectedGameStart.isStart, actualGameStart.isStart);
                    Assert.AreEqual(expectedGameStart.StartTick, actualGameStart.StartTick);
                    break;

                case FrameCommandMessage expectedFrameCommand:
                    Assert.That(actual, Is.TypeOf<FrameCommandMessage>());
                    FrameCommandMessage actualFrameCommand = (FrameCommandMessage)actual;
                    Assert.AreEqual(expectedFrameCommand.Tick, actualFrameCommand.Tick);
                    AssertCommandDataEqual(expectedFrameCommand.CommandData, actualFrameCommand.CommandData);
                    Assert.AreEqual(expectedFrameCommand.CommandDataList.Count, actualFrameCommand.CommandDataList.Count);
                    for (int i = 0; i < expectedFrameCommand.CommandDataList.Count; i++)
                    {
                        AssertCommandDataEqual(expectedFrameCommand.CommandDataList[i], actualFrameCommand.CommandDataList[i]);
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(expected), expected, null);
            }
        }

        private static void AssertCommandDataEqual(CommandData expected, CommandData actual)
        {
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.EntityId, actual.EntityId);
            Assert.AreEqual(expected.Tick, actual.Tick);
            Assert.AreEqual(expected.ClientSeq, actual.ClientSeq);
            Assert.AreEqual(expected.MoveDir.x, actual.MoveDir.x);
            Assert.AreEqual(expected.MoveDir.y, actual.MoveDir.y);
            Assert.AreEqual(expected.MoveDir.z, actual.MoveDir.z);
            Assert.AreEqual(expected.SkillDir.x, actual.SkillDir.x);
            Assert.AreEqual(expected.SkillDir.y, actual.SkillDir.y);
            Assert.AreEqual(expected.SkillDir.z, actual.SkillDir.z);
            Assert.AreEqual(expected.CommandType, actual.CommandType);
            Assert.AreEqual(expected.CommandState, actual.CommandState);
        }
    }
}
