using System;
using System.Collections.Generic;

[Serializable] 
public abstract class SkillBlackVariable 
{
    /// <summary>
    /// 变量Key
    /// </summary>
    public string variableKey;

    public abstract Type VariableType();
    
    public abstract string InitVariableKey { get;}
    
    public abstract object GetValue { get; }

    public abstract void SetValue(object obj);
    
    /// <summary>
    /// 绑定的变量
    /// </summary>
    private List<BaseClipVariable> _bindSVariableList = new List<BaseClipVariable>();

    public void AddBind(BaseClipVariable sBaseClipVariable)
    {
        _bindSVariableList.Add(sBaseClipVariable);
    }
    
    public void RefreshBind()
    {
        for (int i = 0; i < _bindSVariableList.Count; i++)
        {
            _bindSVariableList[i].SetBindBlackBoardVariable(false);
        }
        
        _bindSVariableList.Clear();

        _bindSVariableList = null;
    }
}