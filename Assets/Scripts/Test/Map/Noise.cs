using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地图生成核心逻辑
/// </summary>
public static class Noise 
{
    /// <summary>
    /// 生成噪声地图
    /// </summary>
    /// <param name="mapWidth"></param>
    /// <param name="mapHeight"></param>
    /// <param name="randomSeed">随机种子</param>
    /// <param name="noiseScale">噪声尺度</param>
    /// <param name="octaves"></param>
    /// <param name="persistance">持久性</param>
    /// <param name="lacunarity">间隙指数</param>
    /// <param name="mapOffset">噪音偏移</param>
    /// <returns></returns>
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int randomSeed, float noiseScale,
        int octaves,float persistance, float lacunarity, Vector2 mapOffset)
    {
        if (noiseScale <= 0)
        {
            noiseScale = 0.001f;
        }

        //每个八度做随机偏移
        System.Random random = new System.Random(randomSeed);
        Vector2[] randomOctavesOffset = new Vector2[octaves];
        
        for (int i = 0; i < octaves; i++)
        {
            float tempX = random.Next(-100000, 100000) + mapOffset.x;
            float tempY = random.Next(-100000, 100000) + mapOffset.y;
            randomOctavesOffset[i] = new Vector2(tempX, tempY);
        }
        
        float maxNoiseMapValue = float.MinValue;
        float minNoiseMapValue = float.MaxValue;
        
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;
        
        float[,] noiseMap = new float[mapWidth, mapHeight];

        for (int i = 0; i < mapWidth; i++)
        {
            for (int j = 0; j < mapHeight; j++)
            {
                //广度
                float amplitude = 1;
                //频率
                float frequency = 1;
                //振幅高度
                float noiseHeight = 0; 
                
                for (int k = 0; k < octaves; k++)
                {
                    float tempX = (i - halfWidth) / noiseScale * frequency + randomOctavesOffset[k].x;
                    float tempY = (j - halfHeight) / noiseScale * frequency + randomOctavesOffset[k].y;
                    float noiseValue = Mathf.PerlinNoise(tempX, tempY) * 2 - 1;

                    noiseHeight += noiseValue * amplitude;
                    
                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxNoiseMapValue)
                {
                    maxNoiseMapValue = noiseHeight;
                }
                else if(noiseHeight < minNoiseMapValue)
                {
                    minNoiseMapValue = noiseHeight;
                }
                
                noiseMap[i, j] = noiseHeight;
            }
        }

        for (int i = 0; i < mapWidth; i++)
        {
            for (int j = 0; j < mapHeight; j++)
            {
                noiseMap[i, j] = Mathf.InverseLerp(minNoiseMapValue, maxNoiseMapValue, noiseMap[i, j]);
            }
        }

        return noiseMap;
    }
}
