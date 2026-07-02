using Unity.Mathematics.FixedPoint;

/// <summary>
/// 指令移动组件
/// </summary>
public class CommandMoveComponent : BaseComponent
{
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
    
    private void Movement(fp3 moveDir)
    {
        moveDir.y = fp.zero;

        if ((moveDir == fp3.zero).Bool3ToBool())
        {
            return;
        }

        fp3 normalizedDir = fpmath.normalizesafe(moveDir);
        normalizedDir.y = fp.zero;

        if (Entity.MoveEnable)
        {
            fp3 movePoint = Entity.transform.Position + normalizedDir *
                Entity.GetProperty(PropertyKey.Speed) * Entity.BaseWorld.LogicDeltaTime;
            movePoint.y = Entity.transform.Position.y;
            Entity.transform.Position = movePoint;
        }
        
        if (Entity.RotateEnable)
        {
            fpquaternion tempValue = fpmath1.LookRotation(normalizedDir, fpmath1.up());

            Entity.transform.Rotation = fpmath1.slerp(Entity.transform.Rotation, tempValue, 
                (fp)20 * Entity.BaseWorld.LogicDeltaTime);
        }
    }
}
