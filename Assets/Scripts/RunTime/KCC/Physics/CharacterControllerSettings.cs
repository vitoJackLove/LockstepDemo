using System;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Character Controller 范式配置（单一胶囊，对齐 Unity Character Controller Inspector）。
/// <para>
/// 用于 <see cref="PhysicsMovementMode.CharacterController"/> 实体（通常为英雄）：
/// <see cref="PhysicsBodyComponent"/> 在 <c>OnStart</c> 读取本配置，
/// 调用 <see cref="PhysicsConfigConverter.ToCharacterMotorDimensions"/> 转为定点数后
/// 注册 <see cref="FPKinematicCharacterMotor"/> 单胶囊，由 <see cref="FPKinematicCharacterSystem"/> 每帧模拟。
/// </para>
/// <para>
/// 与 <see cref="PhysicsBodyConfig"/> 互斥：CC 范式下不应再配置复合碰撞体作为身体阻挡核。
/// 受击判定仍走独立的 <c>colliderDataList</c> → <c>VolumeSystem</c>，与本配置无关。
/// </para>
/// </summary>
[Serializable]
public class CharacterControllerSettings
{
    /// <summary>
    /// 胶囊半径（米，Inspector 浮点）。
    /// 运行时经 <see cref="PhysicsConfigConverter.ToCharacterMotorDimensions"/> 转为 <c>fp</c>，
    /// 传给 <see cref="FPKinematicCharacterMotor.SetCapsuleDimensions"/>，参与 sweep 与穿透解算。
    /// </summary>
    [LabelText("半径")]
    public float radius = 0.5f;

    /// <summary>
    /// 胶囊总高度（米，含上下半球）。
    /// 与 <see cref="radius"/> 共同决定 Motor 胶囊几何；Y 轴缩放由实体 Transform 在运行时另行处理。
    /// </summary>
    [LabelText("高度")]
    public float height = 2f;

    /// <summary>
    /// 胶囊中心相对实体原点的局部偏移（世界 authoring 空间，米）。
    /// 仅 <c>center.y</c> 会作为 Motor 的 <c>yOffset</c> 使用，用于将脚底对齐实体 pivot。
    /// </summary>
    [LabelText("中心偏移")]
    public Vector3 center;

    /// <summary>
    /// 可跨越的最大台阶高度（米）。
    /// 对应 KCC 标准台阶处理；超出时角色无法自动抬升，需逻辑层绕行或跳跃。
    /// 当前由 Motor 台阶策略读取（与 Unity CharacterController.stepOffset 语义一致）。
    /// </summary>
    [LabelText("可跨越的最大台阶高度")]
    public float stepOffset = 0.3f;

    /// <summary>
    /// 可行走的最大斜坡角度（度）。
    /// 超过该角度的表面在地面稳定性判定中视为不可站立坡面，角色会滑落或无法贴地吸附。
    /// </summary>
    [LabelText("可行走的最大斜坡角度")]
    public float slopeLimit = 45f;

    /// <summary>
    /// 碰撞皮肤厚度（米）。
    /// 用于 sweep 时提前留出安全间隙，减少与薄墙/共面几何的抖动与穿透；值过小易穿模，过大易悬空。
    /// </summary>
    [LabelText("碰撞皮肤厚度")]
    public float skinWidth = 0.08f;

    /// <summary>
    /// Motor 胶囊注册的碰撞层。
    /// 决定与哪些层发生物理阻挡查询；英雄默认 <see cref="FPCollisionLayer.Hero"/>，
    /// 需与地图墙、怪物等层的 <see cref="FPLayerMask"/> 过滤规则一致以保证确定性。
    /// </summary>
    [LabelText("碰撞层")]
    public FPCollisionLayer layer = FPCollisionLayer.Hero;

    /// <summary>
    /// 是否受重力影响。为 true 时 Motor 在离地期间对 <see cref="FPKinematicCharacterMotor.BaseVelocity"/> 的 Y 分量积分重力。
    /// </summary>
    [LabelText("启用重力")]
    public bool useGravity = true;

    /// <summary>
    /// 世界 Y 轴重力加速度（米/秒²），通常为负值。为 0 时使用 <see cref="FPMathKCC.DefaultGravity"/>。
    /// </summary>
    [LabelText("重力加速度 Y")]
    [ShowIf("useGravity")]
    public float gravity = -9.81f;

    /// <summary>
    /// 物理碰撞/Motor 回写时可改写的 Transform 轴。未勾选轴免疫物理侧位移/旋转，逻辑层仍可写入。
    /// </summary>
    [FoldoutGroup("物理碰撞影响")]
    [HideLabel]
    public PhysicsMotionInfluence collisionInfluence = PhysicsMotionInfluence.CreateCharacterControllerDefault();
}
