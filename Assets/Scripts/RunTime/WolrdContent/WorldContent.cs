/// <summary>
/// 常量
/// </summary>
public static class WorldContent
{
    /// <summary>
    /// 指令执行状态
    /// </summary>
    public enum CommandExecuteState
    {
        /// <summary>
        /// 仅抬起
        /// </summary>
        OnlyUp,
        /// <summary>
        /// 仅按下
        /// </summary>
        OnlyDown,
        
        /// <summary>
        /// 蓄力
        /// </summary>
        DownUp,
        
        /// <summary>
        /// 空
        /// </summary>
        Null,
    }
    
    public class AnimationParameters
    {
        /// <summary>
        /// 位移
        /// </summary>
        public const string IsMoving = "Moving";
        
        public const string MoveSpeed = "MoveSpeed";

    }
}
