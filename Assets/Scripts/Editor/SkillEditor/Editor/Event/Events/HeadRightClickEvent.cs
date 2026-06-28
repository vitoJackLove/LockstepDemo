public class HeadRightClickEvent : BaseEvent
{
    public int TrackIndex;
        
    public HeadRightClickEvent()
    {
        EventType = UIEventType.HeadRightClick;
    }
}