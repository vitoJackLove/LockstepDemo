using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class SceneComponent : RunTimeComponent
{
    /// <summary>
    /// 战斗中使用的相机
    /// </summary>
    public Camera battleCamera;

    public override void Init()
    {
        base.Init();

        if (battleCamera == null)
        {
            GameLog.Error(GameLogChannel.Battle, "SceneComponent init = error : battleCamera == null...");
        }
    }

    public async UniTask LoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode)
    {
        ResourceComponent resourceComponent = GameEntryRunTime.GetComponent<ResourceComponent>();
#if UNITY_EDITOR
        if (resourceComponent.GameResourceMode == ResourceComponent.ResourceMode.Editor)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
            await operation;
            return;
        }
#endif
        await Addressables.LoadSceneAsync(sceneName, loadSceneMode).ToUniTask();
    }

    public AsyncOperation AsyncLoadScene(string sceneName, LoadSceneMode loadSceneMode)
    {
        ResourceComponent resourceComponent = GameEntryRunTime.GetComponent<ResourceComponent>();
#if UNITY_EDITOR
        if (resourceComponent.GameResourceMode == ResourceComponent.ResourceMode.Editor)
        {
            return SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
        }
#endif
        return SceneManager.LoadSceneAsync(GetSceneNameFromAddress(sceneName), loadSceneMode);
    }

    public void LoadScene(string sceneName, LoadSceneMode loadSceneMode)
    {
        ResourceComponent resourceComponent = GameEntryRunTime.GetComponent<ResourceComponent>();
#if UNITY_EDITOR
        if (resourceComponent.GameResourceMode == ResourceComponent.ResourceMode.Editor)
        {
            SceneManager.LoadScene(sceneName, loadSceneMode);
            return;
        }
#endif
        Addressables.LoadSceneAsync(sceneName, loadSceneMode).WaitForCompletion();
    }

    private static string GetSceneNameFromAddress(string sceneAddress)
    {
        if (string.IsNullOrEmpty(sceneAddress))
        {
            return sceneAddress;
        }

        sceneAddress = sceneAddress.Replace("\\", "/");
        int slashIndex = sceneAddress.LastIndexOf('/');
        string fileName = slashIndex >= 0 ? sceneAddress.Substring(slashIndex + 1) : sceneAddress;
        return fileName.EndsWith(".unity", StringComparison.Ordinal)
            ? fileName.Substring(0, fileName.Length - ".unity".Length)
            : fileName;
    }
}
