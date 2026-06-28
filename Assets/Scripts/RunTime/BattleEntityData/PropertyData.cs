using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// 属性数据
/// </summary>
public class PropertyData : ModelBase, IPool
{
    /// <summary>
    /// 当前值
    /// </summary>
    private fp _currentValue;

    /// <summary>
    /// 最小值
    /// </summary>
    private fp _minValue;

    /// <summary>
    /// 最大值
    /// </summary>
    private fp _maxValue;

    public fp CurrentValue
    {
        get => _currentValue;
        private set => this.Set(ref _currentValue, value, "CurrentValue");
    }
    
    public fp MinValue
    {
        get => _minValue;
        private set => this.Set(ref _minValue, value, "MinValue");
    }
    
    public fp MaxValue
    {
        get => _maxValue;
        private set => this.Set(ref _maxValue, value, "MaxValue");
    }

    public static PropertyData Create(fp currentValue, fp minValue, fp maxValue)
    {
        PropertyData propertyData = FPoolHelper.Get<PropertyData>();

        propertyData.CurrentValue = currentValue;
        propertyData.MinValue = minValue;
        propertyData.MaxValue = maxValue;
        
        return propertyData;
    }

    /// <summary>
    /// 按属性值类型增加或减少数值，并返回实际写入后的变化量。
    /// </summary>
    /// <param name="valueType">需要修改的属性值类型，支持 Current、Max 和 Min。</param>
    /// <param name="value">本次尝试追加的变化量，可为正数或负数。</param>
    /// <param name="appliedValue">实际生效的变化量，用于临时属性回滚。</param>
    /// <returns>属性值类型有效时返回 true，否则返回 false。</returns>
    public bool TryChangePropertyValue(PropertyValueType valueType, fp value, out fp appliedValue)
    {
        appliedValue = 0;

        switch (valueType)
        {
            case PropertyValueType.Current:
                fp oldCurrentValue = CurrentValue;
                ChangeProperty(value);
                appliedValue = CurrentValue - oldCurrentValue;
                return true;
            case PropertyValueType.Max:
                fp oldMaxValue = MaxValue;
                MaxValue = fpmath.max(MinValue, MaxValue + value);
                CurrentValue = fpmath.clamp(CurrentValue, MinValue, MaxValue);
                appliedValue = MaxValue - oldMaxValue;
                return true;
            case PropertyValueType.Min:
                fp oldMinValue = MinValue;
                MinValue = fpmath.min(MaxValue, MinValue + value);
                CurrentValue = fpmath.clamp(CurrentValue, MinValue, MaxValue);
                appliedValue = MinValue - oldMinValue;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// 修改属性
    /// </summary>
    /// <param name="value"></param>
    public void ChangeProperty(fp value)
    {
        CurrentValue = fpmath.clamp(CurrentValue + value, MinValue, MaxValue);
    }

    /// <summary>
    /// 设置属性
    /// </summary>
    /// <param name="value"></param>
    public void SetProperty(fp value)
    {
        CurrentValue = fpmath.clamp(value, MinValue, MaxValue);;
    }

    public fp GetProperty()
    {
        return CurrentValue;
    }

    public void Clear()
    {
        
    }
}
