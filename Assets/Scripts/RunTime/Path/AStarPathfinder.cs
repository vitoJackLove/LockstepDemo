using System.Collections.Generic;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// A*寻路算法实现，支持Grid形式
/// </summary>
public class AStarPathfinder
{
    /// <summary>
    /// Grid宽度
    /// </summary>
    private int _gridWidth;
    
    /// <summary>
    /// Grid高度
    /// </summary>
    private int _gridHeight;
    
    /// <summary>
    /// Grid节点数组
    /// </summary>
    private GridNode[,] _grid;
    
    /// <summary>
    /// 开放列表（使用最小堆优化）
    /// </summary>
    private MinHeap<GridNode> _openList;
    
    /// <summary>
    /// 关闭列表
    /// </summary>
    private HashSet<GridNode> _closedList;
    
    /// <summary>
    /// 用于临时存储的节点列表（避免频繁分配）
    /// </summary>
    private List<GridNode> _tempNeighbors;
    
    /// <summary>
    /// 用于临时存储的路径列表（避免频繁分配）
    /// </summary>
    private List<fp2> _tempPath;
    
    /// <summary>
    /// 当前寻路请求的唯一ID（用于避免节点状态冲突）
    /// </summary>
    private int _currentPathId;
    
    /// <summary>
    /// 初始化Grid
    /// </summary>
    /// <param name="width">Grid宽度</param>
    /// <param name="height">Grid高度</param>
    /// <param name="walkableMap">可通行性地图，true表示可通行，false表示不可通行</param>
    public void InitializeGrid(int width, int height, bool[,] walkableMap = null)
    {
        _gridWidth = width;
        _gridHeight = height;
        _grid = new GridNode[width, height];
        
        // 初始化所有节点
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                _grid[x, y] = new GridNode
                {
                    X = x,
                    Y = y,
                    Walkable = walkableMap == null || walkableMap[x, y]
                };
            }
        }
        
        _openList = new MinHeap<GridNode>(width * height);
        _closedList = new HashSet<GridNode>();
        _tempNeighbors = new List<GridNode>(8);
        _tempPath = new List<fp2>(width * height);
        _currentPathId = 0;
    }
    
    /// <summary>
    /// 设置节点可通行性
    /// </summary>
    public void SetNodeWalkable(int x, int y, bool walkable)
    {
        if (IsValidCoordinate(x, y))
        {
            _grid[x, y].Walkable = walkable;
        }
    }
    
    /// <summary>
    /// 获取节点可通行性
    /// </summary>
    public bool IsNodeWalkable(int x, int y)
    {
        if (IsValidCoordinate(x, y))
        {
            return _grid[x, y].Walkable;
        }
        return false;
    }
    
    /// <summary>
    /// 执行A*寻路
    /// </summary>
    /// <param name="startX">起点X坐标</param>
    /// <param name="startY">起点Y坐标</param>
    /// <param name="endX">终点X坐标</param>
    /// <param name="endY">终点Y坐标</param>
    /// <param name="path">输出的路径（世界坐标）</param>
    /// <returns>是否找到路径</returns>
    public bool FindPath(int startX, int startY, int endX, int endY, out List<fp2> path)
    {
        path = null;
        
        // 验证输入
        if (!IsValidCoordinate(startX, startY) || !IsValidCoordinate(endX, endY))
        {
            return false;
        }
        
        GridNode startNode = _grid[startX, startY];
        GridNode endNode = _grid[endX, endY];
        
        // 起点或终点不可通行
        if (!startNode.Walkable || !endNode.Walkable)
        {
            return false;
        }
        
        // 起点和终点相同
        if (startNode == endNode)
        {
            path = new List<fp2> { new fp2(startX, startY) };
            return true;
        }
        
        // 增加路径ID，用于重置节点状态
        _currentPathId++;
        
        // 清空列表
        _openList.Clear();
        _closedList.Clear();
        _tempPath.Clear();
        
        // 重置所有节点的状态（避免上次寻路的状态影响）
        // 注意：这里只重置必要的节点，而不是全部节点，以提高性能
        // 实际上，我们可以在需要时重置，但为了安全，先重置所有节点
        for (int x = 0; x < _gridWidth; x++)
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                var node = _grid[x, y];
                node.G = (fp)0;
                node.H = (fp)0;
                node.Parent = null;
                node.IsInOpenList = false;
                node.IsInClosedList = false;
            }
        }
        
        // 初始化起点
        startNode.G = (fp)0;
        startNode.H = GetHeuristicDistance(startX, startY, endX, endY);
        startNode.Parent = null;
        startNode.IsInOpenList = true;
        startNode.IsInClosedList = false;
        
        _openList.Add(startNode);
        
        // A*主循环
        while (!_openList.IsEmpty)
        {
            // 从开放列表取出F值最小的节点
            GridNode currentNode = _openList.RemoveMin();
            currentNode.IsInOpenList = false;
            currentNode.IsInClosedList = true;
            _closedList.Add(currentNode);
            
            // 到达终点
            if (currentNode == endNode)
            {
                // 回溯路径
                ReconstructPath(currentNode, _tempPath);
                path = new List<fp2>(_tempPath);
                return true;
            }
            
            // 检查所有邻居节点
            GetNeighbors(currentNode, _tempNeighbors);
            
            foreach (GridNode neighbor in _tempNeighbors)
            {
                // 跳过不可通行或已在关闭列表的节点
                if (!neighbor.Walkable || neighbor.IsInClosedList)
                {
                    continue;
                }
                
                // 计算从当前节点到邻居节点的代价
                fp moveCost = GetMoveCost(currentNode, neighbor);
                fp newG = currentNode.G + moveCost;
                
                // 如果节点不在开放列表中，直接添加
                if (!neighbor.IsInOpenList)
                {
                    neighbor.G = newG;
                    neighbor.H = GetHeuristicDistance(neighbor.X, neighbor.Y, endX, endY);
                    neighbor.Parent = currentNode;
                    neighbor.IsInOpenList = true;
                    _openList.Add(neighbor);
                }
                // 如果节点已在开放列表中，检查是否有更短的路径
                else if (newG < neighbor.G)
                {
                    // 找到更短的路径，更新节点
                    neighbor.G = newG;
                    neighbor.Parent = currentNode;
                    // 重新计算H值（虽然通常不变，但为了确保正确性）
                    neighbor.H = GetHeuristicDistance(neighbor.X, neighbor.Y, endX, endY);
                    // 更新堆中的位置
                    _openList.UpdateItem(neighbor);
                }
            }
        }
        
        // 未找到路径
        return false;
    }
    
    /// <summary>
    /// 执行A*寻路（使用世界坐标）
    /// </summary>
    /// <param name="startWorldPos">起点世界坐标</param>
    /// <param name="endWorldPos">终点世界坐标</param>
    /// <param name="cellSize">每个Grid单元格的大小</param>
    /// <param name="path">输出的路径（世界坐标）</param>
    /// <returns>是否找到路径</returns>
    public bool FindPath(fp2 startWorldPos, fp2 endWorldPos, fp cellSize, out List<fp2> path)
    {
        int startX = Mathf.FloorToInt((float)(startWorldPos.x / cellSize));
        int startY = Mathf.FloorToInt((float)(startWorldPos.y / cellSize));
        int endX = Mathf.FloorToInt((float)(endWorldPos.x / cellSize));
        int endY = Mathf.FloorToInt((float)(endWorldPos.y / cellSize));
        
        bool found = FindPath(startX, startY, endX, endY, out List<fp2> gridPath);
        
        if (found && gridPath != null)
        {
            // 将Grid坐标转换为世界坐标
            path = new List<fp2>(gridPath.Count);
            for (int i = 0; i < gridPath.Count; i++)
            {
                fp2 gridPos = gridPath[i];
                fp2 worldPos = new fp2(gridPos.x * cellSize + cellSize * (fp)0.5f, 
                                       gridPos.y * cellSize + cellSize * (fp)0.5f);
                path.Add(worldPos);
            }
        }
        else
        {
            path = null;
        }
        
        return found;
    }
    
    /// <summary>
    /// 获取邻居节点（8方向）
    /// </summary>
    private void GetNeighbors(GridNode node, List<GridNode> neighbors)
    {
        neighbors.Clear();
        
        int x = node.X;
        int y = node.Y;
        
        // 8方向邻居：上、下、左、右、左上、右上、左下、右下
        int[] dx = { 0, 0, -1, 1, -1, 1, -1, 1 };
        int[] dy = { -1, 1, 0, 0, -1, -1, 1, 1 };
        
        for (int i = 0; i < 8; i++)
        {
            int nx = x + dx[i];
            int ny = y + dy[i];
            
            if (!IsValidCoordinate(nx, ny))
            {
                continue;
            }
            
            GridNode neighbor = _grid[nx, ny];
            
            // 如果是对角线移动，检查两个相邻的直线方向是否可通行
            // 如果相邻的直线方向有障碍物，则不能对角线移动（避免穿过障碍物）
            if (dx[i] != 0 && dy[i] != 0) // 对角线移动
            {
                GridNode horizontalNeighbor = _grid[x + dx[i], y];
                GridNode verticalNeighbor = _grid[x, y + dy[i]];
                
                // 如果两个相邻方向都不可通行，则不能对角线移动
                if (!horizontalNeighbor.Walkable && !verticalNeighbor.Walkable)
                {
                    continue;
                }
            }
            
            neighbors.Add(neighbor);
        }
    }
    
    /// <summary>
    /// 计算移动代价
    /// </summary>
    private fp GetMoveCost(GridNode from, GridNode to)
    {
        // 对角线移动代价为√2，直线移动代价为1
        int dx = Mathf.Abs(to.X - from.X);
        int dy = Mathf.Abs(to.Y - from.Y);
        
        if (dx == 1 && dy == 1)
        {
            // 对角线
            return fpmath.sqrt((fp)2); // √2
        }
        else
        {
            // 直线
            return (fp)1;
        }
    }
    
    /// <summary>
    /// 启发式距离（使用欧几里得距离）
    /// </summary>
    private fp GetHeuristicDistance(int x1, int y1, int x2, int y2)
    {
        int dx = x2 - x1;
        int dy = y2 - y1;
        return fpmath.sqrt((fp)(dx * dx + dy * dy));
    }
    
    /// <summary>
    /// 回溯路径
    /// </summary>
    private void ReconstructPath(GridNode endNode, List<fp2> path)
    {
        path.Clear();
        
        GridNode currentNode = endNode;
        while (currentNode != null)
        {
            path.Add(new fp2(currentNode.X, currentNode.Y));
            currentNode = currentNode.Parent;
        }
        
        // 反转路径（从起点到终点）
        path.Reverse();
    }
    
    /// <summary>
    /// 验证坐标是否有效
    /// </summary>
    private bool IsValidCoordinate(int x, int y)
    {
        return x >= 0 && x < _gridWidth && y >= 0 && y < _gridHeight;
    }
    
    /// <summary>
    /// 清理资源
    /// </summary>
    public void Dispose()
    {
        if (_grid != null)
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    _grid[x, y] = null;
                }
            }
            _grid = null;
        }
        
        _openList?.Clear();
        _closedList?.Clear();
        _tempNeighbors?.Clear();
        _tempPath?.Clear();
    }
}
