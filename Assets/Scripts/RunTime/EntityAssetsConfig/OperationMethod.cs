using Sirenix.OdinInspector;

/// <summary>
/// 操作符
/// </summary>
public enum OperationMethod 
{
    [LabelText("设置")]
    
    Set,
    
    [LabelText("加法")]
    Add,
    
    [LabelText("减法")]
    Subtract,
    
    [LabelText("乘法")]
    Multiply,
    
    [LabelText("除法")]
    Divide
}

/// <summary>
/// 比较器
/// </summary>
public enum CompareMethod
{
    [LabelText("等于")]
    EqualTo,
    
    [LabelText("大于")]
    GreaterThan,
    
    [LabelText("小于")]
    LessThan,
    
    [LabelText("大于等于")]
    GreaterOrEqualTo,
    
    [LabelText("小于等于")]
    LessOrEqualTo
}
