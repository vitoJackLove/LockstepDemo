# KCC 运动学平台（KinematicPlatform）设计

> 日期：2026-07-12  
> 状态：待确认  
> 父文档：[KCC 物理系统重构设计](./2026-07-12-kcc-physics-refactor-design.md)

## 目标

补全 **KinematicPlatform / `FPPhysicsMover`** 在新 `PhysicsBodyComponent` 架构下的注册与运行时接线，使：

1. 由 ECS 实体 Transform 驱动的运动学平台（电梯、移动地板等）可被 `FPKinematicCharacterMotor` 正确识别并继承平台速度
2. 配置驱动：平台实体通过 `PhysicsBodyConfig` 声明，不恢复已删除的 `KccRegistrationComponent` / `KccRegistrationKind`
3. 帧同步确定性：平台速度由定点数 `fp` 位姿差分计算，与现有 KCC 管线一致

## 背景与缺口

### 已有基础设施（无需重写）

| 组件 | 路径 | 状态 |
|------|------|------|
| `FPPhysicsMover` | `KCC/Physics/FPPhysicsMover.cs` | ✅ 速度/角速度反推 |
| `IFPPhysicsMoverController` | 同上 | ✅ 控制器接口 |
| `FPEntityPhysicsMoverController` | `KCC/Physics/FPEntityPhysicsMoverController.cs` | ✅ 读 ECS `FTransform` |
| `FPKinematicCharacterSystem.RegisterPhysicsMover` | `KCC/Core/FPKinematicCharacterSystem.cs` | ✅ 模拟管线已集成 |
| `IFPCollider.AttachedMover` | Box/Sphere/Capsule collider | ✅ 字段存在 |
| `FPKinematicCharacterMotor.GetVelocityFromMoverMovement` | `KCC/Core/FPKinematicCharacterMotor.cs` | ✅ 站立平台速度继承 |
| `FPCollisionLayer.KinematicPlatform` | `KCC/Physics/FPCollisionLayer.cs` | ✅ 层与碰撞矩阵已配置 |

### 当前缺口

`PhysicsBodyComponent` 已声明 `_physicsMover` / `_moverController` 字段，但 **从未创建、注册或绑定**：

- 未调用 `FPKinematicCharacterSystem.RegisterPhysicsMover`
- 碰撞体未设置 `AttachedMover`
- `UnregisterAll` 仅清空引用，未注销

旧 `KccRegistrationKind.KinematicPlatform` 已在 Task 16 删除；本 spec 为其 **等价迁移路径**。

## 问题定义

### 普通 Kinematic Rigidbody vs 运动学平台

| 场景 | `bodyType = Kinematic` | `usePhysicsMover = true` |
|------|------------------------|---------------------------|
| 碰撞阻挡 | ✅ 碰撞体跟随 Transform | ✅ 同上 |
| 怪物/寻路单位 | ✅ 默认路径 | ❌ 不适用（无 Motor） |
| **英雄站在平台上被带走** | ❌ Motor 无法从碰撞体推断平台速度 | ✅ `AttachedMover` + 系统速度更新 |
| 平台旋转携带 | ❌ | ✅ 角速度参与 `GetVelocityFromMoverMovement` |

**结论：** 需要平台速度语义时，必须注册 `FPPhysicsMover`；仅做静态/被动 Kinematic 阻挡时不必开启。

## Unity 对齐

| Unity | 本项目 |
|-------|--------|
| Kinematic Rigidbody 移动平台 + Character Controller | `usePhysicsMover` + `FPPhysicsMover` + Motor 地面附着 |
| 平台 Collider 与 Rigidbody 同 GameObject | 实体 `PhysicsBodyComponent` 复合碰撞体 + 单一 `FPPhysicsMover` |
| `Rigidbody.MovePosition` 驱动 | ECS 逻辑写 `FTransform` → `FPEntityPhysicsMoverController` 读目标位姿 |

平台实体仍使用 **Rigidbody 范式**（`movementMode = Rigidbody`），**不**使用 CharacterController 范式。

## 配置结构

在 `PhysicsBodyConfig` 增加：

```csharp
[LabelText("注册为运动学平台")]
[Tooltip("为 true 时注册 FPPhysicsMover，供 CharacterController 单位站立继承平台速度。仅 Rigidbody 范式有效。")]
public bool usePhysicsMover;
```

### 约束

| 规则 | 说明 |
|------|------|
| 仅 Rigidbody 范式 | `CharacterController` 模式下忽略并 Warning |
| 与 `bodyType` 组合 | `Static` + `usePhysicsMover` → **无效**，启动时 Error 并忽略 Mover |
| 推荐组合 | `Kinematic` + `usePhysicsMover = true` |
| 碰撞层 | 平台碰撞体默认 `FPCollisionLayer.KinematicPlatform`（可在 `colliders[].layer` 覆盖） |
| 单 Mover 多 Collider | 一个实体一个 `FPPhysicsMover`，所有复合碰撞体共享同一 `AttachedMover` |

### 配置示例

| 用例 | movementMode | bodyType | usePhysicsMover | layer |
|------|--------------|----------|-----------------|-------|
| 移动地板 | Rigidbody | Kinematic | true | KinematicPlatform |
| 空气墙 | Rigidbody | Static | false | Wall |
| 寻路怪物 | Rigidbody | Kinematic | false | Monster |

## 运行时架构

```
Entity 逻辑 / 动画 / 脚本
        │ 写 FTransform
        ▼
PhysicsBodyComponent.OnFixedUpdate
        │ SyncColliderFromEntity（碰撞体位姿）
        │
        ├─ usePhysicsMover == false → 仅碰撞体 + 可选 FPDynamicRigidbody
        │
        └─ usePhysicsMover == true
                │
                ├─ FPEntityPhysicsMoverController ← Entity.transform
                ├─ FPPhysicsMover.SetPose(当前位姿)
                └─ IFPCollider.AttachedMover = _physicsMover

FPKinematicCharacterSystem.Simulate (Local 更新)
        │
        ├─ 1. movers[i].VelocityUpdate(deltaTime)   // 由 Transform 差分得 Velocity
        ├─ 2. motors Phase1（地面检测 → AttachedMover）
        ├─ 3. movers[i].ApplySimulationPose()
        └─ 4. motors Phase2（Sweep，含平台附加速度）
```

### 注册 / 注销

**OnStart（RegisterRigidbodyBody 末尾或独立方法）：**

```csharp
if (bodyConfig.usePhysicsMover && bodyConfig.bodyType != PhysicsBodyType.Static)
{
    _moverController = new FPEntityPhysicsMoverController(Entity);
    _physicsMover = new FPPhysicsMover
    {
        Id = FPCollisionWorld.Instance.AllocateBodyId(), // 或与 collider id 策略一致
        Controller = _moverController,
    };
    _physicsMover.SetPose(Entity.transform.Position, Entity.transform.Rotation);

    Entity.BaseWorld.GetSystem<FPKinematicCharacterSystem>()?.RegisterPhysicsMover(_physicsMover);

    for each collider in _colliders:
        collider.AttachedMover = _physicsMover;
}
```

**OnDispose / UnregisterAll：**

```csharp
if (_physicsMover != null)
{
    Entity.BaseWorld.GetSystem<FPKinematicCharacterSystem>()?.UnregisterPhysicsMover(_physicsMover);
    for each collider: collider.AttachedMover = null;
    _physicsMover = null;
    _moverController = null;
}
```

### 系统更新顺序（不变）

保持 [KCC 重构设计](./2026-07-12-kcc-physics-refactor-design.md) 约定：

1. `EntitySystem` — AI / 平台位移逻辑写 Transform  
2. `PhysicsBodyComponent` — 同步碰撞体；Mover 不在此步算速度（由 KCC System 统一 `VelocityUpdate`）  
3. `FPKinematicCharacterSystem` — Mover 速度 → Motor Simulate  

**注意：** `VelocityUpdate` 仅在 `WorldUpdateType.Local` 执行（现有实现）。权威/回滚帧平台位姿须由实体 Transform 确定性驱动，Motor 在对应 `WorldUpdateType` 下模拟。

## 物理交互

| 主动方 | 被动方（平台） | 行为 |
|--------|----------------|------|
| CharacterController (Motor) | `usePhysicsMover` 平台 | 阻挡 + **继承平台线/角速度** |
| CharacterController | 普通 Kinematic 碰撞体 | 仅阻挡/滑墙，不继承速度 |
| Kinematic 怪物 | 平台 | 阻挡；怪物无 Motor，不继承（符合现有怪物方案） |
| Dynamic 刚体 | 平台 | 沿用现有 Dynamic 交互（solver 未完整时降级） |

## 实体与资产

| 实体 | 预期 |
|------|------|
| `CubeEntity` | 默认 Static Box，**不**启用 Mover；地图若需移动平台，通过 `SetData(PhysicsBodyData)` 或专用 Cube 资产配置注入 |
| 新平台实体（可选） | 可后续增加 `PlatformEntity` 或在地图生成时创建带 `usePhysicsMover` 的 Cube |

本 spec **不要求** 新增 EntityType；优先通过配置与 `PhysicsEntityConfig` 注入。

## 验收标准

1. `PhysicsBodyConfig.usePhysicsMover = true` 的 Kinematic 实体启动后，`FPKinematicCharacterSystem` 的 `_physicsMovers` 含对应实例
2. 平台碰撞体 `AttachedMover` 非空，且 layer 与 Hero Motor 可碰撞
3. 英雄站在以恒定速度移动的平台上一段时间，**水平位移包含平台速度分量**（PlayMode 冒烟）
4. 平台销毁后 Mover 从系统注销，无残留引用
5. `usePhysicsMover` 与 `CharacterController` 范式互斥校验（Warning，不注册）
6. EditMode：可选单测验证 Register/Unregister 与 `AttachedMover` 绑定（Mock `BaseWorld` / 轻量 fixture）

## 实现范围（预估 Task）

| # | 内容 |
|---|------|
| 1 | `PhysicsBodyConfig.usePhysicsMover` + Odin 标签 |
| 2 | `PhysicsBodyComponent` 注册/注销/绑定 `AttachedMover` |
| 3 | `PhysicsConfigConverter` / Gizmo 无需改形状逻辑；工厂窗口可选显示平台标记 |
| 4 | 示例资产或 Cube 注入示例（若地图已有平台用例） |
| 5 | EditMode 测试 + PlayMode 平台站立冒烟 |

## 不在范围

- 平台路径编辑工具（Timeline / 样条 Authoring）
- 非 ECS 驱动的 `IFPPhysicsMoverController` 自定义实现（接口已存在，后续可扩展）
- 怪物被平台「带走」的 Motor 式解算
- Dynamic 平台推挤完整 solver
- 修改 `FPCollisionWorld` 层矩阵（已含 KinematicPlatform）

## 风险

| 风险 | 缓解 |
|------|------|
| 平台位移与 Motor 模拟时序 | 保持 Entity 写 Transform 在 KCC System 之前；与现有 RogueWorld 系统顺序对齐 |
| 回滚时平台位姿 | 平台实体 Transform 须参与快照/命令重放；Mover 速度在每帧 Local 重算，不单独序列化 |
| `Static` 误配 `usePhysicsMover` | 启动 Error + 忽略 |

## 参考

- [KCC 物理系统重构设计](./2026-07-12-kcc-physics-refactor-design.md)
- KCC 计划 Self-Review：`usePhysicsMover` 可选扩展项（`docs/superpowers/plans/2026-07-12-kcc-physics-refactor.md`）
- 现有类型：`FPPhysicsMover`、`FPEntityPhysicsMoverController`、`FPKinematicCharacterSystem`
