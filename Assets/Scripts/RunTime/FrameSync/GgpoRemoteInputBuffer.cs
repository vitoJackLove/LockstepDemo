using System.Collections.Generic;

/// <summary>
/// GGPO 远端玩家输入历史窗口：缓存已确认的真实输入，并提供预测接口。
/// </summary>
public sealed class GgpoRemoteInputBuffer
{
    private readonly Dictionary<int, SortedDictionary<uint, CommandData>> _confirmedInputsByEntity = new();
    private readonly Dictionary<int, CommandData> _lastConfirmedInputByEntity = new();
    private readonly uint _historyWindow;

    public GgpoRemoteInputBuffer(uint historyWindow)
    {
        _historyWindow = historyWindow > 0 ? historyWindow : 8;
    }

    /// <summary>
    /// 写入远端玩家在指定 tick 的已确认输入。
    /// </summary>
    public void PushConfirmedInput(int remoteEntityId, uint tick, CommandData confirmedInput)
    {
        if (confirmedInput == null)
        {
            return;
        }

        if (!_confirmedInputsByEntity.TryGetValue(remoteEntityId, out SortedDictionary<uint, CommandData> tickMap))
        {
            tickMap = new SortedDictionary<uint, CommandData>();
            _confirmedInputsByEntity[remoteEntityId] = tickMap;
        }

        tickMap[tick] = CloneCommand(confirmedInput, tick, remoteEntityId);
        _lastConfirmedInputByEntity[remoteEntityId] = tickMap[tick];
        TrimHistory(remoteEntityId, tick);
    }

    /// <summary>
    /// 尝试获取已确认输入。
    /// </summary>
    public bool TryGetConfirmedInput(int remoteEntityId, uint tick, out CommandData command)
    {
        command = null;
        if (!_confirmedInputsByEntity.TryGetValue(remoteEntityId, out SortedDictionary<uint, CommandData> tickMap))
        {
            return false;
        }

        return tickMap.TryGetValue(tick, out command);
    }

    /// <summary>
    /// GGPO 默认预测：未来输入等于最近一次已确认输入。
    /// </summary>
    public CommandData PredictInput(int remoteEntityId, uint tick)
    {
        if (_lastConfirmedInputByEntity.TryGetValue(remoteEntityId, out CommandData lastConfirmed))
        {
            return CloneCommand(lastConfirmed, tick, remoteEntityId);
        }

        CommandData empty = CommandData.Create();
        empty.Tick = tick;
        empty.EntityId = remoteEntityId;
        return empty;
    }

    /// <summary>
    /// 清理早于窗口下限的历史，避免内存膨胀。
    /// </summary>
    private void TrimHistory(int remoteEntityId, uint newestTick)
    {
        if (!_confirmedInputsByEntity.TryGetValue(remoteEntityId, out SortedDictionary<uint, CommandData> tickMap))
        {
            return;
        }

        if (newestTick <= _historyWindow)
        {
            return;
        }

        uint minTick = newestTick - _historyWindow;
        List<uint> removeKeys = new List<uint>();
        foreach (KeyValuePair<uint, CommandData> pair in tickMap)
        {
            if (pair.Key < minTick)
            {
                removeKeys.Add(pair.Key);
            }
        }

        for (int i = 0; i < removeKeys.Count; i++)
        {
            tickMap.Remove(removeKeys[i]);
        }
    }

    private static CommandData CloneCommand(CommandData source, uint tick, int entityId)
    {
        CommandData clone = CommandData.Create();
        clone.Tick = tick;
        clone.EntityId = entityId;
        clone.ClientSeq = source.ClientSeq;
        clone.MoveDir = source.MoveDir;
        clone.SkillDir = source.SkillDir;
        clone.CommandType = source.CommandType;
        clone.CommandState = source.CommandState;
        return clone;
    }
}
