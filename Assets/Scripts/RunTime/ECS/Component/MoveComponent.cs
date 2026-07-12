using Unity.Mathematics.FixedPoint;

/// <summary>
/// 移动组件：计算逻辑位移目标并写入 KCC Motor；最终位姿由 ECS FPKinematicCharacterSystem 校验后回写 FTransform。
/// Motor 由 <see cref="PhysicsBodyComponent"/> 在 CharacterController 范式下创建。
/// </summary>
public class MoveComponent : BaseComponent
{
    private FPKinematicCharacterMotor _motor;

    public FPKinematicCharacterMotor Motor => _motor;

    public override void OnStart(object data = null)
    {
        base.OnStart(data);

        _motor = Entity.GetComponent<PhysicsBodyComponent>()?.Motor;
        if (_motor == null)
        {
            GameLog.Error(GameLogChannel.Battle,
                $"MoveComponent requires PhysicsBodyComponent with CharacterController mode. entityId={Entity.EntityId}");
        }
    }

    public override void OnExecuteServerCommand(CommandData commandData)
    {
        base.OnExecuteServerCommand(commandData);

        Movement(commandData.MoveDir);
    }

    public override void OnExecuteLocalCommand(CommandData commandData)
    {
        base.OnExecuteLocalCommand(commandData);

        Movement(commandData.MoveDir);
    }

    public override void OnDispose()
    {
        _motor = null;
        base.OnDispose();
    }

    /// <summary>
    /// 设置运动开关
    /// </summary>
    public void SetMovementState(bool moveEnable, bool rotateEnable)
    {
        Entity.SetMovementState(moveEnable, rotateEnable);
    }

    private void Movement(fp3 moveDir)
    {
        if (_motor == null || (moveDir == fp3.zero).Bool3ToBool())
        {
            return;
        }

        fp3 currentPosition = Entity.transform.Position;
        fpquaternion currentRotation = Entity.transform.Rotation;

        if (Entity.MoveEnable)
        {
            fp3 movePoint = currentPosition + fpmath.normalizesafe(moveDir) *
                Entity.GetProperty(PropertyKey.Speed) * Entity.BaseWorld.LogicDeltaTime;

            _motor.SetMovePositionTarget(movePoint);
        }

        if (Entity.RotateEnable)
        {
            fpquaternion targetRotation = fpmath1.LookRotation(moveDir, fpmath1.up());
            fpquaternion moveRotation = fpmath1.slerp(currentRotation, targetRotation,
                (fp)20 * Entity.BaseWorld.LogicDeltaTime);

            _motor.SetMoveRotationTarget(moveRotation);
        }
    }
}
