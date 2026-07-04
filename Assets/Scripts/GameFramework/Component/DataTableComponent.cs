using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Rogue;
using UnityEngine;

/// <summary>
/// 数据表
/// </summary>
public class DataTableComponent : RunTimeComponent
{
     /// <summary>
     /// 资源表集合 key = 实际的表类型 
     /// </summary>
     private static Dictionary<Type, IAssetsConfig> _assetsConfigDic = new Dictionary<Type, IAssetsConfig>();

     public override async void Init()
     {
          await InitAsync();
     }

     public override async UniTask InitAsync()
     {
          base.Init();
          _assetsConfigDic.Clear();

          if (GameEntry.HotUpdateBootstrap != null)
          {
               await GameEntry.HotUpdateBootstrap.InitializeDataTablesAsync(this);
               return;
          }

          await LoadDataTablesFromResourceAsync();
     }

     public void RegisterAssetsConfig(IAssetsConfig assetsConfig)
     {
          if (assetsConfig == null)
          {
               throw new ArgumentNullException(nameof(assetsConfig));
          }

          Type tableType = assetsConfig.GetDataTableType();
          _assetsConfigDic[tableType] = assetsConfig;
     }

     private async UniTask LoadDataTablesFromResourceAsync()
     {
          ResourceComponent resourceComponent = GameEntryRunTime.GetComponent<ResourceComponent>();

          for (int i = 0; i < DataTableHelper.DataTableNames.Length; i++)
          {
               string tableName = DataTableHelper.DataTableNames[i];
               string address = AssetsPathHelper.GameAssetsConfigHelper(tableName);
               ScriptableObject loadedObject =
                   await resourceComponent.AsyncLoadAsset<ScriptableObject>(address);
               if (loadedObject == null)
               {
                    throw new InvalidOperationException(
                        $"DataTable load failed. address={address}, table={tableName}");
               }

               if (loadedObject is not IAssetsConfig assetsConfig)
               {
                    throw new InvalidOperationException(
                        $"DataTable type mismatch. address={address}, loadedType={loadedObject.GetType().FullName}");
               }

               RegisterAssetsConfig(assetsConfig);
          }
     }

     public T GetDataTable<T>(int id) where T : EntityAssetsConfig
     {
         Type type = typeof(T);

         if (_assetsConfigDic.TryGetValue(type, out IAssetsConfig assetsConfig))
         {
             return (T)assetsConfig.GetDataTable(id);
         }

         return null;
     }

     public List<T> GetAllDataTable<T>() where T :EntityAssetsConfig
     {
         Type type = typeof(T);

         if (_assetsConfigDic.TryGetValue(type, out IAssetsConfig assetsConfig))
         {
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
