using System;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 预测回滚编辑器
/// </summary>
public class RollBackWindow : OdinEditorWindow
{
    /// <summary>
    /// 预测回滚常量
    /// </summary>
    private GameRollBackContent _gameRollBackContent;
    
    /// <summary>
    /// 游戏预测回滚参数
    /// </summary>
    private const string GameRollBackSettingPath = "Assets/GameAssetConfig/RollBackGameConfig.asset";
    
    [MenuItem("Tool/预测回滚")]
    public static void OpenWindow()
    {
        RollBackWindow window = GetWindow<RollBackWindow>($"预测回滚");
    }

    public void Awake()
    {
        //地势噪音设置
        _gameRollBackContent =
            InitAssets<GameRollBackContent>(GameRollBackSettingPath, "GameRollBackSetting");

        AssetDatabase.Refresh();
        
        rollBackTick = _gameRollBackContent.forecastTick;
        maxPredictionTick = _gameRollBackContent.maxPredictionTick;
        lossPacket = _gameRollBackContent.lossPacket;
    }
    
    /// <summary>
    /// 初始化本地配置
    /// </summary>
    private T InitAssets<T>(string assetPath,string assetName) where  T : ScriptableObject
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
            Debug.LogError($"地图制作工具初始化错误：{assetName}未能正常生成本地数据...");
        }

        return scriptableObject;
    }

    [BoxGroup("参数设置")]
    [LabelText("预测帧数")]
    public uint rollBackTick;

    [BoxGroup("参数设置")]
    [LabelText("最大预测领先帧数")]
    public uint maxPredictionTick;
    
    [BoxGroup("参数设置")]
    [LabelText("模拟丢包率")]
    public float lossPacket;
    
    [BoxGroup("参数设置")]
    [Button("设置预测的帧数和丢包率...", ButtonSizes.Large), GUIColor(1, 1, 1)]
    public void SetRollBackData()
    {
        if (_gameRollBackContent == null)
        {
            _gameRollBackContent =
                InitAssets<GameRollBackContent>(GameRollBackSettingPath, "GameRollBackSetting");
        }

        _gameRollBackContent.forecastTick = rollBackTick;
        _gameRollBackContent.maxPredictionTick = maxPredictionTick < rollBackTick ? rollBackTick : maxPredictionTick;
        _gameRollBackContent.lossPacket = lossPacket;
        maxPredictionTick = _gameRollBackContent.maxPredictionTick;
        EditorUtility.SetDirty(_gameRollBackContent);
        AssetDatabase.SaveAssets();

        BaseWorld baseWorld = WorldSystem.Instance?.CurrentRunWorld;
        baseWorld?.InitRollBackData(rollBackTick, lossPacket);
    }
    
    [BoxGroup("动态回滚")]
    [ProgressBar("startTick", "currentTick")]
    public uint dynamicProgressBar = 50;

    [BoxGroup("动态回滚")]
    [ReadOnly]
    public uint startTick = 1;

    [BoxGroup("动态回滚")]
    [ReadOnly]
    public uint currentTick = 100;

    [BoxGroup("动态回滚")]
    [Button("开始回滚...", ButtonSizes.Large), GUIColor(1, 1, 1)]

    public void StartRollBack()
    {
        BaseWorld baseWorld = WorldSystem.Instance.CurrentRunWorld;

        Debug.Log($"开始回滚，回滚类型：{RollBackType.HardRollBack} 回滚帧数：{dynamicProgressBar} 当前权威帧：{baseWorld.AuthorityTick} 当前预测帧{baseWorld.LocalTick}");

        baseWorld.StartRollBack(dynamicProgressBar, RollBackType.HardRollBack);
    }

    [BoxGroup("快照校验设置")]
    [LabelText("字符串校验模式")]
    [InfoBox("勾选后校验时比对 DevelSnapShotData 可读字段，Console 可看到具体差异字段。\n不勾选使用二进制 PooledWriter 逐字节比对（默认，快）。")]
    [OnValueChanged(nameof(OnCompareModeChanged))]
    public bool useDebugStringCompare = false;

    private void OnCompareModeChanged()
    {
        BaseSnapShotData.UseDebugStringCompare = useDebugStringCompare;
    }

    public void Update()
    {
        BaseWorld baseWorld = WorldSystem.Instance?.CurrentRunWorld;

        if (baseWorld == null)
        {
            return;
        }

        currentTick = baseWorld.AuthorityTick;
    }
}
