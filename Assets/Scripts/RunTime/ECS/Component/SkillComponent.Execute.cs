using UnityEngine;

/// <summary>
/// 执行
/// </summary>
public partial class SkillComponent 
{
    /// <summary>
    /// 当前正在执行的技能
    /// </summary>
    private BaseSkillData _currentSkillData;

    /// <summary>
    /// 当前缓存的预输入指令
    /// </summary>
    private CommandCacheData _currentCommandCacheData;
    
    /// <summary>
    /// 刷新技能
    /// </summary>
    private void UpdateSkill()
    {
        if (_currentSkillData != null)
        {
            PlayableStateEnum state = _currentSkillData.TickSkill();

            //当技能结束时 有预输入则执行预输入指令
            if (state == PlayableStateEnum.Error || state == PlayableStateEnum.Exit)
            {
                _currentSkillData.OnSkillEnd();
                
                _currentSkillData = null;

                if (_currentCommandCacheData is { IsDown: true })
                {
                    ExecuteSkill(_currentCommandCacheData.CommandType, WorldContent.CommandExecuteState.OnlyDown);

                    _currentCommandCacheData.ClearCommandCache();

                    _currentCommandCacheData = null;
                }
            }
            else if (state == PlayableStateEnum.Running)
            {
                if (_currentCommandCacheData != null)
                {
                    //预输入可以打断当前技能
                    if (CommandBreakWindowIsOpen(_currentSkillData.Command,_currentCommandCacheData.CommandType))
                    {
                        Debug.Log($"预输入可以打断当前技能");

                        _currentSkillData.BreakSkill(false);
                        
                        _currentSkillData.OnSkillEnd();
                
                        _currentSkillData = null;

                        if (_currentCommandCacheData is { IsDown: true })
                        {
                            int skillId = GetSkillDataByCommandType(_currentCommandCacheData.CommandType);

                            //开始执行缓存指令的按下
                            RealExecuteSkill(skillId, WorldContent.CommandExecuteState.OnlyDown);
                            
                            _currentCommandCacheData.ClearCommandCache();

                            _currentCommandCacheData = null;
                            
                            // TODO 开始执行技能的第一帧?
                            _currentSkillData?.TickSkill();
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 打断当前执行的技能
    /// </summary>
    public void BreakCurrentExecuteSkill(BaseSkillData releaseSkillData,bool isRollBackBreakSkill)
    {
        if (_currentSkillData != null)
        {
            Entity.EntityDebug($"技能ID : {_currentSkillData.SkillId} 被打断");

            _currentSkillData.BreakSkill(isRollBackBreakSkill);
        }
        
        _currentSkillData = releaseSkillData;
        
        Entity.EntityDebug($"当前打断的技能 : {releaseSkillData.SkillId}");
    }

    
    /// <summary>
    /// 打断当前技能
    /// </summary>
    /// <param name="isRollBackBreakSkill">是否是回滚打断的技能</param>
    public void BreakCurrentExecuteSkill(bool isRollBackBreakSkill)
    {
        if (_currentSkillData != null)
        {
            _currentSkillData.BreakSkill(isRollBackBreakSkill);
            
            Entity.EntityDebug($"当前的技能被打断 : {_currentSkillData.SkillId}  没有技能了");
            
            _currentSkillData = null;
        }

        if (_currentCommandCacheData != null)
        {
            _currentCommandCacheData.ClearCommandCache();
        }
    }

    /// <summary>
    /// 回滚清空技能
    /// </summary>
    private void RollBackClearSkill()
    {
        if (_currentSkillData != null)
        {
            _currentSkillData.BreakSkill(true);
            
            _currentSkillData = null;
        }
    }
    
    /// <summary>
    /// 服务器指令
    /// </summary>
    /// <param name="commandData"></param>
    public override void OnExecuteServerCommand(CommandData commandData)
    {
        base.OnExecuteServerCommand(commandData);

        if (commandData.CommandType == CommandType.None)
        {
            return;
        }

        ExecuteSkill(commandData.CommandType, commandData.CommandState);
    }

    /// <summary>
    /// 客户端指令
    /// </summary>
    /// <param name="commandData"></param>
    public override void OnExecuteLocalCommand(CommandData commandData)
    {
        base.OnExecuteLocalCommand(commandData);

        if (commandData.CommandType == CommandType.None)
        {
            return;
        }

        ExecuteSkill(commandData.CommandType, commandData.CommandState);
    }
    
    /// <summary>
    /// 执行一个技能
    /// </summary>
    /// <param name="commandType"></param>
    /// <param name="commandState"></param>
    private void ExecuteSkill(CommandType commandType, WorldContent.CommandExecuteState commandState)
    {
        Entity.EntityDebug($"执行技能 ： {commandType}  {commandState}");
        
        if (!Entity.IsCanReleaseSkill)
        {
            return;
        }
        
        if (_currentSkillData != null)
        {
            //指令是否能打断
            bool isCanExecute = CommandBreakWindowIsOpen(_currentSkillData.Command,commandType);

            if (!isCanExecute)
            {
                // TODO 只缓存按下的指令
                if (commandState == WorldContent.CommandExecuteState.OnlyDown)
                {
                    //刷新指令的缓存时间
                    _currentCommandCacheData = RefreshCacheTime(commandType, commandState);
                }
                
                return;
            }
        }
        
        int skillId = GetSkillDataByCommandType(commandType);

        RealExecuteSkill(skillId, commandState);
    }

    /// <summary>
    /// 真正通过技能ID开始执行技能
    /// </summary>
    /// <param name="skillId"></param>
    /// <param name="commandState"></param>
    private void RealExecuteSkill(int skillId,WorldContent.CommandExecuteState commandState)
    {
        if (_heroSkillDataList.TryGetValue(skillId, out var value))
        {
            if (value != null)
            {
                value.ExecuteCommand(commandState);
            }
        }
    }
    
    public int CurrentSkillId => _currentSkillData?.SkillId ?? 0;
}
