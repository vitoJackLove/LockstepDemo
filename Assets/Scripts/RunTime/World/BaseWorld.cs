using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Mathematics.FixedPoint;
using UnityEngine;
using Random = UnityEngine.Random;

public abstract partial class BaseWorld
{
    /// <summary>
    /// 世界逻辑帧
    /// </summary>
    public readonly fp LogicDeltaTime = new fp(1) * (fp)0.033f;
    
    /// <summary>
    /// 世界ID
    /// </summary>
    private uint _worldId;

    /// <summary>
    /// 场景名字
    /// </summary>
    private string _sceneName;
    
    /// <summary>
    /// 世界根节点
    /// </summary>
    private Transform _worldRoot;

    /// <summary>
    /// 系统
    /// </summary>
    private Dictionary<Type, BaseSystem> _systemDic = new Dictionary<Type, BaseSystem>();
    
    public uint WorldId => _worldId;

    /// <summary>
    /// 实体根节点
    /// </summary>
    private Transform _entityRoot;

    /// <summary>
    /// 地图根节点（保留兼容，PVP 不再生成地图）。
    /// </summary>
    private Transform _mapRoot;

    /// <summary>
    /// SKILL Time Line root 
    /// </summary>
    private Transform _skillTimeLineRoot;

    protected abstract Type[] GetSystemTypes();
    
    /// <summary>
    /// 本地执行帧号 也等价于世界帧号
    /// </summary>
    private uint _localTick;

    public uint LocalTick => _localTick;

    /// <summary>
    /// 游戏时机
    /// </summary>
    protected GameTimeType GameTimeType;

    public GameSessionModeType SessionMode { get; private set; }

    /// <summary>
    /// 当前世界会话配置，控制帧同步、快照与实体同步策略。
    /// </summary>
    public IGameSessionProfile SessionProfile { get; private set; }

    protected BaseWorld(CreateWorldData createWorldData)
    {
        this._localTick = 0;
        this._worldId = createWorldData.WorldId;
        this._worldRoot = createWorldData.WorldRoot;
        this._sceneName = createWorldData.SceneName;
        this.SessionMode = createWorldData.SessionMode;
        this.SessionProfile = createWorldData.SessionProfile ?? GameSessionFactory.CreateProfile(createWorldData.SessionMode);
        GameTimeType = GameTimeType.WaitStart;
        
        _entityRoot = new GameObject().transform;
        _entityRoot.SetParent(_worldRoot);
        _entityRoot.gameObject.name = "EntityRoot";
            
        _skillTimeLineRoot= new GameObject().transform;
        _skillTimeLineRoot.gameObject.name = "SkillTimeLineRoot";
        _skillTimeLineRoot.SetParent(_worldRoot);

        RegisterSystem();
    }

    private void RegisterSystem()
    {
        for (int i = 0; i < GetSystemTypes().Length; i++)
        {
            var system = (BaseSystem)Activator.CreateInstance(GetSystemTypes()[i]);

            _systemDic.Add(GetSystemTypes()[i], system);
        }
    }
    
    public async Task<bool> WorldSystemInit(CreateWorldData createWorldData)
    {
        uint inputHistoryWindow = SessionProfile.InputHistoryWindow;
        if (SessionProfile is OnlineGameSessionProfile onlineProfile && _gameRollBackContent != null)
        {
            onlineProfile.InputHistoryWindow = _gameRollBackContent.forecastTick;
            inputHistoryWindow = onlineProfile.InputHistoryWindow;
        }

        float lossPacket = SessionProfile.SimulatePacketLoss && _gameRollBackContent != null
            ? _gameRollBackContent.lossPacket
            : 0f;
        InitRollBackData(inputHistoryWindow, lossPacket);
        
        foreach (var system in _systemDic.Values)
        {
            system.OnInit(this);
        }
        
        return await GamePreparation(createWorldData);
    }

    /// <summary>
    /// 游戏开始前的准备
    /// </summary>
    protected abstract Task<bool> GamePreparation(CreateWorldData createWorldData);

    public async Task<bool> WorldEnter()
    {
        foreach (var system in _systemDic.Values)
        {
            bool result = await system.OnEnter();
            
            if (!result)
            {
                GameLog.Error(GameLogChannel.Battle, $"系统{system}初始化失败");

                return false;
            }
        }
        
        return true;
    }
    
    public virtual void GameStart()
    {
        foreach (var system in _systemDic.Values)
        {
            system.OnStart();
        }
        
        GameTimeType = GameTimeType.Start;
    }
    
    /// <summary>
    /// 执行客户端指令
    /// </summary>
    /// <param name="commandData"></param>
    private void ExecuteLogicCommand(CommandData commandData)
    {
        if (GameTimeType != GameTimeType.Start)
        {
            return;
        }
        
        foreach (var system in _systemDic.Values)
        {
            system.OnExecuteLocalCommand(commandData);
        }
    }
    
    /// <summary>
    /// 执行服务器指令
    /// </summary>
    /// <param name="commandDataList"></param>
    private void ExecuteServerCommand(List<CommandData> commandDataList)
    {
        if (GameTimeType != GameTimeType.Start)
        {
            return;
        }
        
        foreach (var system in _systemDic.Values)
        {
            system.OnExecuteServerCommand(commandDataList);
        }
    }
    
    public void Update(fp deltaTime)
    {
        if (GameTimeType != GameTimeType.Start)
        {
            return;
        }
        
        foreach (var system in _systemDic.Values)
        {
            system.OnUpdate(deltaTime);
        }
    }

    public void FixedUpdate(fp deltaTime)
    {
        if (GameTimeType != GameTimeType.Start)
        {
            return;
        }

        if (SessionProfile.RequiresRollback)
        {
            FixedUpdateGgpo(deltaTime);
            return;
        }

        _localTick++;
        CommandData data = GetSystem<CommandSystem>().GetCommand();
        if (data != null)
        {
            UpdateGameState(deltaTime, new List<CommandData> { data });
        }
    }

    public virtual void Shutdown()
    {
        foreach (var system in _systemDic.Values)
        {
            system.OnDispose();
        }

        _localTick = 0;
        GameTimeType = GameTimeType.WaitStop;
    }
    
    public virtual void Hide() { }
    
    public virtual void Pause(bool isPause)
    {
        foreach (var system in _systemDic.Values)
        {
            system.OnPause(isPause);
        }
    }

    public T GetSystem<T>() where T : BaseSystem
    {
        Type type = typeof(T);
        
        if (_systemDic.TryGetValue(type,out BaseSystem system))
        {
            return (T)system;
        }

        return null;
    }

    /// <summary>
    /// 世界根节点
    /// </summary>
    public Transform WorldRoot => _worldRoot;

    /// <summary>
    /// 实体根节点
    /// </summary>
    public Transform EntityRoot => _entityRoot;

    /// <summary>
    /// 地图根节点（PVP 未使用）。
    /// </summary>
    public Transform MapRoot => _mapRoot;

    /// <summary>
    /// 技能资产根节点
    /// </summary>
    public Transform SkillTimeLineRoot => _skillTimeLineRoot;

    /// <summary>
    /// 场景名
    /// </summary>
    public string SceneName => _sceneName;
}
