using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public class ResourceComponent : RunTimeComponent
{
     /// <summary>
     /// 资源加载类型
     /// </summary>
     public ResourceMode GameResourceMode;
     
     public async UniTask<T> AsyncLoadAsset<T>(string path) where T : UnityEngine.Object
     {
          if (string.IsNullOrEmpty(path))
          {
               GameLog.Error(GameLogChannel.Resource, $"ResourceComponent load failed. type={typeof(T).Name}, mode={GameResourceMode}, path is null or empty.");
               return null;
          }
          
          switch (GameResourceMode)
          {
               case ResourceMode.Addressables:
                    return await LoadAddressableAssetAsync<T>(path);
#if UNITY_EDITOR
               case ResourceMode.Editor:
                    return AssetDatabase.LoadAssetAtPath<T>(path);
#endif
          }
          
          return default;
     }

     [Obsolete("Use AsyncLoadAsset<T>(path) instead. HybridCLR fallback is built into typed loading.")]
     public UniTask<UnityEngine.Object> AsyncLoadAssetHybridCompatible(string path)
     {
          return AsyncLoadAsset<UnityEngine.Object>(path);
     }
     
     [Obsolete("Obsolete")]
     public T LoadAsset<T>(string path) where T : UnityEngine.Object
     {
          if (string.IsNullOrEmpty(path))
          {
               GameLog.Error(GameLogChannel.Resource, $"ResourceComponent load failed. type={typeof(T).Name}, mode={GameResourceMode}, path is null or empty.");
               return null;
          }
          
          switch (GameResourceMode)
          {
               case ResourceMode.Addressables:
                    return LoadAddressableAsset<T>(path);
#if UNITY_EDITOR
               case ResourceMode.Editor:
                    return AssetDatabase.LoadAssetAtPath<T>(path);
#endif
          }
          
          return default;
     }

     private static async UniTask<T> LoadAddressableAssetAsync<T>(string path) where T : UnityEngine.Object
     {
          if (ShouldLoadViaLocationFirst<T>())
          {
               return await LoadAddressableAssetViaLocationAsync<T>(path);
          }

          AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(path);
          try
          {
               await handle.ToUniTask();
          }
          catch (Exception)
          {
               // The handle keeps the original OperationException, so report through the common path.
          }

          if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
          {
               return handle.Result;
          }

          if (handle.IsValid())
          {
               Addressables.Release(handle);
          }

          // HybridCLR: catalog may register hot-update ScriptableObject as System.Object.
          return await LoadAddressableAssetViaLocationAsync<T>(path);
     }

     private static async UniTask<T> LoadAddressableAssetViaLocationAsync<T>(string path) where T : UnityEngine.Object
     {
          UnityEngine.Object loadedObject = await LoadAddressableObjectViaLocationAsync(path);
          if (loadedObject == null)
          {
               return null;
          }

          if (loadedObject is T typedAsset)
          {
               return typedAsset;
          }

          GameLog.Error(GameLogChannel.Resource,
              $"Addressables type mismatch. requested={typeof(T).Name}, address={path}, loadedType={loadedObject.GetType().FullName}");
          return null;
     }

     private static async UniTask<UnityEngine.Object> LoadAddressableObjectViaLocationAsync(string path)
     {
          AsyncOperationHandle<IList<IResourceLocation>> locationsHandle =
              Addressables.LoadResourceLocationsAsync(path);
          try
          {
               await locationsHandle.ToUniTask();
          }
          catch (Exception)
          {
               // Report through the common result path below.
          }

          if (locationsHandle.Status != AsyncOperationStatus.Succeeded
              || locationsHandle.Result == null
              || locationsHandle.Result.Count == 0)
          {
               string exception = locationsHandle.OperationException == null
                   ? string.Empty
                   : $" exception={locationsHandle.OperationException}";
               GameLog.Error(GameLogChannel.Resource,
                   $"Addressables location lookup failed. address={path}, status={locationsHandle.Status}.{exception}");
               if (locationsHandle.IsValid())
               {
                    Addressables.Release(locationsHandle);
               }

               return null;
          }

          IResourceLocation location = locationsHandle.Result[0];
          AsyncOperationHandle<UnityEngine.Object> handle = Addressables.LoadAssetAsync<UnityEngine.Object>(location);
          try
          {
               await handle.ToUniTask();
          }
          catch (Exception)
          {
               // The handle keeps the original OperationException, so report through the common path.
          }

          UnityEngine.Object result = GetAddressableResult(handle, path);
          if (locationsHandle.IsValid())
          {
               Addressables.Release(locationsHandle);
          }

          return result;
     }

     private static T LoadAddressableAsset<T>(string path) where T : UnityEngine.Object
     {
          if (ShouldLoadViaLocationFirst<T>())
          {
               UnityEngine.Object loadedObject = LoadAddressableObjectViaLocation(path);
               if (loadedObject is T typedAsset)
               {
                    return typedAsset;
               }

               if (loadedObject != null)
               {
                    GameLog.Error(GameLogChannel.Resource,
                        $"Addressables type mismatch. requested={typeof(T).Name}, address={path}, loadedType={loadedObject.GetType().FullName}");
               }
               else
               {
                    GameLog.Error(GameLogChannel.Resource,
                        $"Addressables load failed. type={typeof(T).Name}, address={path}, status=Failed.");
               }

               return null;
          }

          AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(path);
          handle.WaitForCompletion();

          if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
          {
               return handle.Result;
          }

          if (handle.IsValid())
          {
               Addressables.Release(handle);
          }

          UnityEngine.Object fallbackObject = LoadAddressableObjectViaLocation(path);
          if (fallbackObject is T fallbackAsset)
          {
               return fallbackAsset;
          }

          if (fallbackObject != null)
          {
               GameLog.Error(GameLogChannel.Resource,
                   $"Addressables type mismatch. requested={typeof(T).Name}, address={path}, loadedType={fallbackObject.GetType().FullName}");
          }
          else
          {
               GameLog.Error(GameLogChannel.Resource,
                   $"Addressables load failed. type={typeof(T).Name}, address={path}, status=Failed.");
          }

          return null;
     }

     private static UnityEngine.Object LoadAddressableObjectViaLocation(string path)
     {
          AsyncOperationHandle<IList<IResourceLocation>> locationsHandle =
              Addressables.LoadResourceLocationsAsync(path);
          locationsHandle.WaitForCompletion();

          if (locationsHandle.Status != AsyncOperationStatus.Succeeded
              || locationsHandle.Result == null
              || locationsHandle.Result.Count == 0)
          {
               if (locationsHandle.IsValid())
               {
                    Addressables.Release(locationsHandle);
               }

               return null;
          }

          IResourceLocation location = locationsHandle.Result[0];
          AsyncOperationHandle<UnityEngine.Object> handle = Addressables.LoadAssetAsync<UnityEngine.Object>(location);
          handle.WaitForCompletion();

          UnityEngine.Object result = GetAddressableResult(handle, path);
          if (locationsHandle.IsValid())
          {
               Addressables.Release(locationsHandle);
          }

          return result;
     }

     private static T GetAddressableResult<T>(AsyncOperationHandle<T> handle, string path) where T : UnityEngine.Object
     {
          if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
          {
               return handle.Result;
          }

          string exception = handle.OperationException == null ? string.Empty : $" exception={handle.OperationException}";
          GameLog.Error(GameLogChannel.Resource, $"Addressables load failed. type={typeof(T).Name}, address={path}, status={handle.Status}.{exception}");
          if (handle.IsValid())
          {
               Addressables.Release(handle);
          }

          return null;
     }

     /// <summary>
     /// HybridCLR Player 中热更 ScriptableObject 在 Catalog 登记为 System.Object，
     /// 直接 LoadAssetAsync&lt;T&gt; 会先触发 InvalidKeyException；对 SO 走 Location 路径。
     /// </summary>
     private static bool ShouldLoadViaLocationFirst<T>() where T : UnityEngine.Object
     {
#if UNITY_EDITOR
          return false;
#else
          return typeof(ScriptableObject).IsAssignableFrom(typeof(T));
#endif
     }
     
     public enum ResourceMode
     {
          Addressables = 0,
          Editor = 2,
     }
}
