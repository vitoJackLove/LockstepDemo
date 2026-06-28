using Rogue.GameServer.Network;

NetworkServerOptions options = ParseOptions(args);
using FrameSyncServer server = new(options);

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    server.Stop();
};

server.Start();
Console.WriteLine($"Rogue frame sync server started on 0.0.0.0:{options.Port}.");
Console.WriteLine("Press Enter or Ctrl+C to stop.");
Console.ReadLine();
server.Stop();

static NetworkServerOptions ParseOptions(string[] args)
{
    int port = NetworkServerOptions.DefaultPort;
    int maxPlayers = NetworkServerOptions.DefaultMaxPlayers;

    for (int i = 0; i < args.Length; i++)
    {
        string arg = args[i];
        if (arg is "--port" or "-p")
        {
            port = ReadIntArg(args, ref i, port);
        }
        else if (arg is "--max-players" or "-m")
        {
            maxPlayers = ReadIntArg(args, ref i, maxPlayers);
        }
    }

    return new NetworkServerOptions
    {
        Port = port,
        MaxPlayers = Math.Max(1, maxPlayers),
    };
}

static int ReadIntArg(string[] args, ref int index, int fallback)
{
    if (index + 1 >= args.Length)
    {
        return fallback;
    }

    index++;
    return int.TryParse(args[index], out int value) ? value : fallback;
}
