/// <summary>
/// 联机会话配置：启用完整帧同步、快照、回滚与双实体同步。
/// </summary>
public sealed class OnlineGameSessionProfile : IGameSessionProfile
{
    private readonly DualEntitySyncPolicy _entitySyncPolicy = new DualEntitySyncPolicy();

    /// <inheritdoc />
    public GameSessionModeType Mode => GameSessionModeType.Online;

    /// <inheritdoc />
    public bool RequiresRollback => true;

    /// <inheritdoc />
    public bool RequiresLocalSnapshot => true;

    /// <inheritdoc />
    public bool RequiresAuthoritySnapshot => true;

    /// <summary>
    /// 联机预测窗口，实际值在 BaseWorld 初始化时由 RollBack 配置覆盖。
    /// </summary>
    public uint ForecastTick { get; set; } = 2;

    /// <inheritdoc />
    public bool SimulatePacketLoss => true;

    /// <inheritdoc />
    public IEntitySyncPolicy EntitySyncPolicy => _entitySyncPolicy;
}
