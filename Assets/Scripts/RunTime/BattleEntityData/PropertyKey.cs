using Sirenix.OdinInspector;

/// <summary>
/// 属性名
/// </summary>
public enum PropertyKey : uint
{
     [LabelText("空")]
     Null = 0,
     
     [LabelText("血量")]
     Hp = 1,

     [LabelText("能量")]
     Energy = 1,
     
     [LabelText("速度")]
     Speed  = 4,

     [LabelText("攻击力")]
     Attack  = 5,

     [LabelText("防御力")]
     Defence = 6,
    
    [LabelText("旋转速度")]
    RotateSpeed = 10,
}

/// <summary>
/// 属性的类型
/// </summary>
public enum PropertyValueType
{
     [LabelText("当前值")]
     Current,
     
     [LabelText("最大值")]
     Max,
     
     [LabelText("最小值")]
     Min,
}

/// <summary>
/// Buff执行目标
/// </summary>
[System.Flags]
public enum BuffExecuteTarget
{
     [LabelText("自己")]
     Self = 1<<1,
     
     [LabelText("指向的敌人")]
     Other = 1<<2,
     
     [LabelText("所有敌人")]
     AllEnemy = 1 <<3,
}

/// <summary>
/// 战斗时机
/// </summary>
public enum BattleExecuteTiming
{
     /// <summary>
     /// 不监听任何战斗事件，用于未绑定触发时机的 Buff。
     /// </summary>
     [LabelText("无")]
     Null = 0,
     
     /// <summary>
     /// 子弹命中时触发，保留原有枚举值 1 以兼容旧资源。
     /// </summary>
     [LabelText("子弹击中")]
     BulletHit = 1,

     /// <summary>
     /// 每秒触发一次的 Buff 时机，仅声明配置值，周期触发逻辑由后续任务实现。
     /// </summary>
     [LabelText("每秒")]
     PerSecond = 2,
}

/// <summary>
/// Buff 触发事件中持有者需要扮演的角色，旧资源默认值为 Any。
/// </summary>
public enum BuffTriggerRole
{
     /// <summary>
     /// 不限制持有者在事件中的角色，兼容旧配置的默认语义。
     /// </summary>
     [LabelText("任意")]
     Any,

     /// <summary>
     /// 仅当 Buff 持有者是攻击者时触发。
     /// </summary>
     [LabelText("持有者为攻击者")]
     OwnerAsAttacker,

     /// <summary>
     /// 仅当 Buff 持有者是受击者时触发。
     /// </summary>
     [LabelText("持有者为受击者")]
     OwnerAsDefender,
}

/// <summary>
/// Buff 效果执行模式，旧资源默认值为 Instant。
/// </summary>
public enum BuffEffectMode
{
     /// <summary>
     /// 立即执行属性效果，保持现有 Buff 行为。
     /// </summary>
     [LabelText("立即")]
     Instant,

     /// <summary>
     /// 临时属性修正模式，仅提供配置表达能力，具体应用和回滚由后续任务实现。
     /// </summary>
     [LabelText("临时属性修正")]
     TemporaryPropertyModifier,
}

/// <summary>
/// Timing 的 数量
/// </summary>
public enum TimingValue
{
     [LabelText("本次Timing的次数")]
     ThisTimingValue,
     
     [LabelText("本次战斗的Timing的次数")]
     RoundTimingValue,
     
     [LabelText("历史Timing的次数")]
     BattleTimingValue,
}
