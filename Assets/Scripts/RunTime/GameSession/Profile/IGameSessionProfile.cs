/// <summary>
/// 游戏会话配置：描述当前世界的运行模式及其能力开关。
/// </summary>
public interface IGameSessionProfile
{
    /// <summary>会话模式类型（当前仅 Online）。</summary>
    GameSessionModeType Mode { get; }

    /// <summary>是否需要 GGPO 回滚。</summary>
    bool RequiresRollback { get; }

    /// <summary>是否拍摄本地快照。</summary>
    bool RequiresLocalSnapshot { get; }

    /// <summary>是否拍摄权威快照（GGPO 单模拟态下为 false）。</summary>
    bool RequiresAuthoritySnapshot { get; }

    /// <summary>GGPO 远端输入历史窗口帧数。</summary>
    uint InputHistoryWindow { get; }

    /// <summary>是否模拟丢包（调试）。</summary>
    bool SimulatePacketLoss { get; }

    /// <summary>实体同步策略。</summary>
    IEntitySyncPolicy EntitySyncPolicy { get; }
}
