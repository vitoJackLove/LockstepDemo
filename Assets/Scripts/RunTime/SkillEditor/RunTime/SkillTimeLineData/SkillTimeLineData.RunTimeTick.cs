using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

public partial class SkillTimeLineData 
{
    /// <summary>
    /// 当技能树结束执行
    /// </summary>
    private Action _onSkillStopAction;

    /// <summary>
    /// Clip
    /// </summary>
    private List<List<TaskClip>> _behaviours;

    /// <summary>
    /// 当前帧号
    /// </summary>
    private int _currentFrameID;

    /// <summary>
    /// 资源
    /// </summary>
    private SkillLineAsset _skillTimeAssets;

    /// <summary>
    /// 运行时的资源
    /// </summary>
    public SkillLineAsset RunTimeSkillTimeAssets => _skillTimeAssets;

    /// <summary>
    /// 间隔
    /// </summary>
    private float _interval;

    /// <summary>
    /// 绑定的实体
    /// </summary>
    private BaseEntity _executeEntity;

    /// <summary>
    /// 执行状态
    /// </summary>
    private PlayableStateEnum _stateEnum;

    /// <summary>
    /// 暂停
    /// </summary>
    private bool _isPause;

    public void BindStopAction(Action onTimeLineStopAction)
    {
        this._onSkillStopAction = onTimeLineStopAction;
    }

    /// <summary>
    /// 初始化资源
    /// </summary>
    /// <param name="skillTimelineAsset"></param>
    /// <param name="onTimeLineStopAction"></param>
    /// <returns></returns>
    private SkillLineAsset InitAsset(SkillLineAsset skillTimelineAsset, Action onTimeLineStopAction = null)
    {
        _isPause = false;

        _stateEnum = PlayableStateEnum.Init;

        _currentFrameID = 0;

        _skillTimeAssets = skillTimelineAsset;

        if (_skillTimeAssets.fps == 0)
        {
            Debug.LogError($"技能执行错误：技能{_skillTimeAssets.name}的FPS为0");

            return null;
        }

        _interval = 0.033f;

        _behaviours = new List<List<TaskClip>>();

        for (var trackIndex = 0; trackIndex < _skillTimeAssets.tracks.Count; trackIndex++)
        {
            var track = _skillTimeAssets.tracks[trackIndex];

            _behaviours.Add(new List<TaskClip>());

            for (var clipIndex = 0; clipIndex < track.taskClips.Count; clipIndex++)
            {
                InitClipVariable(track.taskClips[clipIndex], _skillTimeAssets);

                var clip = track.taskClips[clipIndex];

                _behaviours[trackIndex].Add((TaskClip)clip);
            }
        }

        BindStopAction(onTimeLineStopAction);

        return _skillTimeAssets;
    }

    /// <summary>
    /// 初始化节点变量
    /// </summary>
    private void InitClipVariable(TaskClip taskClip, SkillLineAsset skillTimelineAsset)
    {
        Type t = taskClip.GetType();

        FieldInfo[] memberInfos = t.GetFields();

        for (int i = 0; i < memberInfos.Length; i++)
        {
            FieldInfo propertyInfo = memberInfos[i];

            Type propertyType = propertyInfo.FieldType;

            object propertyValue = propertyInfo.GetValue(taskClip);

            if (propertyType.BaseType == typeof(BaseClipVariable))
            {
                BaseClipVariable baseClipVariable = (BaseClipVariable)propertyValue;

                baseClipVariable.SetBlackBoard(skillTimelineAsset.blackBoardVariable);
            }
        }
    }

    private void OnTimelineExit(BaseEntity context)
    {
        if (_skillTimeAssets == null)
        {
            _stateEnum = PlayableStateEnum.Error;

            return;
        }

        for (var trackIndex = 0; trackIndex < _skillTimeAssets.tracks.Count; trackIndex++)
        {
            var track = _skillTimeAssets.tracks[trackIndex];
            for (var clipIndex = 0; clipIndex < track.taskClips.Count; clipIndex++)
            {
                var clip = track.taskClips[clipIndex];
                var behavior = _behaviours[trackIndex][clipIndex];
                if (behavior == null) break;
                behavior.OnTimelineEnd(context, _skillTimeAssets.fps, _currentFrameID);
            }
        }

        _onSkillStopAction?.Invoke();
    }

    /// <summary>
    /// 执行
    /// </summary>
    /// <param name="delta"></param>
    /// <returns>执行结果</returns>
    private PlayableStateEnum Tick(fp delta)
    {
        if (_behaviours == null) return PlayableStateEnum.Error;
        if (_interval == 0) return PlayableStateEnum.Error;
        if (_stateEnum == PlayableStateEnum.Exit) return PlayableStateEnum.Error;

        if (_isPause)
        {
            return PlayableStateEnum.Pause;
        }

        _stateEnum = PlayableStateEnum.Running;

        _currentFrameID += 1;

        TickPrivate(delta);

        return _stateEnum;
    }

    /// <summary>
    /// 自定义帧号执行
    /// </summary>
    /// <param name="currentFrameId"></param>
    /// <returns>执行结果</returns>
    public PlayableStateEnum Tick(int currentFrameId)
    {
        if (_behaviours == null) return PlayableStateEnum.Error;

        if (_isPause)
        {
            return PlayableStateEnum.Pause;
        }

        _stateEnum = PlayableStateEnum.Running;

        _currentFrameID = currentFrameId;

        TickPrivate(fpmath1.LogicDeltaTime);

        return _stateEnum;
    }

    private void TickPrivate(fp delta)
    {
        if (_currentFrameID > _skillTimeAssets.duration)
        {
            _stateEnum = PlayableStateEnum.Exit;

            OnTimelineExit(_executeEntity);

            return;
        }

        fp deltaTime = delta;

        for (var trackIndex = 0; trackIndex < _skillTimeAssets.tracks.Count; trackIndex++)
        {
            var track = _skillTimeAssets.tracks[trackIndex];
            
            for (var clipIndex = 0; clipIndex < track.taskClips.Count; clipIndex++)
            {
                var clip = track.taskClips[clipIndex];
                
                var behavior = _behaviours[trackIndex][clipIndex];
                
                if (behavior == null) break;
                
                if (_currentFrameID >= clip.taskStartID && _currentFrameID < clip.taskStartID + clip.taskDuration)
                {
                    if (behavior.State == PlayableStateEnum.Exit)
                    {
                        behavior.OnRunTimeEnter(_executeEntity, _skillTimeAssets.fps);
                    }

                    behavior.RunTimeTick(_currentFrameID - clip.taskStartID, _skillTimeAssets.fps, deltaTime, _executeEntity);
                }
                else if ((_currentFrameID < clip.taskStartID ||
                          _currentFrameID >= clip.taskStartID + clip.taskDuration) &&
                         behavior.State == PlayableStateEnum.Running)
                {
                    behavior.OnRunTimeExit(_executeEntity);
                }
            }
        }
    }

    /// <summary>
    /// 强制执行结束
    /// </summary>
    private void ForceExecuteStop()
    {
        _currentFrameID = _skillTimeAssets.duration + 1;
    }

    public int CurrentFrame => _currentFrameID;
}
