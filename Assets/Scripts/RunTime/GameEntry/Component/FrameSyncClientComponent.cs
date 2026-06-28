using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Rogue;
using Rogue.Network;
using UnityEngine;

public class FrameSyncClientComponent : RunTimeComponent
{
    [SerializeField] private string serverIp = "127.0.0.1";
    [SerializeField] private int port = 8888;

    private readonly ConcurrentQueue<(BattleObserverEventEnum messageType, IObserverParams observerParams)> _pendingMessages = new();
    private TcpClient _client;
    private NetworkStream _stream;
    private Thread _receiveThread;
    private volatile bool _isRunning;
    private int _localPlayerIndex;
    private bool _hasLocalPlayerIndex;

    public bool TryGetLocalPlayerIndex(out int playerIndex)
    {
        playerIndex = _localPlayerIndex;
        return _hasLocalPlayerIndex;
    }

    public override void Init()
    {
        base.Init();
        ConnectToServer();
    }

    private void Update()
    {
        while (_pendingMessages.TryDequeue(out var message))
        {
            GameEntry.Observer.Notify(message.messageType, message.observerParams);
        }
    }

    private void ConnectToServer()
    {
        try
        {
            _client = new TcpClient();
            _client.Connect(serverIp, port);
            _stream = _client.GetStream();

            _isRunning = true;
            _receiveThread = new Thread(ReceiveData)
            {
                IsBackground = true
            };
            _receiveThread.Start();

            GameLog.Info(GameLogChannel.Network, $"Connected to server {serverIp}:{port}");
        }
        catch (Exception e)
        {
            GameLog.Error(GameLogChannel.Network, $"Connection error: {e.Message}");
        }
    }

    public void Send(IObserverParams content, BattleObserverEventEnum messageType)
    {
        if (content == null)
        {
            GameLog.Warn(GameLogChannel.Network, $"Ignore empty message: {messageType}");
            return;
        }

        if (_stream == null || !_stream.CanWrite)
        {
            return;
        }

        try
        {
            byte[] packetData = NetworkProtobufCodec.SerializePacket(messageType, content);
            NetworkPacketStreamUtility.WritePacket(_stream, packetData);
        }
        catch (Exception e)
        {
            GameLog.Error(GameLogChannel.Network, $"Send failed ({messageType}): {e.Message}");
        }
    }

    private void ReceiveData()
    {
        while (_isRunning && _client != null && _client.Connected)
        {
            try
            {
                if (!NetworkPacketStreamUtility.TryReadPacket(_stream, out byte[] packetData))
                {
                    break;
                }

                if (!NetworkProtobufCodec.TryDeserializePacket(packetData, out BattleObserverEventEnum messageType, out IObserverParams observerParams))
                {
                    GameLog.Warn(GameLogChannel.Network, "Protobuf failed to decode packet.");
                    continue;
                }

                if (observerParams is PlayerConnectMessage playerConnectMessage)
                {
                    _localPlayerIndex = playerConnectMessage.PlayerIndex;
                    _hasLocalPlayerIndex = true;
                }

                _pendingMessages.Enqueue((messageType, observerParams));
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (ThreadAbortException)
            {
                break;
            }
            catch (Exception e) when (IsExpectedReceiveShutdown(e))
            {
                break;
            }
            catch (Exception e)
            {
                GameLog.Error(GameLogChannel.Network, $"Receive failed: {e.Message}");
            }
        }
    }

    private bool IsExpectedReceiveShutdown(Exception exception)
    {
        if (!_isRunning || exception is ObjectDisposedException || exception is SocketException)
        {
            return true;
        }

        if (exception is IOException && ContainsThreadAbort(exception))
        {
            return true;
        }

        return false;
    }

    private static bool ContainsThreadAbort(Exception exception)
    {
        while (exception != null)
        {
            if (exception is ThreadAbortException)
            {
                return true;
            }

            exception = exception.InnerException;
        }

        return false;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _isRunning = false;
        _stream?.Close();
        _client?.Close();

        if (_receiveThread != null && _receiveThread.IsAlive && Thread.CurrentThread != _receiveThread)
        {
            _receiveThread.Join(100);
        }

        _receiveThread = null;
        _stream = null;
        _client = null;
    }
}
