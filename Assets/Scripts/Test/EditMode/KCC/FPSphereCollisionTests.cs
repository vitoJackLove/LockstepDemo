using NUnit.Framework;
using Unity.Mathematics.FixedPoint;

namespace Rogue.Tests.EditMode.KCC
{
    public class FPSphereCollisionTests
    {
        [SetUp]
        public void SetUp()
        {
            FPCollisionWorld.Instance.Reset();
        }

        [Test]
        public void SphereCollider_SyncFromTransform_ScalesRadius()
        {
            var sphere = new FPSphereCollider(FPCollisionLayer.Default, fp3.zero, (fp)0.5f);
            sphere.SyncFromTransform(
                new fp3((fp)1, (fp)2, (fp)3),
                fpquaternion.identity,
                fp3.zero,
                (fp)0.5f,
                new fp3((fp)2, (fp)1, (fp)1));

            FPSphereShape shape = sphere.GetSphereShape();
            Assert.AreEqual((fp)1, shape.Center.x);
            Assert.AreEqual((fp)1, shape.Radius);
        }

        [Test]
        public void CapsuleIntersectsSphere_Overlapping_ReturnsTrue()
        {
            var sphere = new FPSphereCollider(FPCollisionLayer.Wall, fp3.zero, (fp)1f);
            sphere.SetWorldPose(new fp3((fp)0, (fp)1, (fp)0), fpquaternion.identity);

            var capsule = new FPCapsuleGeometry
            {
                BottomHemiCenter = new fp3((fp)0, (fp)0, (fp)0),
                TopHemiCenter = new fp3((fp)0, (fp)2, (fp)0),
                Radius = (fp)0.5f,
            };

            Assert.IsTrue(FPCapsuleCollision.OverlapCapsule(capsule, sphere));
        }

        [Test]
        public void CapsuleIntersectsSphere_Separated_ReturnsFalse()
        {
            var sphere = new FPSphereCollider(FPCollisionLayer.Wall, fp3.zero, (fp)0.5f);
            sphere.SetWorldPose(new fp3((fp)10, (fp)0, (fp)0), fpquaternion.identity);

            var capsule = new FPCapsuleGeometry
            {
                BottomHemiCenter = fp3.zero,
                TopHemiCenter = new fp3((fp)0, (fp)2, (fp)0),
                Radius = (fp)0.5f,
            };

            Assert.IsFalse(FPCapsuleCollision.OverlapCapsule(capsule, sphere));
        }
    }
}
