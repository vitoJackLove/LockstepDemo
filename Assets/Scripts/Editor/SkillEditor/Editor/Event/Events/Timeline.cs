public class TimelineScaleEvent : BaseEvent
{
    public TimelineScaleEvent()
    {
        this.EventType = UIEventType.TimelineScale;
    }
}
    
public class TimelineDragEndEvent : BaseEvent
{
    public TimelineDragEndEvent()
    {
        this.EventType = UIEventType.TimelineDragEnd;
    }
}

public class TimelineDragEvent : BaseEvent
{
    public TimelineDragEvent()
    {
        this.EventType = UIEventType.TimelineDrag;
    }
}