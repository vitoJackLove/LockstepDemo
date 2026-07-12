# KCC 物理系统重构设计（Unity 对齐）

> 日期：2026-07-12  
> 状态：已确认

## 目标

- 帧同步项目使用定点数 KCC 物理，禁用 Unity 原生 Rigidbody/Collider
- 配置驱动：物理性由资产配置决定，不与实体类型（Hero/Monster 等）绑定
- 与 Unity 设计一致：CharacterController 与 Rigidbody+Compound Colliders 二选一
- 受击盒（`colliderDataList`）与物理体独立配置

## 背景与约束

| 约束 | 说明 |
|------|------|
| 帧同步 | 全部使用 `fp` 定点数，保证确定性 |
| 禁用 Unity 物理 | 不使用 Unity Rigidbody/Collider 做战斗逻辑 |
| 配置入口 | `HeroAssetsConfig` / `MonsterAssetsConfig`，经配置中心编辑 |
| 怪物移动 | 维持寻路 + Transform 直写 + 碰撞体跟随（不做 Motor 式解算） |

## Unity 对齐原则

| Unity | 本项目 |
|-------|--------|
| [Character Controller](https://docs.unity3d.com/Manual/class-CharacterController.html) — 非物理、内置单一胶囊 | `CharacterControllerSettings` + `FPKinematicCharacterMotor` |
| [Rigidbody + Compound Colliders](https://docs.unity3d.com/Manual/compound-colliders-introduction.html) — 一 Rigidbody、多 Collider、bodyType 在刚体上 | `PhysicsBodyConfig` |
| 二者关系：*"use Rigidbody instead of Character Controller"* | 每实体 **择一**，不叠两套身体 |
| `Rigidbody.isKinematic` | `PhysicsBodyType.Kinematic` |
| 受击 Trigger / Hitbox | `colliderDataList` → `VolumeSystem`（不变） |

## 核心规则

### 1. 每个实体一种移动范式

```csharp
public enum PhysicsMovementMode
{
    CharacterController = 0,  // Motor 内置胶囊
    Rigidbody = 1,            // PhysicsBodyConfig + Compound Colliders
}
```

由 `HeroAssetsConfig` / `MonsterAssetsConfig` 上的 `movementMode` 配置，**不由 EntityType 决定**。

### 2. 同一实体不叠两套身体

- **禁止** 同时用 Motor 胶囊 + `physicsBody.colliders` 作为重叠的身体碰撞体积
- 需要额外碰撞体（盾、武器范围等）应使用受击盒或明确的 Gameplay 层，不在本 spec 扩展

### 3. bodyType 在实体级，不在单个 Collider 上

引用 Unity 文档：*"The collider type (dynamic or kinematic) is defined by the Rigidbody configuration."*

```csharp
public enum PhysicsBodyType
{
    Static = 0,      // 无 Rigidbody，位姿固定
    Kinematic = 1,   // Transform 驱动，可推 Dynamic，不被 Dynamic 推
    Dynamic = 2,     // 物理模拟，可被 Kinematic 推
}
```

### 4. Kinematic 不限制碰撞体形状

- `bodyType = Kinematic` 时，`colliders[]` 可为 Box / Sphere / Capsule **任意数量、任意组合**
- **仅** `CharacterController` 范式要求单一胶囊（对齐 Unity CC）

## 配置结构

### 资产配置（Hero / Monster 通用）

```csharp
public PhysicsMovementMode movementMode;

[ShowIf("movementMode", CharacterController)]
public CharacterControllerSettings characterController;

[ShowIf("movementMode", Rigidbody)]
public PhysicsBodyConfig physicsBody;

// 已有，独立 — 受击盒
public List<HitColliderEditorSetting> colliderDataList;
```

### CharacterControllerSettings（范式 A）

对齐 Unity CC Inspector：Radius、Height、Center、Step Offset、Slope Limit、Skin Width、Layer。

- **单一胶囊**，无 `colliders[]` 列表
- 供 `MoveComponent` + `FPKinematicCharacterMotor` 使用

### PhysicsBodyConfig（范式 B）

```csharp
public class PhysicsBodyConfig
{
    public PhysicsBodyType bodyType;
    public List<PhysicsColliderSetting> colliders;
}

public class PhysicsColliderSetting
{
    public string key;
    public PhysicsShapeType shape;  // Box | Sphere | Capsule
    public Vector3 localOffset;
    public Vector3 localEuler;
    public FPCollisionLayer layer;
    public bool isTrigger;
    // Box: halfExtents | Sphere: radius | Capsule: radius, height, directionAxis
}
```

## 运行时架构

```
AssetsConfig (movementMode)
        │
        ├─ CharacterController ──► FPKinematicCharacterMotor + MoveComponent
        │                          （Sweep 解碰撞，单胶囊）
        │
        └─ Rigidbody ──► PhysicsBodyComponent
                         ├── 1× body（Static / Kinematic / Dynamic）
                         └── N× IFPCollider → FPCollisionWorld

colliderDataList ──► ColliderComponent ──► VolumeSystem（受击，不变）
```

### 系统更新顺序（保持现状）

1. `EntitySystem` — 寻路 / AI 写 Transform
2. `PhysicsBodyComponent` — Kinematic/Dynamic 同步碰撞体位姿
3. `FPKinematicCharacterSystem` — Motor Simulate（仅 CharacterController 范式）

## 物理交互（Unity 语义）

| 主动方 | 被动方 | 行为 |
|--------|--------|------|
| CharacterController (Motor) | Static / Kinematic Collider | 阻挡、滑墙 |
| CharacterController (Motor) | Dynamic Rigidbody | 默认不自动推（可脚本扩展，对应 OnControllerColliderHit） |
| Kinematic Rigidbody（移动中） | Dynamic Rigidbody | Kinematic 推 Dynamic |
| Kinematic Rigidbody | Kinematic Rigidbody | 互不施力；Transform 强制移动时由穿透解析处理 |
| Dynamic Rigidbody | * | 物理模拟 |

不写实体类型特例（如「怪物挤英雄」）；由 `movementMode` + `bodyType` 组合决定。

## 配置示例

| 用例 | movementMode | 配置 |
|------|--------------|------|
| 玩家操作单位 | CharacterController | `characterController`（单胶囊） |
| 寻路怪物 | Rigidbody | `physicsBody`: Kinematic + 多 Box/Sphere/Capsule |
| 可推箱子 | Rigidbody | `physicsBody`: Dynamic + 碰撞核 |
| 空气墙 / 地形 | Rigidbody | `physicsBody`: Static + 碰撞核 |

## KCC 运行时补全

| 项 | 现状 | 目标 |
|----|------|------|
| Box 碰撞体 | ✅ `FPBoxCollider` | 保留 |
| Capsule 碰撞体 | ❌ | 新增 `FPCapsuleCollider` |
| Sphere 碰撞体 | ❌ | 新增 `FPSphereCollider` + 相交检测 |
| 多碰撞体注册 | ❌ 单 `_boxCollider` | `PhysicsBodyComponent` 支持 N 个 |
| 注册组件 | `KccRegistrationComponent` | 重构为 `PhysicsBodyComponent` + 模式分支 |

## 实现策略

**渐进重构（推荐）**

1. 补全 Sphere/Capsule `IFPCollider` 与相交检测
2. 定义配置结构（`PhysicsMovementMode`、`CharacterControllerSettings`、`PhysicsBodyConfig`）
3. 重构注册组件，支持两种范式
4. 配置中心 Gizmo 预览
5. 迁移现有硬编码默认值（怪物 StaticBox、英雄 Motor 等）到资产配置

## 验收标准

1. Hero/Monster 配置中心可配 `movementMode`，不绑定 EntityType
2. CharacterController 模式：单胶囊 + Motor Sweep，行为等同 Unity CC
3. Rigidbody 模式：一刚体 + 多碰撞核，bodyType 在刚体级；Kinematic 形状不限
4. 受击盒系统不受影响
5. 怪物维持寻路 + 碰撞体跟随，无 Motor 解算
6. 帧同步确定性不退化（全 fp 运算）

## 不在范围

- Plane / Mesh 碰撞体
- 同一实体 Motor + PhysicsBody 重叠身体
- 怪物寻路时主动碰撞解算（方案 B/C 推挤解析）
- 修改 Unity 原生物理或 Survivor 等其他模式（除非后续单独 spec）
- 合并受击盒与物理碰撞配置
- **运动学平台（KinematicPlatform / FPPhysicsMover）** — 见 [单独 spec](./2026-07-12-kcc-kinematic-platform-design.md)

## 参考

- [Character Controller](https://docs.unity3d.com/Manual/class-CharacterController.html)
- [Introduction to compound colliders](https://docs.unity3d.com/Manual/compound-colliders-introduction.html)
- [Introduction to rigid body physics](https://docs.unity3d.com/Manual/RigidbodiesOverview.html)
