using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// Editor 调试
/// </summary>
public class SkillTimelineEditorTick
{
    private List<List<TaskClip>> _behaviors;
    private SkillLineAsset _graphAsset;

    public void PlayAsset(SkillLineAsset asset)
    {
        _graphAsset = asset;
        _behaviors = new List<List<TaskClip>>();
        for (var trackIndex = 0; trackIndex < _graphAsset.tracks.Count; trackIndex++)
        {
            var track = _graphAsset.tracks[trackIndex];
            _behaviors.Add(new List<TaskClip>());
            for (var clipIndex = 0; clipIndex < track.taskClips.Count; clipIndex++)
            {
                var clip = track.taskClips[clipIndex];
                InitClipVariable(clip, asset);
                _behaviors[trackIndex].Add(clip);
            }
        }
    }
    
    /// <summary>
    /// 初始化节点变量
    /// </summary>
    private void InitClipVariable(TaskClip taskClip,SkillLineAsset skillLineAsset)
    {
        if (taskClip == null ||skillLineAsset == null )
        {
            return;
        }
        
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

    public void Tick(int currentFrameID, int fps, GameObject context)
    {
        if (_behaviors == null) return;
        if (currentFrameID > _graphAsset.duration) return;
        if (fps <= 0) return;
        fp deltaTime = (fp)(1f / fps);
        for (var trackIndex = 0; trackIndex < _graphAsset.tracks.Count; trackIndex++)
        {
            var track = _graphAsset.tracks[trackIndex];
            for (var clipIndex = 0; clipIndex < track.taskClips.Count; clipIndex++)
            {
                var clip = track.taskClips[clipIndex];
                var behavior = _behaviors[trackIndex][clipIndex];
                if (behavior == null) break;
                if (currentFrameID >= clip.taskStartID && currentFrameID <= clip.taskStartID + clip.taskDuration)
                {
                    if (behavior.State == PlayableStateEnum.Exit)
                    {
                        behavior.EditorEnter(context, fps, currentFrameID - clip.taskStartID);
                    }

                    if (behavior.State == PlayableStateEnum.Running)
                    {
                        behavior.EditorTick(currentFrameID - clip.taskStartID, fps,deltaTime, context);
                    }

                }
                else if ((currentFrameID < clip.taskStartID || currentFrameID >= clip.taskStartID + clip.taskDuration) &&
                         behavior.State == PlayableStateEnum.Running)
                {
                    behavior.EditorExit(context, fps, currentFrameID - clip.taskStartID);
                }
            }
        }
    }

    public void AddBehaviour(int trackIndex, int clipIndex)
    {
        var track = _graphAsset.tracks[trackIndex];
        var clip = track.taskClips[clipIndex];
        _behaviors[trackIndex].Insert(clipIndex, clip);
    }

    public void RemoveBehaviour(int trackIndex, int clipIndex)
    {
        _behaviors[trackIndex].RemoveAt(clipIndex);
    }

    public void AddTrack(int trackIndex)
    {
        _behaviors.Insert(trackIndex, new List<TaskClip>());
    }

    public void RemoveTrack(int trackIndex)
    {
        _behaviors.RemoveAt(trackIndex);
    }
}
