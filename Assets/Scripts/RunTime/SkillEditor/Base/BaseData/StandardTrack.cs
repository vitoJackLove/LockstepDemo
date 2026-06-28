using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 轨道类型
/// </summary>
public enum TrackType
{
    /// <summary>
    /// 纯逻辑
    /// </summary>
    Logic,
    
    /// <summary>
    /// 纯表现
    /// </summary>
    View,
}

[Serializable]
public abstract class StandardTrack
{
    /// <summary>
    /// 轨道类型
    /// </summary>
    public abstract TrackType TrackType { get; }

    [SerializeReference] public List<TaskClip> taskClips;
    
    public StandardTrack()
    {
        taskClips = new List<TaskClip>();
    }
}