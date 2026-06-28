/// <summary>
/// 单机会话配置：本地回环帧同步，禁用快照与回滚，使用统一实体策略。
/// </summary>
public sealed class SinglePlayerGameSessionProfile : IGameSessionProfile
{
    private readonly UnifiedEntitySyncPolicy _entitySyncPolicy = new UnifiedEntitySyncPolicy();

    /// <inheritdoc />
    public GameSessionModeType Mode => GameSessionModeType.SinglePlayer;

    /// <inheritdoc />
    public bool RequiresRollback => false;

    /// <inheritdoc />
    public bool RequiresLocalSnapshot => false;

    /// <inheritdoc />
    public bool RequiresAuthoritySnapshot => false;

    /// <inheritdoc />
    public uint ForecastTick => 0;

    /// <inheritdoc />
    public bool SimulatePacketLoss => false;

    /// <inheritdoc />
    public IEntitySyncPolicy EntitySyncPolicy => _entitySyncPolicy;
}
