using Ase.Serializing;
using Unity.Mathematics.FixedPoint;

[ClipName("Move To Position")]
public class MoveToPositionClip : TaskClip
{
    public fp3 targetPosition;

    private fp _speed;

    public override void OnRunTimeEnter(BaseEntity context, int fps)
    {
        base.OnRunTimeEnter(context, fps);

        fp distance = fpmath.distance(context.transform.Position, targetPosition);

        _speed = distance / taskDuration;

        fp3 dir = targetPosition - context.transform.Position;

        fpquaternion quaternion = fpmath1.LookRotation(dir, fpmath1.up());

        context.transform.Rotation = quaternion;
    }

    public override void RollBackEnter(BaseEntity context, int fps)
    {
        base.RollBackEnter(context, fps);

        fp distance = fpmath.distance(context.transform.Position, targetPosition);

        _speed = distance / taskDuration;

        fp3 dir = targetPosition - context.transform.Position;

        fpquaternion quaternion = fpmath1.LookRotation(dir, fpmath1.up());

        context.transform.Rotation = quaternion;
    }

    public override void RunTimeTick(int currentFrameID, int fps, fp deltaTime, BaseEntity context)
    {
        base.RunTimeTick(currentFrameID, fps, deltaTime, context);

        if (context.MoveEnable)
        {
            context.EntityDebug($"MoveToPosition before={context.transform.Position} speed={_speed} forward={context.transform.Forward}");

            context.transform.Position = (context.transform.Forward * _speed) + context.transform.Position;
        }
    }

    public override void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
    {
        base.TakeSnapShot(hardWriter, softWriter);

        hardWriter.WriterFpData("MoveToPositionClip Speed", _speed);
    }

    public override void RollBackTo(PooledReader authoritySnapShot)
    {
        base.RollBackTo(authoritySnapShot);

        _speed = BaseSnapShotData.ReadFpData(authoritySnapShot);
    }
}
