using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Mathematics.FixedPoint;
using UnityEngine;
using Random = UnityEngine.Random;

public abstract partial class BaseWorld
{
    /// <summary>
    /// 世界逻辑帧 deltaTime，与 <see cref="fpmath1.LogicDeltaTime"/> 保持一致。
    /// </summary>
    public fp LogicDeltaTime => fpmath1.LogicDeltaTime;
    
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
    /// 地图根节点
    /// </summary>
    private Transform _mapRoot;

    /// <summary>
    /// SKILL Time Line root 
    /// </summary>
    private Transform _skillTimeLineRoot;

    /// <summary>
    /// 行为树根节点
    /// </summary>
    private Transform _behaviourTreeRoot;

    private KccPhysicsDebugView _kccPhysicsDebugView;

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

    public bool IsSinglePlayer => SessionProfile.Mode == GameSessionModeType.SinglePlayer;

    protected BaseWorld(CreateWorldData createWorldData)
    {
        this._authorityTick = 0;
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

        _mapRoot = new GameObject().transform;
        _mapRoot.gameObject.name = "MapRoot";
        _mapRoot.SetParent(_worldRoot);
            
        _skillTimeLineRoot= new GameObject().transform;
        _skillTimeLineRoot.gameObject.name = "SkillTimeLineRoot";
        _skillTimeLineRoot.SetParent(_worldRoot);

        _behaviourTreeRoot = new GameObject().transform;
        _behaviourTreeRoot.gameObject.name = "BehaviourTreeRoot";
        _behaviourTreeRoot.SetParent(_worldRoot);

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
        uint forecastTick = SessionProfile.ForecastTick;
        if (SessionProfile is OnlineGameSessionProfile onlineProfile && _gameRollBackContent != null)
        {
            onlineProfile.ForecastTick = _gameRollBackContent.forecastTick;
            forecastTick = onlineProfile.ForecastTick;
        }

        float lossPacket = SessionProfile.SimulatePacketLoss && _gameRollBackContent != null
            ? _gameRollBackContent.lossPacket
            : 0f;
        InitRollBackData(forecastTick, lossPacket);
        
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

        EnsureKccPhysicsDebugView();
        GameTimeType = GameTimeType.Start;
    }

    private void EnsureKccPhysicsDebugView()
    {
        if (_worldRoot == null || _kccPhysicsDebugView != null)
        {
            return;
        }

        var debugObject = new GameObject("KccPhysicsDebugView");
        debugObject.transform.SetParent(_worldRoot, false);
        _kccPhysicsDebugView = debugObject.AddComponent<KccPhysicsDebugView>();
        _kccPhysicsDebugView.Bind(this);
    }

    /// <summary>
    /// 在 Update 阶段 Transform 同步后提交 KCC 调试线框，与角色视图保持同帧相位。
    /// </summary>
    public void RefreshKccPhysicsDebugDraw()
    {
        _kccPhysicsDebugView?.Draw();
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

        if (SessionProfile.RequiresRollback && _isStartRollBackUpdate)
        {
            GameLog.Warn(GameLogChannel.Rollback, $"回放循环 localTick={_localTick} endTick={_rollBackEndTick}");
            for (int i = 0; i < _rollBackSpeed; i++)
            {
                // 回滚刷新世界
                RollBackLocalUpdateWorld(deltaTime);
            }

            return;
        }

        _localTick++;

        CommandData data = GetSystem<CommandSystem>().GetCommand();

        // 统一帧同步管道：记录命令 → 可选快照 → 本地更新 → 权威更新
        RecodeAndSendCommand(data);

        if (SessionProfile.RequiresLocalSnapshot)
        {
            TakeLocalSnapShot();
        }

        LogicUpdateWorld(deltaTime, data);
        AuthorityUpdateWorld(deltaTime);
    }

    /// <summary>
    /// 记录并且发送指令
    /// </summary>
    /// <param name="data"></param>
    private void RecodeAndSendCommand(CommandData data)
    {
        if (data == null)
        {
            GameLog.Error(GameLogChannel.Battle, "Record command failed. CommandData is null.");
            GameTimeType = GameTimeType.WaitStop;
            return;
        }

        BaseEntity actorEntity = SessionProfile.EntitySyncPolicy.GetCommandEntity(GetSystem<EntitySystem>());
        if (actorEntity == null)
        {
            GameLog.Error(GameLogChannel.Battle, "Record command failed. Command actor entity is null. World will stop.");
            GameTimeType = GameTimeType.WaitStop;
            return;
        }

        data.Tick = _localTick;
        data.EntityId = actorEntity.EntityId;

        // 记录指令
        GetSystem<CommandSystem>().RecodeCommand(data);

        if (SessionProfile.SimulatePacketLoss && data.IsSkillPacket())
        {
            
            float value = Random.Range(0, 1.0f);

            if (value <= _lossPacket)
            {
                CommandData sendData = CommandData.Create();

                sendData.Tick = data.Tick;
                sendData.EntityId = data.EntityId;
                
                //发送指令
                GetSystem<ServerCommandSystem>().SendPlayerInput(sendData);
                
                return;
            }
        }
        
        //发送指令
        GetSystem<ServerCommandSystem>().SendPlayerInput(data);
    }

    public virtual void Shutdown()
    {
        foreach (var system in _systemDic.Values)
        {
            system.OnDispose();
        }

        if (_kccPhysicsDebugView != null)
        {
            UnityEngine.Object.Destroy(_kccPhysicsDebugView.gameObject);
            _kccPhysicsDebugView = null;
        }

        _authorityTick = 0;
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
    /// 地图根节点
    /// </summary>
    public Transform MapRoot => _mapRoot;

    /// <summary>
    /// 技能资产根节点
    /// </summary>
    public Transform SkillTimeLineRoot => _skillTimeLineRoot;
    
    /// <summary>
    /// 行为树根节点
    /// </summary>
    public Transform BehaviourTreeRoot => _behaviourTreeRoot;

    /// <summary>
    /// 场景名
    /// </summary>
    public string SceneName => _sceneName;
}
