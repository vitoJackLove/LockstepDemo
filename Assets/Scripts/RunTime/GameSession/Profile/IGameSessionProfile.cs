/// <summary>
/// 游戏会话配置：描述当前世界的运行模式及其能力开关。
/// 联机与单机通过不同 Profile 实现，避免在业务代码中散落模式判断。
/// </summary>
public interface IGameSessionProfile
{
    /// <summary>会话模式类型（联机 / 单机）。</summary>
    GameSessionModeType Mode { get; }

    /// <summary>是否需要预测回滚（单机为 false）。</summary>
    bool RequiresRollback { get; }

    /// <summary>是否拍摄本地快照（单机为 false，成本过高）。</summary>
    bool RequiresLocalSnapshot { get; }

    /// <summary>是否拍摄权威快照（单机为 false）。</summary>
    bool RequiresAuthoritySnapshot { get; }

    /// <summary>预测窗口帧数（单机为 0，权威帧当帧到达）。</summary>
    uint ForecastTick { get; }

    /// <summary>是否模拟丢包（仅联机调试使用）。</summary>
    bool SimulatePacketLoss { get; }

    /// <summary>实体同步策略，决定 Local/Authority 实体如何配对。</summary>
    IEntitySyncPolicy EntitySyncPolicy { get; }
}
