using System.Threading.Tasks;
using Rogue;

/// <summary>
/// 战斗 UI 系统（PVP 双英雄）。
/// </summary>
public class BattleUISystem : BaseSystem
{
    private BattleInfoWindow _battleInfoWindow;
    private BattleInfoViewModel _battleInfoViewModel;

    private async Task<bool> CreateBattleInfoWindow()
    {
        _battleInfoWindow = await GameEntry.UI.OpenUIWindow<BattleInfoWindow>(
            AssetsPathHelper.UIWindowPathHelper("BattleInfoWindow"), Content.UI.UIDefaultGroup, _battleInfoViewModel, null);

        return _battleInfoWindow != null;
    }

    /// <summary>
    /// 注册 1v1 双方英雄。
    /// </summary>
    public void RegisterActors(HeroEntity localHero, HeroEntity remoteHero)
    {
        _battleInfoViewModel = new BattleInfoViewModel(this, localHero.BattleHeroData, remoteHero.BattleHeroData);
        CreateBattleInfoWindow();
    }
}
