namespace Rogue.Network
{
    public interface INetworkPacketCodec
    {
        int MaxPacketBytes { get; }

        byte[] SerializePacket(BattleObserverEventEnum messageType, IObserverParams observerParams);

        bool TryDeserializePacket(byte[] packetData, out BattleObserverEventEnum messageType, out IObserverParams observerParams);

        bool TryGetMessageType(byte[] packetData, out BattleObserverEventEnum messageType);
    }

    /// <summary>
    /// AOT 网络层通过该入口调用热更侧 protobuf 编解码实现。
    /// </summary>
    public static class NetworkPacketCodecProvider
    {
        public const int DefaultMaxPacketBytes = 1024 * 1024;

        public static INetworkPacketCodec Instance { get; set; }
    }
}
