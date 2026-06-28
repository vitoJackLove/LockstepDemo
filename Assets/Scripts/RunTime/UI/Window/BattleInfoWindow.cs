using Loxodon.Framework.Binding;
using Loxodon.Framework.Binding.Builder;
using Loxodon.Framework.Views;
using UnityEngine.UI;

public class BattleInfoWindow : Window
{
    /// <summary>
    /// 血条
    /// </summary>
    public Image hpBar;

    /// <summary>
    /// 英雄头像
    /// </summary>
    public Image heroIcon;

    /// <summary>
    /// 英雄名字
    /// </summary>
    public Text heroName;

    /// <summary>
    /// 移动速度
    /// </summary>
    public Text heroSpeed;

    /// <summary>
    /// 英雄攻击力
    /// </summary>
    public Text heroAttack;

    /// <summary>
    /// 英雄防御力
    /// </summary>
    public Text heroDefence;
    
    
    /// <summary>
    /// 战斗数据
    /// </summary>
    private BattleInfoViewModel _battleInfoViewModel;
    
    
    /// <summary>
    /// 怪物血条
    /// </summary>
    public Image monsterHpBar;

    /// <summary>
    /// 怪物头像
    /// </summary>
    public Image monsterIcon;

    /// <summary>
    /// 怪物名字
    /// </summary>
    public Text monsterName;

    /// <summary>
    /// 怪物速度
    /// </summary>
    public Text monsterSpeed;

    /// <summary>
    /// 怪物攻击力
    /// </summary>
    public Text monsterAttack;

    /// <summary>
    /// 怪物防御力
    /// </summary>
    public Text monsterDefence;
    
    protected override void OnCreate(IBundle bundle)
    {
        _battleInfoViewModel = bundle.Get<BattleInfoViewModel>(BindDataKey.WindowData);
        
        BindingSet<BattleInfoWindow, BattleInfoViewModel> bindingSet = this.CreateBindingSet(_battleInfoViewModel);

        bindingSet.Bind(this.hpBar).For(v => v.fillAmount)
            .ToExpression(vm =>(float)(
                vm.BattleHeroData.EntityPropertyData[PropertyKey.Hp.ToString()].CurrentValue /
                vm.BattleHeroData.EntityPropertyData[PropertyKey.Hp.ToString()].MaxValue));
        
        bindingSet.Bind(this.heroSpeed).For(v => v.text)
            .ToExpression(vm =>$"速度：{(float)vm.BattleHeroData.EntityPropertyData[PropertyKey.Speed.ToString()].CurrentValue}");
        
        bindingSet.Bind(this.heroAttack).For(v => v.text)
            .ToExpression(vm =>$"攻击力：{(float)(vm.BattleHeroData.EntityPropertyData[PropertyKey.Attack.ToString()].CurrentValue)}");
        
        bindingSet.Bind(this.heroDefence).For(v => v.text)
            .ToExpression(vm =>$"防御力：{(float)vm.BattleHeroData.EntityPropertyData[PropertyKey.Defence.ToString()].CurrentValue}");
        
        bindingSet.Bind(this.heroName).For(v => v.text).To(vm =>vm.BattleHeroData.HeroName);
        
        bindingSet.Bind(this.heroIcon).For(v => v.sprite).To(vm =>vm.BattleHeroData.HeroIcon);
        
        
        // 怪物 ---------------------------------------
        bindingSet.Bind(this.monsterHpBar).For(v => v.fillAmount)
            .ToExpression(vm =>
                (float)(vm.BattleMonsterData.EntityPropertyData[PropertyKey.Hp.ToString()].CurrentValue /
                vm.BattleMonsterData.EntityPropertyData[PropertyKey.Hp.ToString()].MaxValue));
        
        bindingSet.Bind(this.monsterSpeed).For(v => v.text)
            .ToExpression(vm =>$"速度：{(float)vm.BattleMonsterData.EntityPropertyData[PropertyKey.Speed.ToString()].CurrentValue}");
        
        bindingSet.Bind(this.monsterAttack).For(v => v.text)
            .ToExpression(vm =>$"攻击力：{(float)vm.BattleMonsterData.EntityPropertyData[PropertyKey.Attack.ToString()].CurrentValue}");
        
        bindingSet.Bind(this.monsterDefence).For(v => v.text)
            .ToExpression(vm =>$"防御力：{(float)vm.BattleMonsterData.EntityPropertyData[PropertyKey.Defence.ToString()].CurrentValue}");
        
        bindingSet.Bind(this.monsterName).For(v => v.text).To(vm =>vm.BattleMonsterData.MonsterName);
        
        bindingSet.Bind(this.monsterIcon).For(v => v.sprite).To(vm =>vm.BattleMonsterData.MonsterIcon);
        
        
        bindingSet.Build();
    }
}
