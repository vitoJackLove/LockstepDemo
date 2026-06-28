/// <summary>
/// 事件效果
/// </summary>
public abstract class EventEffect  : IPool
{
    /// <summary>
    /// 效果执行者
    /// </summary>
    protected BaseEntity Performer;
    
    /// <summary>
    /// 执行效果
    /// </summary>
    public abstract void ExecuteEffect();

    public abstract void Clear();
}
