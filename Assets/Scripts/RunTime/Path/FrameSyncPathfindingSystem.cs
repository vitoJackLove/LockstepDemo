using Unity.Mathematics.FixedPoint;

/// <summary>
/// 幀同步尋路系統：持有 IFrameSyncPathfinder，在世界初始化時構建並鎖定網格，供 AI/怪物尋路使用。
/// </summary>
public class FrameSyncPathfindingSystem : BaseSystem
{
    private IFrameSyncPathfinder _pathfinder;
    private FrameSyncPathfinderWrapper _wrapper;

    public override void OnInit(object data = null)
    {
        base.OnInit(data);
        _wrapper = new FrameSyncPathfinderWrapper();
        fp cellSize = (fp)1;
        int width = 128;
        int height = 128;
        bool[,] walkableMap = null;
        _wrapper.Initialize(width, height, walkableMap, cellSize);
        _pathfinder = _wrapper;
    }

    /// <summary>
    /// 供 ECS 組件使用的尋路接口，僅在邏輯幀內調用以保證確定性。
    /// </summary>
    public IFrameSyncPathfinder GetPathfinder() => _pathfinder;

    public override void OnDispose()
    {
        _wrapper = null;
        _pathfinder = null;
        base.OnDispose();
    }
}
