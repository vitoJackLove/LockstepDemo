using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "RogueLike/CubeAssets")]
[Serializable]
public class CubeAssets: ScriptableObject , IAssetsConfig
{
     [LabelText("魔方列表")]
     public List<CubeAssetsConfig> CubeAssetsList = new List<CubeAssetsConfig>();

     public Type GetDataTableType()
     {
          return typeof(CubeAssetsConfig);
     }

     public EntityAssetsConfig GetDataTable(int id)
     {
          for (int i = 0; i < CubeAssetsList.Count; i++)
          {
               if (CubeAssetsList[i].assetsId == id)
               {
                    return CubeAssetsList[i];
               }
          }

          return null;
     }

     public List<EntityAssetsConfig> GetAllDataTable()
     {
          List<EntityAssetsConfig> list = new List<EntityAssetsConfig>();

          for (int i = 0; i < CubeAssetsList.Count; i++)
          {
               list.Add(CubeAssetsList[i]);
          }

          return list;
     }
}

[Serializable]
public class CubeAssetsConfig : EntityAssetsConfig
{
     [LabelText("魔方预制体")]
     public GameObject cubePrefab;

     [LabelText("魔方类型")]
     public CubeType cubeType;
}
