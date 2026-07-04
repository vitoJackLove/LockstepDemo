using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using HybridCLR;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Rogue.Bootstrap.HotUpdate
{
    /// <summary>
    /// 基于 Addressables 的 AOT 元数据补充策略。
    /// </summary>
    public sealed class AddressablesAotMetadataLoader : IGameAotMetadataLoader
    {
        public async UniTask<int> LoadAsync(GameHotUpdateEntryOptions options)
        {
            IList<IResourceLocation> locations = options.AutoDiscoverAotAssemblies
                ? await LoadLocationsByLabelAsync(options.AotMetadataLabel)
                : BuildManualLocations(options);

            if (locations == null || locations.Count == 0)
            {
                throw new FileNotFoundException(
                    $"未找到 AOT 元数据资源。label={options.AotMetadataLabel}, autoDiscover={options.AutoDiscoverAotAssemblies}");
            }

            int loadedCount = 0;
            for (int i = 0; i < locations.Count; i++)
            {
                loadedCount += await LoadSingleMetadataAsync(locations[i]);
            }

            return loadedCount;
        }

        private static async UniTask<IList<IResourceLocation>> LoadLocationsByLabelAsync(string label)
        {
            AsyncOperationHandle<IList<IResourceLocation>> locHandle =
                Addressables.LoadResourceLocationsAsync(label, typeof(TextAsset));

            try
            {
                await locHandle.ToUniTask();
                if (locHandle.Status != AsyncOperationStatus.Succeeded || locHandle.Result == null)
                {
                    throw new FileNotFoundException($"未找到 AOT 元数据 Label: {label}");
                }

                return locHandle.Result;
            }
            finally
            {
                if (locHandle.IsValid())
                {
                    Addressables.Release(locHandle);
                }
            }
        }

        private static IList<IResourceLocation> BuildManualLocations(GameHotUpdateEntryOptions options)
        {
            List<IResourceLocation> locations = new List<IResourceLocation>();
            for (int i = 0; i < options.AotAssemblyNames.Length; i++)
            {
                string assemblyName = options.AotAssemblyNames[i];
                if (string.IsNullOrWhiteSpace(assemblyName))
                {
                    continue;
                }

                string address = HotUpdateCodePaths.GetAotMetadataAddress(assemblyName);
                AsyncOperationHandle<IList<IResourceLocation>> locHandle =
                    Addressables.LoadResourceLocationsAsync(address, typeof(TextAsset));

                locHandle.WaitForCompletion();
                try
                {
                    if (locHandle.Status == AsyncOperationStatus.Succeeded
                        && locHandle.Result != null
                        && locHandle.Result.Count > 0)
                    {
                        locations.Add(locHandle.Result[0]);
                    }
                    else
                    {
                        GameLog.Warn(GameLogChannel.Bootstrap, $"跳过缺失的 AOT 元数据 Address: {address}");
                    }
                }
                finally
                {
                    if (locHandle.IsValid())
                    {
                        Addressables.Release(locHandle);
                    }
                }
            }

            return locations;
        }

        private static async UniTask<int> LoadSingleMetadataAsync(IResourceLocation location)
        {
            AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(location);
            try
            {
                await handle.ToUniTask();
                if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
                {
                    GameLog.Warn(GameLogChannel.Bootstrap,
                        $"跳过 AOT 元数据: {location.PrimaryKey}, status={handle.Status}");
                    return 0;
                }

                LoadImageErrorCode errorCode = RuntimeApi.LoadMetadataForAOTAssembly(
                    handle.Result.bytes,
                    HomologousImageMode.SuperSet);
                GameLog.Info(GameLogChannel.Bootstrap,
                    $"补充 AOT 元数据: {location.PrimaryKey} => {errorCode}");

                return errorCode == LoadImageErrorCode.OK ? 1 : 0;
            }
            finally
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
        }
    }
}
