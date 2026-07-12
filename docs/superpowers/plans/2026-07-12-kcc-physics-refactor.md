# KCC 物理系统重构实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 KCC 物理重构为 Unity 对齐的双范式配置驱动架构（CharacterController 单胶囊 Motor **或** Rigidbody + Compound Colliders），由 Hero/Monster 资产配置 `movementMode`，保持受击盒独立、怪物寻路直写 Transform、全链路 `fp` 确定性。

**Architecture:** 在 `Assets/Scripts/RunTime/KCC/` 补全 Sphere/Capsule `IFPCollider` 与相交检测；新增 `PhysicsMovementMode` / `CharacterControllerSettings` / `PhysicsBodyConfig` 配置层；用 `PhysicsBodyComponent` 替换 `KccRegistrationComponent`，按 `movementMode` 分支注册 Motor 或 N 个碰撞体；实体 `OnInit` 从 `HeroAssetsConfig` / `MonsterAssetsConfig` 读取配置，`MoveComponent` 仅 CharacterController 范式绑定 Motor；编辑器工厂与 Gizmo 预览同步新字段。

**Tech Stack:** Unity 2023.2、定点数 `Unity.Mathematics.FixedPoint`、Odin Inspector、ECS partial class、NUnit EditMode 测试、`dotnet build Assembly-CSharp.csproj`

**Spec:** [`docs/superpowers/specs/2026-07-12-kcc-physics-refactor-design.md`](../specs/2026-07-12-kcc-physics-refactor-design.md)

## Global Constraints

- 帧同步：战斗逻辑全部使用 `fp` / `fp3` / `fpquaternion`，禁止浮点参与确定性运算
- 禁用 Unity 原生 Rigidbody/Collider 做战斗逻辑
- 每个实体 **择一** 移动范式：`CharacterController`（Motor 单胶囊）或 `Rigidbody`（`PhysicsBodyConfig` + 复合碰撞体），禁止同一实体叠 Motor 胶囊 + physicsBody 碰撞核
- `bodyType`（Static / Kinematic / Dynamic）在实体级 `PhysicsBodyConfig`，不在单个 Collider 上
- Kinematic 范式碰撞体形状不限（Box / Sphere / Capsule 任意数量组合）
- 仅 CharacterController 范式要求单一胶囊
- `colliderDataList` → `ColliderComponent` → `VolumeSystem` 受击盒路径 **不变**
- 怪物维持寻路 + Transform 直写 + 碰撞体跟随，**无** Motor 解算
- 配置入口：`HeroAssetsConfig` / `MonsterAssetsConfig`，经配置中心与快速创建窗口编辑
- 新增公共 API 需中文 XML 注释；Inspector 用 Odin `[LabelText]` / `[ShowIf]`
- **不在范围：** Plane/Mesh 碰撞体、Motor+PhysicsBody 重叠身体、怪物主动推挤解析、合并受击/物理配置、Survivor 等其他模式

---

## File Map

| 文件 | 操作 | 职责 |
|------|------|------|
| `Assets/Scripts/RunTime/KCC/Physics/FPPhysicsTypes.cs` | Modify | 新增 `FPShapeType.Sphere`、`FPSphereShape` |
| `Assets/Scripts/RunTime/KCC/Physics/IFPCollider.cs` | Modify | 新增 `GetSphereShape()` |
| `Assets/Scripts/RunTime/KCC/Physics/FPSphereCollider.cs` | **Create** | 球体 `IFPCollider` 实现 |
| `Assets/Scripts/RunTime/KCC/Physics/FPCapsuleCollider.cs` | **Create** | 胶囊 `IFPCollider` 实现（区别于静态工具类 `FPCapsuleCollision`） |
| `Assets/Scripts/RunTime/KCC/Physics/FPCapsuleCollision.cs` | Modify | 补全 Sphere 相交 / 穿透 / Raycast |
| `Assets/Scripts/RunTime/KCC/Physics/PhysicsMovementMode.cs` | **Create** | `PhysicsMovementMode`、`PhysicsBodyType`、`PhysicsShapeType` 枚举 |
| `Assets/Scripts/RunTime/KCC/Physics/CharacterControllerSettings.cs` | **Create** | CC 范式 Inspector 配置（单胶囊字段） |
| `Assets/Scripts/RunTime/KCC/Physics/PhysicsBodyConfig.cs` | **Create** | Rigidbody 范式配置 + `PhysicsColliderSetting` |
| `Assets/Scripts/RunTime/KCC/Physics/PhysicsConfigConverter.cs` | **Create** | Editor `Vector3`/`float` → 运行时 `fp` 转换 |
| `Assets/Scripts/RunTime/ECS/Component/PhysicsBodyComponent.cs` | **Create** | 统一物理注册（替代 `KccRegistrationComponent`） |
| `Assets/Scripts/RunTime/ECS/Component/KccRegistrationComponent.cs` | Delete (Task 16) | 旧注册组件 |
| `Assets/Scripts/RunTime/KCC/KccBodySettings.cs` | Delete (Task 16) | 旧 `KccRegistrationKind` 配置 |
| `Assets/Scripts/RunTime/ECS/Component/ComponentDataKey.cs` | Modify | `KccBodyData` → `PhysicsBodyData`（保留旧常量 Obsolete 别名可选） |
| `Assets/Scripts/RunTime/ECS/Component/MoveComponent.cs` | Modify | 引用 `PhysicsBodyComponent.Motor` |
| `Assets/Scripts/RunTime/EntityAssetsConfig/HeroAssets.cs` | Modify | `movementMode` + CC/Body 配置字段 |
| `Assets/Scripts/RunTime/EntityAssetsConfig/MonsterAssets.cs` | Modify | 同上 |
| `Assets/Scripts/RunTime/ECS/Entity/HeroEntity.cs` | Modify | 组件类型 + `OnInit` 物理配置注入 |
| `Assets/Scripts/RunTime/ECS/Entity/MonsterEntity.cs` | Modify | 同上 |
| `Assets/Scripts/RunTime/ECS/Entity/CubeEntity.cs` | Modify | 组件类型替换 |
| `Assets/Scripts/Editor/HeroFactory/HeroQuickCreateWindow.cs` | Modify | 默认 CC 物理段 |
| `Assets/Scripts/Editor/MonsterFactory/MonsterQuickCreateWindow.cs` | Modify | 默认 Rigidbody Kinematic 物理段 |
| `Assets/Scripts/Editor/PhysicsBody/PhysicsBodyConfigGizmoDrawer.cs` | **Create** | Scene/Inspector Gizmo 预览复合碰撞体 |
| `Assets/Scripts/Test/EditMode/KCC/FPSphereCollisionTests.cs` | **Create** | Sphere 相交 EditMode 测试 |
| `Assets/Scripts/Test/EditMode/KCC/FPCapsuleColliderTests.cs` | **Create** | Capsule collider 形状测试 |
| `Assets/Scripts/Test/EditMode/KCC/PhysicsConfigConverterTests.cs` | **Create** | 配置转换确定性测试 |
| `Assets/GameAssetConfig/HeroAssets.asset` | Modify | 现有英雄默认 `movementMode=CharacterController` |
| `Assets/GameAssetConfig/MonsterAssets.asset` | Modify | 现有怪物默认 `movementMode=Rigidbody` + Kinematic Box |

**系统更新顺序（保持不变，无需改 `RogueWorld.GetSystemTypes` 顺序）：**

1. `EntitySystem` — 寻路 / AI 写 Transform  
2. `PhysicsBodyComponent.OnFixedUpdate` — Kinematic/Static 同步碰撞体位姿  
3. `FPKinematicCharacterSystem` — 仅 CharacterController 实体 Motor Simulate  

---

## Phase 1 — 碰撞原语补全

### Task 1: FPSphereShape 与 IFPCollider 扩展

**Files:**
- Modify: `Assets/Scripts/RunTime/KCC/Physics/FPPhysicsTypes.cs`
- Modify: `Assets/Scripts/RunTime/KCC/Physics/IFPCollider.cs`
- Modify: `Assets/Scripts/RunTime/KCC/Physics/FPBoxCollider.cs`（补 `GetSphereShape` 默认实现）

**Interfaces:**
- Produces: `FPShapeType.Sphere = 3`
- Produces: `struct FPSphereShape { fp3 Center; fp Radius; }`
- Produces: `IFPCollider.GetSphereShape()`

- [ ] **Step 1: 扩展 FPPhysicsTypes**

在 `FPShapeType` 枚举 `Plane = 2` 之后追加：

```csharp
/// <summary>球体。</summary>
Sphere = 3,
```

在文件末尾追加：

```csharp
/// <summary>
/// 球体碰撞形状（世界空间）。
/// </summary>
public struct FPSphereShape
{
    /// <summary>球心世界坐标。</summary>
    public fp3 Center;

    /// <summary>球体半径。</summary>
    public fp Radius;
}
```

- [ ] **Step 2: 扩展 IFPCollider**

```csharp
/// <summary>
/// 获取球体形状数据。
/// </summary>
/// <returns>球体形状。</returns>
FPSphereShape GetSphereShape();
```

- [ ] **Step 3: FPBoxCollider 补默认实现**

```csharp
public FPSphereShape GetSphereShape() => default;
```

- [ ] **Step 4: 构建验证**

Run: `dotnet build Assembly-CSharp.csproj -nologo -v:minimal`  
Expected: BUILD SUCCESS（`FPCapsuleCollision` 等现有 switch 需补 `Sphere` case 或 default，本 Task 仅保证编译；Task 4 补全相交）

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/RunTime/KCC/Physics/FPPhysicsTypes.cs \
        Assets/Scripts/RunTime/KCC/Physics/IFPCollider.cs \
        Assets/Scripts/RunTime/KCC/Physics/FPBoxCollider.cs
git commit -m "feat(kcc): add FPSphereShape and IFPCollider sphere API"
```

---

### Task 2: FPSphereCollider 实现

**Files:**
- Create: `Assets/Scripts/RunTime/KCC/Physics/FPSphereCollider.cs`
- Create: `Assets/Scripts/RunTime/KCC/Physics/FPSphereCollider.cs.meta`（Unity 自动生成，提交时一并纳入）
- Create: `Assets/Scripts/Test/EditMode/KCC/FPSphereCollisionTests.cs`

**Interfaces:**
- Consumes: `IFPCollider.GetSphereShape()`, `FPCollisionWorld.AllocateColliderId()`
- Produces: `FPSphereCollider` 类，公开 `SyncFromTransform(fp3 position, fpquaternion rotation, fp3 localCenter, fp baseRadius, fp3 scale)`

- [ ] **Step 1: 写失败测试**

```csharp
// Assets/Scripts/Test/EditMode/KCC/FPSphereCollisionTests.cs
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
            Assert.AreEqual((fp)1, shape.Radius); // max(abs(scale)) * baseRadius
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
    }
}
```

- [ ] **Step 2: 运行测试确认 FAIL**

Run: Unity Test Runner → EditMode → `FPSphereCollisionTests`  
Expected: FAIL（`FPSphereCollider` 不存在；`OverlapCapsule` 对 Sphere 返回 false）

- [ ] **Step 3: 实现 FPSphereCollider**

```csharp
// Assets/Scripts/RunTime/KCC/Physics/FPSphereCollider.cs
using Unity.Mathematics.FixedPoint;

/// <summary>
/// 定点数球体碰撞体，供 KCC 查询与角色阻挡。
/// </summary>
public sealed class FPSphereCollider : IFPCollider
{
    private readonly int _id;
    private fp3 _localCenter;
    private fp _radius;
    private fp3 _position;
    private fpquaternion _rotation;

    /// <summary>
    /// 创建球体碰撞体。
    /// </summary>
    public FPSphereCollider(FPCollisionLayer layer, fp3 localCenter, fp radius, bool isTrigger = false)
    {
        _id = FPCollisionWorld.Instance.AllocateColliderId();
        Layer = layer;
        _localCenter = localCenter;
        _radius = radius;
        IsTrigger = isTrigger;
        _rotation = fpquaternion.identity;
        _position = fp3.zero;
    }

    public int Id => _id;
    public FPShapeType ShapeType => FPShapeType.Sphere;
    public FPCollisionLayer Layer { get; set; }
    public bool IsTrigger { get; set; }
    public FPDynamicRigidbody AttachedBody { get; set; }
    public FPPhysicsMover AttachedMover { get; set; }
    public fp3 Position => _position;
    public fpquaternion Rotation => _rotation;

    /// <summary>
    /// 从实体 Transform 同步世界位姿与缩放后半径。
    /// </summary>
    public void SyncFromTransform(fp3 position, fpquaternion rotation, fp3 localCenter, fp baseRadius, fp3 scale)
    {
        _localCenter = localCenter;
        _rotation = rotation;
        _position = position + rotation * ScaleVector(localCenter, scale);
        fp sx = fpmath.abs(scale.x);
        fp sy = fpmath.abs(scale.y);
        fp sz = fpmath.abs(scale.z);
        _radius = baseRadius * fpmath.max(sx, fpmath.max(sy, sz));
    }

    public void SetWorldPose(fp3 position, fpquaternion rotation)
    {
        _position = position;
        _rotation = rotation;
    }

    public FPSphereShape GetSphereShape()
    {
        return new FPSphereShape { Center = _position, Radius = _radius };
    }

    public FPBoxShape GetBoxShape() => default;
    public FPCapsuleShape GetCapsuleShape() => default;
    public FPPlaneShape GetPlaneShape() => default;

    private static fp3 ScaleVector(fp3 value, fp3 scale)
    {
        return new fp3(value.x * scale.x, value.y * scale.y, value.z * scale.z);
    }
}
```

- [ ] **Step 4: 构建验证**

Run: `dotnet build Assembly-CSharp.csproj -nologo -v:minimal`  
Expected: BUILD SUCCESS

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/RunTime/KCC/Physics/FPSphereCollider.cs \
        Assets/Scripts/Test/EditMode/KCC/FPSphereCollisionTests.cs
git commit -m "feat(kcc): add FPSphereCollider"
```

---

### Task 3: FPCapsuleCollider 实现

**Files:**
- Create: `Assets/Scripts/RunTime/KCC/Physics/FPCapsuleCollider.cs`
- Create: `Assets/Scripts/Test/EditMode/KCC/FPCapsuleColliderTests.cs`

**Interfaces:**
- Consumes: `FPCapsuleShape`, `FPMathKCC.BuildCapsuleGeometry`（可选复用）
- Produces: `FPCapsuleCollider.SyncFromTransform(..., fp radius, fp height, fp yOffset, int directionAxis)`

- [ ] **Step 1: 写失败测试**

```csharp
// Assets/Scripts/Test/EditMode/KCC/FPCapsuleColliderTests.cs
using NUnit.Framework;
using Unity.Mathematics.FixedPoint;

namespace Rogue.Tests.EditMode.KCC
{
    public class FPCapsuleColliderTests
    {
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
```

- [ ] **Step 2: 运行测试确认 FAIL**

Run: Unity EditMode → `FPCapsuleColliderTests`  
Expected: FAIL

- [ ] **Step 3: 实现 FPCapsuleCollider**

```csharp
// Assets/Scripts/RunTime/KCC/Physics/FPCapsuleCollider.cs
using Unity.Mathematics.FixedPoint;

/// <summary>
/// 定点数胶囊碰撞体，供 compound rigidbody 注册。
/// </summary>
public sealed class FPCapsuleCollider : IFPCollider
{
    private readonly int _id;
    private fp3 _localCenter;
    private fp _radius;
    private fp _height;
    private int _directionAxis;
    private fp3 _position;
    private fpquaternion _rotation;

    public FPCapsuleCollider(
        FPCollisionLayer layer,
        fp3 localCenter,
        fp radius,
        fp height,
        fp yOffset,
        int directionAxis = 1,
        bool isTrigger = false)
    {
        _id = FPCollisionWorld.Instance.AllocateColliderId();
        Layer = layer;
        _localCenter = localCenter + new fp3((fp)0, yOffset, (fp)0);
        _radius = radius;
        _height = height;
        _directionAxis = directionAxis;
        IsTrigger = isTrigger;
        _rotation = fpquaternion.identity;
    }

    public int Id => _id;
    public FPShapeType ShapeType => FPShapeType.Capsule;
    public FPCollisionLayer Layer { get; set; }
    public bool IsTrigger { get; set; }
    public FPDynamicRigidbody AttachedBody { get; set; }
    public FPPhysicsMover AttachedMover { get; set; }
    public fp3 Position => _position;
    public fpquaternion Rotation => _rotation;

    /// <summary>
    /// 从 Transform 同步位姿；Y 轴缩放影响高度与半径。
    /// </summary>
    public void SyncFromTransform(
        fp3 position,
        fpquaternion rotation,
        fp3 localCenter,
        fp baseRadius,
        fp baseHeight,
        fp yOffset,
        fp3 scale)
    {
        _localCenter = localCenter + new fp3((fp)0, yOffset, (fp)0);
        _position = position + rotation * new fp3(
            localCenter.x * scale.x,
            localCenter.y * scale.y,
            localCenter.z * scale.z);
        _rotation = rotation;
        _radius = baseRadius * fpmath.max(fpmath.abs(scale.x), fpmath.abs(scale.z));
        _height = baseHeight * fpmath.abs(scale.y);
    }

    public void SetWorldPose(fp3 position, fpquaternion rotation)
    {
        _position = position;
        _rotation = rotation;
    }

    public FPCapsuleShape GetCapsuleShape()
    {
        return new FPCapsuleShape
        {
            Center = _localCenter,
            Rotation = _rotation,
            Radius = _radius,
            Height = _height,
            DirectionAxis = _directionAxis,
        };
    }

    public FPBoxShape GetBoxShape() => default;
    public FPSphereShape GetSphereShape() => default;
    public FPPlaneShape GetPlaneShape() => default;
}
```

- [ ] **Step 4: 运行测试确认 PASS**

Run: Unity EditMode → `FPCapsuleColliderTests`  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/RunTime/KCC/Physics/FPCapsuleCollider.cs \
        Assets/Scripts/Test/EditMode/KCC/FPCapsuleColliderTests.cs
git commit -m "feat(kcc): add FPCapsuleCollider for compound bodies"
```

---

### Task 4: FPCapsuleCollision Sphere 相交检测

**Files:**
- Modify: `Assets/Scripts/RunTime/KCC/Physics/FPCapsuleCollision.cs`
- Modify: `Assets/Scripts/Test/EditMode/KCC/FPSphereCollisionTests.cs`（补分离 case）

**Interfaces:**
- Consumes: `FPSphereShape`, `FPSphereCollider`
- Produces: `FPCapsuleCollision.OverlapCapsule` / `TryComputePenetration` / `Raycast` 支持 `FPShapeType.Sphere`

- [ ] **Step 1: 写失败测试（分离 case）**

在 `FPSphereCollisionTests` 追加：

```csharp
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
```

- [ ] **Step 2: 运行测试确认 FAIL**

Expected: `CapsuleIntersectsSphere_Separated_ReturnsFalse` FAIL（当前 default 分支返回 false，重叠 case 仍 FAIL）

- [ ] **Step 3: 在 FPCapsuleCollision 补 Sphere 分支**

在 `OverlapCapsule`、`TryComputePenetration`、`Raycast` 的 switch 中追加 `case FPShapeType.Sphere:`，实现私有方法：

```csharp
private static bool CapsuleIntersectsSphere(FPCapsuleGeometry capsule, FPSphereShape sphere)
{
    fp3 c1;
    fp3 c2;
    fp dist = FPMathKCC.ClosestPointOnSegmentToPoint(
        capsule.BottomHemiCenter,
        capsule.TopHemiCenter,
        sphere.Center,
        out c1);
    return dist <= capsule.Radius + sphere.Radius;
}

private static bool CapsulePenetratesSphere(
    FPCapsuleGeometry capsule,
    FPSphereShape sphere,
    out fp3 direction,
    out fp distance)
{
    direction = fp3.zero;
    distance = (fp)0;
    fp3 closest;
    fp dist = FPMathKCC.ClosestPointOnSegmentToPoint(
        capsule.BottomHemiCenter,
        capsule.TopHemiCenter,
        sphere.Center,
        out closest);
    fp combined = capsule.Radius + sphere.Radius;
    if (dist >= combined)
    {
        return false;
    }
    direction = dist > (fp)0.0000001f
        ? fpmath.normalize(closest - sphere.Center)
        : FPMathKCC.WorldUp;
    distance = combined - dist;
    return true;
}
```

若 `FPMathKCC.ClosestPointOnSegmentToPoint` 不存在，在本 Task 同文件内用 `SegmentSegmentDistance` 推导或新增该 helper（保持 `fp` 运算）。

Raycast 对 Sphere：射线-球体二次方程，取 `[0, distance]` 内最近正根。

- [ ] **Step 4: 运行测试确认 PASS**

Run: Unity EditMode → `FPSphereCollisionTests`  
Expected: 全部 PASS

- [ ] **Step 5: 构建验证**

Run: `dotnet build Assembly-CSharp.csproj -nologo -v:minimal`  
Expected: BUILD SUCCESS

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/RunTime/KCC/Physics/FPCapsuleCollision.cs \
        Assets/Scripts/Test/EditMode/KCC/FPSphereCollisionTests.cs \
        Assets/Scripts/RunTime/KCC/Physics/FPMathKCC.cs
git commit -m "feat(kcc): add sphere intersection to FPCapsuleCollision"
```

---

## Phase 2 — 配置结构

### Task 5: 物理枚举与配置类

**Files:**
- Create: `Assets/Scripts/RunTime/KCC/Physics/PhysicsMovementMode.cs`
- Create: `Assets/Scripts/RunTime/KCC/Physics/CharacterControllerSettings.cs`
- Create: `Assets/Scripts/RunTime/KCC/Physics/PhysicsBodyConfig.cs`

**Interfaces:**
- Produces: `PhysicsMovementMode`, `PhysicsBodyType`, `PhysicsShapeType`
- Produces: `CharacterControllerSettings`, `PhysicsBodyConfig`, `PhysicsColliderSetting`

- [ ] **Step 1: 创建枚举文件**

```csharp
// Assets/Scripts/RunTime/KCC/Physics/PhysicsMovementMode.cs

/// <summary>
/// 实体物理移动范式，与 Unity CharacterController / Rigidbody 对齐。
/// </summary>
public enum PhysicsMovementMode
{
    /// <summary>Motor 内置单胶囊（对齐 Unity Character Controller）。</summary>
    CharacterController = 0,

    /// <summary>PhysicsBodyConfig + 复合碰撞体（对齐 Unity Rigidbody）。</summary>
    Rigidbody = 1,
}

/// <summary>
/// 刚体类型，定义在实体级 PhysicsBodyConfig。
/// </summary>
public enum PhysicsBodyType
{
    /// <summary>固定位姿，无 Rigidbody 模拟。</summary>
    Static = 0,

    /// <summary>Transform 驱动，可推 Dynamic，不被 Dynamic 推。</summary>
    Kinematic = 1,

    /// <summary>物理模拟，可被 Kinematic 推。</summary>
    Dynamic = 2,
}

/// <summary>
/// 复合碰撞体形状类型。
/// </summary>
public enum PhysicsShapeType
{
    Box = 0,
    Sphere = 1,
    Capsule = 2,
}
```

- [ ] **Step 2: CharacterControllerSettings**

```csharp
// Assets/Scripts/RunTime/KCC/Physics/CharacterControllerSettings.cs
using System;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Character Controller 范式配置（单一胶囊，对齐 Unity CC Inspector）。
/// </summary>
[Serializable]
public class CharacterControllerSettings
{
    [LabelText("半径")]
    public float radius = 0.5f;

    [LabelText("高度")]
    public float height = 2f;

    [LabelText("中心偏移")]
    public Vector3 center;

    [LabelText("Step Offset")]
    public float stepOffset = 0.3f;

    [LabelText("Slope Limit")]
    public float slopeLimit = 45f;

    [LabelText("Skin Width")]
    public float skinWidth = 0.08f;

    [LabelText("碰撞层")]
    public FPCollisionLayer layer = FPCollisionLayer.Hero;
}
```

- [ ] **Step 3: PhysicsBodyConfig**

```csharp
// Assets/Scripts/RunTime/KCC/Physics/PhysicsBodyConfig.cs
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Rigidbody 范式配置：实体级 bodyType + N 个复合碰撞体。
/// </summary>
[Serializable]
public class PhysicsBodyConfig
{
    [LabelText("刚体类型")]
    public PhysicsBodyType bodyType = PhysicsBodyType.Kinematic;

    [LabelText("复合碰撞体")]
    public List<PhysicsColliderSetting> colliders = new List<PhysicsColliderSetting>();
}

/// <summary>
/// 单个复合碰撞体 Authoring 数据。
/// </summary>
[Serializable]
public class PhysicsColliderSetting
{
    [LabelText("标识")]
    public string key = "body";

    [LabelText("形状")]
    public PhysicsShapeType shape = PhysicsShapeType.Box;

    [LabelText("本地偏移")]
    public Vector3 localOffset;

    [LabelText("本地欧拉角")]
    public Vector3 localEuler;

    [LabelText("碰撞层")]
    public FPCollisionLayer layer = FPCollisionLayer.Default;

    [LabelText("Trigger")]
    public bool isTrigger;

    [LabelText("盒体半尺寸")]
    [ShowIf("shape", PhysicsShapeType.Box)]
    public Vector3 halfExtents = new Vector3(0.5f, 1f, 0.5f);

    [LabelText("球体半径")]
    [ShowIf("shape", PhysicsShapeType.Sphere)]
    public float radius = 0.5f;

    [LabelText("胶囊半径")]
    [ShowIf("shape", PhysicsShapeType.Capsule)]
    public float capsuleRadius = 0.4f;

    [LabelText("胶囊高度")]
    [ShowIf("shape", PhysicsShapeType.Capsule)]
    public float capsuleHeight = 1.8f;

    [LabelText("胶囊轴 (0=X,1=Y,2=Z)")]
    [ShowIf("shape", PhysicsShapeType.Capsule)]
    public int directionAxis = 1;
}
```

- [ ] **Step 4: 构建验证**

Run: `dotnet build Assembly-CSharp.csproj -nologo -v:minimal`  
Expected: BUILD SUCCESS

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/RunTime/KCC/Physics/PhysicsMovementMode.cs \
        Assets/Scripts/RunTime/KCC/Physics/CharacterControllerSettings.cs \
        Assets/Scripts/RunTime/KCC/Physics/PhysicsBodyConfig.cs
git commit -m "feat(kcc): add physics movement mode and body config types"
```

---

### Task 6: PhysicsConfigConverter 运行时转换

**Files:**
- Create: `Assets/Scripts/RunTime/KCC/Physics/PhysicsConfigConverter.cs`
- Create: `Assets/Scripts/Test/EditMode/KCC/PhysicsConfigConverterTests.cs`

**Interfaces:**
- Produces: `PhysicsConfigConverter.ToFp3(Vector3)`, `ToCharacterMotorDimensions(CharacterControllerSettings)`, `CreateCollider(IFPCollider host, PhysicsColliderSetting, fp3 scale)`

- [ ] **Step 1: 写失败测试**

```csharp
// Assets/Scripts/Test/EditMode/KCC/PhysicsConfigConverterTests.cs
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
    }
}
```

- [ ] **Step 2: 运行测试确认 FAIL**

Expected: FAIL

- [ ] **Step 3: 实现 PhysicsConfigConverter**

```csharp
// Assets/Scripts/RunTime/KCC/Physics/PhysicsConfigConverter.cs
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
```

- [ ] **Step 4: 运行测试确认 PASS**

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/RunTime/KCC/Physics/PhysicsConfigConverter.cs \
        Assets/Scripts/Test/EditMode/KCC/PhysicsConfigConverterTests.cs
git commit -m "feat(kcc): add PhysicsConfigConverter for fp runtime"
```

---

### Task 7: HeroAssets / MonsterAssets 配置字段

**Files:**
- Modify: `Assets/Scripts/RunTime/EntityAssetsConfig/HeroAssets.cs`
- Modify: `Assets/Scripts/RunTime/EntityAssetsConfig/MonsterAssets.cs`

**Interfaces:**
- Consumes: `PhysicsMovementMode`, `CharacterControllerSettings`, `PhysicsBodyConfig`
- Produces: `HeroAssetsConfig.movementMode`, `.characterController`, `.physicsBody`（Monster 同理）

- [ ] **Step 1: HeroAssetsConfig 追加字段**

在 `colliderDataList` **之前**插入（受击盒保持独立）：

```csharp
[LabelText("物理移动范式")]
public PhysicsMovementMode movementMode = PhysicsMovementMode.CharacterController;

[LabelText("Character Controller")]
[ShowIf("movementMode", PhysicsMovementMode.CharacterController)]
public CharacterControllerSettings characterController = new CharacterControllerSettings();

[LabelText("Rigidbody 物理体")]
[ShowIf("movementMode", PhysicsMovementMode.Rigidbody)]
public PhysicsBodyConfig physicsBody = new PhysicsBodyConfig();
```

- [ ] **Step 2: MonsterAssetsConfig 追加相同三字段**

默认值：

```csharp
public PhysicsMovementMode movementMode = PhysicsMovementMode.Rigidbody;

public PhysicsBodyConfig physicsBody = new PhysicsBodyConfig
{
    bodyType = PhysicsBodyType.Kinematic,
    colliders = new List<PhysicsColliderSetting>
    {
        new PhysicsColliderSetting
        {
            key = "body",
            shape = PhysicsShapeType.Box,
            halfExtents = new Vector3(0.5f, 1f, 0.5f),
            layer = FPCollisionLayer.Monster,
        }
    }
};
```

- [ ] **Step 3: 构建验证**

Run: `dotnet build Assembly-CSharp.csproj -nologo -v:minimal`  
Expected: BUILD SUCCESS

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/RunTime/EntityAssetsConfig/HeroAssets.cs \
        Assets/Scripts/RunTime/EntityAssetsConfig/MonsterAssets.cs
git commit -m "feat(kcc): add movementMode and physics settings to hero/monster configs"
```

---

## Phase 3 — PhysicsBodyComponent

### Task 8: PhysicsBodyComponent — CharacterController 分支

**Files:**
- Create: `Assets/Scripts/RunTime/ECS/Component/PhysicsBodyComponent.cs`
- Modify: `Assets/Scripts/RunTime/ECS/Component/ComponentDataKey.cs`

**Interfaces:**
- Consumes: `HeroAssetsConfig` / `MonsterAssetsConfig` 或 `Entity.GetData<PhysicsEntityConfig>(ComponentDataKey.PhysicsBodyData)`
- Produces: `PhysicsBodyComponent.Motor`, `PhysicsBodyComponent.MovementMode`
- Produces: `ComponentDataKey.PhysicsBodyData = "PhysicsBodyData"`

- [ ] **Step 1: 新增 ComponentDataKey**

```csharp
/// <summary>
/// 物理体注册配置（movementMode + CC/Body 设置）。
/// </summary>
public const string PhysicsBodyData = "PhysicsBodyData";
```

保留 `KccBodyData` 并标记 `[Obsolete("Use PhysicsBodyData")]`（迁移期可选）。

- [ ] **Step 2: 创建 PhysicsEntityConfig 运行时 DTO**

在同文件 `PhysicsBodyComponent.cs` 顶部或 `PhysicsBodyConfig.cs` 末尾：

```csharp
/// <summary>
/// 实体物理注册运行时配置（可由 Entity.SetData 注入，覆盖资产默认）。
/// </summary>
public class PhysicsEntityConfig
{
    public PhysicsMovementMode MovementMode;
    public CharacterControllerSettings CharacterController;
    public PhysicsBodyConfig PhysicsBody;
}
```

- [ ] **Step 3: 实现 PhysicsBodyComponent（仅 CC 分支 + 骨架）**

```csharp
// Assets/Scripts/RunTime/ECS/Component/PhysicsBodyComponent.cs
using System.Collections.Generic;
using Unity.Mathematics.FixedPoint;

/// <summary>
/// 统一物理注册：CharacterController（Motor 单胶囊）或 Rigidbody（复合碰撞体）。
/// </summary>
public class PhysicsBodyComponent : BaseComponent
{
    private static readonly FPNullCharacterController NullController = new FPNullCharacterController();

    private PhysicsEntityConfig _config;
    private FPKinematicCharacterMotor _motor;

    /// <summary>CharacterController 范式下的 Motor；Rigidbody 范式为 null。</summary>
    public FPKinematicCharacterMotor Motor => _motor;

    /// <summary>当前移动范式。</summary>
    public PhysicsMovementMode MovementMode => _config?.MovementMode ?? PhysicsMovementMode.CharacterController;

    public override void OnStart(object data = null)
    {
        base.OnStart(data);
        _config = ResolveConfig();

        if (_config.MovementMode == PhysicsMovementMode.CharacterController)
        {
            RegisterCharacterMotor();
        }
        // Rigidbody 分支在 Task 9 实现
    }

    public override void OnDispose()
    {
        UnregisterAll();
        base.OnDispose();
    }

    private PhysicsEntityConfig ResolveConfig()
    {
        PhysicsEntityConfig runtime = Entity.GetData<PhysicsEntityConfig>(ComponentDataKey.PhysicsBodyData);
        if (runtime != null)
        {
            return runtime;
        }

        if (Entity.Config is HeroAssetsConfig hero)
        {
            return new PhysicsEntityConfig
            {
                MovementMode = hero.movementMode,
                CharacterController = hero.characterController,
                PhysicsBody = hero.physicsBody,
            };
        }

        if (Entity.Config is MonsterAssetsConfig monster)
        {
            return new PhysicsEntityConfig
            {
                MovementMode = monster.movementMode,
                CharacterController = monster.characterController,
                PhysicsBody = monster.physicsBody,
            };
        }

        // CubeEntity 等：默认 Static Rigidbody 盒体（Task 11 细化）
        return PhysicsEntityConfig.CreateDefaultStaticBox(Entity);
    }

    private void RegisterCharacterMotor()
    {
        CharacterControllerSettings cc = _config.CharacterController ?? new CharacterControllerSettings();
        PhysicsConfigConverter.ToCharacterMotorDimensions(cc, out fp radius, out fp height, out fp yOffset);

        _motor = new FPKinematicCharacterMotor(NullController)
        {
            CharacterLayer = cc.layer,
        };
        _motor.SetCapsuleDimensions(radius, height, yOffset);
        _motor.SetPositionAndRotation(Entity.transform.Position, Entity.transform.Rotation);
        _motor.BindEntity(Entity);
        _motor.Register();
    }

    private void UnregisterAll()
    {
        if (_motor != null)
        {
            _motor.Unregister();
            _motor.UnbindEntity();
            _motor = null;
        }
    }
}
```

在 `PhysicsEntityConfig` 追加：

```csharp
public static PhysicsEntityConfig CreateDefaultStaticBox(BaseEntity entity)
{
    var body = new PhysicsBodyConfig
    {
        bodyType = PhysicsBodyType.Static,
        colliders = new List<PhysicsColliderSetting>
        {
            new PhysicsColliderSetting
            {
                key = "wall",
                shape = PhysicsShapeType.Box,
                halfExtents = new UnityEngine.Vector3(0.5f, 0.5f, 0.5f),
                layer = FPCollisionLayer.Wall,
            }
        }
    };
    return new PhysicsEntityConfig
    {
        MovementMode = PhysicsMovementMode.Rigidbody,
        PhysicsBody = body,
    };
}
```

- [ ] **Step 4: 构建验证**

Run: `dotnet build Assembly-CSharp.csproj -nologo -v:minimal`  
Expected: BUILD SUCCESS

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/RunTime/ECS/Component/PhysicsBodyComponent.cs \
        Assets/Scripts/RunTime/ECS/Component/ComponentDataKey.cs
git commit -m "feat(kcc): add PhysicsBodyComponent with CharacterController branch"
```

---

### Task 9: PhysicsBodyComponent — Rigidbody 复合碰撞体分支

**Files:**
- Modify: `Assets/Scripts/RunTime/ECS/Component/PhysicsBodyComponent.cs`

**Interfaces:**
- Consumes: `PhysicsConfigConverter.CreateCollider`, `FPCollisionWorld`, `FPDynamicRigidbody`, `FPPhysicsMover`（Kinematic 平台可选）
- Produces: `PhysicsBodyComponent.Colliders`（`IReadOnlyList<IFPCollider>`）

- [ ] **Step 1: 扩展字段与 OnStart 分支**

```csharp
private readonly List<IFPCollider> _colliders = new List<IFPCollider>(4);
private FPDynamicRigidbody _dynamicBody;
private FPPhysicsMover _physicsMover;
private FPEntityPhysicsMoverController _moverController;

public IReadOnlyList<IFPCollider> Colliders => _colliders;
public FPDynamicRigidbody DynamicBody => _dynamicBody;
```

`OnStart` 中 `else if (_config.MovementMode == PhysicsMovementMode.Rigidbody) RegisterRigidbodyBody();`

- [ ] **Step 2: 实现 RegisterRigidbodyBody**

```csharp
private void RegisterRigidbodyBody()
{
    PhysicsBodyConfig bodyConfig = _config.PhysicsBody;
    if (bodyConfig == null || bodyConfig.colliders == null || bodyConfig.colliders.Count == 0)
    {
        GameLog.Error(GameLogChannel.Battle,
            $"PhysicsBodyComponent Rigidbody mode requires colliders. entityId={Entity.EntityId}");
        return;
    }

    switch (bodyConfig.bodyType)
    {
        case PhysicsBodyType.Dynamic:
            _dynamicBody = new FPDynamicRigidbody
            {
                Id = FPCollisionWorld.Instance.AllocateBodyId(),
                IsKinematic = false,
            };
            _dynamicBody.SetPose(Entity.transform.Position, Entity.transform.Rotation);
            break;

        case PhysicsBodyType.Kinematic:
            _dynamicBody = new FPDynamicRigidbody
            {
                Id = FPCollisionWorld.Instance.AllocateBodyId(),
                IsKinematic = true,
            };
            _dynamicBody.SetPose(Entity.transform.Position, Entity.transform.Rotation);
            break;

        case PhysicsBodyType.Static:
        default:
            _dynamicBody = null;
            break;
    }

    fp3 scale = Entity.transform.LocalScale;
    for (int i = 0; i < bodyConfig.colliders.Count; i++)
    {
        PhysicsColliderSetting setting = bodyConfig.colliders[i];
        IFPCollider collider = PhysicsConfigConverter.CreateCollider(setting);
        if (collider == null)
        {
            continue;
        }

        SyncColliderFromEntity(collider, setting, scale);
        if (_dynamicBody != null)
        {
            collider.AttachedBody = _dynamicBody;
        }

        _colliders.Add(collider);
        FPCollisionWorld.Instance.RegisterCollider(collider);
    }

    SyncDynamicBodyPose();
}
```

- [ ] **Step 3: OnFixedUpdate 同步位姿**

```csharp
public override void OnFixedUpdate(fp deltaTime, WorldUpdateType worldUpdateType)
{
    base.OnFixedUpdate(deltaTime, worldUpdateType);
    if (_config.MovementMode != PhysicsMovementMode.Rigidbody)
    {
        return;
    }

    fp3 scale = Entity.transform.LocalScale;
    for (int i = 0; i < _colliders.Count; i++)
    {
        SyncColliderFromEntity(_colliders[i], _config.PhysicsBody.colliders[i], scale);
    }

    SyncDynamicBodyPose();
}

private void SyncColliderFromEntity(IFPCollider collider, PhysicsColliderSetting setting, fp3 scale)
{
    fp3 localOffset = PhysicsConfigConverter.ToFp3(setting.localOffset);
    fpquaternion localRot = fpquaternion.Euler(
        (fp)setting.localEuler.x * fpmath.Deg2Rad,
        (fp)setting.localEuler.y * fpmath.Deg2Rad,
        (fp)setting.localEuler.z * fpmath.Deg2Rad);
    fpquaternion worldRot = Entity.transform.Rotation * localRot;

    switch (collider.ShapeType)
    {
        case FPShapeType.Box:
            ((FPBoxCollider)collider).SyncFromTransform(
                Entity.transform.Position,
                worldRot,
                localOffset,
                PhysicsConfigConverter.ToFp3(setting.halfExtents),
                scale);
            break;
        case FPShapeType.Sphere:
            ((FPSphereCollider)collider).SyncFromTransform(
                Entity.transform.Position,
                worldRot,
                localOffset,
                (fp)setting.radius,
                scale);
            break;
        case FPShapeType.Capsule:
            ((FPCapsuleCollider)collider).SyncFromTransform(
                Entity.transform.Position,
                worldRot,
                localOffset,
                (fp)setting.capsuleRadius,
                (fp)setting.capsuleHeight,
                (fp)0,
                scale);
            break;
    }
}

private void SyncDynamicBodyPose()
{
    if (_dynamicBody == null)
    {
        return;
    }

    _dynamicBody.SetPose(Entity.transform.Position, Entity.transform.Rotation);
}
```

- [ ] **Step 4: UnregisterAll 扩展**

注销所有 `_colliders`、`FPCollisionWorld.UnregisterCollider`；Dynamic/Kinematic body 置空。

- [ ] **Step 5: 互斥校验**

在 `OnStart` 末尾：

```csharp
if (_config.MovementMode == PhysicsMovementMode.CharacterController &&
    _config.PhysicsBody?.colliders != null &&
    _config.PhysicsBody.colliders.Count > 0)
{
    GameLog.Warning(GameLogChannel.Battle,
        $"Entity {Entity.EntityId}: CharacterController mode ignores physicsBody.colliders per spec.");
}
```

- [ ] **Step 6: 构建验证**

Run: `dotnet build Assembly-CSharp.csproj -nologo -v:minimal`  
Expected: BUILD SUCCESS

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/RunTime/ECS/Component/PhysicsBodyComponent.cs
git commit -m "feat(kcc): add Rigidbody compound collider branch to PhysicsBodyComponent"
```

---

## Phase 4 — 实体与组件迁移

### Task 10: 实体组件类型替换

**Files:**
- Modify: `Assets/Scripts/RunTime/ECS/Entity/HeroEntity.cs`
- Modify: `Assets/Scripts/RunTime/ECS/Entity/MonsterEntity.cs`
- Modify: `Assets/Scripts/RunTime/ECS/Entity/CubeEntity.cs`

**Interfaces:**
- Consumes: `PhysicsBodyComponent`
- Produces: 实体 `GetComponentTypes()` 使用 `PhysicsBodyComponent` 替代 `KccRegistrationComponent`

- [ ] **Step 1: HeroEntity**

```csharp
// GetComponentTypes 中：
typeof(PhysicsBodyComponent),  // 替换 typeof(KccRegistrationComponent)
```

- [ ] **Step 2: MonsterEntity**

同上替换；**不**添加 `MoveComponent`（保持寻路直写 Transform）。

- [ ] **Step 3: CubeEntity**

同上替换；Cube 走 `CreateDefaultStaticBox` 或后续 Task 13 资产配置。

- [ ] **Step 4: 全局搜索残留**

Run: `rg "KccRegistrationComponent" Assets/Scripts/RunTime`  
Expected: 仅 `MoveComponent`、`KccRegistrationComponent.cs` 自身（Task 11/16 清理）

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/RunTime/ECS/Entity/HeroEntity.cs \
        Assets/Scripts/RunTime/ECS/Entity/MonsterEntity.cs \
        Assets/Scripts/RunTime/ECS/Entity/CubeEntity.cs
git commit -m "refactor(kcc): switch entities to PhysicsBodyComponent"
```

---

### Task 11: MoveComponent 与 Hero OnInit  wiring

**Files:**
- Modify: `Assets/Scripts/RunTime/ECS/Component/MoveComponent.cs`
- Modify: `Assets/Scripts/RunTime/ECS/Entity/HeroEntity.cs`

**Interfaces:**
- Consumes: `PhysicsBodyComponent.Motor`, `PhysicsMovementMode.CharacterController`

- [ ] **Step 1: MoveComponent 改引用**

```csharp
/// Motor 由 <see cref="PhysicsBodyComponent"/> 在 CharacterController 范式下创建。
_motor = Entity.GetComponent<PhysicsBodyComponent>()?.Motor;
if (_motor == null)
{
    GameLog.Error(GameLogChannel.Battle,
        $"MoveComponent requires PhysicsBodyComponent with CharacterController mode. entityId={Entity.EntityId}");
}
```

- [ ] **Step 2: HeroEntity 注入默认 CC 层**

在 `OnInit` 中（可选，若资产已配则跳过）：

```csharp
HeroAssetsConfig heroConfig = (HeroAssetsConfig)Config;
if (heroConfig.characterController.layer == FPCollisionLayer.Default)
{
    heroConfig.characterController.layer = FPCollisionLayer.Hero;
}
```

不在 `OnInit` 写 `SetData(KccBodyData)`；改由 `PhysicsBodyComponent.ResolveConfig` 直接读 `Entity.Config`。

- [ ] **Step 3: 构建验证**

Run: `dotnet build Assembly-CSharp.csproj -nologo -v:minimal`  
Expected: BUILD SUCCESS

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/RunTime/ECS/Component/MoveComponent.cs \
        Assets/Scripts/RunTime/ECS/Entity/HeroEntity.cs
git commit -m "refactor(kcc): wire MoveComponent to PhysicsBodyComponent"
```

---

## Phase 5 — 资产迁移与编辑器

### Task 12: GameAssetConfig 默认物理迁移

**Files:**
- Modify: `Assets/GameAssetConfig/HeroAssets.asset`（YAML，Unity 序列化）
- Modify: `Assets/GameAssetConfig/MonsterAssets.asset`

**说明:** ScriptableObject 资产需在 Unity Editor 中修改并 Save；若 CLI 不可直接编辑 YAML，在 Unity 打开配置中心批量设置。

- [ ] **Step 1: 英雄默认 CharacterController**

对每个 `HeroAssetsConfig` 条目：

- `movementMode = CharacterController (0)`
- `characterController`: radius=0.5, height=2, center=(0,1,0), layer=Hero
- `physicsBody.colliders` 清空（或忽略）

- [ ] **Step 2: 怪物默认 Rigidbody Kinematic Box**

对每个 `MonsterAssetsConfig` 条目：

- `movementMode = Rigidbody (1)`
- `physicsBody.bodyType = Kinematic (1)`
- `physicsBody.colliders[0]`: Box, halfExtents=(0.5,1,0.5), layer=Monster

- [ ] **Step 3: 验证 CubeEntity 地图块**

确认 `CubeEntity` 经 `CreateDefaultStaticBox` 或 MapSystem 传入 `PhysicsBodyData` 后 Static Wall 层正确注册。

- [ ] **Step 4: Commit**

```bash
git add Assets/GameAssetConfig/HeroAssets.asset Assets/GameAssetConfig/MonsterAssets.asset
git commit -m "chore(kcc): migrate hero/monster assets to new physics config"
```

---

### Task 13: HeroQuickCreateWindow 物理段

**Files:**
- Modify: `Assets/Scripts/Editor/HeroFactory/HeroQuickCreateWindow.cs`

**Interfaces:**
- Produces: 新建英雄 config 含 `movementMode=CharacterController` 与 `characterController` 默认值

- [ ] **Step 1: 新增 DrawPhysicsSection**

在 `DrawColliderSection`（受击盒）**之后**追加 `DrawPhysicsSection`：

```csharp
private PhysicsMovementMode _movementMode = PhysicsMovementMode.CharacterController;
private CharacterControllerSettings _characterController = new CharacterControllerSettings
{
    radius = 0.5f,
    height = 2f,
    center = new Vector3(0f, 1f, 0f),
    layer = FPCollisionLayer.Hero,
};

private void DrawPhysicsSection()
{
    EditorGUILayout.LabelField("物理体（KCC）", EditorStyles.boldLabel);
    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
    {
        _movementMode = (PhysicsMovementMode)EditorGUILayout.EnumPopup("移动范式", _movementMode);
        if (_movementMode == PhysicsMovementMode.CharacterController)
        {
            _characterController.radius = EditorGUILayout.FloatField("胶囊半径", _characterController.radius);
            _characterController.height = EditorGUILayout.FloatField("胶囊高度", _characterController.height);
            _characterController.center = EditorGUILayout.Vector3Field("中心偏移", _characterController.center);
            _characterController.layer = (FPCollisionLayer)EditorGUILayout.EnumFlagsField("碰撞层", _characterController.layer);
        }
        // Rigidbody 复合体编辑可后续迭代；本 Task 英雄默认 CC
    }
}
```

- [ ] **Step 2: BuildHeroConfig 写入新字段**

在创建 `HeroAssetsConfig` 处赋值：

```csharp
movementMode = _movementMode,
characterController = _characterController,
```

- [ ] **Step 3: OnGUI 调用 DrawPhysicsSection**

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Editor/HeroFactory/HeroQuickCreateWindow.cs
git commit -m "feat(editor): add physics section to hero quick create window"
```

---

### Task 14: MonsterQuickCreateWindow 物理段

**Files:**
- Modify: `Assets/Scripts/Editor/MonsterFactory/MonsterQuickCreateWindow.cs`

- [ ] **Step 1: 默认 Rigidbody Kinematic + Box**

```csharp
private PhysicsMovementMode _movementMode = PhysicsMovementMode.Rigidbody;
private PhysicsBodyConfig _physicsBody = new PhysicsBodyConfig
{
    bodyType = PhysicsBodyType.Kinematic,
    colliders = new List<PhysicsColliderSetting>
    {
        new PhysicsColliderSetting
        {
            key = "body",
            shape = PhysicsShapeType.Box,
            halfExtents = new Vector3(0.5f, 1f, 0.5f),
            layer = FPCollisionLayer.Monster,
        }
    },
};
```

- [ ] **Step 2: DrawPhysicsSection + BuildMonsterConfig 写入**

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Editor/MonsterFactory/MonsterQuickCreateWindow.cs
git commit -m "feat(editor): add rigidbody physics defaults to monster quick create"
```

---

### Task 15: 配置 Gizmo 预览

**Files:**
- Create: `Assets/Scripts/Editor/PhysicsBody/PhysicsBodyConfigGizmoDrawer.cs`

**Interfaces:**
- Consumes: `PhysicsBodyConfig`, `PhysicsColliderSetting`
- Produces: Scene 视图绘制复合碰撞体线框（Box/Sphere/Capsule）

- [ ] **Step 1: 实现 Odin 或 Editor 绘制**

```csharp
// Assets/Scripts/Editor/PhysicsBody/PhysicsBodyConfigGizmoDrawer.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 在 Scene 视图预览 PhysicsBodyConfig 复合碰撞体（仅 Editor）。
/// </summary>
[InitializeOnLoad]
public static class PhysicsBodyConfigGizmoDrawer
{
    // 挂到 ConfigCenter 选中项或 Hero/Monster Inspector 的 SceneGUI callback
    public static void DrawPhysicsBodyGizmos(PhysicsBodyConfig config, Matrix4x4 entityMatrix)
    {
        if (config?.colliders == null)
        {
            return;
        }

        for (int i = 0; i < config.colliders.Count; i++)
        {
            PhysicsColliderSetting c = config.colliders[i];
            Matrix4x4 local = Matrix4x4.TRS(c.localOffset, Quaternion.Euler(c.localEuler), Vector3.one);
            Matrix4x4 world = entityMatrix * local;
            using (new Handles.DrawingScope(world))
            {
                switch (c.shape)
                {
                    case PhysicsShapeType.Box:
                        Handles.DrawWireCube(Vector3.zero, c.halfExtents * 2f);
                        break;
                    case PhysicsShapeType.Sphere:
                        Handles.DrawWireDisc(Vector3.zero, Vector3.up, c.radius);
                        Handles.DrawWireDisc(Vector3.zero, Vector3.forward, c.radius);
                        Handles.DrawWireDisc(Vector3.zero, Vector3.right, c.radius);
                        break;
                    case PhysicsShapeType.Capsule:
                        // 简化为线框圆柱预览
                        Handles.DrawWireDisc(Vector3.zero, Vector3.up, c.capsuleRadius);
                        break;
                }
            }
        }
    }
}
#endif
```

- [ ] **Step 2: 在 HeroQuickCreateWindow / MonsterQuickCreateWindow 的 PreviewRenderUtility 回调中调用**

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Editor/PhysicsBody/PhysicsBodyConfigGizmoDrawer.cs \
        Assets/Scripts/Editor/HeroFactory/HeroQuickCreateWindow.cs \
        Assets/Scripts/Editor/MonsterFactory/MonsterQuickCreateWindow.cs
git commit -m "feat(editor): add physics body gizmo preview in factory windows"
```

---

## Phase 6 — 清理与验收

### Task 16: 删除遗留 KCC 注册代码

**Files:**
- Delete: `Assets/Scripts/RunTime/ECS/Component/KccRegistrationComponent.cs`
- Delete: `Assets/Scripts/RunTime/KCC/KccBodySettings.cs`
- Modify: `Assets/Scripts/RunTime/ECS/Component/ComponentDataKey.cs`（移除或 Obsolete `KccBodyData`）

- [ ] **Step 1: 确认零引用**

Run: `rg "KccRegistrationComponent|KccBodySettings|KccRegistrationKind|KccBodyData" Assets/Scripts`  
Expected: 无命中（或仅 Obsolete 注释）

- [ ] **Step 2: 删除文件及 .meta**

- [ ] **Step 3: 构建验证**

Run: `dotnet build Assembly-CSharp.csproj -nologo -v:minimal`  
Expected: BUILD SUCCESS

- [ ] **Step 4: Commit**

```bash
git add -A Assets/Scripts/RunTime/ECS/Component/KccRegistrationComponent.cs \
           Assets/Scripts/RunTime/KCC/KccBodySettings.cs \
           Assets/Scripts/RunTime/ECS/Component/ComponentDataKey.cs
git commit -m "refactor(kcc): remove legacy KccRegistrationComponent and KccBodySettings"
```

---

### Task 17: 验收与回归

**Files:**（只读验证，无代码变更除非发现缺陷）

- [ ] **Step 1: 全量构建**

Run: `dotnet build Assembly-CSharp.csproj -nologo -v:minimal`  
Expected: BUILD SUCCESS，0 Error

- [ ] **Step 2: EditMode 测试**

Run: Unity Test Runner → EditMode → `Rogue.Tests.EditMode.KCC.*`  
Expected: 全部 PASS

- [ ] **Step 3: PlayMode 冒烟（RogueWorld）**

1. `npm run server:single`（可选，单机可跳过联网）
2. Unity Play → Rogue 战斗场景
3. 英雄 WASD 移动：Motor 阻挡墙/怪物，无穿模
4. 怪物寻路移动：碰撞体跟随 Transform，**无** Motor 解算
5. 受击盒命中：技能/子弹仍触发 `VolumeSystem`

- [ ] **Step 4: 确定性 spot-check**

同一输入序列跑 100 tick，对比英雄 `transform.Position` fp 值两次一致（可在 `MoveComponent` 临时日志或现有 RollBack 指纹工具验证）。

- [ ] **Step 5: 验收清单对照 Spec**

| # | 验收项 | 验证方式 |
|---|--------|----------|
| 1 | Hero/Monster 可配 movementMode，不绑 EntityType | 配置中心改怪物为 CC、英雄为 RB，行为随配置变 |
| 2 | CC 模式单胶囊 + Motor Sweep | 英雄仅注册 Motor，无 physicsBody 碰撞核 |
| 3 | RB 模式一 body + N collider，Kinematic 形状不限 | 怪物多 Box/Sphere 组合可注册 |
| 4 | 受击盒不受影响 | colliderDataList 仍进 VolumeSystem |
| 5 | 怪物寻路 + 碰撞体跟随 | 无 MoveComponent / Motor |
| 6 | fp 确定性 | Step 4 |

- [ ] **Step 6: Commit（如有验收修复）**

```bash
git commit -m "test(kcc): complete physics refactor acceptance verification"
```

---

## Self-Review

### 1. Spec 覆盖

| Spec 要求 | 对应 Task |
|-----------|-----------|
| PhysicsMovementMode 二选一 | Task 5, 7, 8-9 |
| CharacterControllerSettings 单胶囊 | Task 5, 8 |
| PhysicsBodyConfig bodyType + compound colliders | Task 5, 9 |
| Kinematic 形状不限 | Task 2-4, 9 |
| colliderDataList 独立 | 未改 ColliderComponent（Task 17 验证 #4） |
| KccRegistrationComponent → PhysicsBodyComponent | Task 8-11, 16 |
| FPSphereCollider + FPCapsuleCollider + sphere 相交 | Task 2-4 |
| Hero/Monster 资产配置 | Task 7, 12-14 |
| 怪物寻路无 Motor | Task 10-11（Monster 无 MoveComponent） |
| 禁止 Motor + PhysicsBody 重叠身体 | Task 9 互斥校验 |
| fp 确定性 | Global Constraints + Task 6 + 17 |
| Gizmo 预览 | Task 15 |
| 不在范围项 | 未纳入任务 |

### 2. Spec 缺口与风险

| 项 | 说明 | 建议 |
|----|------|------|
| **KinematicPlatform 迁移** | 旧 `KccRegistrationKind.KinematicPlatform` + `FPPhysicsMover` 未在 Spec 表示例中列出，但 `CubeEntity`/地图可能依赖 | Task 9 可选：对 `PhysicsBodyConfig` 增加 `usePhysicsMover` 布尔，true 时注册 `FPPhysicsMover`（与旧行为等价）；实施前搜索 `KinematicPlatform` 调用点 |
| **Dynamic 模拟 tick** | Spec 提到 Dynamic 可被 Kinematic 推，但当前 `FPDynamicRigidbody` 无完整 solver tick | 本 refactor 先注册 Dynamic body + 碰撞体；物理积分若未实现，Task 17 冒烟限定 Static/Kinematic 主路径，Dynamic 仅验证注册不报错 |
| **CubeEntity 资产配置** | Cube 无独立 AssetsConfig，仍靠 `CreateDefaultStaticBox` | 可接受；若 MapSystem 需自定义 halfExtents，通过 `SetData(PhysicsBodyData)` 注入 |
| **测试程序集** | `Assets/Scripts/Test/` 目录当前不存在 | 首个测试 Task 创建目录；若 Unity 未识别，参考 `Assets/Scripts/Editor/ProtobufConverter/*Tests.cs` 放入 Editor 程序集或添加 `.asmdef` |
| **ScriptableObject YAML 手工迁移** | Task 12 需 Unity Editor | 不可 CLI 时由人工在配置中心完成并提交 `.asset` |

### 3. Placeholder 扫描

- 无 TBD / TODO / “implement later”
- 每个 Task 含具体路径、代码块、构建/测试命令
- Task 9 `usePhysicsMover` 标注为可选扩展，有明确触发条件，非空 placeholder

### 4. 类型一致性

- `PhysicsMovementMode.CharacterController` / `Rigidbody` 全计划统一
- `PhysicsBodyComponent.Motor` ← `MoveComponent` 引用一致
- `ComponentDataKey.PhysicsBodyData` + `PhysicsEntityConfig` 贯穿 Task 8-11
- `FPShapeType.Sphere = 3` 与 `PhysicsShapeType.Sphere = 1` 分属运行时 collider / Authoring 枚举，Task 9 `SyncColliderFromEntity` 通过 `IFPCollider.ShapeType` 桥接

---

**Plan complete and saved to `docs/superpowers/plans/2026-07-12-kcc-physics-refactor.md`.**

**Execution options:**

1. **Subagent-Driven（推荐）** — 每 Task 派发独立 subagent，Task 间 review，快速迭代  
2. **Inline Execution** — 本会话用 executing-plans 批量执行，检查点暂停 review  

**Which approach?**
