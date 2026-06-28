/// <summary>
/// 回滚信息
/// </summary>
public class RollBackData : IPool
{
    /// <summary>
    /// 回滚类型
    /// </summary>
    private RollBackType _rollBackType = RollBackType.NoRollBack;
    
    public RollBackType RollBackType => _rollBackType;
    
    /// <summary>
    /// 获取实例
    /// </summary>
    /// <returns></returns>
    public static RollBackData Create()
    {
        return FPoolHelper.Get<RollBackData>();
    }
    
    /// <summary>
    /// 附加回滚类型
    /// </summary>
    public void AttachRollBackType(RollBackType type)
    {
        this._rollBackType |= type;
    }
        
    /// <summary>
    /// 移除回滚类型
    /// </summary>
    public void RemoveRollBackType(RollBackType type)
    {
        this._rollBackType ^= type;
    }
    
    /// <summary>
    /// 检查是否包含状态
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public bool CheckType(RollBackType type)
    {
        return (_rollBackType & type) == type;
    }

    public void Clear()
    {
        _rollBackType = RollBackType.NoRollBack;
    }
}
