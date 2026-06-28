using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 数据表
/// </summary>
public class DataTableComponent : RunTimeComponent
{
     /// <summary>
     /// 资源表集合 key = 实际的表类型 
     /// </summary>
     private static Dictionary<Type, ScriptableObject> _assetsConfigDic = new Dictionary<Type, ScriptableObject>();

     public override async void Init()
     {
          await InitAsync();
     }

     public override async UniTask InitAsync()
     {
          base.Init();
          _assetsConfigDic.Clear();

          ResourceComponent resourceComponent = GameEntryRunTime.GetComponent<ResourceComponent>();
          
          for (int i = 0; i < DataTableHelper.DataTableNames.Length; i++)
          {
              ScriptableObject scriptableObject =
                  await resourceComponent.AsyncLoadAsset<ScriptableObject>(
                      AssetsPathHelper.GameAssetsConfigHelper(DataTableHelper.DataTableNames[i]));

              IAssetsConfig assetsConfig = (IAssetsConfig)scriptableObject;

              _assetsConfigDic.Add(assetsConfig.GetDataTableType(), scriptableObject);
          }
     }

     public T GetDataTable<T>(int id) where T : EntityAssetsConfig
     {
         Type type = typeof(T);

         if (_assetsConfigDic.TryGetValue(type, out var value))
         {
             IAssetsConfig assetsConfig = (IAssetsConfig)value;

             return (T)assetsConfig.GetDataTable(id);
         }

         return null;
     }

     public List<T> GetAllDataTable<T>() where T :EntityAssetsConfig
     {
         Type type = typeof(T);

         if (_assetsConfigDic.TryGetValue(type, out var value))
         {
             IAssetsConfig assetsConfig = (IAssetsConfig)value;

             List<EntityAssetsConfig> entityAssetsConfigList = assetsConfig.GetAllDataTable();

             List<T> list = new List<T>();

             for (int i = 0; i < entityAssetsConfigList.Count; i++)
             {
                 list.Add((T)entityAssetsConfigList[i]);
             }

             return list;
         }

         return null;
     }
}
