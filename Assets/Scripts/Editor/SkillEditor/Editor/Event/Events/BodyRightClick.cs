public class BodyRightClick : BaseEvent
{
    public int TrackIndex;
    public int MouseFrameID;
    public BodyRightClick()
    {
        this.EventType = UIEventType.BodyRightClick;
    }
}