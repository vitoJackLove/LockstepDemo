using Ase.Serializing;

/// <summary>
/// 预测回滚
/// </summary>
public partial class BaseSkillData
{
    public void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
    {
        BaseSkillExecute.TakeSnapShot(hardWriter,softWriter);
    }

    public void RollBackTo(PooledReader authoritySnapShot)
    {
        BaseSkillExecute.RollBackTo(authoritySnapShot);
    }
}