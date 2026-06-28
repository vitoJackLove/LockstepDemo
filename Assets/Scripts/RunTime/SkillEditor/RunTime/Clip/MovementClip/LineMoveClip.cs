using Ase.Serializing;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

[ClipName("Line Move")]
public class LineMoveClip : TaskClip
{
    [VariableName("Direction Offset")]
    public float deg;

    [VariableName("Delay Time")]
    public float delayTime;

    [VariableName("Distance")]
    public float distance;

    private fp3 _initPosition;

    private fp _speed;

    private fp3 _dir;

    private void Init(BaseEntity context, int fps)
    {
        _initPosition = context.transform.Position;

        _speed = (fp)(distance / (taskDuration * (1f / fps)));

        _dir = TsUtil.MoveForward2D(context.transform.Position, (fp)deg + context.transform.Rotation.ToEulerAngles().y, (fp)distance) -
               context.transform.Position;

        _dir = fpmath.normalize(_dir);
    }

    public override void EditorEnter(GameObject context, int fps, int currentFrameID)
    {
        base.EditorEnter(context, fps, currentFrameID);

        if (context == null)
        {
            return;
        }

        _initPosition = fpmath1.Vector3ToFp3(context.transform.position);

        _speed = (fp)(distance / (taskDuration * (1f / fps)));

        _dir = TsUtil.MoveForward2D(_initPosition,
            (fp)(deg + context.transform.localEulerAngles.y), (fp)distance) - _initPosition;

        _dir = fpmath.normalize(_dir);
    }

    public override void OnRunTimeEnter(BaseEntity context, int fps)
    {
        base.OnRunTimeEnter(context, fps);

        Init(context, fps);
    }

    public override void RollBackEnter(BaseEntity context, int fps)
    {
        base.RollBackEnter(context, fps);

        Init(context, fps);
    }

    public override void RunTimeTick(int currentFrameID, int fps, fp deltaTime, BaseEntity context)
    {
        context.transform.Position = _initPosition + _speed * deltaTime * currentFrameID * _dir;
    }

    public override void EditorTick(int currentFrameID, int fps, fp deltaTime, GameObject context)
    {
        if (context == null)
        {
            return;
        }

        context.transform.position = fpmath1.Fp3ToVector3(_initPosition + _speed *
            deltaTime * currentFrameID * _dir);
    }

    public override void EditorExit(GameObject context, int fps, int currentFrameID)
    {
        base.EditorExit(context, fps, currentFrameID);

        if (context == null)
        {
            return;
        }

        context.transform.position = fpmath1.Fp3ToVector3(_initPosition);

        _initPosition = fp3.zero;
    }

    public override void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
    {
        base.TakeSnapShot(hardWriter, softWriter);

        hardWriter.WriterFpData("LineMove Speed", _speed);
        hardWriter.WriterFp3Data("LineMove Init Position", _initPosition);
        hardWriter.WriterFp3Data("LineMove Direction", _dir);
    }

    public override void RollBackTo(PooledReader authoritySnapShot)
    {
        base.RollBackTo(authoritySnapShot);

        _speed = BaseSnapShotData.ReadFpData(authoritySnapShot);
        _initPosition = BaseSnapShotData.ReadFp3Data(authoritySnapShot);
        _dir = BaseSnapShotData.ReadFp3Data(authoritySnapShot);
    }
}
