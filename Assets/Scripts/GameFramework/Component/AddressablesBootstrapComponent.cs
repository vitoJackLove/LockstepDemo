using System.Threading;
using Cysharp.Threading.Tasks;
using Rogue;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Addressables 初始化、远程 URL 注入与 Catalog 更新。
/// </summary>
public class AddressablesBootstrapComponent : RunTimeComponent
{
    private const string RemoteBaseUrlPropertyName = "AddressablesRemoteBaseUrl";
    private const string DevRemoteUrlPrefsKey = "Addressables.RemoteBaseUrlOverride";

    private static bool _addressablesInitialized;

    [SerializeField] private AddressablesContentSettings _settings;

    /// <summary>
    /// PreBootstrap：幂等初始化 Addressables，并按策略可选更新 Catalog。
    /// </summary>
    public async UniTask PreBootstrapAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        if (ShouldSkipCatalogCheck())
        {
            GameLog.Info(GameLogChannel.Bootstrap, "PreBootstrap 跳过 Catalog 更新。");
            return;
        }

        await TryUpdateCatalogsAsync(cancellationToken);
    }

    public async UniTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        if (ShouldSkipCatalogCheck())
        {
            return;
        }

        await TryUpdateCatalogsAsync(cancellationToken);
    }

    /// <summary>
    /// 幂等执行 Addressables.InitializeAsync。
    /// </summary>
    public async UniTask EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        ResourceComponent resourceComponent = GameEntryRunTime.GetComponent<ResourceComponent>();
        if (resourceComponent == null
            || resourceComponent.GameResourceMode != ResourceComponent.ResourceMode.Addressables)
        {
            return;
        }

        if (_addressablesInitialized)
        {
            return;
        }

        ApplyRemoteBaseUrl();
        await Addressables.InitializeAsync().ToUniTask(cancellationToken: cancellationToken);
        _addressablesInitialized = true;
    }

    public async UniTask TryUpdateCatalogsAsync(CancellationToken cancellationToken = default)
    {
        float timeoutSeconds = _settings == null ? 10f : _settings.CatalogUpdateTimeoutSeconds;
        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(System.TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            AsyncOperationHandle<System.Collections.Generic.List<string>> checkHandle =
                Addressables.CheckForCatalogUpdates(false);
            await checkHandle.ToUniTask(cancellationToken: timeoutCts.Token);

            if (checkHandle.Status != AsyncOperationStatus.Succeeded
                || checkHandle.Result == null
                || checkHandle.Result.Count == 0)
            {
                if (checkHandle.IsValid())
                {
                    Addressables.Release(checkHandle);
                }

                return;
            }

            var updateHandle = Addressables.UpdateCatalogs(checkHandle.Result, false);
            await updateHandle.ToUniTask(cancellationToken: timeoutCts.Token);

            if (updateHandle.IsValid())
            {
                Addressables.Release(updateHandle);
            }

            if (checkHandle.IsValid())
            {
                Addressables.Release(checkHandle);
            }

            GameLog.Info(GameLogChannel.Resource, "Addressables catalog updated.");
        }
        catch (System.OperationCanceledException)
        {
            GameLog.Warn(GameLogChannel.Resource, "Addressables catalog update timed out. Continue with local catalog.");
        }
        catch (System.Exception ex)
        {
            GameLog.Warn(GameLogChannel.Resource,
                $"Addressables catalog update failed. Continue with local catalog. {ex.Message}");
        }
    }

    private void ApplyRemoteBaseUrl()
    {
        string remoteBaseUrl = ResolveRemoteBaseUrl();
        if (string.IsNullOrEmpty(remoteBaseUrl))
        {
            return;
        }

        AddressablesRuntimeProperties.SetPropertyValue(
            RemoteBaseUrlPropertyName,
            remoteBaseUrl.TrimEnd('/'));
    }

    private string ResolveRemoteBaseUrl()
    {
        if (_settings != null && _settings.ForceLocalOnly)
        {
            return string.Empty;
        }

        if (GameEntry.FrameSyncTransport is IAddressablesRemoteUrlProvider provider
            && provider.TryGetRemoteBaseUrl(out string serverUrl)
            && !string.IsNullOrEmpty(serverUrl))
        {
            return serverUrl;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        string overrideUrl = PlayerPrefs.GetString(DevRemoteUrlPrefsKey, string.Empty);
        if (!string.IsNullOrEmpty(overrideUrl))
        {
            return overrideUrl;
        }
#endif

#if UNITY_EDITOR
        if (_settings != null
            && _settings.UseLocalDevRemoteInEditor
            && !string.IsNullOrEmpty(_settings.LocalDevRemoteBaseUrl))
        {
            return _settings.LocalDevRemoteBaseUrl;
        }
#endif

        return _settings == null ? string.Empty : _settings.DefaultRemoteBaseUrl;
    }

    private bool ShouldSkipCatalogCheck()
    {
        if (_settings != null && _settings.ForceLocalOnly)
        {
            return true;
        }

        if (ClientAgentGameEntryMode.IsRunning)
        {
            return true;
        }

        if (_settings != null
            && _settings.SkipCatalogCheckInBatchMode
            && Application.isBatchMode)
        {
            return true;
        }

        if (_settings != null && !_settings.EnableCatalogUpdateOnStartup)
        {
            return true;
        }

        if (_settings != null && !_settings.EnableHotUpdateCatalogCheck)
        {
            return true;
        }

        return string.IsNullOrEmpty(ResolveRemoteBaseUrl());
    }
}
