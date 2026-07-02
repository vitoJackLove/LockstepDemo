using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class HotUpdateTestRunner : MonoBehaviour
{
    [SerializeField]
    private string _cubeAddress = "Assets/HotUpdateTest/Cube.prefab";

    [SerializeField]
    private Vector3 _spawnPosition = new Vector3(0f, 0.5f, 2f);

    [SerializeField]
    private bool _loadOnStart = true;

    private GameObject _spawnedCube;
    private AsyncOperationHandle<GameObject> _cubeHandle;
    private bool _isBusy;

    private void Start()
    {
        if (_loadOnStart)
        {
            LoadCubeAsync().Forget();
        }
    }

    private void OnDestroy()
    {
        ReleaseCubeHandle();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            LoadCubeAsync().Forget();
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            ApplyHotUpdateAsync().Forget();
        }
    }

    private void OnGUI()
    {
        const int width = 320;
        const int height = 130;
        GUILayout.BeginArea(new Rect(16f, 16f, width, height), GUI.skin.box);
        GUILayout.Label("热更新测试");
        GUILayout.Label("L = 加载 Cube");
        GUILayout.Label("U = 检查 Catalog 并重载");
        GUILayout.Label(_isBusy ? "状态：处理中..." : "状态：就绪");
        GUILayout.EndArea();
    }

    private async UniTaskVoid LoadCubeAsync()
    {
        if (_isBusy)
        {
            return;
        }

        _isBusy = true;
        try
        {
            await LoadCubeInternalAsync();
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async UniTask LoadCubeInternalAsync()
    {
        ReleaseSpawnedCube();
        ReleaseCubeHandle();

        _cubeHandle = Addressables.LoadAssetAsync<GameObject>(_cubeAddress);
        await _cubeHandle;

        if (_cubeHandle.Status != AsyncOperationStatus.Succeeded || _cubeHandle.Result == null)
        {
            GameLog.Error(GameLogChannel.Resource, $"热更新测试加载失败。address={_cubeAddress}, status={_cubeHandle.Status}");
            return;
        }

        _spawnedCube = Instantiate(_cubeHandle.Result, _spawnPosition, Quaternion.identity);
        _spawnedCube.name = $"HotUpdateCube ({_cubeHandle.Result.name})";
        GameLog.Info(GameLogChannel.Resource, $"热更新测试 Cube 已加载：{_cubeAddress}");
    }

    private async UniTaskVoid ApplyHotUpdateAsync()
    {
        if (_isBusy)
        {
            return;
        }

        _isBusy = true;
        try
        {
            AsyncOperationHandle<List<string>> checkHandle = Addressables.CheckForCatalogUpdates(false);
            await checkHandle;

            if (checkHandle.Status != AsyncOperationStatus.Succeeded)
            {
                GameLog.Error(GameLogChannel.Resource, $"热更新测试 Catalog 检查失败。status={checkHandle.Status}");
                Addressables.Release(checkHandle);
                return;
            }

            if (checkHandle.Result == null || checkHandle.Result.Count == 0)
            {
                GameLog.Info(GameLogChannel.Resource, "热更新测试：Catalog 已是最新。");
                Addressables.Release(checkHandle);
                return;
            }

            GameLog.Info(GameLogChannel.Resource, $"热更新测试：发现 {checkHandle.Result.Count} 个 Catalog 更新，正在下载...");
            AsyncOperationHandle<List<UnityEngine.AddressableAssets.ResourceLocators.IResourceLocator>> updateHandle =
                Addressables.UpdateCatalogs(checkHandle.Result, false);
            Addressables.Release(checkHandle);
            await updateHandle;

            if (updateHandle.Status != AsyncOperationStatus.Succeeded)
            {
                GameLog.Error(GameLogChannel.Resource, $"热更新测试 Catalog 更新失败。status={updateHandle.Status}");
                Addressables.Release(updateHandle);
                return;
            }

            Addressables.Release(updateHandle);
            AsyncOperationHandle<bool> clearHandle = Addressables.ClearDependencyCacheAsync(_cubeAddress, true);
            await clearHandle;
            Addressables.Release(clearHandle);

            GameLog.Info(GameLogChannel.Resource, "热更新测试：Catalog 已更新，正在重载 Cube...");
            await LoadCubeInternalAsync();
        }
        finally
        {
            _isBusy = false;
        }
    }

    private void ReleaseSpawnedCube()
    {
        if (_spawnedCube == null)
        {
            return;
        }

        Destroy(_spawnedCube);
        _spawnedCube = null;
    }

    private void ReleaseCubeHandle()
    {
        if (!_cubeHandle.IsValid())
        {
            return;
        }

        Addressables.Release(_cubeHandle);
        _cubeHandle = default;
    }
}
