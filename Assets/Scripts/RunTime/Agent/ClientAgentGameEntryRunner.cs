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
    /// 绛夊緟鏈嶅姟鍣ㄥ箍鎾紑濮嬫父鎴忥紱鑻?Agent 宸茬‘璁ゆ湰鏈虹帺瀹跺拰鑻遍泟閫夋嫨锛屽垯鍏佽缁х画鍒涘缓鏈湴鎴樻枟涓栫晫锛岄伩鍏嶅崟浜鸿嚜鍔ㄥ寲鍏ュ彛鍗″湪缂哄け鍥炲寘涓娿€?    /// </summary>
    /// <param name="heroId">鏈 Agent 鑷姩閫夋嫨鐨勮嫳闆勯厤缃?ID銆?/param>
    /// <returns>鏀跺埌寮€濮嬫父鎴忓箍鎾垨鏈湴闃熶紞鏁版嵁宸茶冻澶熷垱寤轰笘鐣屾椂杩斿洖 true銆?/returns>
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

#if UNITY_EDITOR
            if (WorldSystem.Instance?.CurrentRunWorld != null)
            {
                return true;
            }
#endif
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
    /// 绛夊緟缂栬緫鍣ㄤ晶鍙鍙栧綋鍓嶈繍琛屼笘鐣岋紝纭繚鎴愬姛鏃ュ織涓嶄細鏃╀簬閲戞墜鎸囩獥鍙ｅ彲瑙佺殑涓栫晫鐘舵€併€?    /// </summary>
    /// <returns>缂栬緫鍣ㄥ彲閫氳繃 WorldSystem 璇诲彇褰撳墠杩愯涓栫晫鏃惰繑鍥?true銆?/returns>
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
