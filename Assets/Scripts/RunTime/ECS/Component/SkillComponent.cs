using System.Collections.Generic;
using Unity.Mathematics.FixedPoint;

/// <summary>
/// 技能组件
/// </summary>
public partial class SkillComponent : BaseComponent
{
    /// <summary>
    /// 技能
    /// </summary>
    private readonly Dictionary<int, BaseSkillData> _heroSkillDataList = new Dictionary<int, BaseSkillData>();

    /// <summary>
    /// 指令对应的技能ID组
    /// </summary>
    private readonly Dictionary<CommandType, List<int>> _commandBindSKillId = new Dictionary<CommandType, List<int>>();

    public override void OnStart(object data = null)
    {
        base.OnStart(data);

        InitSkillTimeAssets();

        InitCommandData();
     
        _attackIndex = 0;
    }

    private void InitSkillTimeAssets()
    {
        HeroEntity heroEntity = Entity as HeroEntity;

        if (heroEntity == null)
        {
            return;
        }

        if (heroEntity.BattleHeroData.HeroAssetsConfig.initSkillList.Count == 0)
        {
            return;
        }

        for (int i = 0; i < heroEntity.BattleHeroData.HeroAssetsConfig.initSkillList.Count; i++)
        {
            var skillConfig = heroEntity.BattleHeroData.HeroAssetsConfig.initSkillList[i];

            BaseSkillData baseSkillData;

            if (skillConfig.keyCode == CommandType.Attack)
            {
                baseSkillData = AttackSkillData.Create(skillConfig, Entity);

                InitAttackData(skillConfig);
            }
            else
            {
                baseSkillData = NormalSkillData.Create(skillConfig, Entity);
            }

            _heroSkillDataList.Add(skillConfig.skillId, baseSkillData);

            if (_commandBindSKillId.TryGetValue(skillConfig.keyCode, out var skillIdList))
            {
                skillIdList.Add(skillConfig.skillId);
            }
            else
            {
                skillIdList = new List<int> { skillConfig.skillId };

                _commandBindSKillId.Add(skillConfig.keyCode, skillIdList);
            }
        }

        _attackNumber = _commandBindSKillId[CommandType.Attack].Count - 1;
    }

    public override void OnFixedUpdate(fp deltaTime, WorldUpdateType worldUpdateType)
    {
        base.OnFixedUpdate(deltaTime, worldUpdateType);

        //刷新技能
        UpdateSkill();

        //刷新普通攻击数据
        UpdateAttackData();
    }
    
    /// <summary>
    /// 通过指令类型获取技能的ID
    /// </summary>
    /// <param name="commandType"></param>
    /// <returns></returns>
    private int GetSkillDataByCommandType(CommandType commandType)
    {
        int index = 0;
        
        if (commandType == CommandType.Attack)
        {
            index = _attackIndex;
        }
        
        _commandBindSKillId.TryGetValue(commandType, out var skillList);

        if (skillList != null && skillList.Count > 0)
        {
            return skillList[index];
        }

        return -1;
    }
}