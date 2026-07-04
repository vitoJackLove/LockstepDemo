using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 创建世界
/// </summary>
public class CreateWorldData : IPool
{
    /// <summary>
    /// 实体ID
    /// </summary>
    public uint WorldId;

    /// <summary>
    /// 场景名
    /// </summary>
    public string SceneName;
    
    /// <summary>
    /// 世界根节点
    /// </summary>
    public Transform WorldRoot;

    /// <summary>
    /// 选择的英雄配置
    /// </summary>
    public List<PlayerData> HeroDataList = new List<PlayerData>();

    /// <summary>
    /// 当前世界会话模式。
    /// </summary>
    public GameSessionModeType SessionMode = GameSessionModeType.Online;

    /// <summary>
    /// 当前世界会话配置，决定帧同步能力与实体同步策略。
    /// </summary>
    public IGameSessionProfile SessionProfile;

    /// <summary>
    /// 地图块信息
    /// </summary>
    public MapTerrainCellSetting MapTerrainCell;

    /// <summary>
    /// 地图噪音
    /// </summary>
    public TerrainNoiseDataSetting NoiseDataSetting;
    
    public void Clear()
    {
        WorldId = 0;
        WorldRoot = null;
        SceneName = null;
        SessionMode = GameSessionModeType.Online;
        SessionProfile = null;
        HeroDataList.Clear();
        MapTerrainCell = null;
        NoiseDataSetting = null;
    }
}
