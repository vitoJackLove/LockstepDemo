using System.Collections.Generic;
using Unity.Mathematics.FixedPoint;

/// <summary>
/// world 预测回滚
/// </summary>
public partial class BaseWorld
{
    /// <summary>
    /// 预测的帧数
    /// </summary>
    private uint _forecastTick;

    /// <summary>
    /// 模拟丢包率
    /// </summary>
    private float _lossPacket;
    
    /// <summary>
    /// 客户端权威执行帧号 等价回滚的帧号
    /// </summary>
    private uint _authorityTick;

    public uint AuthorityTick => _authorityTick;

    /// <summary>
    /// 是否开启回滚刷新
    /// </summary>
    private bool _isStartRollBackUpdate;

    /// <summary>
    /// 回滚结束帧
    /// </summary>
    private uint _rollBackEndTick;

    /// <summary>
    /// 回滚速度
    /// </summary>
    private int _rollBackSpeed = 1;

    private uint _skipForecastVerifyUntilTick;

    /// <summary>
    /// 初始化数据
    /// </summary>
    /// <param name="forecastTick"></param>
    /// <param name="lossPacket"></param>
    public void InitRollBackData(uint forecastTick, float lossPacket)
    {
        _forecastTick = forecastTick;
        _lossPacket = lossPacket;
    }

    /// <summary>
    /// 拍摄本地实体快照
    /// </summary>
    private void TakeLocalSnapShot()
    {
        foreach (var system in _systemDic.Values)
        {
            system.TakeLocalSnapShot(_localTick);
        }  
        
        GameLog.Debug(GameLogChannel.Rollback, $"<color=blue> 本地帧：{_localTick} 快照拍摄完毕 </color>");
    }

    /// <summary>
    /// 拍摄权威实体快照
    /// </summary>
    private void TakeAuthoritySnapShot()
    {
        foreach (var system in _systemDic.Values)
        {
            system.TakeAuthoritySnapShot(_authorityTick);
        }  
        
        GameLog.Debug(GameLogChannel.Rollback, $"<color=green> 权威帧：{_authorityTick} 快照拍摄完毕 </color>");
    }

    /// <summary>
    /// 移除错误的快照
    /// </summary>
    private void RemoveLocalSnapShot(uint startErrorTick, RollBackType rollbackType)
    {
        foreach (var system in _systemDic.Values)
        {
            system.RemoveLocalSnapShot(startErrorTick, rollbackType);
        }  
    }

    /// <summary>
    /// 回滚世界
    /// </summary>
    /// <param name="tick"></param>
    /// <param name="rollBackType"></param>
    private void RollBack(uint tick, RollBackType rollBackType)
    {
        if (tick == 0)
        {
            return;
        }
        
        foreach (var system in _systemDic.Values)
        {
            system.RollBack(tick, rollBackType);
        }
    }

    /// <summary>
    /// 开启回滚
    /// </summary>
    /// <param name="rollBackTick"></param>
    /// <param name="rollBackType"></param>
    public void StartRollBack(uint rollBackTick, RollBackType rollBackType)
    {
        if (!SessionProfile.RequiresRollback)
        {
            GameLog.Warn(GameLogChannel.Rollback, $"Ignore rollback. Session mode={SessionProfile.Mode} tick={rollBackTick} localTick={_localTick}");
            return;
        }

        GameLog.Warn(GameLogChannel.Rollback, $"开始回滚 tick={rollBackTick} localTick={_localTick} authTick={_authorityTick}");

        //回滚到错误帧之前
        RollBack(rollBackTick, rollBackType);

        //移除本地错误的快照
        RemoveLocalSnapShot(rollBackTick, rollBackType);

        //如果是硬回滚
        if ((rollBackType & RollBackType.HardRollBack) == RollBackType.HardRollBack)
        {
            _isStartRollBackUpdate = true;

            _rollBackEndTick = _localTick;

            SkipForecastVerifyUntil(_rollBackEndTick + _forecastTick);

            _localTick = rollBackTick;

            GameLog.Warn(GameLogChannel.Rollback, $"回放开始 localTick={_localTick} endTick={_rollBackEndTick}");
        }
    }

    /// <summary>
    /// 本地刷新世界
    /// </summary>
    /// <param name="deltaTime"></param>
    /// <param name="data"></param>
    private void LogicUpdateWorld(fp deltaTime, CommandData data)
    {
        GameLog.Debug(GameLogChannel.Rollback, $"本地刷新世界 ——------------第 {_localTick} Tick ----------------");
        
        if (data != null /*&& !data.IsNullCommand()*/)
        {
            ExecuteLogicCommand(data);
        }   
        
        foreach (var system in _systemDic.Values)
        {
            system.OnFixedUpdate(deltaTime, WorldUpdateType.Local);
        }
    }
    
    /// <summary>
    /// 权威刷新世界
    /// </summary>
    private void AuthorityUpdateWorld(fp deltaTime)
    {
        if (_localTick - _authorityTick < _forecastTick)
        {
            return;
        }
        
        _authorityTick++;
        
        // 获取服务器指令（单机由 LocalLoopbackTransport 当帧注入）
        List<CommandData> commandDataList = GetSystem<ServerCommandSystem>().GetServerCommand(_authorityTick);

        if (commandDataList != null)
        {
            if (SessionProfile.RequiresAuthoritySnapshot)
            {
                TakeAuthoritySnapShot();
            }
            
            GameLog.Debug(GameLogChannel.Rollback, $"权威刷新世界 ——------------第 {_authorityTick} Tick ----------------");
            
            // 执行服务器指令
            ExecuteServerCommand(commandDataList);
        
            foreach (var system in _systemDic.Values)
            {
                system.OnFixedUpdate(deltaTime, WorldUpdateType.Authority);
            }

            if (SessionProfile.RequiresRollback)
            {
                VerifyForecast(_authorityTick, deltaTime);
            }

            AuthorityUpdateWorld(deltaTime);
        }
        else
        {
            _authorityTick--;
        }
    }

    /// <summary>
    /// 回滚本地刷新世界
    /// </summary>
    /// <param name="deltaTime"></param>
    private void RollBackLocalUpdateWorld(fp deltaTime)
    {
        if (_localTick > _rollBackEndTick)
        {
            _localTick--;

            _isStartRollBackUpdate = false;

            GameLog.Warn(GameLogChannel.Rollback, $"回放结束 localTick={_localTick}");

            return;
        }

        CommandData selfCommandData = GetSystem<ServerCommandSystem>().GetServerCommand(_localTick, (uint)GetSystem<EntitySystem>().ActorAuthorityEntity.EntityId);

        ExecuteLogicCommand(selfCommandData);

        foreach (var system in _systemDic.Values)
        {
            system.OnFixedUpdate(deltaTime, WorldUpdateType.RollBack);
        }

        var actorLocal = GetSystem<EntitySystem>().ActorLocalEntity;
        var actorAuth = GetSystem<EntitySystem>().ActorAuthorityEntity;
        GameLog.Warn(GameLogChannel.Rollback, $"回放位置 Tick={_localTick} 本地={actorLocal?.transform.Position} 权威={actorAuth?.transform.Position}");

        _localTick++;
    }
    
    /// <summary>
    /// 校验预测
    /// </summary>
    private void VerifyForecast(uint tick, fp deltaTime)
    {
        if (tick <= _skipForecastVerifyUntilTick)
        {
            return;
        }

        RollBackType rollBackType = RollBackType.NoRollBack;
        
        foreach (var system in _systemDic.Values)
        {
            rollBackType |= system.VerifyForecast(tick);
        }
        
        if (rollBackType != RollBackType.NoRollBack)
        {
            GameLog.Error(GameLogChannel.Rollback, $"权威帧 ： {tick} 校验失败... 开始 {rollBackType} 回滚");

            StartRollBack(tick, rollBackType);
        }
    }

    public void SkipForecastVerifyUntil(uint tick)
    {
        if (_skipForecastVerifyUntilTick < tick)
        {
            _skipForecastVerifyUntilTick = tick;
        }
    }
}
