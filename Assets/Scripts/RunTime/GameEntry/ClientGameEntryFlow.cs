using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Rogue;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ClientGameEntryFlow
{
    private const string BattleSceneName = "RougeBattle";

    public static async UniTask<bool> LoadBattleSceneAndCreateWorldAsync(
        IReadOnlyList<PlayerData> teamList,
        Action<float, string> progressChanged = null,
        GameSessionModeType sessionMode = GameSessionModeType.Online)
    {
        progressChanged?.Invoke(0f, "正在加载....");

        float progress = 0f;
        while (progress < 1f)
        {
            progress = Mathf.Min(1f, progress + 0.01f);
            progressChanged?.Invoke(progress, "正在加载....");
            await new WaitForSecondsRealtime(0.02f);
        }

        AsyncOperation operation = GameEntry.Scene.AsyncLoadScene(
            AssetsPathHelper.LoadScenePathHelper(BattleSceneName),
            LoadSceneMode.Additive);

        while (!operation.isDone)
        {
            await new WaitForSecondsRealtime(0.02f);
        }

        progressChanged?.Invoke(1f, string.Empty);
        return await CreateWorld(teamList, sessionMode);
    }

    public static async UniTask<bool> CreateWorld(
        IReadOnlyList<PlayerData> teamList,
        GameSessionModeType sessionMode = GameSessionModeType.Online)
    {
        if (teamList == null || teamList.Count == 0)
        {
            GameLog.Error(GameLogChannel.Battle, "Create world failed. Team list is empty.");
            return false;
        }

        bool hasSelfPlayer = false;
        List<PlayerData> playerDataList = new List<PlayerData>(teamList.Count);
        for (int i = 0; i < teamList.Count; i++)
        {
            PlayerData playerData = teamList[i];
            hasSelfPlayer |= playerData.IsSelf;
            playerDataList.Add(playerData);
            GameLog.Info(GameLogChannel.Battle, $"Create world player[{i}]: heroId={playerData.HeroId}, serverEntityId={playerData.ServerEntityId}, isSelf={playerData.IsSelf}");
        }

        if (!hasSelfPlayer)
        {
            GameLog.Error(GameLogChannel.Battle, $"Create world failed. Team list has no self player. playerCount={teamList.Count}");
            return false;
        }

        if (WorldSystem.Instance == null)
        {
            Game.AddSingleton<WorldSystem>();
        }

        return await WorldSystem.Instance.CreateWorldChannel(
            WorldModel.Pvp,
            SceneManager.GetSceneByName(BattleSceneName),
            BattleSceneName,
            playerDataList,
            sessionMode);
    }
}
