using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Loxodon.Framework.Commands;
using Loxodon.Framework.Interactivity;
using Loxodon.Framework.ViewModels;
using Rogue;
using UnityEngine;

/// <summary>
/// Game start window view model.
/// </summary>
public class GameStartUpViewModel : ViewModelBase, IObserverHandler
{
    private const int SinglePlayerIndex = 0;

    private int _playerIndex;
    private bool _hasPlayerIndex;
    private bool _isSinglePlayerMode = true;
    private bool _isGameStart;
    private bool _isLoading;

    private readonly SimpleCommand gameStartCommand;
    private readonly SimpleCommand connectServerCommand;
    private readonly ProgressBarModel progressBar;

    public InteractionRequest dismissRequest;

    private readonly List<HeroInfoViewModel> _heroInfoViewModels = new List<HeroInfoViewModel>();
    private readonly List<PlayerData> _teamList = new List<PlayerData>();

    public GameStartUpViewModel()
    {
        gameStartCommand = new SimpleCommand(GameStart);
        connectServerCommand = new SimpleCommand(() => { });
        progressBar = new ProgressBarModel();
        dismissRequest = new InteractionRequest();

        InitHeroInfo();

        GameEntry.Observer.Attach(BattleObserverEventEnum.PlayerConnect, this);
        GameEntry.Observer.Attach(BattleObserverEventEnum.SelectHero, this);
        GameEntry.Observer.Attach(BattleObserverEventEnum.GameStart, this);

        if (GameEntry.FrameSyncTransport != null && GameEntry.FrameSyncTransport.TryGetLocalPlayerIndex(out int playerIndex))
        {
            InitIndexPlayer(playerIndex);
        }

        ApplySessionMode(_isSinglePlayerMode);
    }

    private void ApplySessionMode(bool isSinglePlayerMode)
    {
        GameSessionMode.Current = isSinglePlayerMode
            ? GameSessionModeType.SinglePlayer
            : GameSessionModeType.Online;

        GameSessionContext sessionContext = isSinglePlayerMode
            ? GameSessionFactory.CreateSinglePlayer()
            : GameSessionFactory.CreateOnline(GameEntry.TcpClient);
        GameEntry.ConfigureTransport(sessionContext.Transport);

        if (isSinglePlayerMode)
        {
            InitIndexPlayer(SinglePlayerIndex);
            EnsureSinglePlayerSelection();
            return;
        }

        _teamList.Clear();
        RebuildSelectedHeroStates();

        if (GameEntry.TcpClient != null && !GameEntry.TcpClient.TryConnect())
        {
            GameLog.Warn(GameLogChannel.Network, "Failed to connect frame sync server. Start the server or switch back to single player mode.");
        }

        if (GameEntry.FrameSyncTransport != null && GameEntry.FrameSyncTransport.TryGetLocalPlayerIndex(out int playerIndex))
        {
            InitIndexPlayer(playerIndex);
        }
        else
        {
            _hasPlayerIndex = false;
        }
    }

    public List<HeroInfoViewModel> HeroInfoViewModels => _heroInfoViewModels;

    public ProgressBarModel ProgressBar => progressBar;

    public ICommand StartUp => gameStartCommand;

    public ICommand Connect => connectServerCommand;

    public bool IsSinglePlayerMode
    {
        get { return _isSinglePlayerMode; }
        set
        {
            if (_isSinglePlayerMode == value)
            {
                return;
            }

            this.Set<bool>(ref _isSinglePlayerMode, value, "IsSinglePlayerMode");
            ApplySessionMode(_isSinglePlayerMode);
        }
    }

    private void InitHeroInfo()
    {
        List<HeroAssetsConfig> heroAssetsConfigs = GameEntry.DataTable.GetAllDataTable<HeroAssetsConfig>();

        for (int i = 0; i < heroAssetsConfigs.Count; i++)
        {
            HeroAssetsConfig config = heroAssetsConfigs[i];
            HeroInfoViewModel heroInfoViewModel = new HeroInfoViewModel(this, config);
            _heroInfoViewModels.Add(heroInfoViewModel);
        }
    }

    private void GameStart()
    {
        if (_isLoading)
        {
            return;
        }

        if (_isSinglePlayerMode)
        {
            InitIndexPlayer(SinglePlayerIndex);
            if (!EnsureSinglePlayerSelection())
            {
                GameLog.Error(GameLogChannel.UI, "Single player start failed. No hero is available.");
                return;
            }
        }

        if (_isSinglePlayerMode)
        {
            _isGameStart = false;
            LoadScene();
            return;
        }

        SendGameStartToServer();
    }

    private async void LoadScene()
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        gameStartCommand.Enabled = false;
        progressBar.Enable = true;
        ProgressBar.Tip = "Loading...";

        if (!await ClientGameEntryFlow.LoadBattleSceneAndCreateWorldAsync(
                _teamList,
                (progressValue, tip) =>
                {
                    ProgressBar.Progress = progressValue;
                    ProgressBar.Tip = tip;
                },
                _isSinglePlayerMode ? GameSessionModeType.SinglePlayer : GameSessionModeType.Online))
        {
            gameStartCommand.Enabled = true;
            progressBar.Enable = false;
            _isLoading = false;
            return;
        }

        if (!_isSinglePlayerMode)
        {
            SendGameStartToServer();
        }

        dismissRequest.Raise();
        _isLoading = false;
    }

    public void OnNotify(IObserverParams param)
    {
        if (param == null)
        {
            return;
        }

        if (_isSinglePlayerMode)
        {
            return;
        }

        if (param.ObserverEventType == BattleObserverEventEnum.PlayerConnect)
        {
            PlayerConnectMessage playerConnectMessage = (PlayerConnectMessage)param;
            InitIndexPlayer(playerConnectMessage.PlayerIndex);
        }

        if (param.ObserverEventType == BattleObserverEventEnum.SelectHero)
        {
            SelectHeroMessage selectHeroMessage = (SelectHeroMessage)param;
            UpsertPlayerData(
                selectHeroMessage.SelectHeroId,
                selectHeroMessage.PlayerIndex,
                _hasPlayerIndex && _playerIndex == selectHeroMessage.PlayerIndex);
        }

        if (param.ObserverEventType == BattleObserverEventEnum.GameStart)
        {
            GameStartMessage message = (GameStartMessage)param;
            _isGameStart = message.isStart;
        }
    }

    public void Update()
    {
        if (_isGameStart)
        {
            _isGameStart = false;
            LoadScene();
        }
    }

    private void InitIndexPlayer(int playerIndex)
    {
        _playerIndex = playerIndex;
        _hasPlayerIndex = true;

        GameLog.Info(GameLogChannel.UI, $"Local player index assigned: {_playerIndex}");
    }

    public void SelectHero(int heroId)
    {
        if (_isSinglePlayerMode)
        {
            InitIndexPlayer(SinglePlayerIndex);
            UpsertPlayerData(heroId, SinglePlayerIndex, true);
            return;
        }

        if (!_hasPlayerIndex)
        {
            GameLog.Error(GameLogChannel.UI, "Select hero failed. Player index has not been assigned by server.");
            return;
        }

        if (GameEntry.FrameSyncTransport == null)
        {
            GameLog.Error(GameLogChannel.UI, "Select hero failed. Frame sync transport is not available.");
            return;
        }

        SelectHeroMessage selectHeroMessage = new SelectHeroMessage
        {
            PlayerIndex = _playerIndex,
            SelectHeroId = heroId
        };
        GameEntry.FrameSyncTransport.Send(BattleObserverEventEnum.SelectHero, selectHeroMessage);
    }

    private void SendGameStartToServer()
    {
        if (!_hasPlayerIndex)
        {
            GameLog.Error(GameLogChannel.UI, "Game start failed. Player index has not been assigned by server.");
            return;
        }

        if (GameEntry.FrameSyncTransport == null)
        {
            GameLog.Error(GameLogChannel.UI, "Game start failed. Frame sync transport is not available.");
            return;
        }

        GameStartMessage gameStartMessage = new GameStartMessage
        {
            isStart = true,
        };
        GameEntry.FrameSyncTransport.Send(BattleObserverEventEnum.GameStart, gameStartMessage);
    }

    private bool EnsureSinglePlayerSelection()
    {
        for (int i = 0; i < _teamList.Count; i++)
        {
            if (_teamList[i].IsSelf && _teamList[i].HeroId > 0)
            {
                UpsertPlayerData(_teamList[i].HeroId, SinglePlayerIndex, true);
                return true;
            }
        }

        if (_heroInfoViewModels.Count <= 0)
        {
            return false;
        }

        UpsertPlayerData(_heroInfoViewModels[0].HeroId, SinglePlayerIndex, true);
        return true;
    }

    private void UpsertPlayerData(int heroId, int playerIndex, bool isSelf)
    {
        for (int i = 0; i < _teamList.Count; i++)
        {
            if (_teamList[i].ServerEntityId == playerIndex)
            {
                _teamList[i].HeroId = heroId;
                _teamList[i].IsSelf = isSelf;
                RebuildSelectedHeroStates();
                return;
            }
        }

        _teamList.Add(new PlayerData
        {
            HeroId = heroId,
            IsSelf = isSelf,
            ServerEntityId = playerIndex,
        });

        RebuildSelectedHeroStates();
    }

    private void RebuildSelectedHeroStates()
    {
        for (int i = 0; i < HeroInfoViewModels.Count; i++)
        {
            HeroInfoViewModels[i].IsSelected = IsHeroSelected(HeroInfoViewModels[i].HeroId);
        }
    }

    private bool IsHeroSelected(int heroId)
    {
        for (int i = 0; i < _teamList.Count; i++)
        {
            if (_teamList[i].HeroId == heroId)
            {
                return true;
            }
        }

        return false;
    }
}

public class PlayerData
{
    public int HeroId;

    public int ServerEntityId;

    public bool IsSelf;
}
