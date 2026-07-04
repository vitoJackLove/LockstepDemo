using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cysharp.Threading.Tasks;
using HybridCLR;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Rogue.Bootstrap.HotUpdate
{
    /// <summary>
    /// 基于 Addressables 的热更新程序集加载策略。
    /// </summary>
    public sealed class AddressablesHotUpdateAssemblyLoader : IGameHotUpdateAssemblyLoader
    {
        public async UniTask<Assembly> LoadAsync(GameHotUpdateEntryOptions options)
        {
            AsyncOperationHandle<TextAsset> handle =
                Addressables.LoadAssetAsync<TextAsset>(options.HotUpdateAssemblyAddress);

            try
            {
                await handle.ToUniTask();
                if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
                {
                    throw new FileNotFoundException(
                        $"Addressables 加载热更程序集失败: {options.HotUpdateAssemblyAddress}, status={handle.Status}");
                }

                byte[] dllBytes = handle.Result.bytes;
                Assembly loadedAssembly = Assembly.Load(dllBytes);
                GameLog.Info(GameLogChannel.Bootstrap, $"已加载热更新程序集: {loadedAssembly.FullName}");
                return loadedAssembly;
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
