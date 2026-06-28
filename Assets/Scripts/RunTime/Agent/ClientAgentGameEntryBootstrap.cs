using UnityEngine;
using UnityEngine.SceneManagement;

public static class ClientAgentGameEntryBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnableAgentModeBeforeSceneLoad()
    {
        if (PlayerPrefs.GetInt(ClientAgentGameEntryMode.RunOnPlayPrefsKey, 0) != 1)
        {
            return;
        }

        PlayerPrefs.DeleteKey(ClientAgentGameEntryMode.RunOnPlayPrefsKey);
        PlayerPrefs.Save();
        ClientAgentGameEntryMode.IsRunning = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRunnerAfterSceneLoad()
    {
        if (!ClientAgentGameEntryMode.IsRunning)
        {
            return;
        }

        if (Object.FindObjectOfType<ClientAgentGameEntryRunner>() != null)
        {
            return;
        }

        GameObject runner = new GameObject(nameof(ClientAgentGameEntryRunner));
        runner.AddComponent<ClientAgentGameEntryRunner>();
        SceneManager.MoveGameObjectToScene(runner, SceneManager.GetActiveScene());
    }
}
