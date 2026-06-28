using System;
using Loxodon.Framework.Binding;
using Loxodon.Framework.Binding.Builder;
using Loxodon.Framework.Interactivity;
using Loxodon.Framework.Views;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏开始页面
/// </summary>
public class GameStartUpWindow : Window
{
    public Button startUpBtn;

    public Button connectServer;

    public Toggle singlePlayerModeToggle;

    public Slider slider;

    public Text progressBarText;

    public Text tipText;

    private GameStartUpViewModel _viewModel;

    public Transform heroInfoRoot;

    public GameObject heroInfoPrefab;
    
    protected override void OnCreate(IBundle bundle)
    {
        _viewModel = new GameStartUpViewModel();

        BindingSet<GameStartUpWindow, GameStartUpViewModel> bindingSet = this.CreateBindingSet(_viewModel);
        
        bindingSet.Bind(this.startUpBtn).For(v => v.onClick).To(vm => vm.StartUp);
        
        bindingSet.Bind(this.connectServer).For(v => v.onClick).To(vm => vm.Connect);

        bindingSet.Bind(this.singlePlayerModeToggle).For(v => v.isOn, v => v.onValueChanged).To(vm => vm.IsSinglePlayerMode).TwoWay();
        
        bindingSet.Bind (this.slider).For (v => v.value, v => v.onValueChanged).To (vm => vm.ProgressBar.Progress).TwoWay ();
        
        bindingSet.Bind(this.slider.gameObject).For(v => v.activeSelf).To(vm => vm.ProgressBar.Enable).OneWay();
        
        bindingSet.Bind(this.progressBarText).For(v => v.text).ToExpression(vm => string.Format("{0}%", Mathf.FloorToInt(vm.ProgressBar.Progress * 100f))).OneWay();
        
        bindingSet.Bind(this.tipText).For(v => v.text).To(vm => vm.ProgressBar.Tip).OneWay();
        
        bindingSet.Bind().For(v => v.OnDismissRequest).To(vm => vm.dismissRequest);
        
        bindingSet.Build();

        InitHeroInfoGo();
    }

    public void Update()
    {
        if (_viewModel != null)
        {
            _viewModel.Update();
        }
    }
    

    private void InitHeroInfoGo()
    {
        for (int i = 0; i < _viewModel.HeroInfoViewModels.Count; i++)
        {
            HeroInfoViewModel viewModel = _viewModel.HeroInfoViewModels[i];

            GameObject heroInfoGo = GameObject.Instantiate(heroInfoPrefab);
            
            heroInfoGo.GetComponent<HeroInfoItem>().Init(viewModel);
            
            heroInfoGo.transform.SetParent(heroInfoRoot);
        }
    }
    
    private void OnDismissRequest(object sender, InteractionEventArgs e)
    {
        this.Dismiss();
    }
}
