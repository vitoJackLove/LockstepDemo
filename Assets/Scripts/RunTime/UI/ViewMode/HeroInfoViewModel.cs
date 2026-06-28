using Loxodon.Framework.ViewModels;
using UnityEngine;

/// <summary>
/// 英雄信息数据
/// </summary>
public class HeroInfoViewModel : ViewModelBase
{
    /// <summary>
    /// 英雄的ID
    /// </summary>
    private int _heroId;

    public int HeroId => _heroId;

    /// <summary>
    /// 是否被选择
    /// </summary>
    private bool _isSelected;

    /// <summary>
    /// 英雄头像
    /// </summary>
    private Sprite _sprite;

    private GameStartUpViewModel _startUpViewModel;
        
    public bool IsSelected
    {
        get { return this._isSelected; }
        set { this.Set<bool>(ref this._isSelected, value, "IsSelected"); }
    }
    
    public Sprite HeroSprite
    {
        get { return this._sprite; }
        set { this.Set<Sprite>(ref this._sprite, value, "HeroSprite"); }
    }
    
    private HeroInfoViewModel (){}

    public HeroInfoViewModel(GameStartUpViewModel gameStartUpViewModel, HeroAssetsConfig heroAssetsConfig)
    {
        this.HeroSprite = heroAssetsConfig.heroIcon;
        this.IsSelected = false;
        this._startUpViewModel = gameStartUpViewModel;
        this._heroId = heroAssetsConfig.assetsId;
    }

    public void OnSelectHero()
    {
        _startUpViewModel.SelectHero(_heroId);
    }
}
