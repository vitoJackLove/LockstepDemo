using System.Collections.Generic;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// A*寻路使用示例
/// </summary>
public class AStarPathfinderExample : MonoBehaviour
{
    [Header("Grid设置")]
    [SerializeField] private int gridWidth = 20;
    [SerializeField] private int gridHeight = 20;
    [SerializeField] private fp cellSize = (fp)1.0f;
    
    [Header("调试")]
    [SerializeField] private bool showGrid = true;
    [SerializeField] private bool showPath = true;
    [SerializeField] private bool showStartEnd = true;
    
    private AStarPathfinder _pathfinder;
    private List<fp2> _currentPath;
    private fp2 _startPoint = new fp2(-1, -1);
    private fp2 _endPoint = new fp2(-1, -1);
    
    private void Start()
    {
        // 初始化寻路器
        _pathfinder = new AStarPathfinder();
        
        // 创建可通行性地图（示例：全部可通行）
        bool[,] walkableMap = new bool[gridWidth, gridHeight];
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                walkableMap[x, y] = true; // 默认全部可通行
            }
        }
        
        // 设置障碍物 - 创建一个"U"形障碍物
        SetupObstacles(walkableMap);
        
        // 初始化Grid
        _pathfinder.InitializeGrid(gridWidth, gridHeight, walkableMap);
        
        // 运行测试
        RunAllTests();
    }
    
    /// <summary>
    /// 设置障碍物
    /// </summary>
    private void SetupObstacles(bool[,] walkableMap)
    {
        // 创建一个"U"形障碍物，测试绕路能力
        // 左墙
        for (int y = 5; y <= 12; y++)
        {
            walkableMap[8, y] = false;
        }
        
        // 右墙
        for (int y = 5; y <= 12; y++)
        {
            walkableMap[12, y] = false;
        }
        
        // 底墙（中间留一个缺口）
        for (int x = 8; x <= 12; x++)
        {
            if (x != 10) // 在x=10处留一个缺口
            {
                walkableMap[x, 5] = false;
            }
        }
        
        // 添加一些随机障碍物
        walkableMap[3, 3] = false;
        walkableMap[4, 3] = false;
        walkableMap[3, 4] = false;
        
        walkableMap[15, 15] = false;
        walkableMap[16, 15] = false;
        walkableMap[15, 16] = false;
        
        // 添加一条横向障碍物
        for (int x = 2; x <= 6; x++)
        {
            walkableMap[x, 7] = false;
        }
    }
    
    private void Update()
    {
        // 键盘控制测试
        if (Input.GetKeyDown(KeyCode.T))
        {
            RunAllTests();
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TestCase1_SimplePath();
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TestCase2_AroundObstacle();
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TestCase3_DiagonalPath();
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            TestCase4_NoPath();
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            TestCase5_LongPath();
        }
        
        // 使用DrawDebugTools绘制
        DrawGridAndPath();
    }
    
    /// <summary>
    /// 使用DrawDebugTools绘制网格和路径
    /// </summary>
    private void DrawGridAndPath()
    {
        if (_pathfinder == null || DrawDebugTools.Instance == null) return;
        
        float cellSizeFloat = (float)cellSize;
        Vector3 gridOrigin = Vector3.zero;
        
        // 绘制Grid
        if (showGrid)
        {
            // 绘制网格线
            Color gridLineColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            
            // 绘制垂直线
            for (int x = 0; x <= gridWidth; x++)
            {
                Vector3 start = new Vector3(
                    gridOrigin.x + x * cellSizeFloat,
                    gridOrigin.y,
                    gridOrigin.z
                );
                Vector3 end = new Vector3(
                    gridOrigin.x + x * cellSizeFloat,
                    gridOrigin.y + gridHeight * cellSizeFloat,
                    gridOrigin.z
                );
                DrawDebugTools.DrawLine(start, end, gridLineColor, 0.0f);
            }
            
            // 绘制水平线
            for (int y = 0; y <= gridHeight; y++)
            {
                Vector3 start = new Vector3(
                    gridOrigin.x,
                    gridOrigin.y + y * cellSizeFloat,
                    gridOrigin.z
                );
                Vector3 end = new Vector3(
                    gridOrigin.x + gridWidth * cellSizeFloat,
                    gridOrigin.y + y * cellSizeFloat,
                    gridOrigin.z
                );
                DrawDebugTools.DrawLine(start, end, gridLineColor, 0.0f);
            }
            
            // 绘制障碍物
            Color obstacleColor = Color.red;
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (!_pathfinder.IsNodeWalkable(x, y))
                    {
                        Vector3 center = new Vector3(
                            gridOrigin.x + x * cellSizeFloat + cellSizeFloat * 0.5f,
                            gridOrigin.y + y * cellSizeFloat + cellSizeFloat * 0.5f,
                            gridOrigin.z
                        );
                        Vector3 size = new Vector3(cellSizeFloat * 0.9f, cellSizeFloat * 0.9f, 0.1f);
                        DrawDebugTools.DrawBox(center, Quaternion.identity, size, obstacleColor, 0.0f);
                    }
                }
            }
        }
        
        // 绘制路径
        if (showPath && _currentPath != null && _currentPath.Count > 0)
        {
            Color pathLineColor = Color.green;
            Color pathPointColor = Color.yellow;
            
            // 绘制路径线
            for (int i = 0; i < _currentPath.Count - 1; i++)
            {
                Vector3 start = new Vector3((float)_currentPath[i].x, (float)_currentPath[i].y, 0);
                Vector3 end = new Vector3((float)_currentPath[i + 1].x, (float)_currentPath[i + 1].y, 0);
                DrawDebugTools.DrawLine(start, end, pathLineColor, 0.0f);
            }
            
            // 绘制路径点
            float pointSize = 0.15f;
            foreach (var point in _currentPath)
            {
                Vector3 pos = new Vector3((float)point.x, (float)point.y, 0);
                DrawDebugTools.DrawSphere(pos, pointSize, 8, pathPointColor, 0.0f);
            }
        }
        
        // 绘制起点和终点
        if (showStartEnd && _currentPath != null && _currentPath.Count > 0)
        {
            // 起点（绿色）
            Vector3 startPos = new Vector3((float)_currentPath[0].x, (float)_currentPath[0].y, 0);
            DrawDebugTools.DrawSphere(startPos, 0.3f, 12, Color.green, 0.0f);
            DrawDebugTools.DrawString3D(startPos + Vector3.up * 0.5f, "起点", TextAnchor.MiddleCenter, Color.green, 0.5f, 0.0f);
            
            // 终点（蓝色）
            Vector3 endPos = new Vector3((float)_currentPath[_currentPath.Count - 1].x, (float)_currentPath[_currentPath.Count - 1].y, 0);
            DrawDebugTools.DrawSphere(endPos, 0.3f, 12, Color.blue, 0.0f);
            DrawDebugTools.DrawString3D(endPos + Vector3.up * 0.5f, "终点", TextAnchor.MiddleCenter, Color.blue, 0.5f, 0.0f);
        }
    }
    
    private void OnDestroy()
    {
        // 清理资源
        _pathfinder?.Dispose();
    }
    
    /// <summary>
    /// 运行所有测试
    /// </summary>
    private void RunAllTests()
    {
        Debug.Log("========== 开始运行A*寻路测试 ==========");
        Debug.Log("提示：按T键运行所有测试，按1-5键运行单个测试用例");
        
        TestCase1_SimplePath();
        TestCase2_AroundObstacle();
        TestCase3_DiagonalPath();
        TestCase4_NoPath();
        TestCase5_LongPath();
        TestCase6_SameStartEnd();
        TestCase7_AdjacentNodes();
        
        Debug.Log("========== 所有测试完成 ==========");
    }
    
    /// <summary>
    /// 测试用例1：简单直线路径
    /// </summary>
    private void TestCase1_SimplePath()
    {
        Debug.Log("--- 测试用例1：简单直线路径 (0,0) -> (5,0) ---");
        bool found = _pathfinder.FindPath(0, 0, 5, 0, out List<fp2> path);
        
        if (found && path != null)
        {
            Debug.Log($"✓ 找到路径，长度: {path.Count} 个节点");
            _currentPath = path;
            showPath = true;
            _startPoint = new fp2(0, 0);
            _endPoint = new fp2(5, 0);
            
            // 验证路径正确性
            if (path.Count == 6) // 应该包含起点和终点
            {
                Debug.Log("✓ 路径长度正确");
            }
            else
            {
                Debug.LogWarning($"✗ 路径长度不正确，期望6，实际{path.Count}");
            }
        }
        else
        {
            Debug.LogError("✗ 未找到路径");
        }
    }
    
    /// <summary>
    /// 测试用例2：绕过障碍物
    /// </summary>
    private void TestCase2_AroundObstacle()
    {
        Debug.Log("--- 测试用例2：绕过障碍物 (0,8) -> (15,8) ---");
        bool found = _pathfinder.FindPath(0, 8, 15, 8, out List<fp2> path);
        
        if (found && path != null)
        {
            Debug.Log($"✓ 找到路径，长度: {path.Count} 个节点");
            _currentPath = path;
            showPath = true;
            _startPoint = new fp2(0, 8);
            _endPoint = new fp2(15, 8);
            
            // 验证路径绕过了障碍物（不应该经过障碍物区域）
            bool pathValid = true;
            foreach (var point in path)
            {
                int x = (int)point.x;
                int y = (int)point.y;
                if (!_pathfinder.IsNodeWalkable(x, y))
                {
                    Debug.LogError($"✗ 路径经过障碍物: ({x}, {y})");
                    pathValid = false;
                    break;
                }
            }
            
            if (pathValid)
            {
                Debug.Log("✓ 路径成功绕过障碍物");
            }
        }
        else
        {
            Debug.LogError("✗ 未找到路径");
        }
    }
    
    /// <summary>
    /// 测试用例3：对角线路径
    /// </summary>
    private void TestCase3_DiagonalPath()
    {
        Debug.Log("--- 测试用例3：对角线路径 (2,2) -> (18,18) ---");
        bool found = _pathfinder.FindPath(2, 2, 18, 18, out List<fp2> path);
        
        if (found && path != null)
        {
            Debug.Log($"✓ 找到路径，长度: {path.Count} 个节点");
            _currentPath = path;
            showPath = true;
            _startPoint = new fp2(2, 2);
            _endPoint = new fp2(18, 18);
            
            // 验证路径包含对角线移动
            bool hasDiagonal = false;
            for (int i = 0; i < path.Count - 1; i++)
            {
                int dx = Mathf.Abs((int)path[i + 1].x - (int)path[i].x);
                int dy = Mathf.Abs((int)path[i + 1].y - (int)path[i].y);
                if (dx == 1 && dy == 1)
                {
                    hasDiagonal = true;
                    break;
                }
            }
            
            if (hasDiagonal)
            {
                Debug.Log("✓ 路径包含对角线移动");
            }
            else
            {
                Debug.Log("路径未使用对角线移动（可能因为障碍物）");
            }
        }
        else
        {
            Debug.LogError("✗ 未找到路径");
        }
    }
    
    /// <summary>
    /// 测试用例4：无路径情况（起点被障碍物包围）
    /// </summary>
    private void TestCase4_NoPath()
    {
        Debug.Log("--- 测试用例4：无路径情况 (3,3) -> (15,15) ---");
        
        // 临时添加障碍物包围起点
        _pathfinder.SetNodeWalkable(2, 3, false);
        _pathfinder.SetNodeWalkable(4, 3, false);
        _pathfinder.SetNodeWalkable(3, 2, false);
        _pathfinder.SetNodeWalkable(3, 4, false);
        
        bool found = _pathfinder.FindPath(3, 3, 15, 15, out List<fp2> path);
        
        // 恢复障碍物
        _pathfinder.SetNodeWalkable(2, 3, true);
        _pathfinder.SetNodeWalkable(4, 3, true);
        _pathfinder.SetNodeWalkable(3, 2, true);
        _pathfinder.SetNodeWalkable(3, 4, true);
        
        if (!found)
        {
            Debug.Log("✓ 正确识别无路径情况");
        }
        else
        {
            Debug.LogWarning("✗ 应该找不到路径，但找到了路径");
        }
    }
    
    /// <summary>
    /// 测试用例5：长距离路径
    /// </summary>
    private void TestCase5_LongPath()
    {
        Debug.Log("--- 测试用例5：长距离路径 (0,0) -> (19,19) ---");
        bool found = _pathfinder.FindPath(0, 0, 19, 19, out List<fp2> path);
        
        if (found && path != null)
        {
            Debug.Log($"✓ 找到路径，长度: {path.Count} 个节点");
            _currentPath = path;
            showPath = true;
            _startPoint = new fp2(0, 0);
            _endPoint = new fp2(19, 19);
            
            // 验证路径连续性
            bool pathContinuous = true;
            for (int i = 0; i < path.Count - 1; i++)
            {
                int dx = Mathf.Abs((int)path[i + 1].x - (int)path[i].x);
                int dy = Mathf.Abs((int)path[i + 1].y - (int)path[i].y);
                
                if (dx > 1 || dy > 1 || (dx == 0 && dy == 0))
                {
                    Debug.LogError($"✗ 路径不连续: ({path[i].x}, {path[i].y}) -> ({path[i + 1].x}, {path[i + 1].y})");
                    pathContinuous = false;
                    break;
                }
            }
            
            if (pathContinuous)
            {
                Debug.Log("✓ 路径连续且有效");
            }
        }
        else
        {
            Debug.LogError("✗ 未找到路径");
        }
    }
    
    /// <summary>
    /// 测试用例6：起点和终点相同
    /// </summary>
    private void TestCase6_SameStartEnd()
    {
        Debug.Log("--- 测试用例6：起点和终点相同 (5,5) -> (5,5) ---");
        bool found = _pathfinder.FindPath(5, 5, 5, 5, out List<fp2> path);
        
        if (found && path != null && path.Count == 1)
        {
            Debug.Log("✓ 正确处理起点和终点相同的情况");
        }
        else
        {
            Debug.LogError("✗ 起点和终点相同时处理不正确");
        }
    }
    
    /// <summary>
    /// 测试用例7：相邻节点
    /// </summary>
    private void TestCase7_AdjacentNodes()
    {
        Debug.Log("--- 测试用例7：相邻节点 (5,5) -> (6,5) ---");
        bool found = _pathfinder.FindPath(5, 5, 6, 5, out List<fp2> path);
        
        if (found && path != null)
        {
            Debug.Log($"✓ 找到路径，长度: {path.Count} 个节点");
            
            if (path.Count == 2)
            {
                Debug.Log("✓ 相邻节点路径长度正确");
            }
            else
            {
                Debug.LogWarning($"✗ 相邻节点路径长度不正确，期望2，实际{path.Count}");
            }
        }
        else
        {
            Debug.LogError("✗ 未找到路径");
        }
    }
    
    /// <summary>
    /// 示例：使用Grid坐标寻路
    /// </summary>
    public void ExampleGridCoordinate()
    {
        // 从Grid坐标(0, 0)到(10, 10)
        bool found = _pathfinder.FindPath(0, 0, 5, 10, out List<fp2> path);
        
        _currentPath = path;

        showPath = true;

        if (found)
        {
            Debug.Log($"找到路径，包含 {path.Count} 个节点");
            foreach (var point in path)
            {
                Debug.Log($"路径点: ({point.x}, {point.y})");
            }
        }
    }
    
    /// <summary>
    /// 示例：动态设置障碍物
    /// </summary>
    public void ExampleSetObstacle(int x, int y, bool walkable)
    {
        _pathfinder.SetNodeWalkable(x, y, walkable);
    }
}
