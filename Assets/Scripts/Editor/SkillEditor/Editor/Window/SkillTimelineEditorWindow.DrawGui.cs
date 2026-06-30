using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 页面绘制
/// </summary>
public partial class SkillTimelineEditorWindow : EditorWindow
{
    public static string Title = "技能生成";

    private static SkillTimelineEditorWindow _window;
    
    //打开窗口
    [MenuItem("Tools/技能/技能编辑器")]
    public static void OpenWindow()
    {
        _window = GetWindow<SkillTimelineEditorWindow>(Title);
        _window.Show();
        _window.Focus();
    }

    /// <summary>
    /// 通过 Hero/Monster 配置中的 Skill 相对路径打开技能编辑器。
    /// </summary>
    public static void OpenWindowWithConfigPath(string configPath)
    {
        string assetPath = SkillTimelinePathUtility.ResolveEditorAssetPath(configPath);
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning("SkillTimeLine 配置路径为空，无法打开技能编辑器。");
            return;
        }

        SkillLineAsset asset = SkillTimelineFactory.LoadTimeLineAsset(assetPath);
        if (asset == null)
        {
            Debug.LogWarning($"未找到 SkillTimeLine 资产: {assetPath} (配置路径: {configPath})");
            return;
        }

        _window = GetWindow<SkillTimelineEditorWindow>(Title);
        _window.AssetPath = assetPath;
        _window.Show();
        _window.Focus();
    }

    private int _trackHeight = 30; //轨道高度
    private float _widthPreFrame = 1f;

    public float WidthPreFrame
    {
        get { return _widthPreFrame; }
        private set
        {
            if ((value) <= 0.005f) _widthPreFrame = 0.005f;
            else if (value >= 194f) _widthPreFrame = 194f;
            else _widthPreFrame = value;
        }
    } //每帧多宽

    private int _headWidth = 260; //轨道头部宽度

    private int _blackBoardWidth = 320; // 黑板宽度

    /// <summary>
    /// 变量宽度
    /// </summary>
    private int _variableWidth = 140;
    
    private int _intervalHeight = 10; //轨道间隔
    
    private int _controllerHeight = 60; //控件高度
    
    private Vector2 _scrollPosHead;
    private Vector2 _scrollPosBody;
    private Vector2 _scrollPosTimeline;
    
    private TaskClip _tempObject; //临时的一个变量 保存CtrlC的数据
  
    private int[] HighLight = { -1, -1 }; //选中的Clip

    [SerializeField] private bool _autoSave = true;

    public bool AutoSave
    {
        get => _autoSave;
        set => _autoSave = value;
    }

    private int _startFrameIndex; //从那一帧开始播放
    
    private DateTime _startTime; //开始播放的时间
    
    public int FPS
    {
        get
        {
            if (Asset != null) return Asset.fps;
            return 0;
        }
        set
        {
            if (Asset != null && Asset.fps != value)
            {
                Asset.fps = value;
                SaveTimelineAssetIfAutoSave();
            }
            return;
        }
    } //帧率

    private float _oneFrameTimer; //一帧时长的计时器

    /// <summary>
    /// 技能资源
    /// </summary>
    private SkillLineAsset _skillTimeLinAsset;
    
    public SkillLineAsset Asset
    {
        get => _skillTimeLinAsset;
        
        private set
        {
            if (value == null)
            {
                return;
            }
            
            _skillTimeLinAsset = value;

            SkillEditorBlackBoard.InitBlackKey(_skillTimeLinAsset.blackBoardVariable);
        } 
    }

    private string _assetPath = "";

    public string AssetPath
    {
        get { return _assetPath; }
        set
        {
            if (value == "") return;
            _assetPath = value;
            Asset = SkillTimelineFactory.LoadTimeLineAsset(_assetPath);
            _editorTick.PlayAsset(Asset);
        }
    }

    public void SaveTimelineAsset()
    {
        if (Asset == null)
        {
            return;
        }

        SkillTimelineFactory.Save(Asset, AssetPath);
    }

    public void SaveTimelineAssetIfAutoSave()
    {
        if (!AutoSave)
        {
            return;
        }

        SaveTimelineAsset();
    }

    //初始化
    private void OnEnable()
    {
        Selection.selectionChanged += OnSelectionChanged;

        EventCenter.AddEventListener(this, ProcessEvent);

        _editorTick = new SkillTimelineEditorTick();

        if (AssetPath != "")
        {
            Asset = SkillTimelineFactory.LoadTimeLineAsset(AssetPath);
        }

        if (Asset != null)
        {
            _editorTick.PlayAsset(Asset);
        }
    }

    //销毁
    private void OnDestroy()
    {
        EventCenter.RemoveEventListener(this, ProcessEvent);
        
        if (_boardWindow != null)
        {
            _boardWindow.Close();
            
            _boardWindow = null;
        }
    }

    //获取UI参数
    private List<TrackStyleData> GetTrackStyleFromData()
    {
        if (Asset == null)
        {
            return new List<TrackStyleData>();
        }
        
        var trackStyles = new List<TrackStyleData>();

        for (var i = 0; i < Asset.tracks.Count; i++)
        {
            var trackData = Asset.tracks[i];
            var trackStyle = new TrackStyleData();
            trackStyle.Clips = new List<ClipStyleData>();
            var trackType = trackData.GetType();

            string trackStyleName = trackType.GetCustomAttribute<TrackNameAttribute>()?.Name;

            trackStyle.Name = trackStyleName;

            var colorAttribute = trackType.GetCustomAttribute<TrackColorAttribute>();

            trackStyle.Color = colorAttribute?.Color ?? new Color(251f / 255, 0f / 255, 253f / 255);

            var classNameAttribute = trackType.GetCustomAttribute<ClipStyleAttribute>();
            if (classNameAttribute != null)
            {
                var className = classNameAttribute.ClassName;
                var type = Type.GetType(className);
                if (type != null)
                {
                    MethodInfo methodInfo = type.GetMethod("UpdateUI", BindingFlags.Static | BindingFlags.Public);
                    // 将 MethodInfo 转换为委托
                    if (methodInfo == null)
                    {
                        Debug.LogError("请将自定义样式方法定义为:" + typeof(ClipUIAction));
                        Debug.LogError("请将自定义样式方法定义为静态函数");
                    }
                    else
                    {
                        try
                        {
                            var action = (ClipUIAction)Delegate.CreateDelegate(typeof(ClipUIAction), methodInfo);
                            var overrideUI = classNameAttribute.Override;
                            trackStyle.UpdateUI = action;
                            trackStyle.OverrideUI = overrideUI;
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("请将自定义样式方法定义为:" + typeof(ClipUIAction));
                            Debug.LogError(e);
                        }
                    }
                }
            }

            for (var j = 0; j < trackData.taskClips.Count; j++)
            {
                var clip = trackData?.taskClips[j];

                if (clip == null)
                {
                    continue;
                }
                
                trackStyle.Clips.Add(new ClipStyleData()
                {
                    Name = clip.taskName,
                    StartID = clip.taskStartID,
                    EndID = clip.taskStartID + clip.taskDuration,
                });
            }

            trackStyles.Add(trackStyle);
        }

        return trackStyles;
    }

    //更新UI
    private void OnGUI()
    {
        ProcessWindowKeyboardShortcuts();

        if (_boardWindow != null)
        {
            _boardWindow.position = new Rect(this.position.x - 400, this.position.y, 400, _window?.position.height ?? 450); 
        }
        
        var styleData = GetTrackStyleFromData();

        //划分左右中
        var leftRect = new Rect(0, 0, _blackBoardWidth, position.height);

        var middleRect = new Rect(_blackBoardWidth , 0, _headWidth, position.height);

        var rightRect = new Rect(_headWidth + _blackBoardWidth , 0, position.width - _headWidth - _blackBoardWidth,
            position.height);

        //控件的位置
        var controllerRect = new Rect(middleRect.x, middleRect.y, _headWidth, _controllerHeight);

        //轨道头部位置
        var trackHeadRect = new Rect(middleRect.x, 10 + middleRect.y + _controllerHeight, middleRect.width,
            middleRect.height - _controllerHeight - 10);

        //轨道体位置
        var trackbodyRect = new Rect(rightRect.x, 10 + rightRect.y + _controllerHeight, rightRect.width,
            rightRect.height - _controllerHeight - 10);

        //时间轴位置
        var timelineRect = new Rect(rightRect.x, rightRect.y, rightRect.width, _controllerHeight);

        //黑板位置
        var blackboardRect = new Rect(leftRect.x, leftRect.y, leftRect.width, leftRect.height);

        //绘制黑板
        SkillEditorBlackBoard.DrawBlackBoardWindow(_variableWidth, _blackBoardWidth, Asset, this);

        // 事件添加轨道
        if (Asset != null && middleRect.Contains(UnityEngine.Event.current.mousePosition) &&
            UnityEngine.Event.current.type == UnityEngine.EventType.MouseDown &&
            UnityEngine.Event.current.button == 1)
        {
            GenericMenu menu = new GenericMenu();

            // 添加菜单项
            //拿到所有StandardTrack的子类型
            CreateAddTrackMenu(Asset.tracks.Count, menu);
            // 显示菜单
            menu.ShowAsContext();
        }

        //中间拖动
        if (Asset != null && rightRect.Contains(UnityEngine.Event.current.mousePosition) &&
            UnityEngine.Event.current.type == UnityEngine.EventType.MouseDrag &&
            UnityEngine.Event.current.button == 2)
        {
            _scrollPosBody.x -= UnityEngine.Event.current.delta.x;

            Repaint();
        }

        //时间轴
        var newTimeLineRect = new Rect(timelineRect.x, timelineRect.y, timelineRect.width + _scrollPosTimeline.x,
            timelineRect.height);

        _scrollPosTimeline.y = 0;

        _scrollPosTimeline = GUI.BeginScrollView(timelineRect, _scrollPosTimeline, newTimeLineRect, GUIStyle.none,
            GUIStyle.none);

        //每帧的线条
        TimeLine.UpdateGUI(this, CurrentFrameID, WidthPreFrame, newTimeLineRect);

        GUI.EndScrollView();

        //轨道体
        var newTrackBodyRect = new Rect(trackbodyRect.x, trackbodyRect.y, trackbodyRect.width + _scrollPosBody.x,
            trackbodyRect.height + _scrollPosBody.y);
        _scrollPosBody = GUI.BeginScrollView(trackbodyRect, _scrollPosBody, newTrackBodyRect
            , true, true);
        _scrollPosTimeline.x = _scrollPosBody.x;
        _scrollPosHead.y = _scrollPosBody.y;

        float height = trackbodyRect.y + 1f;
        for (var index = 0; index < styleData.Count; index++)
        {
            var trackData = styleData[index];
            // var track = new TrackStyle();
            //为轨道划分Rect
            var rectBody = new Rect(trackbodyRect.x, height, newTrackBodyRect.width, _trackHeight);
            TrackBodyStyle.UpdateUI(this, rectBody, HighLight, _widthPreFrame, trackData, index);
            height += _trackHeight + _intervalHeight;
        }

        GUI.EndScrollView();

        //时间轴的指针
        TimeLine.UpdateCurFrameUI(this, CurrentFrameID, WidthPreFrame,
            new Rect(timelineRect.x - _scrollPosBody.x, timelineRect.y, timelineRect.width + _scrollPosBody.x,
                timelineRect.height));

        EditorGUI.DrawRect(new Rect(middleRect.x, middleRect.y, middleRect.width - 10f, middleRect.height),
            new Color(56f / 255, 56f / 255, 56f / 255));

        //控件
        Controller.UpdateGUI(this, controllerRect);

        //轨道头部
        var newTrackHeadRect = new Rect(trackHeadRect.x, trackHeadRect.y, trackHeadRect.width,
            trackHeadRect.height + _scrollPosHead.y);
        _scrollPosHead.x = 0;
        _scrollPosHead = GUI.BeginScrollView(trackHeadRect, _scrollPosHead, newTrackHeadRect
            , GUIStyle.none, GUIStyle.none);

        height = trackHeadRect.y;

        for (var index = 0; index < styleData.Count; index++)
        {
            var trackData = styleData[index];

            //为轨道划分Rect
            var rectHead = new Rect(newTrackHeadRect.x, height, newTrackHeadRect.width, _trackHeight);
            TrackHeadStyle.UpdateUI(this, rectHead, trackData, index);
            height += _trackHeight + _intervalHeight;
        }

        GUI.EndScrollView();
    }

    

}
