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