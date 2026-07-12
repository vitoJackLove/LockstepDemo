using NUnit.Framework;
using Unity.Mathematics.FixedPoint;

namespace Rogue.Tests.EditMode.KCC
{
    public class PhysicsMotionInfluenceTests
    {
        [Test]
        public void BlendPosition_OnlyAppliesEnabledAxes()
        {
            var influence = new PhysicsMotionInfluence
            {
                positionX = false,
                positionY = true,
                positionZ = false,
            };

            fp3 current = new fp3((fp)1, (fp)2, (fp)3);
            fp3 physics = new fp3((fp)10, (fp)20, (fp)30);
            fp3 blended = influence.BlendPosition(current, physics);

            Assert.AreEqual((fp)1, blended.x);
            Assert.AreEqual((fp)20, blended.y);
            Assert.AreEqual((fp)3, blended.z);
        }

        [Test]
        public void BlendRotation_OnlyAppliesEnabledAxes()
        {
            var influence = new PhysicsMotionInfluence
            {
                rotationX = false,
                rotationY = true,
                rotationZ = false,
            };

            fpquaternion current = fpmath1.EulerXYZ(new fp3((fp)0, (fp)10, (fp)0));
            fpquaternion physics = fpmath1.EulerXYZ(new fp3((fp)45, (fp)90, (fp)45));
            fpquaternion blended = influence.BlendRotation(current, physics);
            fp3 euler = blended.ToEulerAngles();

            Assert.AreEqual((fp)0, euler.x);
            Assert.AreEqual((fp)90, euler.y);
            Assert.AreEqual((fp)0, euler.z);
        }

        [Test]
        public void CreateCharacterControllerDefault_EnablesPositionAndYaw()
        {
            PhysicsMotionInfluence influence = PhysicsMotionInfluence.CreateCharacterControllerDefault();

            Assert.IsTrue(influence.positionX);
            Assert.IsTrue(influence.positionY);
            Assert.IsTrue(influence.positionZ);
            Assert.IsFalse(influence.rotationX);
            Assert.IsTrue(influence.rotationY);
            Assert.IsFalse(influence.rotationZ);
        }

        [Test]
        public void CreateRigidbodyDefault_DisablesPhysicsRotation()
        {
            PhysicsMotionInfluence influence = PhysicsMotionInfluence.CreateRigidbodyDefault();

            Assert.IsFalse(influence.rotationX);
            Assert.IsFalse(influence.rotationY);
            Assert.IsFalse(influence.rotationZ);
        }
    }
}
