using System.Collections.Generic;

/// <summary>
/// GGPO 输入同步器：对应 ggpo_synchronize_inputs，合并本地真实输入与远端（真实/预测）输入。
/// </summary>
public sealed class GgpoInputSynchronizer
{
    private readonly GgpoRemoteInputBuffer _remoteInputBuffer;
    private readonly Dictionary<uint, CommandData> _predictedRemoteByTick = new();

    private int _localEntityId;
    private int _remoteEntityId;

    public GgpoInputSynchronizer(GgpoRemoteInputBuffer remoteInputBuffer)
    {
        _remoteInputBuffer = remoteInputBuffer;
    }

    /// <summary>
    /// 绑定 1v1 对战双方实体 ID。
    /// </summary>
    public void BindPlayers(int localEntityId, int remoteEntityId)
    {
        _localEntityId = localEntityId;
        _remoteEntityId = remoteEntityId;
    }

    /// <summary>
    /// 合并当前 tick 的全部玩家输入。
    /// </summary>
    public List<CommandData> BuildMergedInputs(uint tick, CommandData localInput)
    {
        List<CommandData> merged = new List<CommandData>(2);

        CommandData local = CloneCommand(localInput, tick, _localEntityId);
        merged.Add(local);

        CommandData remote;
        if (_remoteInputBuffer.TryGetConfirmedInput(_remoteEntityId, tick, out CommandData confirmedRemote))
        {
            remote = CloneCommand(confirmedRemote, tick, _remoteEntityId);
        }
        else
        {
            remote = _remoteInputBuffer.PredictInput(_remoteEntityId, tick);
        }

        merged.Add(remote);
        _predictedRemoteByTick[tick] = CloneCommand(remote, tick, _remoteEntityId);
        return merged;
    }

    /// <summary>
    /// 获取某 tick 模拟时使用的远端输入快照（用于 AdvanceFrame 比对）。
    /// </summary>
    public bool TryGetPredictedRemoteInput(uint tick, out CommandData predictedRemote)
    {
        return _predictedRemoteByTick.TryGetValue(tick, out predictedRemote);
    }

    /// <summary>
    /// 移除错误 tick 之后的预测记录。
    /// </summary>
    public void RemovePredictionsFromTick(uint fromTick)
    {
        List<uint> removeKeys = new List<uint>();
        foreach (KeyValuePair<uint, CommandData> pair in _predictedRemoteByTick)
        {
            if (pair.Key >= fromTick)
            {
                removeKeys.Add(pair.Key);
            }
        }

        for (int i = 0; i < removeKeys.Count; i++)
        {
            _predictedRemoteByTick.Remove(removeKeys[i]);
        }
    }

    public GgpoRemoteInputBuffer RemoteInputBuffer => _remoteInputBuffer;

    private static CommandData CloneCommand(CommandData source, uint tick, int entityId)
    {
        CommandData clone = CommandData.Create();
        clone.Tick = tick;
        clone.EntityId = entityId;
        if (source == null)
        {
            return clone;
        }

        clone.ClientSeq = source.ClientSeq;
        clone.MoveDir = source.MoveDir;
        clone.SkillDir = source.SkillDir;
        clone.CommandType = source.CommandType;
        clone.CommandState = source.CommandState;
        return clone;
    }
}
