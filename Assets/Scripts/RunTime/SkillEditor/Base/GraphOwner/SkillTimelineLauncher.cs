using System;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 技能TimeLine 发射器
/// </summary>
public partial class SkillTimelineLauncher : MonoBehaviour
{
    [LabelText("技能资源")]
    public SkillLineAsset graph;

    [HideInInspector]
    public SkillLineAsset RunTimeGraph;

    /// <summary>
    /// 初始化资源
    /// </summary>
    public void InitAssets(BaseEntity entity, Action onTimeLineStopAction = null)
    {
        RunTimeGraph = InitAsset(entity, graph, onTimeLineStopAction);
    }
}