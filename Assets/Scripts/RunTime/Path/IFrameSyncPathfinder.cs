using Unity.Mathematics.FixedPoint;

/// <summary>
/// 幀同步尋路服務接口。所有影響遊戲判定的尋路必須經由此接口，僅接受/返回定點數與 tick 語義，保證確定性。
/// </summary>
public interface IFrameSyncPathfinder
{
    /// <summary>
    /// 嘗試從起點到終點尋路，結果以定點路徑返回。
    /// </summary>
    /// <param name="start">起點世界坐標 (fp3)</param>
    /// <param name="end">終點世界坐標 (fp3)</param>
    /// <param name="path">輸出路徑；若尋路失敗則為 null 或空</param>
    /// <returns>是否找到路徑</returns>
    bool TryFindPath(fp3 start, fp3 end, out FixedPointPath path);
}
