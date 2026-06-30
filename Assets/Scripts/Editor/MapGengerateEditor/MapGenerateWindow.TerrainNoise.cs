using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 噪音值设置
/// </summary>
public partial class MapGenerateWindow : OdinEditorWindow
{
     /// <summary>
     /// 地势噪音数据路径
     /// </summary>
     private const string TerrainNoiseDataSettingPath = "Assets/Config/Terrain Noise Data Setting.asset";
     
     /// <summary>
     /// 地势数据路径
     /// </summary>
     private const string MapTerrainCellSettingPath = "Assets/Config/Map Terrain Cell Setting.asset";
     

     public const string TabOne = "TabOne";
     public const string TabTwo = "TabTwo";
     
     /// <summary>
     /// 地势噪音设置
     /// </summary>
     private const string TerrainNoiseDataSettingGroupName = "地势噪音参数设置";
     
     /// <summary>
     /// 地势数据设置
     /// </summary>
     private const string MapTerrainSettingGroupName = "地势数据";
     
     /// <summary>
     /// 地势噪音数据
     /// </summary>
     private TerrainNoiseDataSetting _terrainNoiseSetting;

     /// <summary>
     /// 地图地势块数据
     /// </summary>
     private MapTerrainCellSetting _mapTerrainCellSetting;
     
     [MenuItem("Tools/地图/MapGenerate")]
     public static void OpenWindow()
     {
          MapGenerateWindow window = GetWindow<MapGenerateWindow>($"地图制作工具");
     }

     public void Awake()
     {
          //地势噪音设置
          _terrainNoiseSetting =
               InitAssets<TerrainNoiseDataSetting>(TerrainNoiseDataSettingPath, "Terrain Noise Data Setting");
          //地势块噪音设置
          _mapTerrainCellSetting =
               InitAssets<MapTerrainCellSetting>(MapTerrainCellSettingPath, "Map Terrain Cell Setting");
          
          AssetDatabase.Refresh();

          InitData();
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
     
     [Title("噪音参数")]
     [Space(10)]

     [TabGroup(TabOne,TerrainNoiseDataSettingGroupName,SdfIconType.Image,TextColor = "blue")]
     [InfoBox("调试用照片...")]
     [LabelText("地图生成方式")]
     [OnValueChanged("OnNoiseValueChanged")]
     public MapGenerateType mapGenerateType;
     
     [TabGroup(TabOne,TerrainNoiseDataSettingGroupName,SdfIconType.Image,TextColor = "blue")]
     [InfoBox("用于地图随机的参数...")]
     [LabelText("随机种子")]
     [OnValueChanged("OnNoiseValueChanged")]
     public int randomSeed;
     
     [TabGroup(TabOne,TerrainNoiseDataSettingGroupName,SdfIconType.Image,TextColor = "blue")]
     [InfoBox("地图的宽度不能小于1")]
     [LabelText("地图宽度")]
     [OnValueChanged("OnNoiseValueChanged")]
     public int width;
    
     [Space]
     [TabGroup(TabOne,TerrainNoiseDataSettingGroupName,SdfIconType.Image,TextColor = "blue")]
     [InfoBox("地图的长度不能小于1")]
     [MinValue(1)]
     [LabelText("地图宽度")]
     [OnValueChanged("OnNoiseValueChanged")]
     public int height;
     
     [Space]
     [TabGroup(TabOne,TerrainNoiseDataSettingGroupName,SdfIconType.Image,TextColor = "blue")]
     [InfoBox("值越大噪音地图范围越小...")]
     [LabelText("噪音尺寸")]
     [MinValue(1)]
     [OnValueChanged("OnNoiseValueChanged")]
     public float noiseScale;
    
     [Space]
     [TabGroup(TabOne,TerrainNoiseDataSettingGroupName,SdfIconType.Image,TextColor = "blue")]
     [Range(0, 5f)]
     [InfoBox("值越大噪音地图越碎片化...")]
     [LabelText("间隙指数")]
     [OnValueChanged("OnNoiseValueChanged")]
     public float lacunarity;

     [Space]
     [TabGroup(TabOne,TerrainNoiseDataSettingGroupName,SdfIconType.Image,TextColor = "blue")]
     [InfoBox("噪音循环波动的次数...")]
     [Range(1, 50)]
     [LabelText("八度")]
     [OnValueChanged("OnNoiseValueChanged")]
     public int octaves;

     [Space]
     [TabGroup(TabOne,TerrainNoiseDataSettingGroupName,SdfIconType.Image,TextColor = "blue")]
     [InfoBox("值越大噪音波动越大,地图越碎片化...")]
     [LabelText("噪音的持久度")]
     [Range(0.1f, 2f)]
     [OnValueChanged("OnNoiseValueChanged")]
     public float persistance;

     [Space]
     [TabGroup(TabOne,TerrainNoiseDataSettingGroupName,SdfIconType.Image,TextColor = "blue")]
     [InfoBox("主要控制地图的UV...")]
     [LabelText("地图偏移")]
     [OnValueChanged("OnNoiseValueChanged")]
     public Vector2 mapOffset;

     private void InitData()
     {
          //地势噪音
          mapGenerateType = _terrainNoiseSetting.MapGenerateType;
          width = _terrainNoiseSetting.width;
          height = _terrainNoiseSetting.height;
          noiseScale = _terrainNoiseSetting.noiseScale;
          lacunarity = _terrainNoiseSetting.lacunarity;
          octaves = _terrainNoiseSetting.octaves;
          persistance = _terrainNoiseSetting.persistance;
          mapOffset = _terrainNoiseSetting.mapOffset;
          randomSeed = _terrainNoiseSetting.randomSeed;
          
          //地势块数据
          mapTerrainData.Clear();
          for (int i = 0; i < _mapTerrainCellSetting._mapTerrainData.Count; i++)
          {
               mapTerrainData.Add(_mapTerrainCellSetting._mapTerrainData[i]);
          }
     }
     
     public void OnNoiseValueChanged()
     {
          _terrainNoiseSetting.MapGenerateType = mapGenerateType;
          _terrainNoiseSetting.width = width;
          _terrainNoiseSetting.height = height;
          _terrainNoiseSetting.noiseScale = noiseScale;
          _terrainNoiseSetting.lacunarity = lacunarity;
          _terrainNoiseSetting.octaves = octaves;
          _terrainNoiseSetting.persistance = persistance;
          _terrainNoiseSetting.mapOffset = mapOffset;
          _terrainNoiseSetting.randomSeed = randomSeed;
     }
}
