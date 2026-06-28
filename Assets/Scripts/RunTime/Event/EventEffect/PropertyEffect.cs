using System.Collections.Generic;
using Unity.Mathematics.FixedPoint;

/// <summary>
/// 属性效果，负责立即属性变化和临时属性修正的应用与回滚。
/// </summary>
public class PropertyEffect : EventEffect
{
    /// <summary>
    /// 百分比倍率的分母，rate=1000 表示 100%。
    /// </summary>
    private const int RateDenominator = 1000;

    /// <summary>
    /// 需要修改的目标属性键。
    /// </summary>
    private PropertyKey _propertyKey;

    /// <summary>
    /// 是否直接使用固定数值作为修改量。
    /// </summary>
    private bool _isCommonValue;

    /// <summary>
    /// 固定属性修改量，在 isCommonValue 为 true 时生效。
    /// </summary>
    private int _normalValue;

    /// <summary>
    /// 参与计算修改量的来源属性键。
    /// </summary>
    private PropertyKey _influenceProperty;

    /// <summary>
    /// 来源属性换算为修改量的千分比倍率。
    /// </summary>
    private int _rate;

    /// <summary>
    /// 已应用的临时属性修正记录，用于 Buff 生命周期结束时精确回滚。
    /// </summary>
    private readonly List<TemporaryPropertyModifierRecord> _temporaryModifierRecords =
        new List<TemporaryPropertyModifierRecord>();

    /// <summary>
    /// 创建只使用固定数值的属性效果。
    /// </summary>
    /// <param name="propertyKey">需要修改的目标属性键。</param>
    /// <param name="isCommonValue">是否直接使用 normalValue 作为修改量。</param>
    /// <param name="normalValue">固定属性修改量。</param>
    /// <returns>从对象池取得的属性效果实例。</returns>
    public static PropertyEffect Create(PropertyKey propertyKey, bool isCommonValue, int normalValue)
    {
        return Create(null, propertyKey, isCommonValue, normalValue, PropertyKey.Null, 0);
    }

    /// <summary>
    /// 创建属性效果并记录执行者与数值来源配置。
    /// </summary>
    /// <param name="performer">默认执行者，用于无显式来源时读取影响属性。</param>
    /// <param name="propertyKey">需要修改的目标属性键。</param>
    /// <param name="isCommonValue">是否直接使用 normalValue 作为修改量。</param>
    /// <param name="normalValue">固定属性修改量。</param>
    /// <param name="influenceProperty">参与计算修改量的来源属性键。</param>
    /// <param name="rate">来源属性换算为修改量的千分比倍率。</param>
    /// <returns>从对象池取得的属性效果实例。</returns>
    public static PropertyEffect Create(BaseEntity performer, PropertyKey propertyKey, bool isCommonValue,
        int normalValue, PropertyKey influenceProperty, int rate)
    {
        PropertyEffect effect = FPoolHelper.Get<PropertyEffect>();

        effect.Performer = performer;
        effect._propertyKey = propertyKey;
        effect._isCommonValue = isCommonValue;
        effect._normalValue = normalValue;
        effect._influenceProperty = influenceProperty;
        effect._rate = rate;
        effect._temporaryModifierRecords.Clear();

        return effect;
    }

    /// <summary>
    /// 使用执行者作为目标和来源执行一次立即属性效果。
    /// </summary>
    public override void ExecuteEffect()
    {
        ExecuteEffect(Performer, Performer);
    }

    /// <summary>
    /// 对指定目标执行一次立即属性效果。
    /// </summary>
    /// <param name="target">需要被修改属性的目标实体。</param>
    /// <returns>属性成功变化时返回 true，否则返回 false。</returns>
    public bool ExecuteEffect(BaseEntity target)
    {
        return ExecuteEffect(target, Performer);
    }

    /// <summary>
    /// 对指定目标执行一次立即属性效果。
    /// </summary>
    /// <param name="target">需要被修改属性的目标实体。</param>
    /// <param name="source">用于读取影响属性的来源实体。</param>
    /// <returns>属性成功变化时返回 true，否则返回 false。</returns>
    public bool ExecuteEffect(BaseEntity target, BaseEntity source)
    {
        if (!TryCalculateValue(target, source, out fp value))
        {
            return false;
        }

        return TryChangeTargetProperty(target, value);
    }

    /// <summary>
    /// 应用一次临时属性修正，同时修改目标属性的 Current 和 Max，并记录实际应用量。
    /// </summary>
    /// <param name="target">需要被临时修正的目标实体。</param>
    /// <param name="source">用于读取影响属性的来源实体。</param>
    /// <returns>本次临时修正至少对一个属性值生效时返回 true，否则返回 false。</returns>
    public bool ApplyTemporaryModifier(BaseEntity target, BaseEntity source)
    {
        if (HasTemporaryModifier(target))
        {
            return true;
        }

        if (!TryCalculateValue(target, source, out fp value) || value == 0)
        {
            return false;
        }

        TemporaryPropertyModifierRecord record = new TemporaryPropertyModifierRecord(target);

        if (value > 0)
        {
            ApplyTemporaryPropertyValue(target, PropertyValueType.Max, value, record);
            ApplyTemporaryPropertyValue(target, PropertyValueType.Current, value, record);
        }
        else
        {
            ApplyTemporaryPropertyValue(target, PropertyValueType.Current, value, record);
            ApplyTemporaryPropertyValue(target, PropertyValueType.Max, value, record);
        }

        if (!record.HasAppliedValue)
        {
            return false;
        }

        _temporaryModifierRecords.Add(record);

        return true;
    }

    /// <summary>
    /// 回滚当前属性效果已经应用过的所有临时属性修正。
    /// </summary>
    public void RollbackTemporaryModifiers()
    {
        for (int i = _temporaryModifierRecords.Count - 1; i >= 0; i--)
        {
            RollbackTemporaryModifier(_temporaryModifierRecords[i]);
        }

        _temporaryModifierRecords.Clear();
    }

    /// <summary>
    /// 清理属性效果运行时状态，释放前会先回滚尚未清理的临时属性修正。
    /// </summary>
    public override void Clear()
    {
        RollbackTemporaryModifiers();

        Performer = null;
        _propertyKey = PropertyKey.Null;
        _isCommonValue = false;
        _normalValue = 0;
        _influenceProperty = PropertyKey.Null;
        _rate = 0;
    }

    /// <summary>
    /// 计算本次属性效果的最终修改量。
    /// </summary>
    /// <param name="target">需要被修改属性的目标实体。</param>
    /// <param name="source">用于读取影响属性的来源实体。</param>
    /// <param name="value">计算出的最终修改量。</param>
    /// <returns>成功计算出有效修改量时返回 true，否则返回 false。</returns>
    private bool TryCalculateValue(BaseEntity target, BaseEntity source, out fp value)
    {
        if (_isCommonValue)
        {
            value = _normalValue;
            return true;
        }

        if (_influenceProperty == PropertyKey.Null || _rate == 0)
        {
            value = 0;
            return false;
        }

        if (!TryGetInfluenceValue(target, source, out fp influenceValue))
        {
            value = 0;
            return false;
        }

        value = influenceValue * _rate / RateDenominator;

        return true;
    }

    /// <summary>
    /// 修改目标属性的 Current 值，用于兼容 Instant 模式的旧行为。
    /// </summary>
    /// <param name="target">需要被修改属性的目标实体。</param>
    /// <param name="value">准备追加到 Current 的变化量。</param>
    /// <returns>属性成功变化时返回 true，否则返回 false。</returns>
    private bool TryChangeTargetProperty(BaseEntity target, fp value)
    {
        if (target == null || _propertyKey == PropertyKey.Null ||
            !target.TryGetPropertyValue(_propertyKey, PropertyValueType.Current, out _))
        {
            return false;
        }

        target.ChangeProperty(_propertyKey, value);

        return true;
    }

    /// <summary>
    /// 对目标属性的指定值类型应用临时修正，并把实际生效量写入记录。
    /// </summary>
    /// <param name="target">需要被临时修正的目标实体。</param>
    /// <param name="valueType">需要修改的属性值类型。</param>
    /// <param name="value">准备追加的变化量。</param>
    /// <param name="record">用于回滚的临时修正记录。</param>
    private void ApplyTemporaryPropertyValue(BaseEntity target, PropertyValueType valueType, fp value,
        TemporaryPropertyModifierRecord record)
    {
        if (target.TryChangePropertyValue(_propertyKey, valueType, value, out fp appliedValue) &&
            appliedValue != 0)
        {
            record.AddAppliedValue(valueType, appliedValue);
        }
    }

    /// <summary>
    /// 回滚单个目标上记录过的临时属性修正。
    /// </summary>
    /// <param name="record">需要被回滚的临时修正记录。</param>
    private void RollbackTemporaryModifier(TemporaryPropertyModifierRecord record)
    {
        if (record.Target == null)
        {
            return;
        }

        if (record.CurrentAppliedValue < 0)
        {
            RollbackTemporaryPropertyValue(record.Target, PropertyValueType.Max, record.MaxAppliedValue);
            RollbackTemporaryPropertyValue(record.Target, PropertyValueType.Current, record.CurrentAppliedValue);
        }
        else
        {
            RollbackTemporaryPropertyValue(record.Target, PropertyValueType.Current, record.CurrentAppliedValue);
            RollbackTemporaryPropertyValue(record.Target, PropertyValueType.Max, record.MaxAppliedValue);
        }
    }

    /// <summary>
    /// 按记录量对指定属性值类型做反向修改。
    /// </summary>
    /// <param name="target">需要被回滚属性的目标实体。</param>
    /// <param name="valueType">需要回滚的属性值类型。</param>
    /// <param name="appliedValue">原先实际应用的变化量。</param>
    private void RollbackTemporaryPropertyValue(BaseEntity target, PropertyValueType valueType, fp appliedValue)
    {
        if (appliedValue == 0)
        {
            return;
        }

        target.TryChangePropertyValue(_propertyKey, valueType, -appliedValue, out _);
    }

    /// <summary>
    /// 判断指定目标是否已经应用过本属性效果的临时修正，避免重复应用和重复回滚。
    /// </summary>
    /// <param name="target">需要检查的目标实体。</param>
    /// <returns>目标已经存在临时修正记录时返回 true，否则返回 false。</returns>
    private bool HasTemporaryModifier(BaseEntity target)
    {
        if (target == null)
        {
            return true;
        }

        for (int i = 0; i < _temporaryModifierRecords.Count; i++)
        {
            if (_temporaryModifierRecords[i].Target == target)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 读取来源属性值，优先使用来源实体，其次使用目标实体。
    /// </summary>
    /// <param name="target">属性效果的目标实体。</param>
    /// <param name="source">属性效果的来源实体。</param>
    /// <param name="value">读取到的影响属性 Current 值。</param>
    /// <returns>成功读取到影响属性时返回 true，否则返回 false。</returns>
    private bool TryGetInfluenceValue(BaseEntity target, BaseEntity source, out fp value)
    {
        if (source != null &&
            source.TryGetPropertyValue(_influenceProperty, PropertyValueType.Current, out _))
        {
            value = source.GetProperty(_influenceProperty);

            return true;
        }

        if (target != null && target != source &&
            target.TryGetPropertyValue(_influenceProperty, PropertyValueType.Current, out _))
        {
            value = target.GetProperty(_influenceProperty);

            return true;
        }

        value = 0;

        return false;
    }

    /// <summary>
    /// 单个目标的一次临时属性修正实际生效量记录。
    /// </summary>
    private sealed class TemporaryPropertyModifierRecord
    {
        /// <summary>
        /// 被临时属性修正影响的目标实体。
        /// </summary>
        public readonly BaseEntity Target;

        /// <summary>
        /// Current 实际生效的变化量。
        /// </summary>
        public fp CurrentAppliedValue;

        /// <summary>
        /// Max 实际生效的变化量。
        /// </summary>
        public fp MaxAppliedValue;

        /// <summary>
        /// 创建单个目标的临时属性修正记录。
        /// </summary>
        /// <param name="target">被临时属性修正影响的目标实体。</param>
        public TemporaryPropertyModifierRecord(BaseEntity target)
        {
            Target = target;
        }

        /// <summary>
        /// 记录指定属性值类型的实际生效量。
        /// </summary>
        /// <param name="valueType">已经修改的属性值类型。</param>
        /// <param name="appliedValue">该属性值类型实际生效的变化量。</param>
        public void AddAppliedValue(PropertyValueType valueType, fp appliedValue)
        {
            if (valueType == PropertyValueType.Current)
            {
                CurrentAppliedValue += appliedValue;
            }
            else if (valueType == PropertyValueType.Max)
            {
                MaxAppliedValue += appliedValue;
            }
        }

        /// <summary>
        /// 当前记录是否至少有一个属性值类型产生过实际变化。
        /// </summary>
        public bool HasAppliedValue => CurrentAppliedValue != 0 || MaxAppliedValue != 0;
    }
}
