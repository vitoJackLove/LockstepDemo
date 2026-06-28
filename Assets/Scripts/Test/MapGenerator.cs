using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public Material textureRenderer;

    /// <summary>
    /// 地势信息
    /// </summary>
    public MapTerrainCellSetting mapTerrainCellSetting;

    /// <summary>
    /// 地势噪音
    /// </summary>
    public TerrainNoiseDataSetting terrainNoiseDataSetting;

    public void Start()
    {
        if (mapTerrainCellSetting == null)
        {
            Debug.LogError($"地图生成错误：没有基础的地图块信息...");
            return;
        }

        for (int i = 0; i < mapTerrainCellSetting._mapTerrainData.Count; i++)
        {
            if (mapTerrainCellSetting._mapTerrainData[i].mapCell == null)
            {
                Debug.LogError($"地图块生成错误: {mapTerrainCellSetting._mapTerrainData[i].mapTerrainType} 类型地图块预制体为空...");

                return;
            }
        }
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GenerateNoise();
        }
    }

    private void GenerateNoise()
    {
        int randomSeed = terrainNoiseDataSetting.randomSeed;
        int width = terrainNoiseDataSetting.width;
        int height = terrainNoiseDataSetting.height;
        float noiseScale = terrainNoiseDataSetting.noiseScale;
        int octaves = terrainNoiseDataSetting.octaves;
        float persistance = terrainNoiseDataSetting.persistance;
        float lacunarity = terrainNoiseDataSetting.lacunarity;
        Vector2 mapOffset = terrainNoiseDataSetting.mapOffset;

        float[,] noiseMap = Noise.GenerateNoiseMap(width, height, randomSeed, noiseScale, octaves, persistance,
            lacunarity, mapOffset);

        if (terrainNoiseDataSetting.MapGenerateType == MapGenerateType.GameObject)
        {
            GenerateGameObjectMap(noiseMap);
        }
        else
        {
            GenerateTextureMap(noiseMap);
        }
    }

    private void GenerateGameObjectMap(float[,] noiseMap)
    {
        int width = terrainNoiseDataSetting.width;
        int height = terrainNoiseDataSetting.height;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                MapTerrainData mapTerrainData = GetMapTerrainCell(noiseMap[i, j]);

                GameObject go = GameObject.Instantiate(mapTerrainData.mapCell);

                go.transform.position = new Vector3(i, 0, j);
            }
        }
    }

    private void GenerateTextureMap(float[,] noiseMap)
    {
        int width = terrainNoiseDataSetting.width;
        int height = terrainNoiseDataSetting.height;

        Texture2D texture = new Texture2D(width, height);
        Color[] colors = new Color[width * height];

        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                MapTerrainData mapTerrainData = GetMapTerrainCell(noiseMap[i, j]);
                colors[j * width + i] = mapTerrainData.color;
            }
        }

        texture.SetPixels(colors);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();
        textureRenderer.mainTexture = texture;
    }

    private MapTerrainData GetMapTerrainCell(float mapCellValue)
    {
        for (int i = 0; i < mapTerrainCellSetting._mapTerrainData.Count; i++)
        {
            if (mapTerrainCellSetting._mapTerrainData[i].imposeValue.x <= mapCellValue &&
                mapCellValue < mapTerrainCellSetting._mapTerrainData[i].imposeValue.y)
            {
                return mapTerrainCellSetting._mapTerrainData[i];
            }
        }

        return mapTerrainCellSetting._mapTerrainData[^1];
    }
}