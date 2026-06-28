using UnityEngine;

[ClipName("打印当前世界帧")]
public class TakeSnapShotClip : TaskClip
{
    public override void OnRunTimeEnter(BaseEntity context, int fps)
    {
        base.OnRunTimeEnter(context, fps);
        
        Debug.Log($"当前的世界帧 ： {context.BaseWorld.LocalTick}");
    }
}
