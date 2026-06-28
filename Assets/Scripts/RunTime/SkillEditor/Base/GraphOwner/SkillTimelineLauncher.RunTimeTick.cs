using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

public partial class SkillTimelineLauncher
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
    private SkillLineAsset RunTimeSkillTimeAssets => _skillTimeAssets;

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

    /// <summary>
    /// 是否执行表现轨道
    /// </summary>
    private bool _isExecuteViewTrack = true;

    private void BindStopAction(Action onTimeLineStopAction)
    {
        this._onSkillStopAction = onTimeLineStopAction;
    }

    /// <summary>
    /// 初始化资源
    /// </summary>
    /// <param name="executeEntity"></param>
    /// <param name="skillLineAsset"></param>
    /// <param name="onTimeLineStopAction"></param>
    /// <returns></returns>
    private SkillLineAsset InitAsset(BaseEntity executeEntity, SkillLineAsset skillLineAsset,
        Action onTimeLineStopAction = null)
    {
        _executeEntity = executeEntity;

        _isExecuteViewTrack = _executeEntity.IsNeedExecuteView;

        _isPause = false;

        _stateEnum = PlayableStateEnum.Init;

        _currentFrameID = 0;

        _skillTimeAssets = SkillTimeLineAssetsFactory.Current.GetInstance(skillLineAsset);

        if (_skillTimeAssets.fps == 0)
        {
            Debug.LogError($"技能执行错误：技能{skillLineAsset.name}的FPS为0");

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
    private void InitClipVariable(TaskClip taskClip, SkillLineAsset skillLineAsset)
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

                baseClipVariable.SetBlackBoard(skillLineAsset.blackBoardVariable);
            }
        }
    }

    private void OnTimelineExit(BaseEntity context)
    {
        if (_skillTimeAssets == null)
        { 
            _onSkillStopAction?.Invoke();

            _stateEnum = PlayableStateEnum.Error;

            return;
        }

        for (var trackIndex = 0; trackIndex < _skillTimeAssets.tracks.Count; trackIndex++)
        {
            var track = _skillTimeAssets.tracks[trackIndex];

            if (!_isExecuteViewTrack && track.TrackType == TrackType.View)
            {
                continue;
            }

            for (var clipIndex = 0; clipIndex < track.taskClips.Count; clipIndex++)
            {
                var behavior = _behaviours[trackIndex][clipIndex];

                if (behavior != null)
                {
                    behavior.OnTimelineEnd(context, _skillTimeAssets.fps, _currentFrameID);
                }
            }
        }

        _onSkillStopAction?.Invoke();

        _currentFrameID = 0;
    }

    /// <summary>
    /// 执行
    /// </summary>
    /// <param name="delta"></param>
    /// <returns>执行结果</returns>
    public PlayableStateEnum Tick(fp delta)
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

        TickPrivate((fp)0.033f);

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

            if (!_isExecuteViewTrack && track.TrackType == TrackType.View)
            {
                continue;
            }
            
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
                else if (/*(_currentFrameID < clip.taskStartID ||*/
                          _currentFrameID >= clip.taskStartID + clip.taskDuration &&
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
    /// <param name="isRollBackBreakSkill">是否是回滚导致的强制结束</param>
    public void ForceExecuteStop(bool isRollBackBreakSkill)
    {
        if (_stateEnum == PlayableStateEnum.Exit)
        {
            return;
        }
        
        _currentFrameID = 0;

        _stateEnum = PlayableStateEnum.Exit;

        for (var trackIndex = 0; trackIndex < _skillTimeAssets.tracks.Count; trackIndex++)
        {
            var track = _skillTimeAssets.tracks[trackIndex];
            
            if (!_isExecuteViewTrack && track.TrackType == TrackType.View)
            {
                continue;
            }
            
            for (var clipIndex = 0; clipIndex < track.taskClips.Count; clipIndex++)
            {
                var behavior = _behaviours[trackIndex][clipIndex];

                if (behavior != null)
                {
                    behavior.OnRunTimeExit(_executeEntity);
                }
            }
        }

        OnTimelineExit(_executeEntity);
    }

    /// <summary>
    /// 重置到初始化状态
    /// </summary>
    public void RefreshInitState()
    {
        _currentFrameID = 0;
        _stateEnum = PlayableStateEnum.Init;
        _interval = 0.033f;
    }

    /// <summary>
    /// 暂停
    /// </summary>
    /// <param name="isPause"></param>
    public void Pause(bool isPause)
    {
        this._isPause = isPause;
    }

    public PlayableStateEnum State => _stateEnum;

    public int CurrentFrame => _currentFrameID;

    public void Clear()
    {
        SkillTimeLineAssetsFactory.Current.RecycleTree(_skillTimeAssets);
        _stateEnum = PlayableStateEnum.Exit;
        _behaviours.Clear();
        _currentFrameID = 0;
        _interval = 0;
        _executeEntity = null;
        _onSkillStopAction = null;
        _isPause = false;
    }
}