using NUnit.Framework;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

namespace Rogue.Tests.EditMode.KCC
{
    public class PhysicsConfigConverterTests
    {
        [Test]
        public void ToFp3_QuantizesConsistently()
        {
            fp3 a = PhysicsConfigConverter.ToFp3(new Vector3(1.234567f, 0f, -2f));
            fp3 b = PhysicsConfigConverter.ToFp3(new Vector3(1.234567f, 0f, -2f));
            Assert.AreEqual(a, b);
        }

        [Test]
        public void ToCharacterMotorDimensions_MapsCenterYOffset()
        {
            var settings = new CharacterControllerSettings
            {
                radius = 0.5f,
                height = 2f,
                center = new Vector3(0f, 1f, 0f),
            };

            PhysicsConfigConverter.ToCharacterMotorDimensions(
                settings,
                out fp radius,
                out fp height,
                out fp yOffset);

            Assert.AreEqual((fp)0.5f, radius);
            Assert.AreEqual((fp)2f, height);
            Assert.AreEqual((fp)1f, yOffset);
        }

        [Test]
        public void ResolveGravityAcceleration_CharacterController_UsesDefaultWhenZero()
        {
            var settings = new CharacterControllerSettings
            {
                useGravity = true,
                gravity = 0f,
            };

            fp gravity = PhysicsConfigConverter.ResolveGravityAcceleration(settings);
            Assert.AreEqual(FPMathKCC.DefaultGravity, gravity);
        }

        [Test]
        public void ResolveGravityAcceleration_CharacterController_ReturnsZeroWhenDisabled()
        {
            var settings = new CharacterControllerSettings
            {
                useGravity = false,
                gravity = -9.81f,
            };

            fp gravity = PhysicsConfigConverter.ResolveGravityAcceleration(settings);
            Assert.AreEqual((fp)0, gravity);
        }
    }
}
