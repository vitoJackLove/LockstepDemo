using System;
using System.Collections.Generic;
using Rogue;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 世界系统管理世界
/// </summary>
public class WorldSystem : Singleton<WorldSystem>, ISingletonAwake, ISingletonUpdate, ISingletonLateUpdate,
    ISingletonFixedUpdate
{
    private Dictionary<uint, BaseWorld> worldChannels;

    /// <summary>
    /// 世界序列
    /// </summary>
    private uint serialId = 0;

    /// <summary>
    /// 游戏的根节点
    /// </summary>
    private GameObject m_GameRoot;

#if UNITY_EDITOR
    /// <summary>
    /// 当前正在运行的世界  
    /// </summary>
    private BaseWorld _currentRunWorld;

    /// <summary>
    /// 当前正在运行的世界；若缓存引用丢失，则从仍存活的世界频道中恢复一个可供编辑器调试读取的世界。
    /// </summary>
    public BaseWorld CurrentRunWorld
    {
        get
        {
            if (_currentRunWorld != null)
            {
                return _currentRunWorld;
            }

            if (worldChannels == null)
            {
                return null;
            }

            foreach (KeyValuePair<uint, BaseWorld> worldChannel in worldChannels)
            {
                if (worldChannel.Value != null)
                {
                    _currentRunWorld = worldChannel.Value;
                    return _currentRunWorld;
                }
            }

            return null;
        }
    }
#endif


    public void Awake()
    {
        this.worldChannels = new Dictionary<uint, BaseWorld>();
    }

    public uint GenerateWorldId()
    {
        serialId++;
        return serialId;
    }

    /// <summary>
    /// 获取世界频道。
    /// </summary>
    /// <param name="worldId">世界频道编号。</param>
    public BaseWorld GetWorldChannel(uint worldId)
    {
        if (worldChannels.TryGetValue(worldId, out var worldChannel))
        {
            return worldChannel;
        }

        return null;
    }

    /// <summary>
    /// 创建世界频道
    /// </summary>
    /// <param name="worldModel">频道类型</param>
    /// <param name="scene"></param>
    /// <param name="worldName">世界名称</param>
    /// <param name="heroList">选择的角色</param>
    /// <returns></returns>
    public async System.Threading.Tasks.Task<bool> CreateWorldChannel(
        WorldModel worldModel,
        Scene scene,
        string worldName,
        List<PlayerData> heroList,
        GameSessionModeType sessionMode = GameSessionModeType.Online)
    {
        GameObject worldRoot = new GameObject(worldName);
        SceneManager.MoveGameObjectToScene(worldRoot, scene);
        SceneManager.SetActiveScene(scene);

        uint worldId = GenerateWorldId();
        
        CreateWorldData worldData = FPoolHelper.Get<CreateWorldData>();
        worldData.WorldId = worldId;
        worldData.WorldRoot = worldRoot.transform;
        worldData.HeroDataList.AddRange(heroList);
        worldData.SceneName = scene.name;
        worldData.SessionMode = sessionMode;
        worldData.SessionProfile = GameSessionFactory.CreateProfile(sessionMode);

        BaseWorld worldChannel = null;

        switch (worldModel)
        {
            case WorldModel.RogueLike:
            {
                worldChannel = new RogueWorld(worldData);

                break;
            }

            default:
            {
                return false;
            }
        }

        worldChannels.Add(worldId, worldChannel);
        
        await worldChannel.InitGameConfig();
        
        bool initResult = await worldChannel.WorldSystemInit(worldData);
        if (!initResult)
        {
            DestroyWorldChannel(worldChannel);
            FPoolHelper.Release<CreateWorldData>(worldData);
            return false;
        }

        bool result = await worldChannel.WorldEnter();

        if (!result)
        {
            DestroyWorldChannel(worldChannel);
            FPoolHelper.Release<CreateWorldData>(worldData);
            return false;
        }

        worldChannel.GameStart();
        
#if UNITY_EDITOR
        _currentRunWorld = worldChannel;
#endif
        
        FPoolHelper.Release<CreateWorldData>(worldData);

        return true;
    }

    /// <summary>
    /// 隐藏世界频道。
    /// </summary>
    /// <param name="worldId">世界频道编号。</param>
    public bool HideWorldChannel(uint worldId)
    {
        if (worldChannels.TryGetValue(worldId, out var worldChannel))
        {
            worldChannel.Hide();

            return worldChannels.Remove(worldId);
        }

        return false;
    }

    /// <summary>
    /// 销毁世界频道。
    /// </summary>
    /// <param name="worldId">世界频道编号。</param>
    /// <returns>是否销毁世界频道成功。</returns>
    public bool DestroyWorldChannel(uint worldId)
    {
        this.worldChannels.TryGetValue(worldId, out var worldChannel);

        return this.DestroyWorldChannel(worldChannel);
    }

    /// <summary>
    /// 销毁世界。
    /// </summary>
    /// <param name="worldBase">世界。</param>
    /// <returns>是否销毁世界频道成功。</returns>
    public bool DestroyWorldChannel(BaseWorld worldBase)
    {
        if (worldBase == null)
        {
            Debug.LogError("销毁世界错误. 世界不能为空.");

            return false;
        }

        worldBase.Shutdown();
        return this.worldChannels.Remove(worldBase.WorldId);
    }

    public void Update()
    {
        lock (this.worldChannels)
        {
            if (this.worldChannels.Count > 0)
            {
                foreach (var channel in this.worldChannels)
                {
                    channel.Value.Update(fpmath1.LogicDeltaTime);
                    channel.Value.RefreshKccPhysicsDebugDraw();
                }
            }
        }
    }

    public void FixedUpdate()
    {
        lock (this.worldChannels)
        {
            if (this.worldChannels.Count > 0)
            {
                foreach (var channel in this.worldChannels)
                {
                    channel.Value.FixedUpdate(fpmath1.LogicDeltaTime);
                }
            }
        }
    }
    
    public void LateUpdate()
    {
        
    }

    public override void Dispose()
    {
        try
        {
            lock (worldChannels)
            {
                foreach (var channel in worldChannels)
                {
                    channel.Value.Shutdown();
                }

                worldChannels.Clear();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Battle disable error. e : {e}");
        }
    }
}
