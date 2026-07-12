using NUnit.Framework;
using Unity.Mathematics.FixedPoint;

namespace Rogue.Tests.EditMode.KCC
{
    public class FPCapsuleCollisionTests
    {
        [SetUp]
        public void SetUp()
        {
            FPCollisionWorld.Instance.Reset();
        }

        [Test]
        public void CapsuleCast_ZeroDirection_DoesNotThrow()
        {
            var box = CreateMonsterBox();
            FPCollisionWorld.Instance.RegisterCollider(box);

            FPCapsuleGeometry capsule = FPMathKCC.BuildCapsuleGeometry(
                new fp3((fp)0, (fp)1, (fp)0),
                fpquaternion.identity,
                (fp)0.5f,
                (fp)2f,
                (fp)1f);

            Assert.DoesNotThrow(() =>
            {
                FPCapsuleCollision.CapsuleCast(
                    capsule,
                    fp3.zero,
                    (fp)1f,
                    box,
                    out _);
            });
        }

        [Test]
        public void CapsuleCast_AgainstPitchedMonsterBox_DoesNotThrow()
        {
            var box = CreateMonsterBox();
            fpquaternion pitchedRotation = fpmath1.EulerXYZ(new fp3((fp)51.12199f, (fp)(-171.792f), (fp)0));
            box.SyncFromTransform(
                new fp3((fp)0, (fp)0, (fp)0),
                pitchedRotation,
                new fp3((fp)0, (fp)2, (fp)0),
                new fp3((fp)3, (fp)3, (fp)3),
                new fp3((fp)1, (fp)1, (fp)1));
            FPCollisionWorld.Instance.RegisterCollider(box);

            FPCapsuleGeometry capsule = FPMathKCC.BuildCapsuleGeometry(
                new fp3((fp)0, (fp)1, (fp)2),
                fpquaternion.identity,
                (fp)0.5f,
                (fp)2f,
                (fp)1f);

            Assert.DoesNotThrow(() =>
            {
                FPCapsuleCollision.CapsuleCast(
                    capsule,
                    new fp3((fp)0, (fp)0, (fp)1),
                    (fp)2f,
                    box,
                    out _);
            });
        }

        [Test]
        public void LookRotation_ParallelToUp_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                fpmath1.LookRotation(new fp3((fp)0, (fp)1, (fp)0), fpmath1.up());
            });
        }

        [Test]
        public void TryComputePenetration_InsideMonsterBox_ReturnsValidDirectionAndDistance()
        {
            var box = CreateMonsterBox();
            box.SyncFromTransform(
                fp3.zero,
                fpquaternion.identity,
                new fp3((fp)0, (fp)2, (fp)0),
                new fp3((fp)3, (fp)3, (fp)3),
                new fp3((fp)1, (fp)1, (fp)1));

            FPCapsuleGeometry capsule = FPMathKCC.BuildCapsuleGeometry(
                new fp3((fp)0, (fp)2, (fp)0),
                fpquaternion.identity,
                (fp)0.5f,
                (fp)2f,
                (fp)1f);

            bool hit = FPCapsuleCollision.TryComputePenetration(
                capsule,
                box,
                out fp3 direction,
                out fp distance);

            Assert.IsTrue(hit);
            Assert.Greater(fpmath1.sqrMagnitude(direction), (fp)0);
            Assert.Greater(distance, (fp)0);
            Assert.LessOrEqual(distance, (fp)10);
        }

        [Test]
        public void TryComputePenetration_SegmentNearBoxFace_PushesAlongFaceNormal()
        {
            var box = CreateMonsterBox();
            box.SyncFromTransform(
                fp3.zero,
                fpquaternion.identity,
                new fp3((fp)0, (fp)2, (fp)0),
                new fp3((fp)3, (fp)3, (fp)3),
                new fp3((fp)1, (fp)1, (fp)1));

            // 胶囊轴线沿 +Z 靠近盒体 +Z 面：中点在盒内，应沿 +Z 推出
            FPCapsuleGeometry capsule = FPMathKCC.BuildCapsuleGeometry(
                new fp3((fp)0, (fp)2, (fp)2.8f),
                fpquaternion.identity,
                (fp)0.5f,
                (fp)2f,
                (fp)1f);

            bool hit = FPCapsuleCollision.TryComputePenetration(
                capsule,
                box,
                out fp3 direction,
                out fp distance);

            Assert.IsTrue(hit);
            Assert.Greater(fpmath1.sqrMagnitude(direction), (fp)0);
            Assert.Greater(distance, (fp)0);
            Assert.LessOrEqual(distance, (fp)10);
            Assert.Greater(fpmath.abs(direction.z), (fp)0.9f);
        }

        [Test]
        public void TryComputePenetration_TiltedMonsterBox_ReturnsValidMtd()
        {
            var box = CreateMonsterBox();
            fpquaternion pitchedRotation = fpmath1.EulerXYZ(new fp3((fp)51.12199f, (fp)(-171.792f), (fp)0));
            box.SyncFromTransform(
                new fp3((fp)0, (fp)0, (fp)0),
                pitchedRotation,
                new fp3((fp)0, (fp)2, (fp)0),
                new fp3((fp)3, (fp)3, (fp)3),
                new fp3((fp)1, (fp)1, (fp)1));

            FPCapsuleGeometry capsule = FPMathKCC.BuildCapsuleGeometry(
                new fp3((fp)0, (fp)1, (fp)2),
                fpquaternion.identity,
                (fp)0.5f,
                (fp)2f,
                (fp)1f);

            bool hit = FPCapsuleCollision.TryComputePenetration(
                capsule,
                box,
                out fp3 direction,
                out fp distance);

            Assert.IsTrue(hit);
            Assert.Greater(fpmath1.sqrMagnitude(direction), (fp)0);
            Assert.Greater(distance, (fp)0);
            Assert.LessOrEqual(distance, (fp)10);
        }

        [Test]
        public void TryComputePenetration_CapsuleAndSphere_ReturnsValidMtd()
        {
            var sphere = new FPSphereCollider(FPCollisionLayer.Wall, fp3.zero, (fp)1f);
            sphere.SetWorldPose(new fp3((fp)0, (fp)1, (fp)0.8f), fpquaternion.identity);

            FPCapsuleGeometry capsule = FPMathKCC.BuildCapsuleGeometry(
                new fp3((fp)0, (fp)1, (fp)0),
                fpquaternion.identity,
                (fp)0.5f,
                (fp)2f,
                (fp)1f);

            bool hit = FPCapsuleCollision.TryComputePenetration(
                capsule,
                sphere,
                out fp3 direction,
                out fp distance);

            Assert.IsTrue(hit);
            Assert.Greater(fpmath1.sqrMagnitude(direction), (fp)0);
            Assert.Greater(distance, (fp)0);
        }

        [Test]
        public void TryComputePenetration_CapsuleAndCapsule_ReturnsValidMtd()
        {
            var other = new FPCapsuleCollider(
                FPCollisionLayer.Monster,
                fp3.zero,
                (fp)0.5f,
                (fp)2f,
                (fp)1f);
            other.SetWorldPose(new fp3((fp)0, (fp)1, (fp)0.8f), fpquaternion.identity);

            FPCapsuleGeometry capsule = FPMathKCC.BuildCapsuleGeometry(
                new fp3((fp)0, (fp)1, (fp)0),
                fpquaternion.identity,
                (fp)0.5f,
                (fp)2f,
                (fp)1f);

            bool hit = FPCapsuleCollision.TryComputePenetration(
                capsule,
                other,
                out fp3 direction,
                out fp distance);

            Assert.IsTrue(hit);
            Assert.Greater(fpmath1.sqrMagnitude(direction), (fp)0);
            Assert.Greater(distance, (fp)0);
        }

        [Test]
        public void CapsuleCast_AlreadyOverlapping_ReturnsFalse()
        {
            var box = CreateMonsterBox();
            box.SyncFromTransform(
                fp3.zero,
                fpquaternion.identity,
                new fp3((fp)0, (fp)2, (fp)0),
                new fp3((fp)3, (fp)3, (fp)3),
                new fp3((fp)1, (fp)1, (fp)1));

            FPCapsuleGeometry capsule = FPMathKCC.BuildCapsuleGeometry(
                new fp3((fp)0, (fp)2, (fp)0),
                fpquaternion.identity,
                (fp)0.5f,
                (fp)2f,
                (fp)1f);

            Assert.IsFalse(FPCapsuleCollision.CapsuleCast(
                capsule,
                new fp3((fp)0, (fp)0, (fp)1),
                (fp)2f,
                box,
                out _));
        }

        private static FPBoxCollider CreateMonsterBox()
        {
            return new FPBoxCollider(
                FPCollisionLayer.Monster,
                new fp3((fp)0, (fp)2, (fp)0),
                new fp3((fp)3, (fp)3, (fp)3));
        }
    }
}
