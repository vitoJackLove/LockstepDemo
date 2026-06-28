/// <summary>
/// 技能数据
/// </summary>
public partial class BaseSkillData : IPool
{
    protected BaseSkillData() { }

    /// <summary>
    /// 指令类型
    /// </summary>
    protected CommandType CommandType;

    public CommandType Command => CommandType;
    
    /// <summary>
    /// 按下时的技能ID
    /// </summary>
    protected int Id;
    
    /// <summary>
    /// 技能Id
    /// </summary>
    public int SkillId => Id;

    /// <summary>
    /// 执行实体
    /// </summary>
    protected BaseEntity BaseEntity;

    /// <summary>
    /// 技能执行
    /// </summary>
    protected BaseSkillExecute BaseSkillExecute;

    /// <summary>
    /// 技能的执行状态
    /// </summary>
    public SkillExecuteState ExecuteState => BaseSkillExecute.ExecuteState;

    /// <summary>
    /// 是否时蓄力技能
    /// </summary>
    private bool _isChargingSkill;

    public bool IsChargingSkill => _isChargingSkill;

    /// <summary>
    /// 创建技能执行数据
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    protected BaseSkillExecute CreateSkillExecute(HeroSkillConfig config)
    {
        if (config.commandState == WorldContent.CommandExecuteState.DownUp)
        {
            _isChargingSkill = true;
            return ChargingExecuteSkill.Create(this, config, BaseEntity);
        }
        
        if(config.commandState == WorldContent.CommandExecuteState.OnlyDown)
        {
            return DownExecuteSkill.Create(this, config, BaseEntity);
        }

        if (config.commandState == WorldContent.CommandExecuteState.OnlyUp)
        {
            return UpExecuteSkill.Create(this, config, BaseEntity);
        }

        return null;
    }
    
    /// <summary>
    /// 执行技能
    /// </summary>
    /// <param name="commandState"></param>
    public void ExecuteCommand(WorldContent.CommandExecuteState commandState)
    {
        if (!BaseSkillExecute.IsCanExecuteCommand(commandState))
        {
            return;
        }
        
        //蓄力技能的抬起执行
        if (BaseEntity.GetComponent<SkillComponent>().CurrentSkillId == SkillId 
            && IsChargingSkill && commandState == WorldContent.CommandExecuteState.OnlyUp)
        {
            StartExecuteSkill(commandState);

        }
        else
        {
            BaseEntity.GetComponent<SkillComponent>().BreakCurrentExecuteSkill(this, false);
            
            StartExecuteSkill(commandState);
        }
    }

    /// <summary>
    /// 开始
    /// </summary>
    protected virtual void StartExecuteSkill(WorldContent.CommandExecuteState commandState)
    {
        BaseSkillExecute.ExecuteSkill(commandState);
    }
    
    public PlayableStateEnum TickSkill()
    {
       return BaseSkillExecute.TickSkill();
    }

    /// <summary>
    /// 技能被打断 （如果是回滚打断技能 ... 逻辑部分的功能不需要通过打断技能执行 比如 ： 玩家的状态） 因为回滚会把实体最真实的数据覆盖，
    /// 只需要执行表现部分 比如： 特效的回收以及动画逻辑
    /// </summary>
    /// <param name="isRollBackBreakSkill">是否是回滚打断</param>
    public virtual void BreakSkill(bool isRollBackBreakSkill)
    {
        BaseSkillExecute.BreakSkill(isRollBackBreakSkill);
    }
    
    public virtual void OnSkillEnd() { }
    
    public void Clear()
    {
        BaseSkillExecute.Clear();
        BaseSkillExecute = null;
        BaseEntity = null;
        Id = 0;
    }
}
