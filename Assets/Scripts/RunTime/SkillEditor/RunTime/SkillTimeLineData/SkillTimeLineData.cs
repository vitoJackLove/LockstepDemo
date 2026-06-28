using System;
using UnityEngine;

/// <summary>
/// 技能TimeLine数据
/// </summary>
public partial class SkillTimeLineData : IPool
{
    /// <summary>
    /// 执行ID (唯一)
    /// </summary>
    private int _executeId;

    public int ExecuteId => _executeId;
    public PlayableStateEnum State => _timelineLauncher?.State ?? PlayableStateEnum.Error;

    private SkillTimelineLauncher _timelineLauncher;

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="id"></param>
    /// <param name="baseEntity"></param>
    /// <param name="skillTimeLineObj"></param>
    /// <param name="skillTimelineLauncher"></param>
    /// <param name="onTimeLineStopAction"></param>
    public void Init(int id, BaseEntity baseEntity,  
        GameObject skillTimeLineObj,SkillTimelineLauncher skillTimelineLauncher,Action onTimeLineStopAction = null)
    {
        this._executeId = id;
        this._timelineLauncher = skillTimelineLauncher;
     
    }

    /// <summary>
    /// 恢复到初始化状态重新执行
    /// </summary>
    public void RefreshInitState()
    {
      
    }
    
    /// <summary>
    /// 执行
    /// </summary>
    /// <param name="deltaTime"></param>
    public PlayableStateEnum Execute(float deltaTime)
    {
        return _timelineLauncher.Tick(fpmath1.LogicDeltaTime);
    }


    public void Release()
    {
        FreeBack();
    }

    /// <summary>
    /// 暂停
    /// </summary>
    /// <param name="isPause"></param>
    public void Pause(bool isPause)
    {
        _timelineLauncher?.Pause(isPause);
    }

    public void Clear()
    {
        _executeId = 0;
    }

    /// <summary>
    /// 回收
    /// </summary>
    private void FreeBack()
    {
       
    }
}