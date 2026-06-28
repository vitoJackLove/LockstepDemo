using System.Net.Sockets;

namespace Rogue.GameServer.Network;

public sealed class NetworkPacketStreamFramer
{
    private readonly List<byte> _receiveBuffer = new();

    public bool TryReadAvailablePacket(NetworkStream stream, out byte[] packetData, out bool shouldDisconnect)
    {
        packetData = Array.Empty<byte>();
        shouldDisconnect = false;

        if (!stream.CanRead)
        {
            shouldDisconnect = true;
            return false;
        }

        while (stream.DataAvailable)
        {
            byte[] buffer = new byte[4096];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead <= 0)
            {
                shouldDisconnect = true;
                return false;
            }

            for (int i = 0; i < bytesRead; i++)
            {
                _receiveBuffer.Add(buffer[i]);
            }
        }

        if (_receiveBuffer.Count < sizeof(int))
        {
            return false;
        }

        int packetLength = BitConverter.ToInt32(_receiveBuffer.GetRange(0, sizeof(int)).ToArray(), 0);
        if (packetLength <= 0 || packetLength > NetworkPacketStreamUtility.MaxPacketBytes)
        {
            _receiveBuffer.Clear();
            shouldDisconnect = true;
            return false;
        }

        int totalLength = sizeof(int) + packetLength;
        if (_receiveBuffer.Count < totalLength)
        {
            return false;
        }

        packetData = _receiveBuffer.GetRange(sizeof(int), packetLength).ToArray();
        _receiveBuffer.RemoveRange(0, totalLength);
        return true;
    }
}
