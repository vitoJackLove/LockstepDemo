using System.Collections.Generic;

[System.Serializable]
public class FrameCommandMessage : IObserverParams
{
    public BattleObserverEventEnum ObserverEventType => BattleObserverEventEnum.FrameCommand;

    /// <summary>
    /// Client input when sent upward. Kept for compatibility with the local input path.
    /// </summary>
    public CommandData CommandData;

    /// <summary>
    /// Server-assigned authoritative frame tick.
    /// </summary>
    public uint Tick;

    /// <summary>
    /// Complete authoritative inputs for Tick, one command per connected player.
    /// </summary>
    public List<CommandData> CommandDataList = new List<CommandData>();
}
