using System;
using Ase.Serializing;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

[Serializable]
public abstract class TaskClip : ScriptableObject
{
    [VariableName("节点名字")]
    public string taskName;
    [VariableName("节点开始的帧号")]
    public int taskStartID;
    [VariableName("节点时间长度")]
    public int taskDuration;
    
    protected PlayableStateEnum StateEnum = PlayableStateEnum.Exit;

    public virtual void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter) { }

    public virtual void RollBackTo(PooledReader authoritySnapShot) { }

    /// <summary>
    /// 回滚导致的退出
    /// </summary>
    /// <param name="context"></param>
    public virtual void RollBackExit(BaseEntity context)
    {
        this.StateEnum = PlayableStateEnum.Exit;
    }

    /// <summary>
    /// 回滚导致再一次进入Enter
    /// </summary>
    /// <param name="context"></param>
    /// <param name="fps"></param>
    public virtual void RollBackEnter(BaseEntity context,int fps)
    {
        StateEnum = PlayableStateEnum.Running;
    }
    
    /// <summary>
    /// RunTime 执行
    /// </summary>
    /// <param name="context"></param>
    /// <param name="fps"></param>
    public virtual void OnRunTimeEnter(BaseEntity context, int fps)
    {
        StateEnum = PlayableStateEnum.Running;
    }
    
    /// <summary>
    /// Editor 模式下执行
    /// </summary>
    /// <param name="context"></param>
    /// <param name="fps"></param>
    /// <param name="currentFrameID"></param>
    public virtual void EditorEnter(GameObject context, int fps, int currentFrameID)
    {
        StateEnum = PlayableStateEnum.Running;
    }
    
    public virtual void RunTimeTick(int currentFrameID, int fps,fp deltaTime, BaseEntity context){}

    public virtual void EditorTick(int currentFrameID, int fps, fp deltaTime, GameObject context){}

    public virtual void OnRunTimeExit(BaseEntity context)
    {
        StateEnum = PlayableStateEnum.Exit;
    }

    public virtual void EditorExit(GameObject context, int fps, int currentFrameID)
    {
        StateEnum = PlayableStateEnum.Exit;
    }

    public virtual void OnTimelineEnd(BaseEntity context, int fps, int currentFrameID)
    {
        if (StateEnum != PlayableStateEnum.Exit) OnRunTimeExit(context);
    }

    public PlayableStateEnum State => StateEnum;
}