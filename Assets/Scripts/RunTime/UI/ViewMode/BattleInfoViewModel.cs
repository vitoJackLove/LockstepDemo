using Loxodon.Framework.ViewModels;

/// <summary>
/// PVP 战斗信息 ViewModel（本地英雄 + 远端英雄）。
/// </summary>
public class BattleInfoViewModel : ViewModelBase
{
    private BattleHeroData _localHeroData;
    private BattleHeroData _remoteHeroData;
    private BattleUISystem _battleUiSystem;

    public BattleInfoViewModel() { }

    public BattleInfoViewModel(BattleUISystem battleUiSystem, BattleHeroData localHeroData, BattleHeroData remoteHeroData)
    {
        _battleUiSystem = battleUiSystem;
        LocalHeroData = localHeroData;
        RemoteHeroData = remoteHeroData;
    }

    public BattleHeroData LocalHeroData
    {
        get => _localHeroData;
        private set => Set(ref _localHeroData, value, nameof(LocalHeroData));
    }

    public BattleHeroData RemoteHeroData
    {
        get => _remoteHeroData;
        private set => Set(ref _remoteHeroData, value, nameof(RemoteHeroData));
    }
}
