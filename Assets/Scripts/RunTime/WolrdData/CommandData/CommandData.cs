using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Mathematics.FixedPoint;

public class CommandData : IPool
{
    public int EntityId;

    public uint Tick;

    public uint ClientSeq;

    public fp3 MoveDir;

    public fp3 SkillDir;

    public CommandType CommandType = CommandType.None;

    public WorldContent.CommandExecuteState CommandState = WorldContent.CommandExecuteState.Null;

    public static CommandData Create()
    {
        CommandData data = new CommandData();

        return data;
    }

    public void TestLossPacket()
    {
        MoveDir = fp3.zero;
        SkillDir = fp3.zero;
        CommandType = CommandType.None;
        CommandState = WorldContent.CommandExecuteState.Null;
    }

    public bool IsSkillPacket()
    {
        return CommandType != CommandType.None && CommandState == WorldContent.CommandExecuteState.OnlyDown;
    }

    public bool IsNullCommand()
    {
        bool3 value = (MoveDir == fp3.zero);

        if (value.Bool3ToBool(true) && CommandType == CommandType.None && CommandState == WorldContent.CommandExecuteState.Null)
        {
            return true;
        }

        return false;
    }

    public static void Release(CommandData data)
    {
        FPoolHelper.Release<CommandData>(data);
    }

    public void Clear()
    {
        EntityId = 0;
        Tick = 0;
        ClientSeq = 0;
        MoveDir = fp3.zero;
        SkillDir = fp3.zero;
        CommandType = CommandType.None;
        CommandState = WorldContent.CommandExecuteState.Null;
    }
}
