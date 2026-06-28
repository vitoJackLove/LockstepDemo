using System.Collections.Generic;
using Rogue;
using UnityEngine;

/// <summary>
/// 地图系统 主要处理地图
/// </summary>
public class MapSystem : BaseSystem
{
    /// <summary>
    /// 地图块集合
    /// </summary>
    private Dictionary<uint, CubeEntity> _cubeEntityDic = new Dictionary<uint, CubeEntity>();

    /// <summary>
    /// 地势信息
    /// </summary>
    private MapTerrainCellSetting _mapTerrainCellSetting;

    /// <summary>
    /// 地势噪音
    /// </summary>
    private TerrainNoiseDataSetting _terrainNoiseDataSetting;

    public async void RamGenerateMonster()
    {
        uint id = (uint)Random.Range(0, _cubeEntityDic.Count);

        CubeEntity cubeEntity = _cubeEntityDic[id];

        MonsterAssetsConfig monsterAssetsConfig = GameEntry.DataTable.GetDataTable<MonsterAssetsConfig>(2001);

        GameObject go = await GetSystem<EntityViewSystem>().SyncGetEntityView(monsterAssetsConfig.assetsPath);
    }
    
    /// <summary>
    /// 生成地图
    /// </summary>
    /// <param name="mapTerrainCellSetting"></param>
    /// <param name="terrainNoiseDataSetting"></param>
    public void GenerateMap(MapTerrainCellSetting mapTerrainCellSetting, TerrainNoiseDataSetting terrainNoiseDataSetting)
    {
        this._mapTerrainCellSetting = mapTerrainCellSetting;
        this._terrainNoiseDataSetting = terrainNoiseDataSetting;
        
        if (_terrainNoiseDataSetting == null)
        {
            Debug.Log($"地图噪音为空...");
            
            return;
        }

        if (_mapTerrainCellSetting == null)
        {
            Debug.Log($"地图块信息为空...");
            
            return;
        }
        
        GenerateNoise();
    }
    
    private void GenerateNoise()
    {
        int randomSeed = _terrainNoiseDataSetting.randomSeed;
        int width = _terrainNoiseDataSetting.width;
        int height = _terrainNoiseDataSetting.height;
        float noiseScale = _terrainNoiseDataSetting.noiseScale;
        int octaves = _terrainNoiseDataSetting.octaves;
        float persistance = _terrainNoiseDataSetting.persistance;
        float lacunarity = _terrainNoiseDataSetting.lacunarity;
        Vector2 mapOffset = _terrainNoiseDataSetting.mapOffset;

        float[,] noiseMap = Noise.GenerateNoiseMap(width, height, randomSeed, noiseScale, octaves, persistance,
            lacunarity, mapOffset);

        if (_terrainNoiseDataSetting.MapGenerateType == MapGenerateType.GameObject)
        {
            GenerateGameObjectMap(noiseMap);
        }
        else
        {
            Debug.Log($"图片生成未开放...");
            //GenerateTextureMap(noiseMap);
        }
    }
    
    private async void GenerateGameObjectMap(float[,] noiseMap)
    {
        int width = _terrainNoiseDataSetting.width;
        int height = _terrainNoiseDataSetting.height;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                /*MapTerrainData mapTerrainData = GetMapTerrainCell(noiseMap[i, j]);

                GameObject go = await GetSystem<EntityViewSystem>()
                    .GetEntityView(mapTerrainData.assetsPath, CurrentWorld.MapRoot);
                
                Vector3 scale = new Vector3(1.5f, 1, 1.5f);

                Vector3 position = new Vector3(i * scale.x, 0, j * scale.z);

                EntityCreateData createData = EntityCreateData.Create(position, Vector3.zero, scale, go);
                
                CubeEntity cubeEntity = GetSystem<EntitySystem>().CreateEntity<CubeEntity>(createData);

                _cubeEntityDic.Add(cubeEntity.EntityId, cubeEntity);*/
            }
        }
    }

    private MapTerrainData GetMapTerrainCell(float mapCellValue)
    {
        for (int i = 0; i < _mapTerrainCellSetting._mapTerrainData.Count; i++)
        {
            if (_mapTerrainCellSetting._mapTerrainData[i].imposeValue.x <= mapCellValue &&
                mapCellValue < _mapTerrainCellSetting._mapTerrainData[i].imposeValue.y)
            {
                return _mapTerrainCellSetting._mapTerrainData[i];
            }
        }

        return _mapTerrainCellSetting._mapTerrainData[^1];
    }
}
