using UnityEngine;

/// <summary>
/// 英雄数据
/// </summary>
public class BattleHeroData : BattleEntityData
{
    /// <summary>
    /// 英雄名字
    /// </summary>
    private string _heroName;

    /// <summary>
    /// 英雄头像
    /// </summary>
    private Sprite _heroIcon;
    
    /// <summary>
    /// 表数据
    /// </summary>
    private HeroAssetsConfig _heroAssetsConfig;

    public static BattleHeroData CreatNull()
    {
        BattleHeroData battleHeroData = FPoolHelper.Get<BattleHeroData>();

        return battleHeroData;
    }
    
    public static BattleHeroData Creat(HeroAssetsConfig heroAssetsConfig)
    {
        BattleHeroData battleHeroData = FPoolHelper.Get<BattleHeroData>();

        battleHeroData._heroAssetsConfig = heroAssetsConfig;
        battleHeroData.HeroIcon = heroAssetsConfig.heroIcon;
        battleHeroData.HeroName = heroAssetsConfig.heroName;
        
        battleHeroData.InitProperty();
        
        return battleHeroData;
    }
    
    public override void InitProperty()
    {
        EntityPropertyDataDic.Add(PropertyKey.Hp.ToString(), PropertyData.Create(_heroAssetsConfig.hp, 0, _heroAssetsConfig.hp));
        EntityPropertyDataDic.Add(PropertyKey.Speed.ToString(), PropertyData.Create(_heroAssetsConfig.speed, 0, _heroAssetsConfig.speed));
        EntityPropertyDataDic.Add(PropertyKey.Attack.ToString(), PropertyData.Create(_heroAssetsConfig.attack, 0, _heroAssetsConfig.attack));
        EntityPropertyDataDic.Add(PropertyKey.Defence.ToString(), PropertyData.Create(_heroAssetsConfig.defence, 0, _heroAssetsConfig.defence));
        EntityPropertyDataDic.Add(PropertyKey.RotateSpeed.ToString(), PropertyData.Create(_heroAssetsConfig.rotateSpeed, 0, _heroAssetsConfig.rotateSpeed));
    }
    
    public string HeroName
    {
        get => _heroName;
        private set => this.Set(ref _heroName, value, "HeroName");
    }
    
    public Sprite HeroIcon
    {
        get => _heroIcon;
        private set => this.Set(ref _heroIcon, value, "HeroIcon");
    }

    public HeroAssetsConfig HeroAssetsConfig => _heroAssetsConfig;
} 
