using System;
using Cysharp.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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
                    return  AssetDatabase.LoadAssetAtPath<T>(path);
#endif
          }
          
          return default;
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
                    return  AssetDatabase.LoadAssetAtPath<T>(path);
#endif
          }
          
          return default;
     }

     private static async UniTask<T> LoadAddressableAssetAsync<T>(string path) where T : UnityEngine.Object
     {
          AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(path);
          try
          {
               await handle.ToUniTask();
          }
          catch (Exception)
          {
               // The handle keeps the original OperationException, so report through the common path.
          }

          return GetAddressableResult(handle, path);
     }

     private static T LoadAddressableAsset<T>(string path) where T : UnityEngine.Object
     {
          AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(path);
          handle.WaitForCompletion();
          return GetAddressableResult(handle, path);
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
     
     public enum ResourceMode
     {
          Addressables = 0,
          Editor = 2,
     }
}


