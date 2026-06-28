using System;
using System.Collections.Generic;
using System.Globalization;
using Rogue;
using UnityEngine;

/// <summary>
/// 伤害文字
/// </summary>
public class UIDamageTextSystem : BaseSystem,IBattleObserverHandle
{
    /// <summary>
    /// 战斗UI画布
    /// </summary>
    private CanvasComponent.CanvasGroup _canvasGroup = null;

    /// <summary>
    /// 伤害文本渲染相机
    /// </summary>
    private Camera _barCamera = null;

    /// <summary>
    /// key = 伤害文字的Key value = 表配置
    /// </summary>
    private Dictionary<string, DamageTextAssetsConfig> _damageTextAssetDic =  new ();

    //目前不需要
    /*/// <summary>
    /// 需要堆叠的伤害文字
    /// </summary>
    private Dictionary<int , DamageStack> cacheStackDamage = new Dictionary<int, DamageStack>();*/

    public override void OnInit(object data = null)
    {
        base.OnInit(data);
        this._canvasGroup = GameEntry.Canvas.GetCanvasGroup("DamageText");
        
        this._barCamera = this.GetSystem<CameraSystem>().BattleCamera;
        
        if (this._canvasGroup == null)
        {
            Debug.LogError($"UIDamageTextSystem初始化失败： 缺少伤害文字面板...");

            return;
        }

        List<DamageTextAssetsConfig> drDamageTextList = GameEntry.DataTable.GetAllDataTable<DamageTextAssetsConfig>();

        for (int i = 0; i < drDamageTextList.Count; i++)
        {
            DamageTextAssetsConfig drDamageText = drDamageTextList[i];

            if (drDamageText == null)
            {
                continue;
            }

            GetSystem<EntityViewSystem>().SyncLoadEntityView(drDamageText.assetsPath, this._canvasGroup.Root);
            
            _damageTextAssetDic.Add(drDamageText.damageKey,drDamageText);
        }
    }
    
    public void OnNotify(IBattleObserverParams param)
    {
        BattleAttackEventParams attackEventParams = param as BattleAttackEventParams;

        if (attackEventParams == null)
        {
            return;
        }

        if (attackEventParams.Attacker == null)
        {
            return;
        }

        if (attackEventParams.Defender == null)
        {
            return;
        }
        
        if (attackEventParams.Attacker.IsNeedExecuteView)
        {
            //ShowStackDamageText(attackEventParams.Damage.ToString(CultureInfo.InvariantCulture), attackEventParams.Defender, "Damage");
        }
    }

    /// <summary>
    /// 获取一个伤害显示文字
    /// </summary>
    /// <param name="keyType"></param>
    /// <param name="root"></param>
    /// <returns></returns>
    [Obsolete("Obsolete")]
    private UITextDamage AllocateDamageText(string keyType, Transform root)
    {
        //获取对象池
        if (this._damageTextAssetDic.TryGetValue(keyType, out DamageTextAssetsConfig config))
        {
            GameObject textGo = GetSystem<EntityViewSystem>().GetEntityView(config.assetsPath, this._canvasGroup.Root);
            
            UITextDamage uiTextDamage = textGo.GetComponent<UITextDamage>();

            uiTextDamage.Init(_barCamera, _canvasGroup.Canvas);

            uiTextDamage.BindFollowRoot(root);

            return uiTextDamage;
        }
        return null;
    }

    public void ShowStackDamageText(string text, BaseEntity owner, Vector3 offset, int maxSiteLevel,
        string key = "default", bool isUp = true)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        Transform followRoot = owner.GameObject.transform;

        UITextDamage uiToUse = AllocateDamageText(key, followRoot);

        if (uiToUse == null)
        {
            Debug.LogError($"找不到跳字类型 {key}");
            return;
        }

        uiToUse.UpdateData(text, offset, maxSiteLevel, owner is MonsterEntity);
    }

    /// <summary>
    /// 显示堆叠伤害文字
    /// </summary>
    /// <param name="text">显示文本</param>
    /// <param name="owner">主人</param>
    /// <param name="key">索引</param>
    /// <param name="isUp">堆叠方向</param>
    public void ShowStackDamageText(string text, BaseEntity owner, string key = "default", bool isUp = true)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        Transform followRoot = owner.GameObject.transform;

        UITextDamage uiToUse = AllocateDamageText(key, followRoot);

        if (uiToUse == null)
        {
            return;
        }

        uiToUse.UpdateData(text, owner is MonsterEntity);
    }

    /// <summary>
    /// 伤害堆
    /// </summary>
    public class DamageStack : IPool
    {
        /// <summary>
        /// 上行堆次数
        /// </summary>
        private int upStackCount;

        private int surplusUpStackCount;

        /// <summary>
        /// 下行堆次数
        /// </summary>
        private int downStackCount;

        private int surplusDownStackCount;

        /// <summary>
        /// 添加伤害
        /// </summary>
        public void AddDamage(bool isUp)
        {
            if (isUp)
            {
                this.upStackCount++;
                this.surplusUpStackCount++;
            }
            else
            {
                this.downStackCount++;
                this.surplusDownStackCount++;
            }
        }

        /// <summary>
        /// 伤害表现结束（单次）
        /// </summary>
        /// <param name="isUp"></param>
        public void EndDamage(bool isUp)
        {
            if (isUp)
                this.surplusUpStackCount--;
            else
                this.surplusUpStackCount--;

            if (surplusUpStackCount <= 0)
            {
                this.upStackCount = 0;
            }

            if (surplusUpStackCount <= 0)
            {
                this.downStackCount = 0;
            }
        }

        public int GetStackCount(bool isUp)
        {
            return isUp ? this.upStackCount : this.downStackCount;
        }

        public void Clear()
        {
            this.upStackCount = 0;
            this.downStackCount = 0;
            this.surplusUpStackCount = 0;
            this.surplusDownStackCount = 0;
        }
    }

}