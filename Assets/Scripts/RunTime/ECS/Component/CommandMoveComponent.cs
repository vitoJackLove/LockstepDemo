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
        if ((moveDir == fp3.zero).Bool3ToBool())
        {
            return;
        }
        
        if (Entity.MoveEnable)
        {
            fp3 movePoint = Entity.transform.Position + fpmath.normalizesafe(moveDir) *
                Entity.GetProperty(PropertyKey.Speed) * Entity.BaseWorld.LogicDeltaTime;

            Entity.transform.Position = movePoint;
        }
        
        if (Entity.RotateEnable)
        {
            fpquaternion tempValue  = fpmath1.LookRotation(moveDir,fpmath1.up());

            Entity.transform.Rotation = fpmath1.slerp(Entity.transform.Rotation, tempValue, 
                (fp)20 * Entity.BaseWorld.LogicDeltaTime);
        }
    }
}
