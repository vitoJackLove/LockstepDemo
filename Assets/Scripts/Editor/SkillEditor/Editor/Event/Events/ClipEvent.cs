public class ClipEvent : BaseEvent
{
    public int TrackIndex;
    public int ClipIndex;
}

public class ClipMoveEvent : ClipEvent
{
    public ClipMoveEvent()
    {
        this.EventType = UIEventType.ClipMove;
    }

    public float OffsetMouseX; //鼠标相对Start的偏移 单位是Rect的x
}
    
public class ClipMoveEndEvent : ClipEvent
{
    public ClipMoveEndEvent()
    {
        this.EventType = UIEventType.ClipMoveEnd;
    }
}

public class ClipClickEvent : ClipEvent
{
    public ClipClickEvent()
    {
        this.EventType = UIEventType.ClipClick;
    }

    public float OffsetMouseX; //鼠标相对Start的偏移 单位是Rect的x
}

public class ClipMiddleClickEvent : ClipEvent
{
    public ClipMiddleClickEvent()
    {
        this.EventType = UIEventType.ClipMiddleClick;
    }

    public float OffsetMouseX; //鼠标相对Start的偏移 单位是Rect的x
}

public class ClipResizeEvent : ClipEvent
{
    public ClipResizeEvent()
    {
        this.EventType = UIEventType.ClipResize;
    }

    public float OffsetMouseX; //鼠标相对End的偏移 单位是Rect的x
}

public class ClipResizeEndEvent : ClipEvent
{
    public ClipResizeEndEvent()
    {
        this.EventType = UIEventType.ClipResizeEnd;
    }
}
    
public class ClipRightClickEvent : ClipEvent
{
    public ClipRightClickEvent()
    {
        this.EventType = UIEventType.ClipRightClick;
    }
}

public class KeyboradEvent : ClipEvent
{
    public KeyboradEvent()
    {
        this.EventType = UIEventType.ClipKeyborad;
    }

    public Shortcut Shortcut;
}