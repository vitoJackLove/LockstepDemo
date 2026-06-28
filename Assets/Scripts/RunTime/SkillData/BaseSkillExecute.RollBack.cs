using Ase.Serializing;

/// <summary>
/// 预测回滚
/// </summary>
public partial class BaseSkillExecute
{
    /// <summary>
    /// 快照
    /// </summary>
    /// <param name="hardWriter"></param>
    /// <param name="softWriter"></param>
    public abstract void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter);

    /// <summary>
    /// 回滚
    /// </summary>
    /// <param name="authoritySnapShot"></param>
    public abstract void RollBackTo(PooledReader authoritySnapShot);
}
