using UnityEngine;
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

    public AsyncOperation AsyncLoadScene(string sceneName,LoadSceneMode loadSceneMode)
    {
        return SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
    }
    
    public void LoadScene(string sceneName,LoadSceneMode loadSceneMode)
    {
        SceneManager.LoadScene(sceneName, loadSceneMode);
    }
}
