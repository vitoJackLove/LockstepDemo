using NUnit.Framework;
using Unity.Mathematics.FixedPoint;

namespace Rogue.Tests.EditMode.KCC
{
    public class FPCapsuleColliderTests
    {
        [SetUp]
        public void SetUp()
        {
            FPCollisionWorld.Instance.Reset();
        }

        [Test]
        public void CapsuleCollider_GetCapsuleShape_MatchesDimensions()
        {
            var capsule = new FPCapsuleCollider(
                FPCollisionLayer.Monster,
                fp3.zero,
                (fp)0.4f,
                (fp)1.8f,
                (fp)0.9f,
                directionAxis: 1);

            capsule.SetWorldPose(new fp3((fp)5, (fp)0, (fp)0), fpquaternion.identity);

            FPCapsuleShape shape = capsule.GetCapsuleShape();
            Assert.AreEqual((fp)0.4f, shape.Radius);
            Assert.AreEqual((fp)1.8f, shape.Height);
            Assert.AreEqual(1, shape.DirectionAxis);
        }
    }
}
