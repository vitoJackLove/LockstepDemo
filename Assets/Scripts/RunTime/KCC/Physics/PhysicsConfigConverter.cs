using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// 将 Inspector 浮点配置转换为运行时定点数结构。
/// </summary>
public static class PhysicsConfigConverter
{
    /// <summary>
    /// Vector3 转 fp3（与项目 fpmath1.Vector3ToFp3 保持一致）。
    /// </summary>
    public static fp3 ToFp3(Vector3 value) => fpmath1.Vector3ToFp3(value);

    /// <summary>
    /// 提取 Motor 胶囊尺寸。
    /// </summary>
    public static void ToCharacterMotorDimensions(
        CharacterControllerSettings settings,
        out fp radius,
        out fp height,
        out fp yOffset)
    {
        radius = (fp)settings.radius;
        height = (fp)settings.height;
        yOffset = (fp)settings.center.y;
    }

    /// <summary>
    /// 根据 Authoring 设置创建 IFPCollider 实例（未注册到 World）。
    /// </summary>
    public static IFPCollider CreateCollider(PhysicsColliderSetting setting)
    {
        fp3 localOffset = ToFp3(setting.localOffset);
        fpquaternion localRot = fpquaternion.Euler(
            (fp)setting.localEuler.x * fpmath.Deg2Rad,
            (fp)setting.localEuler.y * fpmath.Deg2Rad,
            (fp)setting.localEuler.z * fpmath.Deg2Rad);

        switch (setting.shape)
        {
            case PhysicsShapeType.Box:
            {
                var box = new FPBoxCollider(setting.layer, localOffset, ToFp3(setting.halfExtents), setting.isTrigger);
                box.SetWorldPose(localOffset, localRot);
                return box;
            }
            case PhysicsShapeType.Sphere:
            {
                var sphere = new FPSphereCollider(setting.layer, localOffset, (fp)setting.radius, setting.isTrigger);
                sphere.SetWorldPose(localOffset, localRot);
                return sphere;
            }
            case PhysicsShapeType.Capsule:
            {
                return new FPCapsuleCollider(
                    setting.layer,
                    localOffset,
                    (fp)setting.capsuleRadius,
                    (fp)setting.capsuleHeight,
                    (fp)0,
                    setting.directionAxis,
                    setting.isTrigger);
            }
            default:
                return null;
        }
    }
}
