using System;
using UnityEngine;

[CreateAssetMenu(menuName = "RougeLikeMap/TerrainNoiseDataSetting")]
[Serializable]
public class TerrainNoiseDataSetting : ScriptableObject
{
    /// <summary>
    /// 地图生成方式
    /// </summary>
    public MapGenerateType MapGenerateType;
    
    public int width;
    
    public int height;

    /// <summary>
    /// 随机种子
    /// </summary>
    public int randomSeed;
    
    /// <summary>
    /// 间隙指数
    /// </summary>
    [Range(0, 5f)]
    public float lacunarity;

    /// <summary>
    /// 噪音尺寸
    /// </summary>
    public float noiseScale;

    /// <summary>
    /// 八度
    /// </summary>
    public int octaves;

    /// <summary>
    /// 持久性
    /// </summary>
    public float persistance;

    /// <summary>
    /// 地图偏移
    /// </summary>
    public Vector2 mapOffset;
}
