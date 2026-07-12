using System.Collections.Generic;
using Ase.Serializing;
using Sirenix.OdinInspector;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// 位移组件 用于实体自定义一段位移 
/// </summary>
public class DisplacementComponent : BaseComponent
{
    /// <summary>
    /// 正在运行的运动
    /// </summary>
    private BaseDisplacement _runTimeCommand;

    public override void OnStart(object data = null)
    {
        base.OnStart(data);
        
        var movementData = Entity.GetData<MovementData>(ComponentDataKey.MovementData);

        if (movementData == null)
        {
            return;
        }
        
        CreateMove(movementData.MoveTick, movementData.MovementSpeed, movementData.MoveType);
    }

    /// <summary>
    /// 创建一个位移
    /// </summary>
    /// <param name="moveTick">位移时间</param>
    /// <param name="moveSpeed">位移速度</param>
    /// <param name="delay">位移前的延迟</param>
    /// <param name="displacementEnum">位移类型</param>
    private void CreateMove(int moveTick, fp moveSpeed, DisplacementEnum displacementEnum, int delay = 0)
    {
        LineDisplacement displacement = BaseDisplacement.Create<LineDisplacement>(Entity, displacementEnum);

        displacement.InitData(moveTick, moveSpeed, Entity.transform.Forward, delay);

        _runTimeCommand = displacement;
    }

    public override void OnFixedUpdate(fp deltaTime, WorldUpdateType worldUpdateType)
    {
        base.OnFixedUpdate(deltaTime, worldUpdateType);

        if (_runTimeCommand != null)
        {
            _runTimeCommand.StartUp();
            
            _runTimeCommand.FixUpdate(deltaTime);

            if (_runTimeCommand.RunTimeState == DisplacementState.Exit)
            {
                _runTimeCommand = null;
            }
        }
    }

    public override void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
    {
        base.TakeSnapShot(hardWriter, softWriter);

        if (_runTimeCommand == null)
        {
            hardWriter.WriterBoolData($"是否存在运行的运动", false);
        }
        else
        {
            hardWriter.WriterBoolData($"是否存在运行的运动", true);
            
            _runTimeCommand.TakeSnapShot(hardWriter, softWriter);
        }
    }

    public override void HardRollBackTo(PooledReader authoritySnapShot)
    {
        base.HardRollBackTo(authoritySnapShot);

        bool haveDisplacement = authoritySnapShot.ReadBoolean();

        if (haveDisplacement)
        {
            _runTimeCommand.HardRollBackTo(authoritySnapShot);
        }
        else
        {
            _runTimeCommand = null;
        }
    }
}

/// <summary>
/// 位移类型
/// </summary>
public enum DisplacementEnum
{
    [LabelText("直线运动")]
    Line,
}

public class MovementData : IPool
{
    public int MoveTick;
    public fp MovementSpeed;
    public DisplacementEnum MoveType;

    public static MovementData Create(int moveTick,fp movementSpeed,DisplacementEnum moveType)
    {
        MovementData data = FPoolHelper.Get<MovementData>();
        
        data.MovementSpeed = movementSpeed;
        data.MoveTick = moveTick;
        data.MoveType = moveType;

        return data;
    }
    
    public void Clear()
    {
        MoveTick = 0;
        MovementSpeed = 0;
        MoveType = DisplacementEnum.Line;
    }
}

/// <summary>
/// 场景调试移动平台：沿指定方向往复运动，驱动实体 Transform 供 FPPhysicsMover 读取。
/// </summary>
public sealed class SceneDebugPlatformMotionComponent : BaseComponent
{
    private fp3 _moveDirection = new fp3((fp)1, (fp)0, (fp)0);
    private fp _moveSpeed = (fp)2;
    private fp _travelDistance = (fp)5;
    private fp _traveled;
    private int _directionSign = 1;
    private bool _configured;

    public override void OnStart(object data = null)
    {
        base.OnStart(data);
        SceneDebugPlatformMotionData motionData =
            Entity.GetData<SceneDebugPlatformMotionData>(ComponentDataKey.SceneDebugPlatformMotion);
        if (motionData != null)
        {
            ApplyMotionData(motionData);
        }
    }

    public void ApplyMotionData(SceneDebugPlatformMotionData motionData)
    {
        if (motionData == null || Entity?.transform == null)
        {
            return;
        }

        _moveDirection = NormalizeDirection(ToFp3(motionData.moveDirection));
        _moveSpeed = (fp)Mathf.Max(0f, motionData.moveSpeed);
        _travelDistance = (fp)Mathf.Max(0.01f, motionData.travelDistance);
        _traveled = (fp)0;
        _directionSign = 1;
        _configured = _moveSpeed > (fp)0;
    }

    public bool TryReadMotionData(out SceneDebugPlatformMotionData motionData)
    {
        motionData = new SceneDebugPlatformMotionData
        {
            moveDirection = fpmath1.Fp3ToVector3(_moveDirection),
            moveSpeed = (float)_moveSpeed,
            travelDistance = (float)_travelDistance,
        };
        return _configured;
    }

    public override void OnFixedUpdate(fp deltaTime, WorldUpdateType worldUpdateType)
    {
        base.OnFixedUpdate(deltaTime, worldUpdateType);
        if (!_configured || Entity?.transform == null || _moveSpeed <= (fp)0)
        {
            return;
        }

        fp step = _moveSpeed * deltaTime * (fp)_directionSign;
        Entity.transform.Position += _moveDirection * step;

        _traveled += fpmath.abs(step);
        if (_traveled >= _travelDistance)
        {
            _traveled = (fp)0;
            _directionSign = -_directionSign;
        }
    }

    private static fp3 NormalizeDirection(fp3 direction)
    {
        if (fpmath.lengthsq(direction) <= (fp)0)
        {
            return new fp3((fp)1, (fp)0, (fp)0);
        }

        return fpmath.normalize(direction);
    }

    private static fp3 ToFp3(Vector3 value)
    {
        return new fp3((fp)value.x, (fp)value.y, (fp)value.z);
    }
}

/// <summary>
/// 场景调试移动平台往复运动参数。
/// </summary>
public sealed class SceneDebugPlatformMotionData
{
    public Vector3 moveDirection = Vector3.right;
    public float moveSpeed = 2f;
    public float travelDistance = 5f;
}