using Ase.Serializing;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

namespace TheKiwiCoder
{
    [System.Serializable]
    public class MoveToPosition : ActionNode
    {
        public Vector3 targetPosition;

        public int moveTick;

        private int _tampTick;

        private fp _speed;

        private PathFindingComponent _findingComponent;

        private const float PositionEpsilon = 0.001f;

        protected override void OnStart(WorldUpdateType worldUpdateType)
        {
            _tampTick = moveTick <= 0 ? 0 : moveTick;

            fp3 targetFp3 = fpmath1.Vector3ToFp3(targetPosition);
            fp3 currentPos = context.Entity.transform.Position;
            fp3 flatDir = new fp3(targetFp3.x - currentPos.x, (fp)0, targetFp3.z - currentPos.z);
            fp flatDistance = fpmath.length(flatDir);

            _speed = _tampTick > 0 && flatDistance > (fp)PositionEpsilon
                ? flatDistance / (fp)_tampTick
                : (fp)0;

            if (flatDistance > (fp)PositionEpsilon)
            {
                fpquaternion quaternion = fpmath1.LookRotation(flatDir, fpmath1.up());
                context.Entity.transform.EulerAngles = quaternion.ToEulerAngles();
            }

            _findingComponent = context.Entity.GetComponent<PathFindingComponent>();
            _findingComponent.SetPosition(targetPosition.ToFp3());
        }

        protected override void OnStop()
        {
            _findingComponent?.ClearPath();
        }

        protected override State OnUpdate(uint tick, WorldUpdateType worldUpdateType)
        {
            if (_findingComponent == null)
            {
                return State.Failure;
            }

            fp3 current = context.Entity.transform.Position;
            fp3 targetFp3 = fpmath1.Vector3ToFp3(targetPosition);
            fp distToTarget = fpmath.distance(current, targetFp3);

            if (distToTarget <= (fp)PositionEpsilon)
            {
                return State.Success;
            }

            if (_findingComponent.IsPathComplete)
            {
                return State.Success;
            }

            if (!_findingComponent.HasValidPath)
            {
                return State.Failure;
            }

            context.Entity.EntityDebug($"Path update position={context.Entity.transform.Position} Tick={_tampTick}");

            _tampTick--;
            if (_tampTick == 0)
            {
                return State.Failure;
            }

            return State.Running;
        }

        public override void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
        {
            base.TakeSnapShot(hardWriter, softWriter);

            hardWriter.WriteInt32Data($"MoveToPosition Remaining Tick", _tampTick);
            hardWriter.WriterFpData($"MoveToPosition Move Speed", _speed);
        }

        public override void RollBackTo(PooledReader authoritySnapShot)
        {
            base.RollBackTo(authoritySnapShot);

            int authorityTampTick = authoritySnapShot.ReadInt32();
            fp authoritySpeed = BaseSnapShotData.ReadFpData(authoritySnapShot);

            _tampTick = authorityTampTick;
            _speed = authoritySpeed;
        }

        public override void OnDrawGizmos()
        {
        }
    }
}
