using System;
using Unity.Mathematics.FixedPoint;

/// <summary>
/// Grid节点，用于A*寻路
/// </summary>
public class GridNode : IPool, IComparable<GridNode>
{
    /// <summary>
    /// 节点在Grid中的X坐标
    /// </summary>
    public int X { get; set; }
    
    /// <summary>
    /// 节点在Grid中的Y坐标
    /// </summary>
    public int Y { get; set; }
    
    /// <summary>
    /// 是否可通行
    /// </summary>
    public bool Walkable { get; set; }
    
    /// <summary>
    /// G值：从起点到当前节点的实际代价
    /// </summary>
    public fp G { get; set; }
    
    /// <summary>
    /// H值：从当前节点到终点的启发式估计代价
    /// </summary>
    public fp H { get; set; }
    
    /// <summary>
    /// F值：G + H
    /// </summary>
    public fp F => G + H;
    
    /// <summary>
    /// 父节点，用于回溯路径
    /// </summary>
    public GridNode Parent { get; set; }
    
    /// <summary>
    /// 是否在开放列表中
    /// </summary>
    public bool IsInOpenList { get; set; }
    
    /// <summary>
    /// 是否在关闭列表中
    /// </summary>
    public bool IsInClosedList { get; set; }
    
    /// <summary>
    /// 在堆中的索引（用于优化）
    /// </summary>
    public int HeapIndex { get; set; }
    
    public void Clear()
    {
        X = 0;
        Y = 0;
        Walkable = true;
        G = (fp)0;
        H = (fp)0;
        Parent = null;
        IsInOpenList = false;
        IsInClosedList = false;
        HeapIndex = 0;
    }
    
    /// <summary>
    /// 比较两个节点的F值（用于堆排序）
    /// </summary>
    public int CompareTo(GridNode other)
    {
        if (other == null) return 1;
        
        // F值越小优先级越高（在最小堆中，CompareTo返回负数表示优先级更高）
        int compare = F.CompareTo(other.F);
        if (compare == 0)
        {
            // 如果F值相同，H值越小优先级越高
            compare = H.CompareTo(other.H);
        }
        return compare; // 直接返回比较结果，最小堆会自动处理
    }
}
