/// <summary>
/// 联机会话配置：严格 GGPO，单模拟态 + 快照 + 回滚。
/// </summary>
public sealed class OnlineGameSessionProfile : IGameSessionProfile
{
    private readonly UnifiedEntitySyncPolicy _entitySyncPolicy = new UnifiedEntitySyncPolicy();

    /// <inheritdoc />
    public GameSessionModeType Mode => GameSessionModeType.Online;

    /// <inheritdoc />
    public bool RequiresRollback => true;

    /// <inheritdoc />
    public bool RequiresLocalSnapshot => true;

    /// <inheritdoc />
    public bool RequiresAuthoritySnapshot => false;

    /// <inheritdoc />
    public uint InputHistoryWindow { get; set; } = 8;

    /// <inheritdoc />
    public bool SimulatePacketLoss => false;

    /// <inheritdoc />
    public IEntitySyncPolicy EntitySyncPolicy => _entitySyncPolicy;
}
