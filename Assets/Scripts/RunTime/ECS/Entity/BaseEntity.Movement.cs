/// <summary>
/// 实体运动数据
/// </summary>
public abstract partial class BaseEntity
{
    /// <summary>
    /// 是否可以移动
    /// </summary>
    private bool _moveEnable = true;

    public bool MoveEnable => _moveEnable;
    
    /// <summary>
    /// 是否可以旋转
    /// </summary>
    private bool _rotateEnable = true;

    public bool RotateEnable => _rotateEnable;

    /// <summary>
    /// 是否能释放技能
    /// </summary>
    private bool _isCanReleaseSkill;

    public bool IsCanReleaseSkill => _isCanReleaseSkill;

    /// <summary>
    /// 设置运动状态
    /// </summary>
    /// <param name="moveEnable"></param>
    /// <param name="rotateEnable"></param>
    public void SetMovementState(bool moveEnable, bool rotateEnable)
    {
        this._moveEnable = moveEnable;
        this._rotateEnable = rotateEnable;
        
        EntityDebug($"_moveEnable = {_moveEnable}  _rotateEnable = {_rotateEnable}");
    }

    /// <summary>
    /// 设置释放技能状态
    /// </summary>
    /// <param name="isCanReleaseSkill"></param>
    public void SetReleaseSKillState(bool isCanReleaseSkill)
    {
        this._isCanReleaseSkill = isCanReleaseSkill;
    }
}
