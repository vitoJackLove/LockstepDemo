using System;

[Serializable]
public abstract class BaseClipVariable
{
    /// <summary>
    /// 获取运行时变量
    /// </summary>
    public object GetRunTimeValue()
    {
        if (isBindBlackBoardVariable)
        {
            return _blackBoardVariable.GetVariable(blackBoardVariableKey).GetValue;
        }

        return GetVariableValue();
    }
    
    /// <summary>
    /// 是否绑定黑板
    /// </summary>
    public bool isBindBlackBoardVariable;

    /// <summary>
    /// 绑定的黑板变量Key
    /// </summary>
    public string blackBoardVariableKey;
    
    /// <summary>
    /// 黑板
    /// </summary>
    private BlackBoardVariable _blackBoardVariable;
    
    /// <summary>
    /// 变量类型
    /// </summary>
    public abstract Type SVariableType();

    /// <summary>
    /// 获取变量值
    /// </summary>
    /// <returns></returns>
    public abstract object GetVariableValue();

    /// <summary>
    /// 设置变量值
    /// </summary>
    public abstract void SetVariableValue(object obj);

    /// <summary>
    /// 设置绑定黑板的状态
    /// </summary>
    /// <param name="isBind"></param>
    public void SetBindBlackBoardVariable(bool isBind)
    {
        isBindBlackBoardVariable = isBind;
        
        if (!isBindBlackBoardVariable)
        {
            blackBoardVariableKey = string.Empty;
        }
    }

    /// <summary>
    /// 设置bind的黑板变量
    /// </summary>
    /// <param name="blackVariable"></param>
    public void SetBindBlackBoardVariableKey(SkillBlackVariable blackVariable)
    {
        blackBoardVariableKey = blackVariable.variableKey;
        blackVariable.AddBind(this);
    }

    /// <summary>
    /// 设置黑板
    /// </summary>
    /// <param name="blackBoardVariable"></param>
    public void SetBlackBoard(BlackBoardVariable blackBoardVariable)
    {
        this._blackBoardVariable = blackBoardVariable;
    }
}