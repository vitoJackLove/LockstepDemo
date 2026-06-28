//事件类型
public enum UIEventType
{
    ClipMove,
    ClipMoveEnd,
    ClipClick,
    ClipMiddleClick,
    ClipResize,
    ClipResizeEnd,
    ClipRightClick,
    ClipKeyborad,
    Controller,
    TimelineScale,
    TimelineDrag,
    HeadRightClick,
    BodyRightClick,
    TimelineDragEnd,
    LeftMouseUp
}

//快捷键
public enum Shortcut
{
    CtrlC,
    CtrlV,
    CtrlX,
    CtrlS,
    Delete
}

//控件
public enum ControllerType
{
    Play,
    ToPre,
    ToNext,
    ToMostBegin,
    ToMostEnd,
}