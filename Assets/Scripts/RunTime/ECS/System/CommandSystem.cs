using System.Collections.Generic;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// 指令系统
/// </summary>
public class CommandSystem : BaseSystem
{
    /// <summary>
    /// 等待执行的列表
    /// </summary>
    private Queue<CommandData> _waitExecuteInputQueue;

    /// <summary>
    /// 记录指令
    /// </summary>
    private Dictionary<uint, CommandData> _recodeCommandDic = new Dictionary<uint, CommandData>();

    /// <summary>
    /// 按钮移动方向
    /// </summary>
    private fp3 _inputTouchUV;
    
    public override void OnInit(object data = null)
    {
        base.OnInit(data);

        _waitExecuteInputQueue = new Queue<CommandData>();
    }

    /// <summary>
    /// 记录指令
    /// </summary>
    /// <param name="commandData"></param>
    public void RecodeCommand(CommandData commandData)
    {
        _recodeCommandDic.TryAdd(commandData.Tick, commandData);
    }

    /// <summary>
    /// 获取记录的指令
    /// </summary>
    public CommandData GetRecodeCommand(uint tick)
    {
        if (_recodeCommandDic.TryGetValue(tick, out var commandData))
        {
            return commandData;
        }

        return null;
    }
    
    /// <summary>
    /// 更新按钮位移UV
    /// </summary>
    /// <param name="inputUv"></param>
    public void UpdateInputUv(fp2 inputUv)
    {
        this._inputTouchUV = new fp3(inputUv.x, 0, inputUv.y);
    }

    /// <summary>
    /// 收集指令
    /// </summary>
    /// <param name="data"></param>
    public void CollectCommand(CommandData data)
    {
        _waitExecuteInputQueue.Enqueue(data);
    }

    public CommandData GetCommand()
    {
        CommandData data;
        
        if (_waitExecuteInputQueue.Count > 0)
        {
             data = _waitExecuteInputQueue.Dequeue();
        }
        else
        {
             data = CommandData.Create();
        }

        data.MoveDir = _inputTouchUV;
        
        return data;
    }
}
