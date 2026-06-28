using System.Collections.Generic;
using Ase.Serializing;
using Unity.Mathematics.FixedPoint;

/// <summary>
/// 怪物技能组件
/// </summary>
public class MonsterSkillComponent : BaseComponent
{
    /// <summary>
    /// 怪物技能组 key = 技能ID
    /// </summary>
    private Dictionary<int, MonsterSkillData> _monsterSkillDic;

    /// <summary>
    /// 当前技能数据
    /// </summary>
    private MonsterSkillData _currentSkillData;

    public override void OnInit(object data = null)
    {
        base.OnInit(data);

        _monsterSkillDic = new Dictionary<int, MonsterSkillData>();
    }

    public override void OnStart(object data = null)
    {
        base.OnStart(data);
        
        InitMonsterSkillData();
    }

    /// <summary>
    /// 初始化技能组
    /// </summary>
    private void InitMonsterSkillData()
    {
        MonsterEntity monsterEntity = (MonsterEntity)Entity;
         
        for (int i = 0; i < monsterEntity.BattleMonsterData.MonsterAssetsConfig.monsterSkillList.Count; i++)
        {
            MonsterSkillConfig skillConfig = monsterEntity.BattleMonsterData.MonsterAssetsConfig.monsterSkillList[i];

            MonsterSkillData skillData = MonsterSkillData.Create(Entity, skillConfig);

            _monsterSkillDic.Add(skillConfig.skillId, skillData);
        }
    }

    /// <summary>
    /// 获取技能
    /// </summary>
    /// <param name="skillId"></param>
    /// <returns></returns>
    private MonsterSkillData GetSkillData(int skillId)
    {
        if (_monsterSkillDic.TryGetValue(skillId, out var skillData))
        {
            return skillData;
        }

        return null;
    }

    /// <summary>
    /// 打断当前技能
    /// </summary>
    public void BreakSkill(bool isRollBackBreakSkill)
    {
        if (_currentSkillData != null)
        {
            _currentSkillData.BreakSkill(isRollBackBreakSkill);
        }
    }

    /// <summary>
    /// 执行技能
    /// </summary>
    /// <param name="skillId"></param>
    /// <param name="isRollBackBreakSkill"></param>
    public void ExecuteSkill(int skillId,bool isRollBackBreakSkill)
    {
        if (_monsterSkillDic.TryGetValue(skillId, out var skillData))
        {
            if (_currentSkillData != null)
            {
                _currentSkillData.BreakSkill(isRollBackBreakSkill);
                
                _currentSkillData = null;
            }
            
            _currentSkillData = skillData;

            _currentSkillData.ExecuteSkill();
            
            Entity.EntityDebug($"释放技能 : {skillId}");
        }
        
    }

    public override void OnFixedUpdate(fp deltaTime, WorldUpdateType worldUpdateType)
    {
        base.OnFixedUpdate(deltaTime, worldUpdateType);

        if (_currentSkillData != null)
        {
            PlayableStateEnum stateEnum = _currentSkillData.UpdateSkill(deltaTime);

            if (stateEnum == PlayableStateEnum.Error || stateEnum == PlayableStateEnum.Exit)
            {
                _currentSkillData = null;
            }
        }
    }

    public override void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
    {
        if (_currentSkillData == null)
        {
            hardWriter.WriterBoolData($"_currentSkillData != null?", false);
            hardWriter.WriteInt32Data($"SkillId", 0);
        }
        else
        {
            hardWriter.WriterBoolData($"_currentSkillData != null?", true);
            hardWriter.WriteInt32Data($"SkillId", _currentSkillData.SKillId);
            
            _currentSkillData.TakeSnapShot(hardWriter, softWriter);
        }
    }

    public override void HardRollBackTo(PooledReader authoritySnapShot)
    {
        bool isAuthorityExecuteSkill = authoritySnapShot.ReadBoolean();
        int authoritySkillId = authoritySnapShot.ReadInt32();

        //权威有技能
        if (isAuthorityExecuteSkill)
        {
            ExecuteSkill(authoritySkillId, true);
            
            _currentSkillData?.RollBackTo(authoritySnapShot);
        }
        else
        {
            BreakSkill(true);
        }
    }
}
