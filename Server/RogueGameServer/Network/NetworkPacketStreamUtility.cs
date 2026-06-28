using System.Net.Sockets;

namespace Rogue.GameServer.Network;

public static class NetworkPacketStreamUtility
{
    public const int MaxPacketBytes = 1024 * 1024;

    public static bool TryReadPacket(NetworkStream stream, out byte[] packetData)
    {
        packetData = Array.Empty<byte>();

        if (!stream.CanRead)
        {
            return false;
        }

        Span<byte> lengthBuffer = stackalloc byte[sizeof(int)];
        if (!ReadExact(stream, lengthBuffer))
        {
            return false;
        }

        int packetLength = BitConverter.ToInt32(lengthBuffer);
        if (packetLength <= 0 || packetLength > MaxPacketBytes)
        {
            return false;
        }

        packetData = new byte[packetLength];
        return ReadExact(stream, packetData);
    }

    public static void WritePacket(NetworkStream stream, ReadOnlySpan<byte> packetData)
    {
        if (!stream.CanWrite)
        {
            throw new InvalidOperationException("Network stream is not writable.");
        }

        if (packetData.Length <= 0 || packetData.Length > MaxPacketBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(packetData), $"Packet length must be between 1 and {MaxPacketBytes}.");
        }

        Span<byte> lengthBuffer = stackalloc byte[sizeof(int)];
        BitConverter.TryWriteBytes(lengthBuffer, packetData.Length);
        stream.Write(lengthBuffer);
        stream.Write(packetData);
    }

    private static bool ReadExact(NetworkStream stream, Span<byte> buffer)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int bytesRead = stream.Read(buffer[totalRead..]);
            if (bytesRead <= 0)
            {
                return false;
            }

            totalRead += bytesRead;
        }

        return true;
    }
}
