using Ase.Serializing;

/// <summary>
/// 技能的执行状态
/// </summary>
public enum SkillExecuteState
{
    /// <summary>
    /// 未执行
    /// </summary>
    Null,
    
    /// <summary>
    /// 执行按下中
    /// </summary>
    ExecuteDown,
    
    /// <summary>
    /// 按下执行完毕
    /// </summary>
    DownExecuteEnd,
    
    /// <summary>
    /// 执行抬起中
    /// </summary>
    ExecuteUp,
}

/// <summary>
/// 蓄力技能
/// </summary>
public class ChargingExecuteSkill : BaseSkillExecute
{
    /// <summary>
    /// 最大按下帧
    /// </summary>
    private int _maxDownTick;

    /// <summary>
    /// 计时按下帧
    /// </summary>
    private int _downTick;

    /// <summary>
    /// 技能抬起资产
    /// </summary>
    private SkillTimelineLauncher _skillUpTimeLineLauncher;
    
    /// <summary>
    /// 技能按下资产
    /// </summary>
    private SkillTimelineLauncher _skillDownTimeLineLauncher;

    /// <summary>
    /// 等待执行抬起技能
    /// </summary>
    private bool _waitExecuteUp;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="skillData">技能数据</param>
    /// <param name="config"></param>
    /// <param name="entity"></param>
    /// <returns></returns>
    public static ChargingExecuteSkill Create(BaseSkillData skillData,HeroSkillConfig config,BaseEntity entity)
    {
        ChargingExecuteSkill daSkill = FPoolHelper.Get<ChargingExecuteSkill>();
        daSkill.InitSkillTimeLine(config, entity);
        daSkill._maxDownTick = config.maxDownTick;
        daSkill._downTick = config.maxDownTick;
        daSkill.SkillExecuteState = SkillExecuteState.Null;
        return daSkill;
    }

    private async void InitSkillTimeLine(HeroSkillConfig config,BaseEntity entity)
    {
        _skillDownTimeLineLauncher = await entity.GetSystem<SkillTimeLineSystem>().InitSkillTimeLine(config.skillDownAssetsPath, entity);
        _skillUpTimeLineLauncher = await entity.GetSystem<SkillTimeLineSystem>().InitSkillTimeLine(config.skillUpAssetsPath, entity);
    }
    
    public override bool IsCanExecuteCommand(WorldContent.CommandExecuteState commandState)
    {
        return true;
    }

    public override void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
    {
        hardWriter.WriteInt32Data($"当前执行按下后的Tick", _downTick);
        hardWriter.WriteInt32Data($"技能的执行状态", (int)SkillExecuteState);
        hardWriter.WriterBoolData($"等待执行抬起技能", _waitExecuteUp);

        _skillUpTimeLineLauncher.TakeSnapShot(hardWriter, softWriter);
        _skillDownTimeLineLauncher.TakeSnapShot(hardWriter, softWriter);
    }

    public override void RollBackTo(PooledReader authoritySnapShot)
    {
        int authorityDownTick = authoritySnapShot.ReadInt32();
        SkillExecuteState authoritySkillState = (SkillExecuteState)authoritySnapShot.ReadInt32();
        bool authorityWaitExecuteUp = authoritySnapShot.ReadBoolean();

        _downTick = authorityDownTick;
        SkillExecuteState = authoritySkillState;
        _waitExecuteUp = authorityWaitExecuteUp;

        //如果蓄力技能从抬起回滚到按下状态 ... 应该先回滚抬起，然后再回滚按下
        _skillUpTimeLineLauncher.RollBackTo(authoritySnapShot);
        _skillDownTimeLineLauncher.RollBackTo(authoritySnapShot);
    }

    public override void ExecuteSkill(WorldContent.CommandExecuteState commandState)
    {
        //按下
        if (commandState == WorldContent.CommandExecuteState.OnlyDown)
        {
            _skillDownTimeLineLauncher.RefreshInitState();
            SkillExecuteState = SkillExecuteState.ExecuteDown;
            _waitExecuteUp = false;
            StartCounting();
        }

        //抬起
        if (commandState == WorldContent.CommandExecuteState.OnlyUp)
        {
            //已经执行抬起了 
            if (SkillExecuteState == SkillExecuteState.ExecuteUp)
            {
                return;
            }

            //按下执行完毕 直接开始执行
            if (SkillExecuteState == SkillExecuteState.DownExecuteEnd)
            {
                _skillUpTimeLineLauncher.RefreshInitState();
                
                SkillExecuteState = SkillExecuteState.ExecuteUp;
            }
            
            //抬起的时候按下还没有执行完毕
            if (SkillExecuteState == SkillExecuteState.ExecuteDown)
            {
                _skillDownTimeLineLauncher.ForceExecuteStop(false);
                
                _skillUpTimeLineLauncher.RefreshInitState();
                
                _waitExecuteUp = true;
                
                SkillExecuteState = SkillExecuteState.ExecuteUp;
                
            }
        }
    }

    public override void BreakSkill(bool isRollBackBreakSkill)
    {
        _skillDownTimeLineLauncher.ForceExecuteStop(isRollBackBreakSkill);
        _skillUpTimeLineLauncher.ForceExecuteStop(isRollBackBreakSkill);
        _waitExecuteUp = false;
        _downTick = _maxDownTick;
        SkillExecuteState = SkillExecuteState.Null;
    }

    public override PlayableStateEnum TickSkill()
    {
        PlayableStateEnum state = PlayableStateEnum.Exit;
        
        if (SkillExecuteState == SkillExecuteState.Null)
        {
            state = PlayableStateEnum.Exit;
        }

        if (SkillExecuteState == SkillExecuteState.ExecuteDown)
        {
            var downState = _skillDownTimeLineLauncher.Tick(fpmath1.LogicDeltaTime);

            if (downState == PlayableStateEnum.Exit)
            {
                SkillExecuteState = SkillExecuteState.DownExecuteEnd;

                if (_waitExecuteUp)
                {
                    SkillExecuteState = SkillExecuteState.ExecuteUp;
                }
            }
            
            state = PlayableStateEnum.Running;
        }

        if (SkillExecuteState == SkillExecuteState.DownExecuteEnd)
        {
            state = PlayableStateEnum.Running;
        }
        
        if (SkillExecuteState == SkillExecuteState.ExecuteUp)
        {
            state = _skillUpTimeLineLauncher.Tick(fpmath1.LogicDeltaTime);
        }

        UpdateCount();
        
        return state;
    }

    private void UpdateCount()
    {
        if (_waitExecuteUp)
        {
            return;
        }
        
        if (SkillExecuteState == SkillExecuteState.ExecuteDown || 
            SkillExecuteState == SkillExecuteState.DownExecuteEnd)
        {
            _downTick--;

            if (_downTick == 0)
            {
                _skillUpTimeLineLauncher.RefreshInitState();
                SkillExecuteState = SkillExecuteState.ExecuteUp;
            }
        }
    }

    /// <summary>
    /// 开始按下倒计时
    /// </summary>
    private void StartCounting()
    {
        _downTick = _maxDownTick;
    }

    public override void Clear()
    {
    }
}