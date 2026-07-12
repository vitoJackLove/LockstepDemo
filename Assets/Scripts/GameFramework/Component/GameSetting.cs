using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 全局游戏设置，可在编辑器中配置逻辑帧率等运行时参数。
/// </summary>
[CreateAssetMenu(menuName = "RogueLike/GameSetting")]
public class GameSetting : ScriptableObject
{
    public const int DefaultLogicFrameRate = 30;
    public const int MinLogicFrameRate = 1;
    public const int MaxLogicFrameRate = 120;

    [LabelText("逻辑帧率 (FPS)")]
    [MinValue(MinLogicFrameRate)]
    [MaxValue(MaxLogicFrameRate)]
    public int logicFrameRate = DefaultLogicFrameRate;

    /// <summary>
    /// 单帧逻辑 deltaTime（秒）。
    /// </summary>
    public float LogicDeltaTimeSeconds => 1f / Mathf.Clamp(logicFrameRate, MinLogicFrameRate, MaxLogicFrameRate);

    public int GetClampedLogicFrameRate()
    {
        return Mathf.Clamp(logicFrameRate, MinLogicFrameRate, MaxLogicFrameRate);
    }
}
