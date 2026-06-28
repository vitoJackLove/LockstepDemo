using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RougeLikeMap/MapTerrainCellSetting")]
[Serializable]
public class MapTerrainCellSetting : ScriptableObject
{
     public List<MapTerrainData> _mapTerrainData = new List<MapTerrainData>();
}
