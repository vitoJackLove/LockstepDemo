using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Rogue.Network
{
    public static class NetworkPacketStreamUtility
    {
        public static bool TryReadPacket(NetworkStream stream, out byte[] packetData)
        {
            packetData = null;

            if (stream == null || !stream.CanRead)
            {
                return false;
            }

            byte[] lengthBuffer = new byte[sizeof(int)];
            if (!ReadExact(stream, lengthBuffer, 0, lengthBuffer.Length))
            {
                return false;
            }

            int packetLength = BitConverter.ToInt32(lengthBuffer, 0);
            int maxPacketBytes = NetworkPacketCodecProvider.Instance?.MaxPacketBytes ?? NetworkPacketCodecProvider.DefaultMaxPacketBytes;
            if (packetLength <= 0 || packetLength > maxPacketBytes)
            {
                GameLog.Error(GameLogChannel.Network, $"Invalid packet length: {packetLength}");
                return false;
            }

            packetData = new byte[packetLength];
            return ReadExact(stream, packetData, 0, packetData.Length);
        }

        public static void WritePacket(NetworkStream stream, byte[] packetData)
        {
            if (stream == null || !stream.CanWrite)
            {
                throw new InvalidOperationException("Network stream is not writable.");
            }

            if (packetData == null)
            {
                throw new ArgumentNullException(nameof(packetData));
            }

            byte[] lengthBuffer = BitConverter.GetBytes(packetData.Length);
            stream.Write(lengthBuffer, 0, lengthBuffer.Length);
            stream.Write(packetData, 0, packetData.Length);
        }

        public static bool TryReadAvailablePacket(NetworkStream stream, List<byte> receiveBuffer, out byte[] packetData)
        {
            return TryReadAvailablePacket(stream, receiveBuffer, out packetData, out _);
        }

        public static bool TryReadAvailablePacket(NetworkStream stream, List<byte> receiveBuffer, out byte[] packetData, out bool shouldDisconnect)
        {
            packetData = null;
            shouldDisconnect = false;

            if (stream == null || !stream.CanRead || receiveBuffer == null)
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
                    receiveBuffer.Add(buffer[i]);
                }
            }

            if (receiveBuffer.Count < sizeof(int))
            {
                return false;
            }

            int packetLength = BitConverter.ToInt32(receiveBuffer.GetRange(0, sizeof(int)).ToArray(), 0);
            int maxPacketBytes = NetworkPacketCodecProvider.Instance?.MaxPacketBytes ?? NetworkPacketCodecProvider.DefaultMaxPacketBytes;
            if (packetLength <= 0 || packetLength > maxPacketBytes)
            {
                GameLog.Error(GameLogChannel.Network, $"Invalid packet length: {packetLength}");
                receiveBuffer.Clear();
                shouldDisconnect = true;
                return false;
            }

            int packetTotalLength = sizeof(int) + packetLength;
            if (receiveBuffer.Count < packetTotalLength)
            {
                return false;
            }

            packetData = receiveBuffer.GetRange(sizeof(int), packetLength).ToArray();
            receiveBuffer.RemoveRange(0, packetTotalLength);
            return true;
        }

        private static bool ReadExact(NetworkStream stream, byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int bytesRead = stream.Read(buffer, offset + totalRead, count - totalRead);
                if (bytesRead <= 0)
                {
                    return false;
                }

                totalRead += bytesRead;
            }

            return true;
        }
    }
}
