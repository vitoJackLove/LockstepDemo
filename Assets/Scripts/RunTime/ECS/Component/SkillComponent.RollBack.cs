using Ase.Serializing;

/// <summary>
/// 技能的预测回滚
/// </summary>
public partial class SkillComponent
{
    public override void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
    {
        //普攻段数
        hardWriter.WriteInt32Data($"普攻段数", _attackIndex);
        
        //普攻连击记录
        _attackIndexCacheData.TakeSnapShot(hardWriter, softWriter);

        if (_currentSkillData == null)
        {
            hardWriter.WriterBoolData($"英雄是否有技能：", false);
            hardWriter.WriteInt32Data($"技能ID", 0);
            hardWriter.WriteInt32Data($"技能执行的状态", (int)SkillExecuteState.Null);
        }
        else
        {
            hardWriter.WriterBoolData($"英雄是否有技能：", true);
            hardWriter.WriteInt32Data($"技能ID", _currentSkillData.SkillId);
            hardWriter.WriteInt32Data($"技能执行的状态", (int)_currentSkillData.ExecuteState);
            
            //当前技能
            _currentSkillData.TakeSnapShot(hardWriter, softWriter);
        }

        if (_currentCommandCacheData == null)
        {
            hardWriter.WriterBoolData($"当前指令是否有缓存", false);
            hardWriter.WriteInt32Data($"缓存的指令类型", (int)CommandType.None);
        }
        else
        {
            hardWriter.WriterBoolData($"当前指令是否有缓存", true);
            hardWriter.WriteInt32Data($"缓存的指令类型", (int)_currentCommandCacheData.CommandType);

            //当前指令缓存
            _currentCommandCacheData.TakeSnapShot(hardWriter, softWriter);
        }
    }

    public override void HardRollBackTo(PooledReader authoritySnapShot)
    {
        //回滚普攻序列
        int authorityAttackIndex  = authoritySnapShot.ReadInt32();
        _attackIndex = authorityAttackIndex;

        //回滚普攻缓存
        _attackIndexCacheData.RollBackTo(authoritySnapShot);

        //回滚技能
        //1. 是否有技能
        bool authorityIsExecuteSkill = authoritySnapShot.ReadBoolean();
        
        //2.技能的ID
        int authoritySkillId = authoritySnapShot.ReadInt32();
        
        //3.技能的执行状态
        SkillExecuteState authoritySkillState = (SkillExecuteState)authoritySnapShot.ReadInt32();
        
        GameLog.Debug(GameLogChannel.Rollback, $"authorityIsExecuteSkill={authorityIsExecuteSkill} authoritySkillId={authoritySkillId} authoritySkillState={authoritySkillState}");
        
        //权威没有执行技能 本地预测了技能 清空技能
        if (authorityIsExecuteSkill == false)
        {
            //直接把组件上的技能数值 = null 这个技能数据再一次执行的时候会刷新到初始状态 ，所以回滚的时候不需要还原数据
            RollBackClearSkill();
        }
        //权威有技能  预测也有技能
        else 
        {
            if (_heroSkillDataList.TryGetValue(authoritySkillId, out var currentSkillData))
            {
                BreakCurrentExecuteSkill(currentSkillData, true);
                    
                _currentSkillData.RollBackTo(authoritySnapShot);
            }
            else
            {
                GameLog.Error(GameLogChannel.Rollback, "实体回滚异常 : 快照是存的技能实体没有...");
            }
        }
        
        //只能是否有缓存
        bool isAuthorityCommandCache = authoritySnapShot.ReadBoolean();
        
        //缓存的指令类型
        CommandType authorityCommandType = (CommandType)authoritySnapShot.ReadInt32();

        if (isAuthorityCommandCache)
        {
            _currentCommandCacheData = GetCommandCacheData(authorityCommandType);
            
            _currentCommandCacheData.RollBackTo(authoritySnapShot);
        }
        else
        {
            _currentCommandCacheData = null;
        }
    }
}
