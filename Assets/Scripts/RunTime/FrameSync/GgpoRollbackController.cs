using System;
using System.Collections.Generic;
using Unity.Mathematics.FixedPoint;
/// <summary>
/// GGPO 回滚控制器：对应 ggpo_advance_frame，在远端真实输入到达后比对预测并回滚重放。
/// </summary>
public sealed class GgpoRollbackController
{
    private readonly GgpoInputSynchronizer _inputSynchronizer;
    private readonly GgpoRemoteInputBuffer _remoteInputBuffer;

    private bool _syncTestMode;
    private bool _isResimulating;
    private uint _resimulateTick;
    private uint _resimulateEndTick;

    public GgpoRollbackController(GgpoInputSynchronizer inputSynchronizer, GgpoRemoteInputBuffer remoteInputBuffer)
    {
        _inputSynchronizer = inputSynchronizer;
        _remoteInputBuffer = remoteInputBuffer;
    }

    public bool IsResimulating => _isResimulating;

    public bool SyncTestMode
    {
        get => _syncTestMode;
        set => _syncTestMode = value;
    }

    /// <summary>
    /// 扫描新确认输入，必要时触发回滚。
    /// </summary>
    public bool TryBeginRollback(int remoteEntityId, uint currentTick, out uint firstErrorTick)
    {
        firstErrorTick = 0;

        if (_syncTestMode)
        {
            firstErrorTick = currentTick > 0 ? currentTick : 1;
            BeginResimulate(firstErrorTick, currentTick);
            return true;
        }

        uint scanStart = _isResimulating ? _resimulateTick : 1;
        for (uint tick = scanStart; tick <= currentTick; tick++)
        {
            if (!_remoteInputBuffer.TryGetConfirmedInput(remoteEntityId, tick, out CommandData confirmedRemote))
            {
                continue;
            }

            if (!_inputSynchronizer.TryGetPredictedRemoteInput(tick, out CommandData predictedRemote))
            {
                continue;
            }

            if (IsSameCommand(predictedRemote, confirmedRemote))
            {
                continue;
            }

            firstErrorTick = tick;
            BeginResimulate(firstErrorTick, currentTick);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 单步重放（在 FixedUpdate 中循环调用直到追上 currentTick）。
    /// </summary>
    public bool TryStepResimulate(
        BaseWorld world,
        fp deltaTime,
        Func<uint, CommandData> localInputProvider,
        out bool completed)    {
        completed = false;
        if (!_isResimulating)
        {
            return false;
        }

        if (_resimulateTick > _resimulateEndTick)
        {
            _isResimulating = false;
            completed = true;
            return false;
        }

        CommandData localInput = localInputProvider(_resimulateTick);
        List<CommandData> merged = _inputSynchronizer.BuildMergedInputs(_resimulateTick, localInput);

        if (world.SessionProfile.RequiresLocalSnapshot)
        {
            world.TakeLocalSnapShotForGgpo(_resimulateTick);
        }

        world.UpdateGameState(deltaTime, merged);
        _resimulateTick++;
        return true;
    }

    private void BeginResimulate(uint firstErrorTick, uint endTick)
    {
        _isResimulating = true;
        _resimulateTick = firstErrorTick;
        _resimulateEndTick = endTick;
        _inputSynchronizer.RemovePredictionsFromTick(firstErrorTick);
    }

    /// <summary>
    /// 加载 firstErrorTick 之前的状态。
    /// </summary>
    public void LoadStateBeforeTick(BaseWorld world, uint firstErrorTick)
    {
        if (firstErrorTick == 0)
        {
            return;
        }

        world.RollBackToTick(firstErrorTick - 1);
    }

    private static bool IsSameCommand(CommandData a, CommandData b)
    {
        if (a == null || b == null)
        {
            return false;
        }

        return a.CommandType == b.CommandType
               && a.CommandState == b.CommandState
               && a.MoveDir.Equals(b.MoveDir)
               && a.SkillDir.Equals(b.SkillDir);
    }
}
