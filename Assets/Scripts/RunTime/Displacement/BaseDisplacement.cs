using Ase.Serializing;
using Unity.Mathematics.FixedPoint;

/// <summary>
/// 位移基类
/// </summary>
public class BaseDisplacement : IPool
{
    /// <summary>
    /// 执行的实体
    /// </summary>
    protected BaseEntity BaseEntity;
    
    /// <summary>
    /// 位移前的延迟
    /// </summary>
    protected int MoveDelay;
    
    /// <summary>
    /// 移动的时间
    /// </summary>
    protected int MoveTime;

    /// <summary>
    /// 位移类型
    /// </summary>
    protected DisplacementEnum DisplacementEnum;

    /// <summary>
    /// 运行状态
    /// </summary>
    private DisplacementState _runTimeState;
    
    /// <summary>
    /// 创建位移
    /// </summary>
    /// <param name="baseEntity"></param>
    /// <param name="displacementEnum"></param>
    /// <returns></returns>
    public static T Create<T>(BaseEntity baseEntity, DisplacementEnum displacementEnum)where T : BaseDisplacement, new()
    {
        var baseDisplacement = FPoolHelper.Get<T>();
        baseDisplacement.BaseEntity = baseEntity;
        baseDisplacement.DisplacementEnum = displacementEnum;
        baseDisplacement._runTimeState = DisplacementState.Exit;
        return baseDisplacement;

    }

    /// <summary>
    /// 启动
    /// </summary>
    public void StartUp()
    {
        _runTimeState = DisplacementState.Prepare;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="deltaTime"></param>
    public void FixUpdate(fp deltaTime)
    {
        if (_runTimeState == DisplacementState.Exit)
        {
            return;
        }
        
        _runTimeState = DisplacementState.Running;
        
        //延迟
        MoveDelay--;

        if (MoveDelay <= 0)
        {
            MoveTime--;

            if (MoveTime > 0)
            {
                ComputePosition(deltaTime);
            }
            else
            {
                _runTimeState = DisplacementState.Exit;
            }
        }
    }

    /// <summary>
    /// 计算位置
    /// </summary>
    protected virtual void ComputePosition(fp deltaTime) { }

    /// <summary>
    /// 运行状态
    /// </summary>
    public DisplacementState RunTimeState => _runTimeState;
    
    public void Clear()
    {
        BaseEntity = null;
        _runTimeState = DisplacementState.Exit;
    }

    public virtual void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
    {
        hardWriter.WriteInt32Data($"MoveDelay", MoveDelay);
        hardWriter.WriteInt32Data($"MoveTime", MoveTime);
        hardWriter.WriteInt32Data($"_runTimeState", (int)_runTimeState);
    }

    public virtual void HardRollBackTo(PooledReader authoritySnapShot)
    {
        MoveDelay = authoritySnapShot.ReadInt32();
        MoveTime = authoritySnapShot.ReadInt32();
        _runTimeState = (DisplacementState)authoritySnapShot.ReadInt32();
    }
}

/// <summary>
/// 位移状态
/// </summary>
public enum DisplacementState
{
    /// <summary>
    /// 准备
    /// </summary>
    Prepare,
    
    /// <summary>
    /// 运行
    /// </summary>
    Running,
    
    /// <summary>
    /// 结束
    /// </summary>
    Exit,
}
