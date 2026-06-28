using System;
using System.Collections.Generic;

public static class EventCenter
{
    private static Dictionary<SkillTimelineEditorWindow, Action<BaseEvent>> events;

    static EventCenter()
    {
        events = new Dictionary<SkillTimelineEditorWindow, Action<BaseEvent>>();
    }

    public static void AddEventListener(SkillTimelineEditorWindow window, Action<BaseEvent> callback)
    {
        if (events.ContainsKey(window)) events[window] = callback;
        else events.Add(window, callback);
    }

    public static void RemoveEventListener(SkillTimelineEditorWindow window, Action<BaseEvent> callback)
    {
        if (events.ContainsKey(window)) events.Remove(window);
    }

    public static void TrigerEvent(SkillTimelineEditorWindow window, BaseEvent baseEvent)
    {
        events[window]?.Invoke(baseEvent);
    }
}