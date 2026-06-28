using Loxodon.Framework.Binding;
using Loxodon.Framework.Binding.Builder;
using Loxodon.Framework.Views;
using UnityEngine;
using UnityEngine.UI;

public class HeroInfoItem : UIView
{
    public Image heroIcon;

    public GameObject isSelected;

    public Button select;

    public void Init(HeroInfoViewModel heroInfoViewModel)
    {
        BindingSet<HeroInfoItem, HeroInfoViewModel> bindingSet = this.CreateBindingSet(heroInfoViewModel);
        
        bindingSet.Bind(this.isSelected).For(v => v.activeSelf).To(vm => vm.IsSelected);
        bindingSet.Bind(this.heroIcon).For(v => v.sprite).To(vm => vm.HeroSprite);
        bindingSet.Bind(this.select).For(v => v.onClick).To(vm => vm.OnSelectHero);
        
        bindingSet.Build();
    }
}
