using System.Collections.Generic;

/// <summary>
/// 技能的打断窗口
/// </summary>
public partial class SkillComponent
{
    /// <summary>
    /// 指令缓存时间
    /// </summary>
    private Dictionary<CommandType, CommandCacheData> _commandCacheTime;

    /// <summary>
    /// 初始化指令数据
    /// </summary>
    private void InitCommandData()
    {
        _commandCacheTime = new Dictionary<CommandType, CommandCacheData>();

        CreateCommandData(30, CommandType.Attack);
        CreateCommandData(30, CommandType.SKill);
        CreateCommandData(30, CommandType.Roll);
    }

    /// <summary>
    /// 创建指令数据
    /// </summary>
    /// <param name="cacheTick"></param>
    /// <param name="heroSkillTypeEnum"></param>
    private void CreateCommandData(int cacheTick, CommandType heroSkillTypeEnum)
    {
        CommandCacheData commandCacheData = FPoolHelper.Get<CommandCacheData>();
        
        commandCacheData.InitData(cacheTick, heroSkillTypeEnum);

        _commandCacheTime.Add(heroSkillTypeEnum, commandCacheData);
    }
    
    /// <summary>
    /// 获取指令数据
    /// </summary>
    /// <returns></returns>
    private CommandCacheData GetCommandCacheData(CommandType commandType)
    {
        if (_commandCacheTime == null)
        {
            return null;
        }

        _commandCacheTime.TryGetValue(commandType, out var data);

        return data;
    }
    
    /// <summary>
    /// 设置指令的打断窗口
    /// </summary>
    /// <param name="commandType"></param>
    /// <param name="breakCommandList"></param>
    /// <param name="openBreak"></param>
    public void SetCommandBreak(CommandType commandType, List<CommandType> breakCommandList, bool openBreak)
    {
        CommandCacheData data = GetCommandCacheData(commandType);

        if (data == null)
        {
            return;
        }

        data.SetCommandBreak(breakCommandList, openBreak);
    }
    
    /// <summary>
    /// 技能的打断窗口是否打开
    /// </summary>
    /// <param name="commandType"></param>
    /// <param name="breakHeroSkillTypeEnum"></param>
    /// <returns></returns>
    private bool CommandBreakWindowIsOpen(CommandType commandType, CommandType breakHeroSkillTypeEnum)
    {
        CommandCacheData data = GetCommandCacheData(commandType);

        if (data == null)
        {
            return false;
        }

        return data.CommandBreakWindowIsOpen(breakHeroSkillTypeEnum);
    }
    
    /// <summary>
    /// 刷新指令的缓存时间
    /// </summary>
    /// <param name="commandType"></param>
    /// <param name="commandState"></param>
    private CommandCacheData RefreshCacheTime(CommandType commandType, WorldContent.CommandExecuteState commandState)
    {
        CommandCacheData data = GetCommandCacheData(commandType);

        if (data == null)
        {
            return null;
        }
            
        data.RefreshTime(commandState);

        return data;
    }
    
    /// <summary>
    /// 修改指令缓存时间
    /// </summary>
    public void AmendCommandCacheTime(CommandType commandType, int cacheTick)
    {
        CommandCacheData data = GetCommandCacheData(commandType);

        if (data == null)
        {
            return;
        }

        data.AmendCommandCacheTime(cacheTick);
    }
    
    /// <summary>
    /// 还原缓存时间
    /// </summary>
    /// <param name="commandType"></param>
    public void RestoreCacheTime(CommandType commandType)
    {
        CommandCacheData data = GetCommandCacheData(commandType);

        if (data == null)
        {
            return;
        }

        data.RestoreCacheTime();
    }
    
    /// <summary>
    /// 指令是否缓存
    /// </summary>
    /// <param name="commandType"></param>
    /// <param name="commandState"></param>
    /// <returns></returns>
    public bool CommandIsCache(CommandType commandType, WorldContent.CommandExecuteState commandState)
    {
        CommandCacheData data = GetCommandCacheData(commandType);

        if (data == null)
        {
            return false;
        }

        if (commandState == WorldContent.CommandExecuteState.OnlyDown)
        {
            return data.IsDown;
        }
        else if (commandState == WorldContent.CommandExecuteState.OnlyUp)
        {
            return data.IsUp;
        }

        return false;
    }
    
    /// <summary>
    /// 清空指令缓存
    /// </summary>
    /// <param name="commandType"></param>
    public void ClearCommandCache(CommandType commandType)
    {
        CommandCacheData data = GetCommandCacheData(commandType);

        if (data == null)
        {
            return;
        }
            
        data.ClearCommandCache();
    }
}
