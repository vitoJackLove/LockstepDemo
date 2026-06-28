using System.Net.Sockets;

namespace Rogue.GameServer.Network;

internal sealed class FrameSyncConnection : IDisposable
{
    private readonly NetworkPacketStreamFramer _framer = new();
    private readonly Dictionary<uint, Rogue.Network.Proto.CommandData> _inputsByTick = new();
    private readonly TcpClient _client;
    private uint _lastClientSeq;
    private int _selectedHeroId;
    private bool _isBattleReady;

    public FrameSyncConnection(TcpClient client, int playerIndex)
    {
        _client = client;
        PlayerIndex = playerIndex;
    }

    public int PlayerIndex { get; }

    public bool HasSelectedHero => _selectedHeroId > 0;

    public int SelectedHeroId => _selectedHeroId;

    public bool IsBattleReady => _isBattleReady;

    public NetworkStream Stream => _client.GetStream();

    public bool TryReadPacket(out byte[] packetData, out bool shouldDisconnect)
    {
        return _framer.TryReadAvailablePacket(Stream, out packetData, out shouldDisconnect);
    }

    public void RememberInput(Rogue.Network.Proto.CommandData commandData)
    {
        if (commandData.ClientSeq < _lastClientSeq)
        {
            return;
        }

        _lastClientSeq = commandData.ClientSeq;
        _inputsByTick[commandData.Tick] = commandData;
    }

    public bool TryGetInput(uint tick, out Rogue.Network.Proto.CommandData? commandData)
    {
        return _inputsByTick.TryGetValue(tick, out commandData);
    }

    public bool HasInputAfter(uint tick)
    {
        foreach (uint inputTick in _inputsByTick.Keys)
        {
            if (inputTick > tick)
            {
                return true;
            }
        }

        return false;
    }

    public void DiscardInputsThrough(uint tick)
    {
        List<uint>? staleTicks = null;

        foreach (uint inputTick in _inputsByTick.Keys)
        {
            if (inputTick <= tick)
            {
                staleTicks ??= new List<uint>();
                staleTicks.Add(inputTick);
            }
        }

        if (staleTicks == null)
        {
            return;
        }

        for (int i = 0; i < staleTicks.Count; i++)
        {
            _inputsByTick.Remove(staleTicks[i]);
        }
    }

    public void SelectHero(int heroId)
    {
        _selectedHeroId = heroId;
    }

    public void MarkBattleReady()
    {
        _isBattleReady = true;
    }

    public void WritePacket(byte[] packetData)
    {
        NetworkPacketStreamUtility.WritePacket(Stream, packetData);
    }

    public void Dispose()
    {
        _client.Close();
        _client.Dispose();
    }
}
