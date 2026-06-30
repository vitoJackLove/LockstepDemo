using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Buff 制作窗口
/// </summary>
public class BuffGenerateEditorWindow : OdinEditorWindow
{
    private INeedBuffWindow _generateBuffWindow;

    public const string ParentGroup = "ParentGroup";
    
    #region Buff的一般属性TabGroup

    private const string TabGroupCommonProperty = "Buff的一般属性";
    
    private const string BoxGroupCommonProperty = "ParentGroup/Buff的一般属性/一般属性";

    #endregion
    
    #region BuffConditionTabGroup

    private const string TabGroupExecuteCondition = "Buff执行前的条件";

    private const string CharacterPropertyCondition = "ParentGroup/Buff执行前的条件/实体属性的条件";
    
    #endregion

    #region BuffEffectTabGroup

    private const string TabGroupExecuteEffect = "Buff执行的效果";
    
    private const string BuffPropertyEffect= "ParentGroup/Buff执行的效果/属性效果";

    private const string BuffCardEffect = "ParentGroup/Buff执行的效果/Buff效果";

    private const string BuffCardEffectProperty = "ParentGroup/Buff执行的效果/Buff效果属性";

    #endregion

    [MenuItem("Tools/Buff/Buff制作工具")]
    public static void OpenWindow()
    {
        BuffGenerateEditorWindow window = GetWindow<BuffGenerateEditorWindow>($"Buff制作工具");
    }

    public void SetNeedBuffWindow(INeedBuffWindow generateBuffWindow)
    {
        _generateBuffWindow = generateBuffWindow;
    }

    protected override void OnDisable()
    {
        _generateBuffWindow = null;
    }

    // ------------------------一般属性------------------------------------

    [TabGroup(ParentGroup, TabGroupCommonProperty)]

    [BoxGroup(BoxGroupCommonProperty)]
    [Title("Buff ID")]
    [HideLabel]
    public int buffId;

    [BoxGroup(BoxGroupCommonProperty)]
    [Title("生命周期（Tick）")] [HideLabel]
    public int lifeTime;

    [BoxGroup(BoxGroupCommonProperty)]
    [Sirenix.OdinInspector.Title("BuffIcon")][HideLabel]
    public Sprite buffIcon;

    [BoxGroup(BoxGroupCommonProperty)]
    [Sirenix.OdinInspector.Title("Buff的描述")]
    [HideLabel]
    public string buffDoc;

    // ------------------------条件------------------------------------

    [TabGroup(ParentGroup, TabGroupExecuteCondition)]
    
    [BoxGroup(CharacterPropertyCondition)]
    [Title("监听的事件")] [HideLabel]
    public BattleExecuteTiming gameEventType;

    /// <summary>
    /// Buff 持有者在触发事件中的角色要求，默认 Any 用于兼容旧配置。
    /// </summary>
    [BoxGroup(CharacterPropertyCondition)]
    [Title("触发角色")]
    [HideLabel]
    public BuffTriggerRole triggerRole;
    
    [BoxGroup(CharacterPropertyCondition)]
    [Title("判断的目标")][HideLabel]
    public BuffExecuteTarget buffConditionTarget;
    
    [BoxGroup(CharacterPropertyCondition)]
    [Space(10)]  [Title("比较的属性")] [HideLabel]
    public PropertyKey propertyKey;
    
    [BoxGroup(CharacterPropertyCondition)]
    [Space(10)]  [Title("属性的类型")] [HideLabel]
    public PropertyValueType propertyValueType;
    
    [BoxGroup(CharacterPropertyCondition)]
    [Space(10)]  [Title("比较符")] [HideLabel]
    public CompareMethod compareMethod;
    
    [BoxGroup(CharacterPropertyCondition)]
    [Space(10)]  [Title("具体数值")] [HideLabel]
    public int value;
    
    //-------------------------------效果------------------------------
    
    [TabGroup(ParentGroup, TabGroupExecuteEffect)]

    /// <summary>
    /// Buff 效果执行模式，默认 Instant 保持现有立即属性效果。
    /// </summary>
    [BoxGroup(BuffPropertyEffect, true, true)]
    [Title("效果模式")]
    [HideLabel]
    public BuffEffectMode effectMode;
    
    [BoxGroup(BuffPropertyEffect,true,true)]
    [Title("操作的目标")][HideLabel]
    public BuffExecuteTarget buffExecuteTarget;
    
    [BoxGroup(BuffPropertyEffect,true,true)]
    [Title("操作的属性")] [HideLabel]
    public PropertyKey executeProperty;
    
    [BoxGroup(BuffPropertyEffect,true,true)]
    [Title("是否是简单的数值","True = 阿拉伯数字直接操作属性，False = 其他属性的影响 比如：每一点血量增加两点攻击力...")] [HideLabel]
    public bool isCommonValue;
    
    [BoxGroup(BuffPropertyEffect,true,true)]
    [ShowIf("isCommonValue")][Space(10)]  [Title("具体数值")] [HideLabel]
    public int normalValue;
    
    [BoxGroup(BuffPropertyEffect,true,true)]
    [HideIf("isCommonValue")][Space(10)]  [Title("影响的属性")] [HideLabel]
    public PropertyKey influenceProperty;
    
    [BoxGroup(BuffPropertyEffect,true,true)]
    [HideIf("isCommonValue")][Space(10)]  [Title("倍率")] [HideLabel]
    public int rate;

    //--------------------------------------Buff效果属性 --------------------------------------------

    [BoxGroup(BuffCardEffectProperty, true, true)]
    [Title("执行战斗Timing")]
    [HideLabel]
    public BattleExecuteTiming ExecuteGameEventType;

    [BoxGroup(BuffCardEffectProperty, true, true)]
    [Title("执行次数")]
    [HideLabel]
    public int executeEffectValue;

    [BoxGroup(BuffCardEffectProperty, true, true)]
    [Title("该效果可以执行的次数", "效果次数执行完毕后会移除Buff")]
    [HideLabel]
    public int effectLifeTime;

    [BoxGroup(BuffCardEffectProperty, true, true)]
    [Title("效果执行间隔")]
    [HideLabel]
    public float effectExecuteDelay;

    /// <summary>
    /// 根据窗口字段生成 Buff 配置，并把新增触发角色与效果模式写入资源数据。
    /// </summary>
    [Button("生成Buff...", ButtonSizes.Large), GUIColor(0, 1, 0)]
    public void GenerateBuff()
    {
        BuffAssets buffAssets = InitAssets<BuffAssets>("Assets/GameAssetConfig/BuffAssets.asset", "BuffAssets");

        BuffAssetsConfig buffConfig = new BuffAssetsConfig
        {
            assetsId = buffId,
            lifeTime = lifeTime,
            gameEventType = gameEventType,
            buffIcon = buffIcon,
            buffDoc = buffDoc,
            buffConditionAssets = new BuffConditionAssets
            {
                triggerRole = triggerRole,
                buffConditionTarget = buffConditionTarget,
                propertyKey = propertyKey,
                propertyValueType = propertyValueType,
                compareMethod = compareMethod,
                value = value
            },
            buffEffectAssets = new BuffEffectAssets
            {
                effectMode = effectMode,
                buffExecuteTarget = buffExecuteTarget,
                executeProperty = executeProperty,
                isCommonValue = isCommonValue,
                normalValue = normalValue,
                influenceProperty = influenceProperty,
                rate = rate
            }
        };

        buffConfig.gameEventType = ExecuteGameEventType;

        buffConfig.executeValue = executeEffectValue;

        if (_generateBuffWindow == null)
        {
            buffAssets.buffAssetsConfigList.Add(buffConfig);
        }

        _generateBuffWindow?.GenerateBuff(buffConfig);

        Close();
    }


    /// <summary>
    /// 初始化本地配置
    /// </summary>
    private T InitAssets<T>(string assetPath, string assetName) where T : ScriptableObject
    {
        var scriptableObject = AssetDatabase.LoadAssetAtPath<T>(assetPath);

        if (scriptableObject == null)
        {
            scriptableObject = ScriptableObject.CreateInstance<T>();

            scriptableObject.name = assetName;

            AssetDatabase.CreateAsset(scriptableObject, assetPath);
        }

        if (scriptableObject == null)
        {
            Debug.LogError($"Buff制作工具初始化错误：{assetName}未能正常生成本地数据...");
        }

        return scriptableObject;
    }
}
