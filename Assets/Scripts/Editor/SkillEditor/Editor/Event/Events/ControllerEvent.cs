public class ControllerEvent : BaseEvent
{
    public ControllerEvent()
    {
        this.EventType = UIEventType.Controller;
    }

    public ControllerType ControllerType;
}