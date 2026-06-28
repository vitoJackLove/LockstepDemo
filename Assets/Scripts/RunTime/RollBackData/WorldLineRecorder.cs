using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public static class WorldLineRecorder
{
    public enum WorldLineType
    {
        Authority,
        Local
    }

    private static readonly SortedDictionary<int, List<string>> authorityEvents =
        new SortedDictionary<int, List<string>>();

    private static readonly SortedDictionary<int, List<string>> localEvents = new SortedDictionary<int, List<string>>();

    public static void RecordEvent(WorldLineType worldLine, int frame, string eventDescription)
    {
        var target = worldLine == WorldLineType.Authority ? authorityEvents : localEvents;

        if (!target.ContainsKey(frame))
        {
            target[frame] = new List<string>();
        }

        target[frame].Add(eventDescription);
    }

    public static SortedDictionary<int, List<string>> GetEvents(WorldLineType worldLine)
    {
        return worldLine == WorldLineType.Authority ? authorityEvents : localEvents;
    }

    public static void ClearEvents()
    {
        authorityEvents.Clear();
        localEvents.Clear();
    }
}

[Serializable]
public class WorldLineEventData
{
    [HorizontalGroup("Frame", Width = 100)] [LabelText("帧号")]
    public int Frame;

    [HorizontalGroup("Authority")] [LabelText("权威世界线")] [MultiLineProperty(3)]
    public string AuthorityEvents;

    [HorizontalGroup("Local")] [LabelText("本地世界线")] [MultiLineProperty(3)]
    public string LocalEvents;

    [HorizontalGroup("Diff", Width = 100)]
    [LabelText("差异")]
    [ShowIf("@!string.IsNullOrEmpty(AuthorityEvents) && !string.IsNullOrEmpty(LocalEvents)")]
    public bool HasDifference =>
        !string.IsNullOrEmpty(AuthorityEvents) &&
        !string.IsNullOrEmpty(LocalEvents) &&
        AuthorityEvents != LocalEvents;
}