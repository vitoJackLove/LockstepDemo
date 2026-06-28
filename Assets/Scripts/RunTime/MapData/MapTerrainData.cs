using System;
using Sirenix.OdinInspector;
using UnityEngine;
/// <summary>
/// 地图地势数据
/// </summary>
[Serializable]
public class MapTerrainData 
{
    [HorizontalGroup("游戏物体")]
    [AssetsOnly]
    [InfoBox("地图GameObject模式使用...")]
    [LabelText("地块预制体")]
    [InlineEditor(InlineEditorModes.LargePreview)]
    public GameObject mapCell;
    
    [Space(30)]
    [TitleGroup("地势数据")]
    [LabelText("预制体路径")]
    public string assetsPath;
    
    [Space(30)]
    [TitleGroup("地势数据")]
    [LabelText("地图块类型")]
    public MapTerrainType mapTerrainType;
    
    [Space(30)]
    [TitleGroup("地势数据")]
    [MinMaxSlider(0, 1, true)]
    [LabelText("生成概率")]
    public Vector2 imposeValue;

    [Space(30)]
    [TitleGroup("地势数据")]
    [InfoBox("地图Texture模式使用...")]
    [LabelText("地块颜色")]
    public Color color;
}