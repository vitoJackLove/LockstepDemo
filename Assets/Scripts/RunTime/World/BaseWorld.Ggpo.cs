using System.Collections.Generic;
using Unity.Mathematics.FixedPoint;

/// <summary>
/// BaseWorld 的 GGPO 帧同步扩展（严格按 Figure 7）。
/// </summary>
public partial class BaseWorld
{
    private GgpoRemoteInputBuffer _ggpoRemoteInputBuffer;
    private GgpoInputSynchronizer _ggpoInputSynchronizer;
    private GgpoRollbackController _ggpoRollbackController;
    private int _ggpoLocalEntityId;
    private int _ggpoRemoteEntityId;

    /// <summary>
    /// 初始化 GGPO 模块并绑定 1v1 实体。
    /// </summary>
    public void InitializeGgpo(int localEntityId, int remoteEntityId)
    {
        uint historyWindow = SessionProfile.InputHistoryWindow;
        _ggpoRemoteInputBuffer = new GgpoRemoteInputBuffer(historyWindow);
        _ggpoInputSynchronizer = new GgpoInputSynchronizer(_ggpoRemoteInputBuffer);
        _ggpoRollbackController = new GgpoRollbackController(_ggpoInputSynchronizer, _ggpoRemoteInputBuffer);
        _ggpoLocalEntityId = localEntityId;
        _ggpoRemoteEntityId = remoteEntityId;
        _ggpoInputSynchronizer.BindPlayers(localEntityId, remoteEntityId);
    }

    /// <summary>
    /// 供 ServerCommandSystem 写入远端已确认输入。
    /// </summary>
    public void PushGgpoRemoteInput(int remoteEntityId, uint tick, CommandData command)
    {
        _ggpoRemoteInputBuffer?.PushConfirmedInput(remoteEntityId, tick, command);
    }

    /// <summary>
    /// 供回滚控制器加载快照。
    /// </summary>
    public void RollBackToTick(uint tick)
    {
        RollBack(tick, RollBackType.HardRollBack | RollBackType.SoftRollBack);
        RemoveLocalSnapShot(tick + 1, RollBackType.HardRollBack | RollBackType.SoftRollBack);
    }

    /// <summary>
    /// 对外暴露快照拍摄，供 GGPO 重放使用。
    /// </summary>
    public void TakeLocalSnapShotForGgpo(uint tick)
    {
        foreach (BaseSystem system in _systemDic.Values)
        {
            system.TakeLocalSnapShot(tick);
        }
    }

    /// <summary>
    /// 单模拟态推进：合并后的全玩家指令一次执行。
    /// </summary>
    public void UpdateGameState(fp deltaTime, List<CommandData> frameCommands)
    {
        if (GameTimeType != GameTimeType.Start)
        {
            return;
        }

        ExecuteSimulation(frameCommands);

        foreach (BaseSystem system in _systemDic.Values)
        {
            system.OnFixedUpdate(deltaTime, WorldUpdateType.Local);
        }
    }

    /// <summary>
    /// 按 EntityId 分发帧指令。
    /// </summary>
    private void ExecuteSimulation(List<CommandData> frameCommands)
    {
        if (frameCommands == null)
        {
            return;
        }

        for (int i = 0; i < frameCommands.Count; i++)
        {
            CommandData command = frameCommands[i];
            if (command == null)
            {
                continue;
            }

            foreach (BaseSystem system in _systemDic.Values)
            {
                system.OnExecuteLocalCommand(command);
            }
        }
    }

    /// <summary>
    /// GGPO FixedUpdate 主循环。
    /// </summary>
    private void FixedUpdateGgpo(fp deltaTime)
    {
        if (_ggpoRollbackController != null && _ggpoRollbackController.TryStepResimulate(
                this,
                deltaTime,
                tick => GetSystem<CommandSystem>().GetRecodeCommand(tick),
                out bool resimCompleted))
        {
            return;
        }

        _localTick++;
        CommandData localInput = GetSystem<CommandSystem>().GetCommand();
        if (localInput == null)
        {
            GameLog.Error(GameLogChannel.Battle, "GGPO 记录指令失败：CommandData 为空。");
            GameTimeType = GameTimeType.WaitStop;
            return;
        }

        localInput.Tick = _localTick;
        localInput.EntityId = _ggpoLocalEntityId;
        GetSystem<CommandSystem>().RecodeCommand(localInput);
        GetSystem<ServerCommandSystem>().SendPlayerInput(localInput);

        List<CommandData> mergedInputs = _ggpoInputSynchronizer.BuildMergedInputs(_localTick, localInput);

        if (SessionProfile.RequiresLocalSnapshot)
        {
            TakeLocalSnapShotForGgpo(_localTick);
        }

        UpdateGameState(deltaTime, mergedInputs);

        if (_ggpoRollbackController.TryBeginRollback(_ggpoRemoteEntityId, _localTick, out uint firstErrorTick))
        {
            _ggpoRollbackController.LoadStateBeforeTick(this, firstErrorTick);
            while (_ggpoRollbackController.TryStepResimulate(
                       this,
                       deltaTime,
                       tick => GetSystem<CommandSystem>().GetRecodeCommand(tick),
                       out _))
            {
            }
        }
    }
}
