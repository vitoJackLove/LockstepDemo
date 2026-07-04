public enum GameSessionModeType
{
    Online,
    SinglePlayer,
}

public static class GameSessionMode
{
    /// <summary>
    /// 全局会话模式缓存，仅供 UI 层在切换模式时使用。
    /// 世界实例以 CreateWorldData.SessionProfile 为准。
    /// </summary>
    public static GameSessionModeType Current { get; set; } = GameSessionModeType.SinglePlayer;

    public static bool IsSinglePlayer => Current == GameSessionModeType.SinglePlayer;
}
