using System.Net;
using System.Net.Sockets;
using Rogue.Network.Proto;

namespace Rogue.GameServer.Network;

public sealed class FrameSyncServer : IDisposable
{
    private const int MaxAuthorityFramesPerLoop = 16;

    private readonly NetworkServerOptions _options;
    private readonly object _syncRoot = new();
    private readonly List<FrameSyncConnection> _connections = new();
    private readonly Queue<int> _playerIndexes = new();

    private TcpListener? _listener;
    private Thread? _acceptThread;
    private Thread? _gameLoopThread;
    private volatile bool _isRunning;
    private uint _serverTick;
    private bool _loadStarted;
    private bool _battleStarted;

    public FrameSyncServer(NetworkServerOptions options)
    {
        _options = options;
        InitializePlayerIndexes(options.MaxPlayers, options.PlayerIndexSeed);
    }

    public void Start()
    {
        if (_isRunning)
        {
            return;
        }

        _listener = new TcpListener(IPAddress.Any, _options.Port);
        _listener.Start();
        _isRunning = true;

        _acceptThread = new Thread(AcceptClients)
        {
            IsBackground = true,
            Name = "FrameSyncServer.Accept",
        };
        _acceptThread.Start();

        _gameLoopThread = new Thread(GameLoop)
        {
            IsBackground = true,
            Name = "FrameSyncServer.GameLoop",
        };
        _gameLoopThread.Start();
    }

    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }

        _isRunning = false;
        _listener?.Stop();

        JoinThread(_acceptThread);
        JoinThread(_gameLoopThread);

        lock (_syncRoot)
        {
            foreach (FrameSyncConnection connection in _connections)
            {
                connection.Dispose();
            }

            _connections.Clear();
        }

        _listener = null;
        _acceptThread = null;
        _gameLoopThread = null;
    }

    public void Dispose()
    {
        Stop();
    }

    private void AcceptClients()
    {
        while (_isRunning && _listener != null)
        {
            try
            {
                TcpClient client = _listener.AcceptTcpClient();
                client.NoDelay = true;
                AddClient(client);
            }
            catch (SocketException) when (!_isRunning)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Network] Accept client failed: {ex.Message}");
            }
        }
    }

    private void AddClient(TcpClient client)
    {
        lock (_syncRoot)
        {
            if (_playerIndexes.Count == 0)
            {
                client.Close();
                Console.WriteLine("[Network] Reject client: server is full.");
                return;
            }

            int playerIndex = _playerIndexes.Dequeue();
            FrameSyncConnection connection = new(client, playerIndex);
            _connections.Add(connection);

            byte[] connectPacket = NetworkPacketCodec.SerializePlayerConnect(playerIndex);
            connection.WritePacket(connectPacket);
            Console.WriteLine($"[Room] Player entered room. PlayerIndex={playerIndex}, Online={_connections.Count}/{_options.MaxPlayers}");
        }
    }

    private void GameLoop()
    {
        while (_isRunning)
        {
            lock (_syncRoot)
            {
                GatherInputs();
            }

            Thread.Sleep(_options.GameLoopSleepMilliseconds);
        }
    }

    private void GatherInputs()
    {
        for (int i = _connections.Count - 1; i >= 0; i--)
        {
            FrameSyncConnection connection = _connections[i];
            try
            {
                bool shouldDisconnect = false;
                bool readAnyPacket = false;

                while (connection.TryReadPacket(out byte[] packetData, out shouldDisconnect))
                {
                    readAnyPacket = true;
                    ProcessPacket(connection, packetData);
                }

                if (!readAnyPacket)
                {
                    if (shouldDisconnect)
                    {
                        RemoveConnectionAt(i);
                    }

                    continue;
                }

                i = Math.Min(i + 1, _connections.Count);
            }
            catch (Exception ex) when (ex is IOException || ex is SocketException || ex is ObjectDisposedException)
            {
                RemoveConnectionAt(i);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Network] Receive failed: {ex.Message}");
                RemoveConnectionAt(i);
            }
        }
    }

    private void ProcessPacket(FrameSyncConnection connection, byte[] packetData)
    {
        LogMessageType(packetData, connection.PlayerIndex);
        if (NetworkPacketCodec.TryGetMessageType(packetData, out BattleObserverEventMessage messageType)
            && messageType == BattleObserverEventMessage.FrameCommand)
        {
            if (NetworkPacketCodec.TryDeserializeFrameCommand(packetData, out CommandData commandData))
            {
                connection.RememberInput(commandData);
                RelayFrameCommandToOthers(connection, commandData);
            }
        }
        else if (messageType == BattleObserverEventMessage.SelectHero)
        {
            if (NetworkPacketCodec.TryDeserializeSelectHero(packetData, out SelectHeroMessage selectHeroMessage))
            {
                connection.SelectHero(selectHeroMessage.SelectHeroId);
                Console.WriteLine($"[Room] Player ready for hero select. PlayerIndex={connection.PlayerIndex}, HeroId={selectHeroMessage.SelectHeroId}");
                BroadcastPacket(NetworkPacketCodec.SerializeSelectHero(connection.PlayerIndex, selectHeroMessage.SelectHeroId));
            }
        }
        else if (messageType == BattleObserverEventMessage.GameStart)
        {
            ProcessGameStart(connection);
        }
        else
        {
            BroadcastPacket(packetData);
        }
    }

    private bool TryBroadcastAuthorityFrame()
    {
        if (_connections.Count == 0)
        {
            return false;
        }

        uint nextServerTick = _serverTick + 1;

        FrameSyncConnection[] snapshot = _connections.ToArray();
        List<(int PlayerIndex, CommandData? Command)> frameInputs = new(snapshot.Length);

        foreach (FrameSyncConnection sourceConnection in snapshot)
        {
            if (!sourceConnection.TryGetInput(nextServerTick, out CommandData? commandData))
            {
                if (sourceConnection.HasInputAfter(nextServerTick))
                {
                    commandData = null;
                }
                else
                {
                    return false;
                }
            }

            frameInputs.Add((sourceConnection.PlayerIndex, commandData));
        }

        _serverTick = nextServerTick;

        byte[] authorityPacket = NetworkPacketCodec.SerializeFrameCommand(_serverTick, frameInputs);
        BroadcastPacket(authorityPacket);
        
        foreach (FrameSyncConnection sourceConnection in snapshot)
        {
            sourceConnection.DiscardInputsThrough(_serverTick);
        }

        return true;
    }

    private void ProcessGameStart(FrameSyncConnection connection)
    {
        if (!_loadStarted)
        {
            TryBroadcastLoadStart();
            return;
        }

        connection.MarkBattleReady();
        TryStartAuthorityFrames();
    }

    private void TryBroadcastLoadStart()
    {
        if (_connections.Count == 0)
        {
            return;
        }

        for (int i = 0; i < _connections.Count; i++)
        {
            if (!_connections[i].HasSelectedHero)
            {
                return;
            }
        }

        _loadStarted = true;

        Console.WriteLine("[Battle] Loading phase started. Waiting for all clients to report ready.");

        BroadcastPacket(NetworkPacketCodec.SerializeGameStart(true, 0));
    }

    private void TryStartAuthorityFrames()
    {
        if (_battleStarted || _connections.Count == 0)
        {
            return;
        }

        for (int i = 0; i < _connections.Count; i++)
        {
            if (!_connections[i].IsBattleReady)
            {
                return;
            }
        }

        _serverTick = 0;
        _battleStarted = true;
        Console.WriteLine($"[Battle] Battle started. ServerTick={_serverTick}");
    }

    private void RelayFrameCommandToOthers(FrameSyncConnection sourceConnection, CommandData commandData)
    {
        if (!_battleStarted || commandData == null)
        {
            return;
        }

        byte[] relayPacket = NetworkPacketCodec.SerializeFrameCommand(
            sourceConnection.PlayerIndex,
            commandData.Tick,
            commandData);

        for (int i = _connections.Count - 1; i >= 0; i--)
        {
            FrameSyncConnection target = _connections[i];
            if (target.PlayerIndex == sourceConnection.PlayerIndex)
            {
                continue;
            }

            try
            {
                target.WritePacket(relayPacket);
            }
            catch
            {
                RemoveConnectionAt(i);
            }
        }
    }

    private void BroadcastPacket(byte[] packetData)
    {
        for (int i = _connections.Count - 1; i >= 0; i--)
        {
            try
            {
                _connections[i].WritePacket(packetData);
            }
            catch
            {
                RemoveConnectionAt(i);
            }
        }
    }

    private void RemoveConnectionAt(int index)
    {
        if (index < 0 || index >= _connections.Count)
        {
            return;
        }

        FrameSyncConnection connection = _connections[index];
        _connections.RemoveAt(index);
        _playerIndexes.Enqueue(connection.PlayerIndex);
        connection.Dispose();
        Console.WriteLine($"[Network] Client disconnected. PlayerIndex={connection.PlayerIndex}, Online={_connections.Count}");
    }

    private static void JoinThread(Thread? thread)
    {
        if (thread != null && thread.IsAlive && Thread.CurrentThread != thread)
        {
            thread.Join(500);
        }
    }

    private static void LogMessageType(byte[] packetData, int playerIndex)
    {
        if (NetworkPacketCodec.TryGetMessageType(packetData, out BattleObserverEventMessage messageType))
        {
            Console.WriteLine($"[Network] {messageType} from PlayerIndex={playerIndex}");
        }
    }

    private void InitializePlayerIndexes(int maxPlayers, int seed)
    {
        Random random = new(seed);
        HashSet<int> used = new();

        while (_playerIndexes.Count < maxPlayers)
        {
            int playerIndex = random.Next(300, 1000);
            if (used.Add(playerIndex))
            {
                _playerIndexes.Enqueue(playerIndex);
            }
        }
    }
}
