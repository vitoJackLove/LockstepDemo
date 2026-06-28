using Ase.Serializing;

/// <summary>
/// 普通攻击管理
/// </summary>
public partial class SkillComponent
{
    /// <summary>
    /// 普攻连击记录
    /// </summary>
    private AttackIndexCacheData _attackIndexCacheData;
    
    /// <summary>
    /// 普攻序列
    /// </summary>
    private int _attackIndex;

    /// <summary>
    /// 普通攻击数量
    /// </summary>
    private int _attackNumber;

    private void InitAttackData(HeroSkillConfig config)
    {
        _attackIndexCacheData = AttackIndexCacheData.Create(config.attackIndexCacheTick, this);
    }
    
    public void ExecuteAttack()
    {
        if (_attackIndex == _attackNumber)
        {
            _attackIndex = 0;

            return;
        }

        _attackIndex++;
    }

    public void RefreshAttack()
    {
        _attackIndex = 0;
    }
    
    /// <summary>
    /// 刷新普通攻击数据
    /// </summary>
    private void UpdateAttackData()
    {
        _attackIndexCacheData.FixUpdate();
    }

    /// <summary>
    /// 普通攻击计时
    /// </summary>
    /// <param name="isStart"></param>
    public void AttackTickCounting(bool isStart)
    {
        _attackIndexCacheData.TickCounting(isStart);
    }
}

/// <summary>
/// 
/// </summary>
public class AttackIndexCacheData : IPool 
{
    /// <summary>
    /// 缓存的Tick
    /// </summary>
    private int _cacheTick;

    /// <summary>
    /// 执行
    /// </summary>
    private int _tempTick;

    private SkillComponent _skillComponent;

    /// <summary>
    /// 开始计时
    /// </summary>
    private bool _startTimeCount;

    public static AttackIndexCacheData Create(int tick,SkillComponent skillComponent)
    {
        AttackIndexCacheData data = FPoolHelper.Get<AttackIndexCacheData>();

        data._cacheTick = tick;
        data._tempTick = data._cacheTick;
        data._skillComponent = skillComponent;
        
        return data;
    }

    public void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
    {
        hardWriter.WriteInt32Data($"普通缓存的Tick", _tempTick);
        hardWriter.WriterBoolData($"是否开始计时", _startTimeCount);
    }

    public void RollBackTo(PooledReader authoritySnapShot)
    {
        int authorityTempTick = authoritySnapShot.ReadInt32();
        _tempTick = authorityTempTick;
        
        bool authorityStartTimeCount = authoritySnapShot.ReadBoolean();
        _startTimeCount = authorityStartTimeCount;
    }
    
    /// <summary>
    /// 计时
    /// </summary>
    /// <param name="isStart"></param>
    public void TickCounting(bool isStart)
    {
        _startTimeCount = isStart;

        if (!_startTimeCount)
        {
            _tempTick = _cacheTick;
        }
    }
    
    public void FixUpdate()
    {
        if (_startTimeCount)
        {
            _tempTick--;
            
            if (_tempTick == 0)
            {
                _skillComponent.RefreshAttack();
                _tempTick = _cacheTick;
                _startTimeCount = false;
            }
        }
    }
    
    public void Clear()
    {
        _skillComponent = null;
        _tempTick = 0;
        _cacheTick = 0;
    }
}