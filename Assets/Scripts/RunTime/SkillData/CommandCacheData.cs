using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 指令缓存数据
/// </summary>
public partial class CommandCacheData : IPool
{
    /// <summary>
    /// 指令类型
    /// </summary>
    private CommandType _heroSkillTypeEnum;

    public CommandType CommandType => _heroSkillTypeEnum;

    /// <summary>
    /// 按下
    /// </summary>
    private bool _down;

    /// <summary>
    /// 抬起
    /// </summary>
    private bool _up;
    
    /// <summary>
    /// 缓存时间
    /// </summary>
    private int _cacheTick;

    /// <summary>
    /// 当前按下缓存时间
    /// </summary>
    private int _currentDownCacheTick;

    /// <summary>
    /// 当前抬起缓存时间
    /// </summary>
    private int _currentUpCacheTick;

    /// <summary>
    /// 指令可接受窗口
    /// </summary>
    private bool _openCommandReceive;

    /// <summary>
    /// 初始的缓存Tick
    /// </summary>
    private int _initCacheTick;

    /// <summary>
    /// 设置可以打断改指令的指令集合
    /// </summary>
    private List<CommandType> _breakSkillTypeEnumList;

    /// <summary>
    /// 是否时蓄力技能
    /// </summary>
    private bool _isChargeSkill;

    public void InitData(int cacheTick, CommandType heroSkillTypeEnum)
    {
        this._cacheTick = cacheTick;
        this._initCacheTick = cacheTick;
        this._heroSkillTypeEnum = heroSkillTypeEnum;
        _openCommandReceive = true;
        _breakSkillTypeEnumList = new List<CommandType>();

        if (heroSkillTypeEnum == CommandType.SKill)
        {
            _isChargeSkill = true;
        }
    }

    public void FixedUpdate()
    {
        _currentDownCacheTick = Mathf.Clamp(_currentDownCacheTick--, 0, _currentDownCacheTick);

        if (_currentDownCacheTick <= 0)
        {
            _currentDownCacheTick = _cacheTick;
            
            _down = false;
        }
        
        _currentUpCacheTick = Mathf.Clamp(_currentUpCacheTick--, 0, _currentUpCacheTick);

        if (_currentUpCacheTick <= 0)
        {
            _currentUpCacheTick = _cacheTick;
            
            _up = false;
        }
    }

    public void RefreshTime(WorldContent.CommandExecuteState commandType)
    {
        if (commandType == WorldContent.CommandExecuteState.OnlyDown)
        {
            _currentDownCacheTick = _cacheTick;
            
            _down = true;
        }
        else if(commandType == WorldContent.CommandExecuteState.OnlyUp)
        {
            _currentUpCacheTick = _cacheTick;
            
            _up = true;
        }
    }

    /// <summary>
    /// 修改指令缓存时间
    /// </summary>
    public void AmendCommandCacheTime(int cacheTick)
    {
        _cacheTick = cacheTick;
    }

    /// <summary>
    /// 还原缓存时间
    /// </summary>
    public void RestoreCacheTime()
    {
        _cacheTick = _initCacheTick;
    }

    public void ClearCommandCache()
    {
        _down = false;
        _up = false;
    }

    /// <summary>
    /// 设置指令打断
    /// </summary>
    /// <param name="breakCommandList"></param>
    /// <param name="openBreak"></param>
    public bool SetCommandBreak(List<CommandType> breakCommandList, bool openBreak)
    {
        if (breakCommandList == null || breakCommandList.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < breakCommandList.Count; i++)
        {
            var heroSkillTypeEnum = breakCommandList[i];
            
            if (openBreak)
            {
                if (!_breakSkillTypeEnumList.Contains(heroSkillTypeEnum))
                {
                    _breakSkillTypeEnumList.Add(heroSkillTypeEnum);
                }
            }
            else
            {
                if (_breakSkillTypeEnumList.Contains(heroSkillTypeEnum))
                {
                    _breakSkillTypeEnumList.Remove(heroSkillTypeEnum);
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 指令的打断窗口是否打开
    /// </summary>
    /// <param name="heroSkillTypeEnum"></param>
    /// <returns></returns>
    public bool CommandBreakWindowIsOpen(CommandType heroSkillTypeEnum)
    {
        if (heroSkillTypeEnum == CommandType.SKill && CommandType == heroSkillTypeEnum)
        {
            return true;
        }
        
        return _breakSkillTypeEnumList.Contains(heroSkillTypeEnum);
    }

    public bool IsReceive => _openCommandReceive;
    
    public bool IsDown => _down;

    public bool IsUp => _up;

    public void Clear()
    
    
    {
        _down = false;
        _up = false;
        _cacheTick = 0;
        _currentDownCacheTick = 0;
        _currentUpCacheTick = 0;
        _breakSkillTypeEnumList.Clear();
        _breakSkillTypeEnumList = null;
    }
}
