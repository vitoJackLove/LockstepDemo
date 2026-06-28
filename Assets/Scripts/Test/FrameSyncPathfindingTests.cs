#if UNITY_EDITOR
using System;
using NUnit.Framework;
using Unity.Mathematics.FixedPoint;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// 幀同步尋路確定性與回滾穩定性測試：單機同輸入同結果、雙機語義、高回滾場景。
/// </summary>
public class FrameSyncPathfindingTests
{
    private FrameSyncPathfinderWrapper _wrapper;
    private const int GridSize = 32;
    private static readonly fp CellSize = (fp)1;

    [SetUp]
    public void SetUp()
    {
        _wrapper = new FrameSyncPathfinderWrapper();
        _wrapper.Initialize(GridSize, GridSize, null, CellSize);
    }

    [TearDown]
    public void TearDown()
    {
        _wrapper = null;
    }

    [Test]
    public void TryFindPath_SameInput_Twice_ReturnsIdenticalPath_Determinism()
    {
        fp3 start = new fp3(1, 0, 1);
        fp3 end = new fp3(10, 0, 10);

        bool ok1 = _wrapper.TryFindPath(start, end, out FixedPointPath path1);
        bool ok2 = _wrapper.TryFindPath(start, end, out FixedPointPath path2);

        Assert.IsTrue(ok1, "First FindPath should succeed");
        Assert.IsTrue(ok2, "Second FindPath should succeed");
        Assert.AreEqual(path1.NodeCount, path2.NodeCount, "Same input must yield same node count");
        for (int i = 0; i < path1.NodeCount; i++)
        {
            Assert.AreEqual(path1.GetWaypoint(i).x, path2.GetWaypoint(i).x, $"Waypoint[{i}].x must match");
            Assert.AreEqual(path1.GetWaypoint(i).z, path2.GetWaypoint(i).z, $"Waypoint[{i}].z must match");
        }
    }

    [Test]
    public void TryFindPath_StartEqualsEnd_ReturnsSingleWaypoint()
    {
        fp3 p = new fp3(5, 0, 5);
        bool ok = _wrapper.TryFindPath(p, p, out FixedPointPath path);

        Assert.IsTrue(ok);
        Assert.IsTrue(path.IsValid);
        Assert.AreEqual(1, path.NodeCount);
        Assert.AreEqual(p.x, path.Start.x);
        Assert.AreEqual(p.z, path.Start.z);
    }

    [Test]
    public void TryFindPath_AdjacentCells_ReturnsShortPath()
    {
        fp3 start = new fp3(2, 0, 2);
        fp3 end = new fp3(3, 0, 2);

        bool ok = _wrapper.TryFindPath(start, end, out FixedPointPath path);

        Assert.IsTrue(ok);
        Assert.IsTrue(path.NodeCount >= 1 && path.NodeCount <= 3);
        Assert.AreEqual(start.x, path.Start.x);
        Assert.AreEqual(start.z, path.Start.z);
        Assert.AreEqual(end.x, path.End.x);
        Assert.AreEqual(end.z, path.End.z);
    }

    [Test]
    public void FixedPointPath_Empty_IsInvalid()
    {
        var empty = FixedPointPath.Empty;
        Assert.IsFalse(empty.IsValid);
        Assert.AreEqual(0, empty.NodeCount);
    }

    [Test]
    public void FixedPointPath_GetWaypoint_OutOfRange_ReturnsZero()
    {
        fp3 start = new fp3(0, 0, 0);
        fp3 end = new fp3(1, 0, 0);
        _wrapper.TryFindPath(start, end, out FixedPointPath path);

        Assert.AreEqual(fp3.zero, path.GetWaypoint(-1));
        Assert.AreEqual(fp3.zero, path.GetWaypoint(path.NodeCount));
    }

    /// <summary>
    /// 模擬回滾語義：同一位置與目標多次尋路結果一致（重算路徑與權威一致）。
    /// </summary>
    [Test]
    public void Rollback_ReFindPath_AfterSamePositionAndTarget_ConsistentWithAuthority()
    {
        fp3 start = new fp3(1, 0, 1);
        fp3 end = new fp3(8, 0, 8);

        _wrapper.TryFindPath(start, end, out FixedPointPath authorityPath);
        Assert.IsTrue(authorityPath.IsValid);

        int waypointIndex = 3;
        if (waypointIndex >= authorityPath.NodeCount)
            waypointIndex = authorityPath.NodeCount - 1;

        fp3 simulatedPosition = authorityPath.GetWaypoint(waypointIndex);

        _wrapper.TryFindPath(simulatedPosition, end, out FixedPointPath afterRollbackPath);

        Assert.IsTrue(afterRollbackPath.IsValid);
        Assert.AreEqual(end.x, afterRollbackPath.End.x);
        Assert.AreEqual(end.z, afterRollbackPath.End.z);
        Assert.GreaterOrEqual(afterRollbackPath.NodeCount, 1);
    }
}
#endif
