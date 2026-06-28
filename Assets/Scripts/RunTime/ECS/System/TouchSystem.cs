using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// 输入系统
/// </summary>
public class TouchSystem : BaseSystem
{
    private CommandSystem _commandSystem;

    public override void OnInit(object data = null)
    {
        base.OnInit(data);

        _commandSystem = GetSystem<CommandSystem>();
    }

    public override void OnUpdate(fp deltaTime)
    {
        base.OnUpdate(deltaTime);

        var command = CommandData.Create();

        fp2 moveDir = fp2.zero;
        
        if (Input.GetKey(KeyCode.W))
        {
            moveDir += new fp2(0, 1);
        }
        
        if (Input.GetKey(KeyCode.A))
        {
            moveDir += new fp2(-1, 0);
        }
        
        if (Input.GetKey(KeyCode.S))
        {
            moveDir += new fp2(0, -1);
        }
        
        if (Input.GetKey(KeyCode.D))
        {
            moveDir += new fp2(1, 0);
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            command.CommandType = CommandType.Attack;
            command.CommandState = WorldContent.CommandExecuteState.OnlyDown;
            _commandSystem.CollectCommand(command);
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            command.CommandType = CommandType.SKill;
            command.CommandState = WorldContent.CommandExecuteState.OnlyDown;
            _commandSystem.CollectCommand(command);
        }
        
        if (Input.GetKeyUp(KeyCode.J))
        {
            command.CommandType = CommandType.Attack;
            command.CommandState = WorldContent.CommandExecuteState.OnlyUp;
            _commandSystem.CollectCommand(command);
        }

        if (Input.GetKeyUp(KeyCode.K))
        {
            command.CommandType = CommandType.SKill;
            command.CommandState = WorldContent.CommandExecuteState.OnlyUp;
            _commandSystem.CollectCommand(command);
        }
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            command.CommandType = CommandType.Roll;
            command.CommandState = WorldContent.CommandExecuteState.OnlyDown;
            _commandSystem.CollectCommand(command);
        }

        _commandSystem.UpdateInputUv(moveDir);
    }
}
