using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
public partial class MapGenerateWindow : OdinEditorWindow
{
    [OnValueChanged("OnMapTerrainDataChanged")]
    [TabGroup(TabOne,MapTerrainSettingGroupName,SdfIconType.BarChartLineFill,TextColor = "green")]
    [LabelText("地势数据",SdfIconType.Bag)]
    [TableList(DrawScrollView = true, MaxScrollViewHeight = 200, MinScrollViewHeight = 1000,DefaultMinColumnWidth = 100,CellPadding = 10)]
    public List<MapTerrainData> mapTerrainData = new List<MapTerrainData>();

    public void OnMapTerrainDataChanged()
    {
        _mapTerrainCellSetting._mapTerrainData.Clear();

        for (int i = 0; i < mapTerrainData.Count; i++)
        {
            _mapTerrainCellSetting._mapTerrainData.Add(mapTerrainData[i]);
        }
    }
}
