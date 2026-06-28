using Ase.Serializing;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// 直线位移
/// </summary>
public class LineDisplacement : BaseDisplacement
{
    /// <summary>
    /// 位移的方向
    /// </summary>
    private fp3 _moveDir;

    /// <summary>
    /// 位移速度
    /// </summary>
    private fp _moveSpeed;
    
    public void InitData(int time, fp speed, fp3 dir, int delay = 0)
    {
        MoveTime = time;
        MoveDelay = delay;
        _moveDir = fpmath.normalize(dir);
        _moveSpeed = speed;
    }

    protected override void ComputePosition(fp deltaTime)
    {
        base.ComputePosition(deltaTime);

        BaseEntity.transform.Position += (_moveSpeed * deltaTime) * _moveDir;
    }

    public override void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
    {
        base.TakeSnapShot(hardWriter, softWriter);

        hardWriter.WriterFp3Data($"_moveDir", _moveDir);
        hardWriter.WriterFpData($"_moveSpeed", _moveSpeed);
    }

    public override void HardRollBackTo(PooledReader authoritySnapShot)
    {
        base.HardRollBackTo(authoritySnapShot);

        _moveDir = BaseSnapShotData.ReadFp3Data(authoritySnapShot);
        _moveSpeed = BaseSnapShotData.ReadFpData(authoritySnapShot);
    }
}
