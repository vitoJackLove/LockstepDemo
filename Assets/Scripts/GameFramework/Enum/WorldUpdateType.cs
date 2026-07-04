/// <summary>
/// 世界更新类型
/// </summary>
public enum WorldUpdateType : byte
{
    /// <summary>
    /// 本地更新
    /// </summary>
    Local = 1,
        
    /// <summary>
    /// 权威刷新
    /// </summary>
    Authority,
        
    /// <summary>
    /// 回滚
    /// </summary>
    RollBack,
}
