using System;

/// <summary>
/// 回滚类型
/// </summary>
[Flags]
public enum RollBackType 
{
    /// <summary>
    /// 不需要回滚
    /// </summary>
    NoRollBack,
    
    /// <summary>
    /// 软回滚
    /// </summary>
    SoftRollBack,
    
    /// <summary>
    /// 硬回滚
    /// </summary>
    HardRollBack,
}
