using System;
using GameContent;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using Object = UnityEngine.Object;

/// <summary>
/// 处理编辑器事件
/// </summary>
public partial class SkillTimelineEditorWindow: EditorWindow
{
    #region 事件处理

    //处理事件
    private void ProcessEvent(BaseEvent baseEvent)
    {
        switch (baseEvent.EventType)
        {
            //Clip事件
            case UIEventType.ClipMove:
            {
                ClipMove((ClipMoveEvent)baseEvent);
                break;
            }
            case UIEventType.ClipResize:
            {
                ClipResize((ClipResizeEvent)baseEvent);
                break;
            }
            case UIEventType.ClipKeyborad:
            {
                ProcessKeyBorad((KeyboradEvent)baseEvent);
                break;
            }
            case UIEventType.ClipClick:
                ClipClick((ClipClickEvent)baseEvent);
                break;
            case UIEventType.ClipRightClick:
                ClipRightClick((ClipRightClickEvent)baseEvent);
                break;
            case UIEventType.ClipResizeEnd:
            case UIEventType.ClipMoveEnd:
                SaveTimelineAssetIfAutoSave();
                break;
            //控件事件
            case UIEventType.Controller:
            {
                ProcessController((ControllerEvent)baseEvent);
                break;
            }
            //Timeline
            case UIEventType.TimelineScale:
            {
                ProcessTimelineScale((TimelineScaleEvent)baseEvent);
                break;
            }
            case UIEventType.TimelineDrag:
            {
                ProcessTimelineDrag((TimelineDragEvent)baseEvent);
                break;
            }
            case UIEventType.TimelineDragEnd:
            {
                ProcessTimelineDragEnd((TimelineDragEndEvent)baseEvent);
                break;
            }
            //右键轨道头部
            case UIEventType.HeadRightClick:
                ProcessHeadRightClick((HeadRightClickEvent)baseEvent);
                break;
            //右键轨道体
            case UIEventType.BodyRightClick:
                ProcessBodyRightClick((BodyRightClick)baseEvent);
                break;
        }
    }

    private void ProcessWindowKeyboardShortcuts()
    {
        var currentEvent = UnityEngine.Event.current;
        if (currentEvent.type != UnityEngine.EventType.KeyDown || EditorGUIUtility.editingTextField)
        {
            return;
        }

        if (currentEvent.keyCode == KeyCode.Delete && DeleteSelectedClip())
        {
            currentEvent.Use();
        }
    }

    //当前帧改变事件
    private void CurrentFrameIDChange()
    {
        if (Asset == null) return;

        if (!Application.isPlaying)
        {
            _editorTick.Tick(CurrentFrameID, FPS, objectContext);
        }
        else
        {
            if (_runtimeTick == null)
            {
                return;
            }

            if (_runtimeTick.State != PlayableStateEnum.Running)
            {
                Debug.Log($"当前执行的Tick  = {CurrentFrameID}");
            
                _runtimeTick.Tick(CurrentFrameID);
            }
        }
    }

    //在时间轴上拖动
    private void ProcessTimelineDrag(TimelineDragEvent dragEvent)
    {
        _targetFrameID = MouseCurrentFrameID;
    }

    //在时间轴上结束
    private void ProcessTimelineDragEnd(TimelineDragEndEvent baseEvent)
    {
        _targetFrameID = -1;
    }

    //时轴缩放
    private void ProcessTimelineScale(TimelineScaleEvent scaleEvent)
    {
        WidthPreFrame -= Mathf.Sign(UnityEngine.Event.current.delta.y) * WidthPreFrame * 0.2f;
        //仅仅是+=1的话没有卡死
        Repaint();
    }

    //控件事件
    private void ProcessController(ControllerEvent controller)
    {
        switch (controller.ControllerType)
        {
            case ControllerType.ToMostBegin:
                CurrentFrameID = 0;
                break;
            case ControllerType.ToPre:
                CurrentFrameID -= 1;
                break;
            case ControllerType.Play:
                IsPlaying = !IsPlaying;
                break;
            case ControllerType.ToNext:
                CurrentFrameID += 1;
                break;
            case ControllerType.ToMostEnd:
                CurrentFrameID = Asset.duration;
                break;
        }
    }

    //Clip选中
    private void ClipClick(ClipClickEvent click)
    {
        SkillTimelineInspector.ShowInspector(this, Asset.tracks[click.TrackIndex].taskClips[click.ClipIndex]);
        HighLight[0] = click.TrackIndex;
        HighLight[1] = click.ClipIndex;
        ShowClipBlackBoard(click.TrackIndex, click.ClipIndex);
        Repaint();
    }

    private ClipBlackBoardWindow _boardWindow;
    
    private void ShowClipBlackBoard(int trackIndex, int clipIndex)
    {
        if (_boardWindow != null)
        {
            _boardWindow.Close();
        }
        
        _boardWindow = EditorWindow.CreateWindow<ClipBlackBoardWindow>("节点面板");
        _boardWindow.Show();
        ClipBlackBoardWindow.ShowWindow();
        ClipBlackBoardWindow.SetSelectClip(Asset.tracks[trackIndex].taskClips[clipIndex],
            Asset.blackBoardVariable, this);
    }

    //Clip右键
    private void ClipRightClick(ClipRightClickEvent click)
    {
      
    }

    //Clip大小改变
    private void ClipResize(ClipResizeEvent clipResize)
    {
        SkillTimelineFactory.ResizeClip(Asset, clipResize.TrackIndex, clipResize.ClipIndex, (int)(MouseCurrentFrameID -
            (clipResize.OffsetMouseX / WidthPreFrame) -
            Asset.tracks[clipResize.TrackIndex].taskClips[clipResize.ClipIndex].taskStartID));
        Repaint();
    }

    //Clip移动事件
    private void ClipMove(ClipMoveEvent clipMove)
    {
        SkillTimelineFactory.MoveClip(Asset, AssetPath, clipMove.TrackIndex, clipMove.ClipIndex,
            (int)(MouseCurrentFrameID - (clipMove.OffsetMouseX / WidthPreFrame)));
        Repaint();
    }

    //处理按键事件
    private void ProcessKeyBorad(KeyboradEvent keyborad)
    {
        switch (keyborad.Shortcut)
        {
            case Shortcut.CtrlC:

                _tempObject = SkillTimelineFactory.CopyClip(Asset, keyborad.TrackIndex, keyborad.ClipIndex);

                break;
            case Shortcut.CtrlV:

                _tempObject = Object.Instantiate(_tempObject);

                SkillTimelineFactory.AddClip(Asset, _editorTick, AssetPath, keyborad.TrackIndex, MouseCurrentFrameID,
                    _tempObject, AutoSave);
                break;

            case Shortcut.CtrlX:

                _tempObject = SkillTimelineFactory.CopyClip(Asset, keyborad.TrackIndex, keyborad.ClipIndex);

                SkillTimelineFactory.RemoveClip(Asset, _editorTick, AssetPath, keyborad.TrackIndex, keyborad.ClipIndex,
                    AutoSave);

                HighLight[0] = -1;
                HighLight[1] = -1;
                break;
            case Shortcut.Delete:
                DeleteSelectedClip();
                break;
            
            case Shortcut.CtrlS:

                SaveTimelineAsset();
                
                break;
        }

        Repaint();
    }

    //右键轨道头部
    private void ProcessHeadRightClick(HeadRightClickEvent click)
    {
        GenericMenu menu = new GenericMenu();
        //创建轨道菜单
        CreateAddTrackMenu(click.TrackIndex, menu);
        menu.AddItem(new GUIContent("删除轨道"), false, () =>
        {
            SkillTimelineFactory.RemoveTrack(Asset, _editorTick, AssetPath, click.TrackIndex, AutoSave);
            HighLight[0] = -1;
            HighLight[1] = -1;
        });
        // 显示菜单
        menu.ShowAsContext();
    }

    // 添加创建轨道菜单项
    private void CreateAddTrackMenu(int trackIndex, GenericMenu menu)
    {
        //拿到所有StandardTrack的子类型
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        //遍历子类型得到菜单
        foreach (Assembly assembly in assemblies)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsSubclassOf(typeof(StandardTrack)))
                {
                    string trackName = type.GetCustomAttribute<TrackNameAttribute>().Name;
                    menu.AddItem(new GUIContent("添加轨道/" + trackName), false,
                        () => { SkillTimelineFactory.CreatTrack(Asset, _editorTick, AssetPath, trackIndex, type, AutoSave); });
                }
            }
        }
    }

    //右键轨道体
    private void ProcessBodyRightClick(BodyRightClick click)
    {
        GenericMenu menu = new GenericMenu();

        var types = Asset.tracks[click.TrackIndex].GetType().GetCustomAttribute<TrackBindClipAttribute>().Types;

        foreach (var clipType in types)
        {
            string clipName = clipType.GetCustomAttribute<ClipNameAttribute>().ClipName;
            // 添加添加Clip
            menu.AddItem(new GUIContent("添加Clip/" + clipName), false,
                () =>
                {
                    SkillTimelineFactory.CreateClip(Asset, _editorTick, AssetPath, click.TrackIndex, click.MouseFrameID,
                        clipType, AutoSave);
                });
        }

        // 显示菜单
        menu.ShowAsContext();
    }

    /// <summary>
    /// 添加一个变量
    /// </summary>
    /// <param name="skillBlackVariable"></param>
    public void AddVariable(SkillBlackVariable skillBlackVariable)
    {
        SkillTimelineFactory.AddVariable(skillBlackVariable, Asset, AutoSave);
        SkillEditorBlackBoard.RefreshVariable(Asset.blackBoardVariable);
    }

    /// <summary>
    /// 移除一个变量
    /// </summary>
    /// <param name="indexVariable"></param>
    /// <param name="skillBlackVariable"></param>
    public void RemoveVariable(int indexVariable,SkillBlackVariable skillBlackVariable)
    {
        SkillTimelineFactory.RemoveVariable(indexVariable, skillBlackVariable, Asset, AutoSave);
        SkillEditorBlackBoard.RefreshVariable(Asset.blackBoardVariable);
        skillBlackVariable.RefreshBind();
    }

    public bool DeleteSelectedClip()
    {
        if (!HasSelectedClip())
        {
            return false;
        }

        int trackIndex = HighLight[0];
        int clipIndex = HighLight[1];
        SkillTimelineFactory.RemoveClip(Asset, _editorTick, AssetPath, trackIndex, clipIndex, AutoSave);
        HighLight[0] = -1;
        HighLight[1] = -1;

        if (_boardWindow != null)
        {
            _boardWindow.Close();
            _boardWindow = null;
        }

        Repaint();
        return true;
    }

    private bool HasSelectedClip()
    {
        if (Asset == null || HighLight[0] < 0 || HighLight[1] < 0)
        {
            return false;
        }

        if (HighLight[0] >= Asset.tracks.Count)
        {
            return false;
        }

        return HighLight[1] < Asset.tracks[HighLight[0]].taskClips.Count;
    }
    
    #endregion
}
