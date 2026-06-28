using System;
using System.Collections.Generic;
using Unity.Mathematics.FixedPoint;

/// <summary>
/// 基於 AStarPathfinder / GridNode 的純邏輯 A* 尋路包裝器，僅使用定點數與純 C#，不依賴 Unity Time/Random/物理，供幀同步邏輯層使用。
/// </summary>
public class FrameSyncPathfinderWrapper : IFrameSyncPathfinder
{
    private AStarPathfinder _pathfinder;
    private fp _cellSize;
    /// <summary>世界平面 XZ 上，網格 (0,0) 對應的世界座標（fp2.x=世界X, fp2.y=世界Z）</summary>
    private fp2 _origin;
    private bool _initialized;

    /// <summary>
    /// 使用定點數做向零取整（與 C# 整數轉換一致），用於網格索引，保證確定性。
    /// </summary>
    private static int FloorToInt(fp value)
    {
        int t = (int)value;
        if (value >= (fp)0)
            return t;
        if (value == (fp)t)
            return t;
        return t - 1;
    }

    /// <summary>
    /// 使用世界平面 XZ 與現有 2D 網格對應：fp3 的 x -> fp2.x, fp3.z -> fp2.y。
    /// </summary>
    private static fp2 Fp3ToFp2XZ(fp3 p)
    {
        return new fp2(p.x, p.z);
    }

    /// <summary>
    /// 將 2D 路徑點轉為 fp3，y 使用起點的 y 以保持高度一致。
    /// </summary>
    private static fp3 Fp2ToFp3(fp2 p, fp y)
    {
        return new fp3(p.x, y, p.y);
    }

    /// <summary>
    /// 初始化網格與單元格大小。在世界創建時調用一次，之後圖結構不變以保證確定性。
    /// 網格原點默認設為使網格中心對齊世界 (0,0)，從而支持負座標（如 -5,0,0）。
    /// </summary>
    /// <param name="width">網格寬度</param>
    /// <param name="height">網格高度</param>
    /// <param name="walkableMap">可通行性，true 可通行；null 表示全部可通行</param>
    /// <param name="cellSize">每個格子對應的世界單位（定點數）</param>
    public void Initialize(int width, int height, bool[,] walkableMap, fp cellSize)
    {
        _pathfinder = new AStarPathfinder();
        _pathfinder.InitializeGrid(width, height, walkableMap);
        _cellSize = cellSize;
        // 網格 (0,0) 對應世界 (-width*cellSize/2, -height*cellSize/2)，使網格中心在世界 (0,0)，支持負座標
        _origin = new fp2(
            -(fp)width * cellSize / (fp)2,
            -(fp)height * cellSize / (fp)2);
        _initialized = true;
    }

    /// <summary>
    /// 僅用定點數與整數運算將世界坐標轉為網格坐標（先減去原點再除以 cellSize 取整），避免 float/Mathf 以保證確定性。
    /// </summary>
    private void WorldToGrid(fp2 worldPos, out int gx, out int gy)
    {
        gx = FloorToInt((worldPos.x - _origin.x) / _cellSize);
        gy = FloorToInt((worldPos.y - _origin.y) / _cellSize);
    }

    public bool TryFindPath(fp3 start, fp3 end, out FixedPointPath path)
    {
        path = FixedPointPath.Empty;

        if (!_initialized || _pathfinder == null)
            return false;

        fp2 start2 = Fp3ToFp2XZ(start);
        fp2 end2 = Fp3ToFp2XZ(end);

        WorldToGrid(start2, out int startX, out int startY);
        WorldToGrid(end2, out int endX, out int endY);

        bool found = _pathfinder.FindPath(startX, startY, endX, endY, out List<fp2> gridPath);

        if (!found || gridPath == null || gridPath.Count == 0)
            return false;

        var waypoints = new fp3[gridPath.Count];
        fp y = start.y;
        for (int i = 0; i < gridPath.Count; i++)
        {
            if (i == 0)
            {
                waypoints[i] = start;
            }
            else if (i == gridPath.Count - 1)
            {
                waypoints[i] = end;
            }
            else
            {
                fp2 gridPos = gridPath[i];
                fp2 worldPos = new fp2(
                    _origin.x + gridPos.x * _cellSize + _cellSize * (fp)0.5f,
                    _origin.y + gridPos.y * _cellSize + _cellSize * (fp)0.5f);
                waypoints[i] = Fp2ToFp3(worldPos, y);
            }
        }

        path = new FixedPointPath { Waypoints = waypoints };
        return true;
    }
}
