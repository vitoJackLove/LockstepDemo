/// <summary>
/// 事件的条件
/// </summary>
public abstract class EventCondition : IPool
{
    /// <summary>
    /// 检查条件
    /// </summary>
    /// <returns></returns>
    public abstract bool CheckCondition();

    public abstract void Clear();
}
