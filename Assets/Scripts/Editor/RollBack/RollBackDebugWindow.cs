using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class RollBackDebugWindow : OdinEditorWindow
{
    [MenuItem("Tools/调试/双世界线查看器")]
    private static void OpenWindow()
    {
        var window = GetWindow<RollBackDebugWindow>();
        window.position = GUIHelper.GetEditorWindowRect().AlignCenter(800, 600);
        window.titleContent = new GUIContent("双世界线查看器");
    }
    
    [TitleGroup("控制面板")]
    [HorizontalGroup("控制面板/Buttons")]
    [Button("清除记录", ButtonSizes.Medium), GUIColor(1, 0.5f, 0.5f)]
    public void ClearEvents()
    {
        WorldLineRecorder.ClearEvents();
        UpdateEventList();
    }
    
    [HorizontalGroup("控制面板/Buttons")]
    [Button("刷新数据", ButtonSizes.Medium), GUIColor(0.5f, 1, 0.5f)]
    public void RefreshData()
    {
        UpdateEventList();
    }
    
    [HorizontalGroup("控制面板/Settings")]
    [ToggleLeft, LabelText("自动刷新")]
    public bool AutoRefresh = true;
    
    [HorizontalGroup("控制面板/Settings")]
    [ToggleLeft, LabelText("仅显示差异帧")]
    public bool ShowOnlyDifferences;
    
    [HorizontalGroup("控制面板/Settings")]
    [ToggleLeft, LabelText("高亮差异")]
    public bool HighlightDifferences = true;
    
    [HorizontalGroup("控制面板/Search")]
    [LabelText("帧号过滤"), LabelWidth(60)]
    public int FrameFilter;
    
    [HorizontalGroup("控制面板/Search")]
    [LabelText("事件过滤"), LabelWidth(60)]
    public string EventFilter;
    
    [Title("事件时间线")]
    [Searchable]
    [ListDrawerSettings(
        HideAddButton = true, 
        HideRemoveButton = true,
        DraggableItems = false,
        Expanded = true
    )]
    [ShowInInspector]
    private List<WorldLineEventData> eventList = new List<WorldLineEventData>();
    
    private void Update()
    {
        /*// 在绘制前更新数据
        if (Event.current.type == EventType.Layout)
        {
            UpdateEventList();
        }*/
        
        UpdateEventList();
        
        if (AutoRefresh)
        {
            UpdateEventList();
            Repaint();
        }
    }
    
    private void UpdateEventList()
    {
        var authEvents = WorldLineRecorder.GetEvents(WorldLineRecorder.WorldLineType.Authority);
        var localEvents = WorldLineRecorder.GetEvents(WorldLineRecorder.WorldLineType.Local);
        
        // 合并所有帧号并排序
        var allFrames = new SortedSet<int>(authEvents.Keys.Union(localEvents.Keys));
        
        eventList.Clear();
        
        foreach (int frame in allFrames)
        {
            // 应用过滤
            if (FrameFilter != 0 && frame != FrameFilter) continue;
            
            string authText = authEvents.ContainsKey(frame) 
                ? string.Join("\n", authEvents[frame])
                : "-";
            
            string localText = localEvents.ContainsKey(frame) 
                ? string.Join("\n", localEvents[frame])
                : "-";
            
            // 事件内容过滤
            if (!string.IsNullOrEmpty(EventFilter))
            {
                if (!authText.Contains(EventFilter))
                {
                    authText = "";
                }
                if (!localText.Contains(EventFilter)) localText = "";
                
                if (string.IsNullOrEmpty(authText) && string.IsNullOrEmpty(localText))
                    continue;
            }
            
            // 仅显示差异模式
            if (ShowOnlyDifferences && authText == localText)
                continue;
            
            eventList.Add(new WorldLineEventData
            {
                Frame = frame,
                AuthorityEvents = authText,
                LocalEvents = localText
            });
        }
    }

    [OnInspectorGUI]
    private void DrawSummary()
    {
        int authCount = WorldLineRecorder.GetEvents(WorldLineRecorder.WorldLineType.Authority).Count;
        int localCount = WorldLineRecorder.GetEvents(WorldLineRecorder.WorldLineType.Local).Count;
        int diffCount = eventList.Count(d => d.HasDifference);
        
        SirenixEditorGUI.BeginBox();
        GUILayout.BeginHorizontal();
        GUILayout.Label($"权威事件: {authCount}", GUILayout.Width(120));
        GUILayout.Label($"本地事件: {localCount}", GUILayout.Width(120));
        GUILayout.Label($"差异帧: {diffCount}", GUILayout.Width(120));
        GUILayout.EndHorizontal();
        SirenixEditorGUI.EndBox();
    }
}