using System.Collections.Generic;
using Unity.Mathematics.FixedPoint;

/// <summary>
/// world GGPO 回滚（严格单模拟态，无权威轨）。
/// </summary>
public partial class BaseWorld
{
    /// <summary>
    /// 输入历史窗口大小（替代原 forecastTick 语义）。
    /// </summary>
    private uint _inputHistoryWindow;

    /// <summary>
    /// 模拟丢包率（调试）。
    /// </summary>
    private float _lossPacket;

    /// <summary>
    /// 初始化 GGPO 回滚参数。
    /// </summary>
    public void InitRollBackData(uint inputHistoryWindow, float lossPacket)
    {
        _inputHistoryWindow = inputHistoryWindow;
        _lossPacket = lossPacket;
    }

    private void TakeLocalSnapShot()
    {
        TakeLocalSnapShotForGgpo(_localTick);
        GameLog.Debug(GameLogChannel.Rollback, $"<color=blue>本地帧 {_localTick} 快照完成</color>");
    }

    private void RemoveLocalSnapShot(uint startErrorTick, RollBackType rollbackType)
    {
        foreach (BaseSystem system in _systemDic.Values)
        {
            system.RemoveLocalSnapShot(startErrorTick, rollbackType);
        }
    }

    private void RollBack(uint tick, RollBackType rollBackType)
    {
        if (tick == 0)
        {
            return;
        }

        foreach (BaseSystem system in _systemDic.Values)
        {
            system.RollBack(tick, rollBackType);
        }
    }
}
