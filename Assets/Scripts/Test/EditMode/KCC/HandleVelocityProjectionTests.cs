using NUnit.Framework;
using Unity.Mathematics.FixedPoint;

namespace Rogue.Tests.EditMode.KCC
{
    public class HandleVelocityProjectionTests
    {
        [Test]
        public void HandleVelocityProjection_GroundedWallHit_KeepsSlideAlongWallNotBackward()
        {
            var motor = new FPKinematicCharacterMotor();
            motor.GroundingStatus.IsStableOnGround = true;
            motor.GroundingStatus.GroundNormal = FPMathKCC.WorldUp;

            // 沿 +Z 贴 +X 墙斜行时，分量相乘会把 Z 翻转为负，表现为倒退。
            fp3 velocity = new fp3((fp)(-0.5f), (fp)0, (fp)3f);
            fp3 wallNormal = new fp3((fp)1, (fp)0, (fp)0);

            motor.HandleVelocityProjection(ref velocity, wallNormal, stableOnHit: false);

            Assert.Greater(velocity.z, (fp)0, "贴墙滑动应沿前进方向，不应倒退");
            Assert.Less(fpmath.abs(velocity.x), (fp)0.01f);
        }

        [Test]
        public void NormalizeSafe_ZeroVector_ReturnsZeroWithoutThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                fp3 result = FPMathKCC.NormalizeSafe(fp3.zero);
                Assert.AreEqual(fp3.zero, result);
            });
        }

        [Test]
        public void GetDirectionTangentToSurface_ParallelDirection_DoesNotThrow()
        {
            var motor = new FPKinematicCharacterMotor();
            fp3 direction = FPMathKCC.WorldUp;
            fp3 surfaceNormal = FPMathKCC.WorldUp;

            Assert.DoesNotThrow(() =>
            {
                fp3 tangent = motor.GetDirectionTangentToSurface(direction, surfaceNormal);
                Assert.AreEqual(fp3.zero, tangent);
            });
        }

        [Test]
        public void GetDirectionTangentToSurface_ReturnsUnitDirection()
        {
            var motor = new FPKinematicCharacterMotor();
            fp3 direction = new fp3((fp)3f, (fp)0, (fp)4f);
            fp3 surfaceNormal = FPMathKCC.WorldUp;

            fp3 tangent = motor.GetDirectionTangentToSurface(direction, surfaceNormal);

            Assert.Less(fpmath.abs(fpmath.length(tangent) - (fp)1), (fp)0.01f);
        }

        [Test]
        public void HandleVelocityProjection_GroundedWallHit_DoesNotAmplifySpeedWithInputMagnitude()
        {
            var motor = new FPKinematicCharacterMotor();
            motor.GroundingStatus.IsStableOnGround = true;
            motor.GroundingStatus.GroundNormal = FPMathKCC.WorldUp;
            fp3 wallNormal = new fp3((fp)1, (fp)0, (fp)0);

            fp3 slowVelocity = new fp3((fp)(-0.5f), (fp)0, (fp)3f);
            fp3 fastVelocity = slowVelocity * (fp)2f;

            motor.HandleVelocityProjection(ref slowVelocity, wallNormal, stableOnHit: false);
            motor.HandleVelocityProjection(ref fastVelocity, wallNormal, stableOnHit: false);

            fp slowAlongWall = fpmath.length(slowVelocity);
            fp fastAlongWall = fpmath.length(fastVelocity);
            fp ratio = fastAlongWall / fpmath.max(slowAlongWall, (fp)0.0000001f);

            Assert.Less(fpmath.abs(ratio - (fp)2f), (fp)0.05f);
        }

        [Test]
        public void HandleVelocityProjection_GroundedWallHit_PreservesSpeedMagnitude()
        {
            var motor = new FPKinematicCharacterMotor();
            motor.GroundingStatus.IsStableOnGround = true;
            motor.GroundingStatus.GroundNormal = FPMathKCC.WorldUp;

            fp3 velocity = new fp3((fp)3f, (fp)0, (fp)4f);
            fp speed = fpmath.length(velocity);
            fp3 wallNormal = new fp3((fp)1, (fp)0, (fp)0);

            motor.HandleVelocityProjection(ref velocity, wallNormal, stableOnHit: false);

            Assert.Less(fpmath.abs(speed - fpmath.length(velocity)), (fp)0.01f);
        }
    }
}
