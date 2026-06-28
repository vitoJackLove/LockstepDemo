using UnityEngine;
using UnityEngine.Pool;
using GameObject = UnityEngine.GameObject;

public class GameObjectFactory
{
    private GameObject _gameObject;

    private ObjectPool<GameObject> _pool;

    private Transform _root;

    public GameObjectFactory (GameObject gameObject, Transform root)
    {
        _gameObject = gameObject;

        this._root = root;
        
        _pool = new ObjectPool<GameObject>(CreateGameObject,GetGameObject, ReleaseGameObject,DestroyGameObject);
    }
    
    /// <summary>
    /// 获取一个游戏物体
    /// </summary>
    /// <returns></returns>
    public GameObject Create()
    {
        return _pool.Get();
    }

    /// <summary>
    /// 回收一个游戏物体
    /// </summary>
    /// <param name="gameObject"></param>
    public void Release(GameObject gameObject)
    {
        _pool.Release(gameObject);
    }

    /// <summary>
    /// 清除
    /// </summary>
    public void Dispose()
    {
        _pool.Dispose();
    }

    private void DestroyGameObject(GameObject obj)
    {
        GameObject.Destroy(obj);
    }

    private void GetGameObject(GameObject obj)
    {
        obj.SetActive(true);
    }

    private void ReleaseGameObject(GameObject obj)
    {
        obj.SetActive(false);
    }

    private GameObject CreateGameObject()
    {
        return GameObject.Instantiate(_gameObject, _root);
    }
}
