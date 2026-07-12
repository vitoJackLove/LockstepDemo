using NUnit.Framework;
using Unity.Mathematics.FixedPoint;

namespace Rogue.Tests.EditMode.KCC
{
    public class PhysicsBodyGroundingTests
    {
        [Test]
        public void GroundProbe_FromEntityPivot_ReachesFloorAboveColliderBottom()
        {
            FPCollisionWorld.Instance.Reset();

            var floor = new FPBoxCollider(
                FPCollisionLayer.Wall,
                fp3.zero,
                new fp3((fp)10, (fp)0.25f, (fp)10),
                false);
            floor.SetWorldPose(new fp3((fp)0, (fp)(-0.25f), (fp)0), fpquaternion.identity);
            FPCollisionWorld.Instance.RegisterCollider(floor);

            fp3 entityPosition = new fp3((fp)0, (fp)0, (fp)5);
            fp3 colliderBottom = new fp3((fp)0, (fp)(-1), (fp)5);
            fp entityToBottom = entityPosition.y - colliderBottom.y;

            fp3 probeOrigin = entityPosition + FPMathKCC.WorldUp * FPMathKCC.GroundProbeSkin;
            fp probeDistance = entityToBottom + FPMathKCC.GroundProbeSkin * (fp)2;

            FPLayerMask mask = FPCollisionLayer.Wall;
            var hits = new FPRaycastHit[4];
            int hitCount = FPPhysicsQuery.RaycastNonAlloc(
                probeOrigin,
                -FPMathKCC.WorldUp,
                probeDistance,
                hits,
                mask);

            Assert.AreEqual(1, hitCount);
            Assert.GreaterOrEqual(hits[0].Point.y, (fp)(-0.01f));
            Assert.LessOrEqual(hits[0].Point.y, (fp)0.01f);
        }

        [Test]
        public void GroundProbe_FromColliderBottomMissesFloorAbove()
        {
            FPCollisionWorld.Instance.Reset();

            var floor = new FPBoxCollider(
                FPCollisionLayer.Wall,
                fp3.zero,
                new fp3((fp)10, (fp)0.25f, (fp)10),
                false);
            floor.SetWorldPose(new fp3((fp)0, (fp)(-0.25f), (fp)0), fpquaternion.identity);
            FPCollisionWorld.Instance.RegisterCollider(floor);

            fp3 colliderBottom = new fp3((fp)0, (fp)(-1), (fp)5);
            var hits = new FPRaycastHit[4];
            int hitCount = FPPhysicsQuery.RaycastNonAlloc(
                colliderBottom,
                -FPMathKCC.WorldUp,
                (fp)2,
                hits,
                FPCollisionLayer.Wall);

            Assert.AreEqual(0, hitCount);
        }
    }
}
