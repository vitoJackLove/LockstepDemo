/// <summary>
/// 技能执行
/// </summary>
public abstract partial class BaseSkillExecute : IPool
{
    /// <summary>
    /// 技能的执行状态
    /// </summary>
    protected SkillExecuteState SkillExecuteState { get; set; }

    public SkillExecuteState ExecuteState => SkillExecuteState;

    /// <summary>
    /// 执行指令
    /// </summary>
    /// <param name="commandState"></param>
    public abstract void ExecuteSkill(WorldContent.CommandExecuteState commandState);

    /// <summary>
    /// 是否能执行指令
    /// </summary>
    /// <returns></returns>
    public abstract bool IsCanExecuteCommand(WorldContent.CommandExecuteState commandState);
    
    /// <summary>
    /// 技能被打断
    /// </summary>
    public abstract void BreakSkill(bool isRollBackBreakSkill);

    /// <summary>
    /// UpdateSkill
    /// </summary>
    public abstract PlayableStateEnum TickSkill();
    
    public abstract void Clear();
}
