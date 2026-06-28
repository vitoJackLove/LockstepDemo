using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 游戏预测数据
/// </summary>
public class GameRollBackContent : ScriptableObject
{
    [LabelText("预测帧数")]
    public uint forecastTick = 2;

    [LabelText("最大预测领先帧数")]
    public uint maxPredictionTick = 20;

    [LabelText("模拟丢包率")]
    public float lossPacket = 0.2f;
}
