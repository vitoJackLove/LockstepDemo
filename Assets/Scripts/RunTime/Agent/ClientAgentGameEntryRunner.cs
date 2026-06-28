using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Rogue;
using UnityEngine;

public sealed class ClientAgentGameEntryRunner : MonoBehaviour, IObserverHandler
{
    public const string LogPrefix = "[AgentClientEntry]";
    public const string SuccessLog = LogPrefix + " Enter game succeeded";
    public const string FailureLog = LogPrefix + " Enter game failed";

    [SerializeField] private float gameEntryTimeoutSeconds = 30f;
    [SerializeField] private float serverMessageTimeoutSeconds = 30f;
    [SerializeField] private int fallbackHeroId = 1205;

    private readonly List<PlayerData> _teamList = new List<PlayerData>();
    private bool _started;
    private bool _completed;
    private bool _hasPlayerIndex;
    private bool _hasGameStart;
    private int _playerIndex;

    private async void Start()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        DontDestroyOnLoad(gameObject);
        ClientAgentGameEntryMode.IsRunning = true;

        await RunAsync();
    }

    private void OnDestroy()
    {
        if (!_completed)
        {
            ClientAgentGameEntryMode.IsRunning = false;
        }
    }

    public void OnNotify(IObserverParams param)
    {
        if (param == null)
        {
            return;
        }

        if (param.ObserverEventType == BattleObserverEventEnum.PlayerConnect)
        {
            PlayerConnectMessage playerConnectMessage = (PlayerConnectMessage)param;
            _playerIndex = playerConnectMessage.PlayerIndex;
            _hasPlayerIndex = true;
            GameLog.Info(GameLogChannel.AgentTest, $"{LogPrefix} Player index assigned: {_playerIndex}");
            return;
        }

        if (param.ObserverEventType == BattleObserverEventEnum.SelectHero)
        {
            SelectHeroMessage selectHeroMessage = (SelectHeroMessage)param;
            UpsertPlayer(selectHeroMessage);
            return;
        }

        if (param.ObserverEventType == BattleObserverEventEnum.GameStart)
        {
            GameStartMessage gameStartMessage = (GameStartMessage)param;
            _hasGameStart = gameStartMessage.isStart;
        }
    }

    private async UniTask RunAsync()
    {
        try
        {
            GameLog.Info(GameLogChannel.AgentTest, $"{LogPrefix} Starting client enter game flow.");

            if (!await WaitUntil(IsGameEntryReady, gameEntryTimeoutSeconds, "GameEntryReady"))
            {
                return;
            }

            AttachObservers();

            if (GameEntry.FrameSyncTransport != null &&
                GameEntry.FrameSyncTransport.TryGetLocalPlayerIndex(out int playerIndex))
            {
                _playerIndex = playerIndex;
                _hasPlayerIndex = true;
            }

            if (!await WaitUntil(() => _hasPlayerIndex, serverMessageTimeoutSeconds, "PlayerConnect"))
            {
                return;
            }

            int heroId = ResolveHeroId();
            if (heroId <= 0)
            {
                Fail("ResolveHero", "No valid HeroAssetsConfig was found.");
                return;
            }

            GameLog.Info(GameLogChannel.AgentTest, $"{LogPrefix} Select hero. heroId={heroId}, playerIndex={_playerIndex}");
            GameEntry.FrameSyncTransport.Send(BattleObserverEventEnum.SelectHero, new SelectHeroMessage
            {
                PlayerIndex = _playerIndex,
                SelectHeroId = heroId,
            });

            if (!await WaitUntil(() => HasSelfPlayer(heroId), serverMessageTimeoutSeconds, "SelectHero"))
            {
                return;
            }

            GameLog.Info(GameLogChannel.AgentTest, $"{LogPrefix} Request load start.");
            SendGameStart();

            if (!await WaitForGameStartOrLocalReady(heroId))
            {
                return;
            }

            GameLog.Info(GameLogChannel.AgentTest, $"{LogPrefix} Load battle scene and create world.");
            bool createdWorld = await ClientGameEntryFlow.LoadBattleSceneAndCreateWorldAsync(
                _teamList,
                (progress, tip) => GameLog.Info(GameLogChannel.AgentTest, $"{LogPrefix} Loading progress={Mathf.FloorToInt(progress * 100f)}% {tip}"));

            if (!createdWorld)
            {
                Fail("CreateWorld", "Battle world creation returned false.");
                return;
            }

            if (!await WaitForEditorRunWorldReady())
            {
                return;
            }

            SendGameStart();
            _completed = true;
            ClientAgentGameEntryMode.IsRunning = false;
            GameLog.Info(GameLogChannel.AgentTest, SuccessLog);
        }
        catch (Exception ex)
        {
            Fail("Exception", ex.ToString());
        }
    }

    private static bool IsGameEntryReady()
    {
        return GameEntry.FrameSyncTransport != null
            && GameEntry.Observer != null
            && GameEntry.DataTable != null
            && GameEntry.Scene != null;
    }

    private void AttachObservers()
    {
        GameEntry.Observer.Attach(BattleObserverEventEnum.PlayerConnect, this);
        GameEntry.Observer.Attach(BattleObserverEventEnum.SelectHero, this);
        GameEntry.Observer.Attach(BattleObserverEventEnum.GameStart, this);
    }

    private int ResolveHeroId()
    {
        List<HeroAssetsConfig> heroAssetsConfigs = GameEntry.DataTable.GetAllDataTable<HeroAssetsConfig>();
        for (int i = 0; i < heroAssetsConfigs.Count; i++)
        {
            if (heroAssetsConfigs[i] != null && heroAssetsConfigs[i].assetsId > 0)
            {
                return heroAssetsConfigs[i].assetsId;
            }
        }

        return fallbackHeroId;
    }

    private void SendGameStart()
    {
        GameEntry.FrameSyncTransport.Send(BattleObserverEventEnum.GameStart, new GameStartMessage
        {
            isStart = true,
        });
    }

    private void UpsertPlayer(SelectHeroMessage selectHeroMessage)
    {
        for (int i = 0; i < _teamList.Count; i++)
        {
            if (_teamList[i].ServerEntityId == selectHeroMessage.PlayerIndex)
            {
                _teamList[i].HeroId = selectHeroMessage.SelectHeroId;
                _teamList[i].IsSelf = _hasPlayerIndex && _playerIndex == selectHeroMessage.PlayerIndex;
                return;
            }
        }

        _teamList.Add(new PlayerData
        {
            HeroId = selectHeroMessage.SelectHeroId,
            IsSelf = _hasPlayerIndex && _playerIndex == selectHeroMessage.PlayerIndex,
            ServerEntityId = selectHeroMessage.PlayerIndex,
        });
    }

    private bool HasSelfPlayer(int heroId)
    {
        for (int i = 0; i < _teamList.Count; i++)
        {
            PlayerData playerData = _teamList[i];
            if (playerData.IsSelf && playerData.HeroId == heroId)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 等待服务器广播开始游戏；若 Agent 已确认本机玩家和英雄选择，则允许继续创建本地战斗世界，避免单人自动化入口卡在缺失回包上。
    /// </summary>
    /// <param name="heroId">本次 Agent 自动选择的英雄配置 ID。</param>
    /// <returns>收到开始游戏广播或本地队伍数据已足够创建世界时返回 true。</returns>
    private async UniTask<bool> WaitForGameStartOrLocalReady(int heroId)
    {
        float deadline = Time.realtimeSinceStartup + serverMessageTimeoutSeconds;
        bool fallbackLogged = false;

        while (Time.realtimeSinceStartup < deadline)
        {
            if (_hasGameStart)
            {
                return true;
            }

            if (WorldSystem.Instance?.CurrentRunWorld != null)
            {
                return true;
            }

            if (HasSelfPlayer(heroId))
            {
                if (!fallbackLogged)
                {
                    GameLog.Info(GameLogChannel.AgentTest,
                        $"{LogPrefix} GameStart fallback: self player is ready, continue local battle world creation. heroId={heroId}, playerIndex={_playerIndex}");
                    fallbackLogged = true;
                }

                return true;
            }

            await UniTask.Yield();
        }

        Fail("GameStart", $"Timed out after {serverMessageTimeoutSeconds:0.##} seconds.");
        return false;
    }

    /// <summary>
    /// 等待编辑器侧可读取当前运行世界，确保成功日志不会早于金手指窗口可见的世界状态。
    /// </summary>
    /// <returns>编辑器可通过 WorldSystem 读取当前运行世界时返回 true。</returns>
    private async UniTask<bool> WaitForEditorRunWorldReady()
    {
#if UNITY_EDITOR
        float deadline = Time.realtimeSinceStartup + gameEntryTimeoutSeconds;

        while (Time.realtimeSinceStartup < deadline)
        {
            BaseWorld currentWorld = WorldSystem.Instance?.CurrentRunWorld;

            if (currentWorld != null)
            {
                GameLog.Info(GameLogChannel.AgentTest,
                    $"{LogPrefix} Runtime world ready for editor tools. scene={currentWorld.SceneName}, localTick={currentWorld.LocalTick}, authorityTick={currentWorld.AuthorityTick}");
                return true;
            }

            await UniTask.Yield();
        }

        Fail("EditorWorldReady", $"Timed out after {gameEntryTimeoutSeconds:0.##} seconds.");
        return false;
#else
        await UniTask.Yield();
        return true;
#endif
    }

    private async UniTask<bool> WaitUntil(Func<bool> condition, float timeoutSeconds, string stage)
    {
        float deadline = Time.realtimeSinceStartup + timeoutSeconds;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (condition())
            {
                return true;
            }

            await UniTask.Yield();
        }

        Fail(stage, $"Timed out after {timeoutSeconds:0.##} seconds.");
        return false;
    }

    private void Fail(string stage, string message)
    {
        _completed = true;
        ClientAgentGameEntryMode.IsRunning = false;
        GameLog.Error(GameLogChannel.AgentTest, $"{FailureLog}. stage={stage}, reason={message}");
    }
}
