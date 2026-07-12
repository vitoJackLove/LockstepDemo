using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// 将 Inspector / ScriptableObject 浮点 Authoring 配置转换为 KCC 运行时定点数结构。
/// <para>
/// 战斗与帧同步路径统一使用 <c>fp</c> / <c>fp3</c> / <c>fpquaternion</c>，本类是配置进入
/// <see cref="PhysicsBodyComponent"/> 与 <see cref="FPKinematicCharacterMotor"/> 前的唯一量化入口，
/// 保证与 <see cref="fpmath1.Vector3ToFp3"/> 等项目工具一致、可复现。
/// </para>
/// </summary>
public static class PhysicsConfigConverter
{
    /// <summary>
    /// 将 Unity <see cref="Vector3"/> 转为定点 <see cref="fp3"/>。
    /// </summary>
    /// <param name="value">Inspector 或配置表中的浮点三维向量。</param>
    /// <returns>量化后的定点向量，与项目其它模块使用相同缩放规则。</returns>
    public static fp3 ToFp3(Vector3 value) => fpmath1.Vector3ToFp3(value);

    /// <summary>
    /// 从 <see cref="CharacterControllerSettings"/> 提取 Motor 胶囊尺寸。
    /// </summary>
    /// <param name="settings">Character Controller 范式配置，不可为 null。</param>
    /// <param name="radius">输出胶囊半径（<c>fp</c>）。</param>
    /// <param name="height">输出胶囊总高度（<c>fp</c>）。</param>
    /// <param name="yOffset">输出胶囊中心沿 Y 的偏移（<c>settings.center.y</c> 量化值）。</param>
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
    /// 将 Inspector 欧拉角（度）转换为局部定点四元数。
    /// </summary>
    /// <param name="localEuler">相对父实体朝向的 XYZ 欧拉角（度），来自 <see cref="PhysicsColliderSetting.localEuler"/>。</param>
    /// <returns>XYZ 顺序的定点四元数，供与实体世界旋转相乘得到碰撞体世界朝向。</returns>
    public static fpquaternion ToLocalRotation(Vector3 localEuler) =>
        fpmath1.EulerXYZ(new fp3((fp)localEuler.x, (fp)localEuler.y, (fp)localEuler.z));

    /// <summary>
    /// 解析重力加速度配置；未启用或值为 0 时分别返回 0 或 <see cref="FPMathKCC.DefaultGravity"/>。
    /// </summary>
    public static fp ResolveGravityAcceleration(bool useGravity, float gravity)
    {
        if (!useGravity)
        {
            return (fp)0;
        }

        return gravity == 0f
            ? FPMathKCC.DefaultGravity
            : (fp)gravity;
    }

    /// <summary>
    /// 从 <see cref="PhysicsBodyConfig"/> 解析重力加速度。
    /// </summary>
    public static fp ResolveGravityAcceleration(PhysicsBodyConfig bodyConfig)
    {
        if (bodyConfig == null)
        {
            return (fp)0;
        }

        return ResolveGravityAcceleration(bodyConfig.useGravity, bodyConfig.gravity);
    }

    /// <summary>
    /// 从 <see cref="CharacterControllerSettings"/> 解析重力加速度。
    /// </summary>
    public static fp ResolveGravityAcceleration(CharacterControllerSettings settings)
    {
        if (settings == null)
        {
            return (fp)0;
        }

        return ResolveGravityAcceleration(settings.useGravity, settings.gravity);
    }

    /// <summary>
    /// 根据 Authoring 设置创建 <see cref="IFPCollider"/> 实例（尚未注册到 <see cref="FPCollisionWorld"/>）。
    /// </summary>
    /// <param name="setting">单个复合碰撞体配置。</param>
    /// <returns>
    /// 对应形状的碰撞体实例；不支持的 <see cref="PhysicsShapeType"/> 返回 null。
    /// 调用方（<see cref="PhysicsBodyComponent"/>）负责 <c>SyncFromTransform</c> 与 <c>RegisterCollider</c>。
    /// </returns>
    public static IFPCollider CreateCollider(PhysicsColliderSetting setting)
    {
        fp3 localOffset = ToFp3(setting.localOffset);
        fpquaternion localRot = ToLocalRotation(setting.localEuler);

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
