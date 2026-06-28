using System.Collections.Generic;
using Ase.Serializing;
using Rogue;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// 状态组件
/// </summary>
public class StateComponent : BaseComponent
{
    /// <summary>
    /// 普通状态ID
    /// </summary>
    private readonly int _normalStateId = 1;
    
    /// <summary>
    /// 当前状态的优先级
    /// </summary>
    private int _currentStatePriority;

    /// <summary>
    /// 当前状态ID
    /// </summary>
    protected int CurrentStateId;
    
    /// <summary>
    /// 当前状态TimeLine
    /// </summary>
    protected SkillTimelineLauncher CurrentStateTimeLine;
    
    /// <summary>
    /// 逻辑状态
    /// </summary>
    protected Dictionary<int, SkillTimelineLauncher> StateTimeline = new();

    public override void OnStart(object data = null)
    {
        base.OnStart(data);

        ChangeState(_normalStateId, true, false);
    }
    
    public override void OnFixedUpdate(fp deltaTime, WorldUpdateType worldUpdateType)
    {
        base.OnFixedUpdate(deltaTime, worldUpdateType);

        if (CurrentStateTimeLine != null)
        {
            CurrentStateTimeLine.Tick(deltaTime);
        }
    }

    /// <summary>
    /// 切换状态
    /// </summary>
    /// <param name="stateId"></param>
    /// <param name="isForce"></param>
    /// <param name="isRollBackBreakState"></param>
    public void ChangeState(int stateId,bool isForce, bool isRollBackBreakState)
    {
        StateAssetsConfig config = GameEntry.DataTable.GetDataTable<StateAssetsConfig>(stateId);

        if (config == null)
        {
            return ;
        }
        
        if (_currentStatePriority < config.priority || isForce)
        {
            BreakState(config, isRollBackBreakState);

            EnableState(config);
        }
    }
    
    /// <summary>
    /// 打断当前状态
    /// </summary>
    protected virtual void BreakState(StateAssetsConfig config,bool isRollBackBreakState) { }

    /// <summary>
    /// 进入状态
    /// </summary>
    /// <param name="config"></param>
    protected virtual void EnableState(StateAssetsConfig config)
    {
        Entity.SetMovementState(config.isCanMove, config.isCanRotate);
        Entity.SetReleaseSKillState(config.isCanPlaySkill);
        _currentStatePriority = config.priority;
        CurrentStateId = config.assetsId;
        Entity.EntityDebug($"进入状态 {CurrentStateId}");
    }

    /// <summary>
    /// 进入正常状态
    /// </summary>
    public void EnterNormalState()
    {
        ChangeState(_normalStateId, true,false);
    }
    
    public override void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
    {
        hardWriter.WriteInt32Data($"当前的状态ID", CurrentStateId);

        if (CurrentStateTimeLine == null)
        {
            hardWriter.WriterBoolData($"是否存在状态TimeLine", false);
        }
        else
        {
            hardWriter.WriterBoolData($"是否存在状态TimeLine", true);
            
            CurrentStateTimeLine.TakeSnapShot(hardWriter, softWriter);
        }
    }

    public override void HardRollBackTo(PooledReader authoritySnapShot)
    {
        int authorityState = authoritySnapShot.ReadInt32();
        bool isStateTimeLine = authoritySnapShot.ReadBoolean();

        //权威有异常状态 并且现在的状态和权威的状态一致 只用回滚Timeline
        if (isStateTimeLine && CurrentStateId == authorityState && CurrentStateTimeLine != null)
        {
            CurrentStateTimeLine.RollBackTo(authoritySnapShot);
        }
        // 权威和当前状态不一致 权威在异常状态中
        else if (CurrentStateId !=  authorityState && isStateTimeLine)
        {
            ChangeState(authorityState, true,true);
            
            CurrentStateTimeLine.RollBackTo(authoritySnapShot);
        }
        //权威和当前状态不一致 权威在正常状态中
        else if (CurrentStateId !=  authorityState && isStateTimeLine == false)
        {
            ChangeState(authorityState, true, true);
        }
    }
}
