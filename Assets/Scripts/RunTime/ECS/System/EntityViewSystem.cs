using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class EntityViewSystem : BaseSystem
{
    private Dictionary<string, GameObjectFactory> _entityViewCacheDic = new Dictionary<string, GameObjectFactory>();
    private Dictionary<string, Task<GameObjectFactory>> _entityViewLoadingTaskDic = new Dictionary<string, Task<GameObjectFactory>>();

    public async Task<GameObject> SyncGetEntityView(string viewPath)
    {
        return await SyncGetEntityView(viewPath, CurrentWorld.EntityRoot);
    }

    public async void SyncLoadEntityView(string viewPath, Transform root)
    {
        if (string.IsNullOrEmpty(viewPath))
        {
            return;
        }

        await GetOrLoadFactory(viewPath, root);
    }

    public async Task<GameObject> SyncGetEntityView(string viewPath, Transform root)
    {
        if (string.IsNullOrEmpty(viewPath))
        {
            return null;
        }

        GameObjectFactory objectFactory = await GetOrLoadFactory(viewPath, root);
        return objectFactory?.Create();
    }

    [Obsolete("Obsolete")]
    public GameObject GetEntityView(string viewPath, Transform root)
    {
        if (string.IsNullOrEmpty(viewPath))
        {
            return null;
        }

        if (!_entityViewCacheDic.TryGetValue(viewPath, out GameObjectFactory objectFactory))
        {
            GameObject go = GameEntryRunTime.GetComponent<ResourceComponent>()
                .LoadAsset<GameObject>(AssetsPathHelper.EntityPathHelper(viewPath));

            if (go == null)
            {
                Debug.LogError($"EntityViewSystem load failed. path={viewPath}");
                return null;
            }

            objectFactory = new GameObjectFactory(go, root);
            _entityViewCacheDic.Add(viewPath, objectFactory);
        }

        return objectFactory.Create();
    }

    public override void OnDispose()
    {
        base.OnDispose();

        foreach (GameObjectFactory factory in _entityViewCacheDic.Values)
        {
            factory.Dispose();
        }

        _entityViewCacheDic.Clear();
        _entityViewLoadingTaskDic.Clear();

        _entityViewCacheDic = null;
        _entityViewLoadingTaskDic = null;
    }

    public void ReleaseGameObject(GameObject unityGameObject)
    {
        GameObject.Destroy(unityGameObject);
    }

    private Task<GameObjectFactory> GetOrLoadFactory(string viewPath, Transform root)
    {
        if (_entityViewCacheDic.TryGetValue(viewPath, out GameObjectFactory objectFactory))
        {
            return Task.FromResult(objectFactory);
        }

        if (_entityViewLoadingTaskDic.TryGetValue(viewPath, out Task<GameObjectFactory> loadingTask))
        {
            return loadingTask;
        }

        loadingTask = LoadFactoryAsync(viewPath, root);
        _entityViewLoadingTaskDic.Add(viewPath, loadingTask);
        return loadingTask;
    }

    private async Task<GameObjectFactory> LoadFactoryAsync(string viewPath, Transform root)
    {
        try
        {
            GameObject go = await GameEntryRunTime.GetComponent<ResourceComponent>()
                .AsyncLoadAsset<GameObject>(AssetsPathHelper.EntityPathHelper(viewPath));

            if (go == null)
            {
                Debug.LogError($"EntityViewSystem load failed. path={viewPath}");
                return null;
            }

            if (_entityViewCacheDic.TryGetValue(viewPath, out GameObjectFactory cachedFactory))
            {
                return cachedFactory;
            }

            GameObjectFactory objectFactory = new GameObjectFactory(go, root);
            _entityViewCacheDic.Add(viewPath, objectFactory);
            return objectFactory;
        }
        finally
        {
            _entityViewLoadingTaskDic.Remove(viewPath);
        }
    }
}
