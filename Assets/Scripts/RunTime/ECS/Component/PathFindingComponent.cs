using Ase.Serializing;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

public class PathFindingComponent : BaseComponent
{
    private IFrameSyncPathfinder _pathfinder;
    private fp3 _targetPosition;
    private FixedPointPath _currentPath;
    private int _currentWaypointIndex;
    private fp _moveSpeed;

    public override void OnInit(object data = null)
    {
        base.OnInit(data);
        _moveSpeed = (fp)1;
        _currentPath = FixedPointPath.Empty;
        _currentWaypointIndex = 0;
        var system = Entity?.BaseWorld?.GetSystem<FrameSyncPathfindingSystem>();
        _pathfinder = system?.GetPathfinder();
    }

    public void SetPosition(fp3 target)
    {
        _targetPosition = target;
        if (_pathfinder == null)
        {
            _currentPath = FixedPointPath.Empty;
            _currentWaypointIndex = 0;
            return;
        }

        fp3 start = Entity.transform.Position;
        if (_pathfinder.TryFindPath(start, target, out FixedPointPath path))
        {
            _currentPath = path;
            _currentWaypointIndex = 0;
        }
        else
        {
            _currentPath = FixedPointPath.Empty;
            _currentWaypointIndex = 0;
        }
    }

    public override void OnFixedUpdate(fp deltaTime, WorldUpdateType worldUpdateType)
    {
        base.OnFixedUpdate(deltaTime, worldUpdateType);

        if (!_currentPath.IsValid || _currentWaypointIndex >= _currentPath.NodeCount)
        {
            Entity.EntityDebug($"Path: empty or complete hasPath={_currentPath.IsValid} index={_currentWaypointIndex}");
            return;
        }

        fp3 currentPos = Entity.transform.Position;
        fp3 waypoint = _currentPath.GetWaypoint(_currentWaypointIndex);
        fp3 toWaypoint = waypoint - currentPos;
        fp dist = fpmath.distance(currentPos, waypoint);

        if (dist <= (fp)0.001f)
        {
            _currentWaypointIndex++;
            if (_currentWaypointIndex >= _currentPath.NodeCount)
            {
                Entity.transform.Position = _currentPath.End;
                Entity.EntityDebug("Path: reached end");
                return;
            }

            waypoint = _currentPath.GetWaypoint(_currentWaypointIndex);
            toWaypoint = waypoint - currentPos;
            dist = fpmath.distance(currentPos, waypoint);
        }

        if (dist > (fp)0)
        {
            fp step = _moveSpeed * deltaTime;
            fp3 move = step >= dist ? toWaypoint : fpmath.normalize(toWaypoint) * step;
            Entity.transform.Position = currentPos + move;
        }

        Entity.EntityDebug($"Path: position={Entity.transform.Position} waypointIndex={_currentWaypointIndex} hasPath={_currentPath.IsValid}");
    }

    public bool IsPathComplete => _currentPath.IsValid && _currentWaypointIndex >= _currentPath.NodeCount;

    public bool HasValidPath => _currentPath.IsValid;

    public void ClearPath()
    {
        Entity.EntityDebug($"Path cleared. Entity position={Entity.transform.Position}");
        _currentPath = FixedPointPath.Empty;
        _currentWaypointIndex = 0;
    }

    public override void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
    {
        base.TakeSnapShot(hardWriter, softWriter);

        bool hasPath = _currentPath.IsValid && _currentWaypointIndex < _currentPath.NodeCount;
        hardWriter.WriterBoolData("Has Path", hasPath);
        hardWriter.WriterFp3Data("Target Position", _targetPosition);
        hardWriter.WriteInt32Data("Current Waypoint Index", _currentWaypointIndex);
    }

    public override void HardRollBackTo(PooledReader authoritySnapShot)
    {
        base.HardRollBackTo(authoritySnapShot);

        bool isHasPath = authoritySnapShot.ReadBoolean();
        _targetPosition = BaseSnapShotData.ReadFp3Data(authoritySnapShot);
        int savedIndex = authoritySnapShot.ReadInt32();

        if (isHasPath && _pathfinder != null)
        {
            fp3 currentPos = Entity.transform.Position;
            if (_pathfinder.TryFindPath(currentPos, _targetPosition, out FixedPointPath path))
            {
                _currentPath = path;
                _currentWaypointIndex = savedIndex < path.NodeCount ? savedIndex : path.NodeCount - 1;
            }
            else
            {
                _currentPath = FixedPointPath.Empty;
                _currentWaypointIndex = 0;
            }
        }
        else
        {
            _currentPath = FixedPointPath.Empty;
            _currentWaypointIndex = 0;
        }
    }
}
