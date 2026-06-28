using Loxodon.Framework.ViewModels;

/// <summary>
/// 战斗信息
/// </summary>
public class BattleInfoViewModel : ViewModelBase
{
     /// <summary>
     /// 英雄数据
     /// </summary>
     private BattleHeroData _battleHeroData;

     /// <summary>
     /// 对抗的怪物数据
     /// </summary>
     private BattleMonsterData _battleMonsterData;
     
     /// <summary>
     /// 战斗UISystem
     /// </summary>
     private BattleUISystem _battleUiSystem;

     public BattleInfoViewModel() { }

     public BattleInfoViewModel(BattleUISystem battleUiSystem, BattleHeroData heroData, BattleMonsterData monsterData)
     {
          BattleHeroData = heroData;

          BattleMonsterData = monsterData;

          this._battleUiSystem = battleUiSystem;
     }
     public BattleHeroData BattleHeroData
     {
          get => _battleHeroData;
          private set => this.Set(ref _battleHeroData, value, "BattleHeroData");
     }
     
     public BattleMonsterData BattleMonsterData
     {
          get => _battleMonsterData;
          private set => this.Set(ref _battleMonsterData, value, "BattleMonsterData");
     }
}
