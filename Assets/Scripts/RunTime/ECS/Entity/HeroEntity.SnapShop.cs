using Ase.Serializing;
using Unity.Mathematics.FixedPoint;

public partial class HeroEntity
{
    public override void TakeSnapShot(uint tick, BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
    {
        base.TakeSnapShot(tick, hardWriter, softWriter);

        fp hp = GetProperty(PropertyKey.Hp);

        softWriter.WriterFpData($"Hp", hp);
    }

    public override void SoftRollBack(PooledReader authoritySnapShot)
    {
        base.SoftRollBack(authoritySnapShot);

        fp authorityHp = BaseSnapShotData.ReadFpData(authoritySnapShot);

        SetProperty(PropertyKey.Hp, authorityHp);
    }
}
