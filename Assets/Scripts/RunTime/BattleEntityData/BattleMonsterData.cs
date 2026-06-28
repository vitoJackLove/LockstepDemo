using UnityEngine;

/// <summary>
/// 怪物数据
/// </summary>
public class BattleMonsterData : BattleEntityData
{
    /// <summary>
    /// 怪物名字
    /// </summary>
    private string _monsterName;

    /// <summary>
    /// 怪物头像
    /// </summary>
    private Sprite _monsterIcon;

    /// <summary>
    /// 怪物表数据
    /// </summary>
    private MonsterAssetsConfig _monsterAssetsConfig;
    
    public static BattleMonsterData Creat(MonsterAssetsConfig monsterAssetsConfig)
    {
        BattleMonsterData battleMonsterData = FPoolHelper.Get<BattleMonsterData>();

        battleMonsterData._monsterAssetsConfig = monsterAssetsConfig;
        battleMonsterData.MonsterIcon = monsterAssetsConfig.monsterIcon;
        battleMonsterData.MonsterName = monsterAssetsConfig.monsterName;
        battleMonsterData.InitProperty();
        
        return battleMonsterData;
    }
    
    public override void InitProperty()
    {
        EntityPropertyDataDic.Add(PropertyKey.Hp.ToString(), PropertyData.Create(_monsterAssetsConfig.hp, 0, _monsterAssetsConfig.hp));
        EntityPropertyDataDic.Add(PropertyKey.Speed.ToString(), PropertyData.Create(_monsterAssetsConfig.speed, 0, _monsterAssetsConfig.speed));
        EntityPropertyDataDic.Add(PropertyKey.Attack.ToString(), PropertyData.Create(_monsterAssetsConfig.attack, 0, _monsterAssetsConfig.attack));
        EntityPropertyDataDic.Add(PropertyKey.Defence.ToString(), PropertyData.Create(_monsterAssetsConfig.defence, 0, _monsterAssetsConfig.defence));
    }
    
    public string MonsterName
    {
        get => _monsterName;
        private set => this.Set(ref _monsterName, value, "MonsterName");
    }
    
    public Sprite MonsterIcon
    {
        get => _monsterIcon;
        private set => this.Set(ref _monsterIcon, value, "MonsterIcon");
    }

    public MonsterAssetsConfig MonsterAssetsConfig => _monsterAssetsConfig;
}
