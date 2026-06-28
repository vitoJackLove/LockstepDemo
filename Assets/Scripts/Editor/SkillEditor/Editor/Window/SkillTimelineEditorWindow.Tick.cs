using System;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 处理事件
/// </summary>
public partial class SkillTimelineEditorWindow: EditorWindow
{
    private int _currentFrameID;

    /// <summary>
    /// 换算值
    /// </summary>
    private int _targetFrameID = -1;

    /// <summary>
    /// 当前帧
    /// </summary>
    private int CurrentFrameID
    {
        get { return _currentFrameID; }
        set
        {
            if (value < 0) return;
            if (_currentFrameID == value) return;
            _currentFrameID = value;
            CurrentFrameIDChange();
            Repaint();
        }
    } 
    
    /// <summary>
    /// 鼠标位置转化为FrameID
    /// </summary>
    public int MouseCurrentFrameID 
        => (int)((UnityEngine.Event.current.mousePosition.x - _headWidth - _blackBoardWidth) / WidthPreFrame);
    
    private bool _isPlaying; 

    /// <summary>
    /// 是否在播放
    /// </summary>
    private bool IsPlaying
    {
        get { return _isPlaying; }
        
        set
        {
            _isPlaying = value;
            
            if (_isPlaying)
            {
                EditorCoroutineUtility.StartCoroutine(PlayCoroutine(), this);
            }
        }
    } 
    
    /// <summary>
    /// 运行依附的游戏对象
    /// </summary>
    public GameObject objectContext;

    /// <summary>
    /// Editor 执行器
    /// </summary>
    private SkillTimelineEditorTick _editorTick;

    /// <summary>
    /// 运行 执行器
    /// </summary>
    private SkillTimelineLauncher _runtimeTick;
    
    /// <summary>
    /// 当游戏物体选中
    /// </summary>
    private void OnSelectionChanged()
    {
        if (Selection.activeObject != null)
        {
            if (Selection.activeObject is GameObject go)
            {
                if (Application.isPlaying)
                {
                    if (go.GetComponent<SkillTimelineLauncher>())
                    {
                        Asset = go.GetComponent<SkillTimelineLauncher>().RunTimeGraph;
                        _runtimeTick = go.GetComponent<SkillTimelineLauncher>();
                    }
                }
                else
                {
                    if (go.GetComponent<Animator>())
                    {
                        objectContext = go;
                    }
                    
                    Asset = go.GetComponent<SkillTimelineLauncher>()?.graph;
                    
                    if (Asset == null)
                    {
                        return;
                    }
                    
                    _editorTick.PlayAsset(Asset);
                }
            }
            else if (Selection.activeObject is SkillLineAsset skillTimelineAsset)
            {
                Asset = skillTimelineAsset;
                
                _editorTick.PlayAsset(Asset);
            }
        }
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            if (_targetFrameID != -1 && CurrentFrameID != _targetFrameID)
            {
                for (int i = 0; i < Mathf.Abs(_targetFrameID - CurrentFrameID); i++)
                {
                    if (_targetFrameID != -1 && CurrentFrameID != _targetFrameID)
                    {
                        CurrentFrameID += (_targetFrameID - CurrentFrameID) > 0 ? 1 : -1;
                    }
                    else break;
                }
            }
        }
        else
        {
            if (!IsPlaying)
            {
                if (_runtimeTick == null)
                {
                    return;
                }
                
                if (_runtimeTick.State == PlayableStateEnum.Running)
                {
                    CurrentFrameID = _runtimeTick?.CurrentFrame ?? 0;
                }
            }
        }
    }

    private void RefreshFrame()
    {
        CurrentFrameID = 0;
    }

    private IEnumerator PlayCoroutine()
    {
        _startTime = DateTime.Now;
        _startFrameIndex = CurrentFrameID;
        while (IsPlaying)
        {
            //时间差
            float differ = (float)DateTime.Now.Subtract(_startTime).TotalSeconds;
            //计算当前帧
            CurrentFrameID = (int)(differ * FPS * 1) + _startFrameIndex;
            if (Asset != null && CurrentFrameID >= Asset.duration)
            {
                IsPlaying = false;
            }
            yield return null;
        }
    }
}


