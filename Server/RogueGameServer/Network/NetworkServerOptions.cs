namespace Rogue.GameServer.Network;

public sealed class NetworkServerOptions
{
    public const int DefaultPort = 8888;
    public const int DefaultMaxPlayers = 2;
    public const int DefaultGameLoopSleepMilliseconds = 1;

    public int Port { get; init; } = DefaultPort;
    public int MaxPlayers { get; init; } = DefaultMaxPlayers;
    public int GameLoopSleepMilliseconds { get; init; } = DefaultGameLoopSleepMilliseconds;
    public int PlayerIndexSeed { get; init; } = Environment.TickCount;
}
