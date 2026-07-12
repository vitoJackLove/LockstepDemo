using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Rigidbody 范式配置：实体级刚体类型 + 任意数量复合碰撞体。
/// <para>
/// 用于 <see cref="PhysicsMovementMode.Rigidbody"/> 实体（通常为怪物、地图块）：
/// <see cref="PhysicsBodyComponent"/> 按 <see cref="bodyType"/> 决定是否创建
/// <see cref="FPDynamicRigidbody"/>，并将 <see cref="colliders"/> 逐项实例化为
/// <see cref="IFPCollider"/> 注册到 <see cref="FPCollisionWorld"/>。
/// </para>
/// <para>
/// 怪物寻路仍直写 <c>Entity.transform</c>，本组件在 <c>OnFixedUpdate</c> 同步碰撞体位姿跟随 Transform，
/// 不参与 Motor sweep。Static 体仅注册碰撞体、不创建刚体对象。
/// </para>
/// </summary>
[Serializable]
[InlineProperty(LabelWidth = 120)]
public class PhysicsBodyConfig
{
    /// <summary>
    /// 实体级刚体类型（Static / Kinematic / Dynamic）。
    /// <list type="bullet">
    /// <item><see cref="PhysicsBodyType.Static"/>：碰撞体固定位姿，由地图或逻辑直接设置 Transform。</item>
    /// <item><see cref="PhysicsBodyType.Kinematic"/>：Transform 驱动，可阻挡 Dynamic，典型怪物身体盒。</item>
    /// <item><see cref="PhysicsBodyType.Dynamic"/>：预留物理推挤；当前以注册为主，完整 solver 可能未启用。</item>
    /// </list>
    /// </summary>
    [LabelText("刚体类型")]
    public PhysicsBodyType bodyType = PhysicsBodyType.Kinematic;

    /// <summary>
    /// 是否受重力影响。Static 体忽略；Kinematic / Dynamic 在 <see cref="PhysicsBodyComponent"/> 中积分竖直速度并贴地。
    /// </summary>
    [LabelText("启用重力")]
    [ShowIf("@bodyType != PhysicsBodyType.Static")]
    public bool useGravity = true;

    /// <summary>
    /// 世界 Y 轴重力加速度（米/秒²），通常为负值。
    /// </summary>
    [LabelText("重力加速度 Y")]
    [ShowIf("@bodyType != PhysicsBodyType.Static && useGravity")]
    public float gravity = -9.81f;

    /// <summary>
    /// 物理碰撞/Motor 回写时可改写的 Transform 轴。未勾选轴免疫物理侧变化，寻路与行为树等逻辑层仍可写入。
    /// </summary>
    [FoldoutGroup("物理碰撞影响")]
    [ShowIf("@bodyType != PhysicsBodyType.Static")]
    [HideLabel]
    public PhysicsMotionInfluence collisionInfluence = PhysicsMotionInfluence.CreateRigidbodyDefault();

    /// <summary>
    /// 复合碰撞体列表。每项对应一个 <see cref="PhysicsColliderSetting"/>，可混合 Box / Sphere / Capsule。
    /// 列表为空时 <see cref="PhysicsBodyComponent"/> 会记录错误并跳过 Rigidbody 注册。
    /// 顺序与运行时 <see cref="PhysicsBodyComponent.Colliders"/> 索引一致，用于每帧位姿同步。
    /// </summary>
    [LabelText("复合碰撞体")]
    [ListDrawerSettings(
        ShowIndexLabels = true,
        ListElementLabelName = "key",
        AlwaysAddDefaultValue = true,
        DraggableItems = true,
        CustomAddFunction = nameof(AddDefaultCollider))]
    public List<PhysicsColliderSetting> colliders = new List<PhysicsColliderSetting>();

    /// <summary>
    /// 注册 <see cref="FPPhysicsMover"/> 运动学平台，供角色站立时继承平台速度。
    /// 典型用于升降台、往复移动地板；需配合逻辑层每帧更新实体 Transform。
    /// </summary>
    [LabelText("运动学平台")]
    [Tooltip("启用后注册 FPPhysicsMover，并将碰撞体 AttachedMover 指向该平台。")]
    public bool usePhysicsMover;

#if UNITY_EDITOR
    private PhysicsColliderSetting AddDefaultCollider()
    {
        int index = colliders == null ? 0 : colliders.Count;
        return new PhysicsColliderSetting
        {
            key = index == 0 ? "body" : $"body_{index}",
            shape = PhysicsShapeType.Box,
            halfExtents = new Vector3(0.5f, 1f, 0.5f),
            layer = FPCollisionLayer.Default,
        };
    }
#endif
}

/// <summary>
/// 单个复合碰撞体的 Authoring 数据（Inspector / ScriptableObject 序列化）。
/// <para>
/// 由 <see cref="PhysicsConfigConverter.CreateCollider"/> 转为具体 <see cref="IFPCollider"/> 实现，
/// 再由 <see cref="PhysicsBodyComponent"/> 根据实体 Transform 每帧调用 <c>SyncFromTransform</c>。
/// </para>
/// </summary>
[Serializable]
[InlineProperty(LabelWidth = 100)]
public class PhysicsColliderSetting
{
    /// <summary>
    /// 碰撞体在实体内的逻辑标识（如 "body"、"head"）。
    /// 仅用于配置辨识与调试日志，不参与物理查询；同一实体内建议唯一。
    /// </summary>
    [LabelText("标识")]
    public string key = "body";

    /// <summary>
    /// 碰撞体形状类型。决定读取下方哪组尺寸字段及实例化的 <see cref="IFPCollider"/> 类型。
    /// </summary>
    [LabelText("形状")]
    public PhysicsShapeType shape = PhysicsShapeType.Box;

    /// <summary>
    /// 相对实体原点的局部位置偏移（米）。
    /// 运行时转为 <c>fp3</c>，与实体旋转共同计算碰撞体世界中心。
    /// </summary>
    [LabelText("中心偏移")]
    public Vector3 localOffset;

    /// <summary>
    /// 相对实体朝向的局部欧拉角（度，XYZ 顺序）。
    /// 经 <see cref="PhysicsConfigConverter.ToLocalRotation"/> 转为定点四元数后与实体世界旋转相乘。
    /// </summary>
    [LabelText("本地欧拉角")]
    public Vector3 localEuler;

    /// <summary>
    /// 该碰撞体注册的碰撞层。
    /// 参与 <see cref="FPCollisionWorld"/> 查询时的层过滤；怪物身体常用 <see cref="FPCollisionLayer.Monster"/>，
    /// 地图墙常用 <see cref="FPCollisionLayer.Wall"/>。
    /// </summary>
    [LabelText("碰撞层")]
    public FPCollisionLayer layer = FPCollisionLayer.Default;

    /// <summary>
    /// 是否为触发器。为 true 时不参与 Motor/刚体的物理阻挡解算，仅可用于重叠检测类逻辑（若查询侧支持）。
    /// </summary>
    [LabelText("Trigger")]
    public bool isTrigger;

    /// <summary>
    /// 盒体半尺寸（米，各轴从中心到面的距离）。
    /// 仅当 <see cref="shape"/> 为 <see cref="PhysicsShapeType.Box"/> 时生效；
    /// 运行时乘以实体 <c>LocalScale</c> 绝对值得到世界半尺寸。
    /// </summary>
    [LabelText("盒体半尺寸")]
    [ShowIf("shape", PhysicsShapeType.Box)]
    public Vector3 halfExtents = new Vector3(0.5f, 1f, 0.5f);

    /// <summary>
    /// 球体半径（米）。仅当 <see cref="shape"/> 为 <see cref="PhysicsShapeType.Sphere"/> 时生效；
    /// 运行时取 scale 三轴绝对值最大值缩放半径。
    /// </summary>
    [LabelText("球体半径")]
    [ShowIf("shape", PhysicsShapeType.Sphere)]
    public float radius = 0.5f;

    /// <summary>
    /// 胶囊圆柱段半径（米，不含半球帽）。仅 Capsule 形状生效；X/Z scale 影响半径。
    /// </summary>
    [LabelText("胶囊半径")]
    [ShowIf("shape", PhysicsShapeType.Capsule)]
    public float capsuleRadius = 0.4f;

    /// <summary>
    /// 胶囊总高度（米，含上下半球）。仅 Capsule 形状生效；Y scale 影响高度。
    /// </summary>
    [LabelText("胶囊高度")]
    [ShowIf("shape", PhysicsShapeType.Capsule)]
    public float capsuleHeight = 1.8f;

    /// <summary>
    /// 胶囊主轴方向：0=X，1=Y（默认人形竖直），2=Z。
    /// 传入 <see cref="FPCapsuleCollider"/> 的 <c>directionAxis</c>，影响 <see cref="FPCapsuleShape"/> 几何构建。
    /// </summary>
    [LabelText("胶囊轴 (0=X,1=Y,2=Z)")]
    [ShowIf("shape", PhysicsShapeType.Capsule)]
    public int directionAxis = 1;
}
