using System.Threading.Tasks;
using Rogue;

/// <summary>
/// 主要管理战斗UI
/// </summary>
public class BattleUISystem : BaseSystem
{
   /// <summary>
   /// 战斗信息主窗口
   /// </summary>
   private BattleInfoWindow _battleInfoWindow;

   /// <summary>
   /// 战斗信息数据
   /// </summary>
   private BattleInfoViewModel _battleInfoViewModel;
   
   private async Task<bool> CreateBattleInfoWindow()
   {
      _battleInfoWindow = await GameEntry.UI.OpenUIWindow<BattleInfoWindow>(
         AssetsPathHelper.UIWindowPathHelper($"BattleInfoWindow"), Content.UI.UIDefaultGroup, _battleInfoViewModel, null);

      return _battleInfoWindow != null;
   }

   /// <summary>
   /// 注册角色
   /// </summary>
   /// <param name="heroEntity"></param>
   /// <param name="monsterEntity"></param>
   public void RegisterActor(HeroEntity heroEntity, MonsterEntity monsterEntity)
   {
      _battleInfoViewModel = new BattleInfoViewModel(this, heroEntity.BattleHeroData, monsterEntity.BattleMonsterData);

      CreateBattleInfoWindow();
   }
}
