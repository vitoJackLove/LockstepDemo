using System;
using Unity.Mathematics.FixedPoint;

/// <summary>
/// 定點數路徑表示，用於幀同步邏輯層。所有節點坐標為 fp3，保證確定性。
/// </summary>
public struct FixedPointPath
{
    /// <summary>
    /// 路徑航點（世界坐標），從起點到終點順序。可能為 null 或空表示無效路徑。
    /// </summary>
    public fp3[] Waypoints;

    /// <summary>
    /// 航點數量（與 Waypoints 長度一致，無路徑時為 0）。
    /// </summary>
    public int NodeCount => Waypoints?.Length ?? 0;

    /// <summary>
    /// 是否為有效路徑（有至少一個航點）。
    /// </summary>
    public bool IsValid => Waypoints != null && Waypoints.Length > 0;

    /// <summary>
    /// 起點（第一個航點）。
    /// </summary>
    public fp3 Start => (Waypoints != null && Waypoints.Length > 0) ? Waypoints[0] : fp3.zero;

    /// <summary>
    /// 終點（最後一個航點）。
    /// </summary>
    public fp3 End => (Waypoints != null && Waypoints.Length > 0) ? Waypoints[Waypoints.Length - 1] : fp3.zero;

    /// <summary>
    /// 取得指定索引的航點；索引超出範圍時返回 fp3.zero。
    /// </summary>
    public fp3 GetWaypoint(int index)
    {
        if (Waypoints == null || index < 0 || index >= Waypoints.Length)
            return fp3.zero;
        return Waypoints[index];
    }

    /// <summary>
    /// 創建空路徑。
    /// </summary>
    public static FixedPointPath Empty => new FixedPointPath { Waypoints = null };
}
