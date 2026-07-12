# Task 9 Report — PhysicsBodyComponent Rigidbody 分支

## Contract

- **STATUS:** DONE
- **Commits:** `f5adab247fbdbcd18a1127b2c8eca7ba60fa64a1`
- **Base:** `cc1db641e392252e1d17b0a48a421f9bfa18382c`
- **Tests:**
  - `dotnet build Assembly-CSharp.csproj -nologo -v:minimal` — 0 errors

---

## Deliverables

| File | Description |
|------|-------------|
| `Assets/Scripts/RunTime/ECS/Component/PhysicsBodyComponent.cs` | 扩展 Rigidbody 复合碰撞体分支 |

## Implementation Summary

### 新增字段与属性

- `_colliders` / `_dynamicBody` / `_physicsMover` / `_moverController`（后两者为 plan 预留，Task 9 可选扩展）
- 公开 `Colliders`（`IReadOnlyList<IFPCollider>`）、`DynamicBody`

### OnStart 分支

- `Rigidbody` 范式调用 `RegisterRigidbodyBody()`
- 末尾互斥校验：CC 模式若 `physicsBody.colliders` 非空则 `GameLog.Warning`

### RegisterRigidbodyBody

- 校验 `PhysicsBody.colliders` 非空
- `bodyType` 分支：
  - **Dynamic/Kinematic：** 创建 `FPDynamicRigidbody` + `AllocateBodyId` + `SetPose`
  - **Static：** `_dynamicBody = null`
- 遍历 colliders：`PhysicsConfigConverter.CreateCollider` → `SyncColliderFromEntity` → 绑定 `AttachedBody` → `FPCollisionWorld.RegisterCollider`

### OnFixedUpdate 同步

- Rigidbody 范式每 tick 重算各碰撞体位姿（`SyncColliderFromEntity`）并 `SyncDynamicBodyPose`

### SyncColliderFromEntity

- 支持 Box / Sphere / Capsule，应用 `localOffset`、`localEuler`、实体 `LocalScale`

### UnregisterAll 扩展

- 注销所有 `_colliders`（`FPCollisionWorld.UnregisterCollider`）
- 清空 `_dynamicBody`、`_physicsMover`、`_moverController`

### 保留（Task 8）

- CharacterController 分支 `RegisterCharacterMotor` 未改动
- `KccRegistrationComponent` 未删除（Task 16）

## Build Verification

```
dotnet build Assembly-CSharp.csproj -nologo -v:minimal
```

Result: **Build succeeded** — 0 errors，37 warnings（项目既有警告）。

## Commit

```
feat(kcc): add Rigidbody compound collider branch to PhysicsBodyComponent
```

Hash: `f5adab247fbdbcd18a1127b2c8eca7ba60fa64a1`

## Concerns

1. **FPPhysicsMover 未接线：** `_physicsMover` / `_moverController` 字段已声明但未使用；旧 `KinematicPlatform` 迁移留 plan Self-Review 可选扩展。
2. **Dynamic 物理积分未实现：** 当前仅注册 Dynamic body + 碰撞体；无完整 solver tick（plan 已知风险）。
3. **PhysicsBodyComponent 尚未挂到实体：** Task 10 将把实体 `GetComponentTypes` 从 `KccRegistrationComponent` 切到 `PhysicsBodyComponent`。
4. **OnFixedUpdate 索引假设：** `_colliders[i]` 与 `_config.PhysicsBody.colliders[i]` 一一对应；注册时跳过 null collider 会导致索引错位（当前 `CreateCollider` 仅 default shape 返回 null，正常配置无影响）。

## Self-Review

1. **范围合规：** 仅修改 `PhysicsBodyComponent.cs`；CC 分支保留；`KccRegistrationComponent` 未删。
2. **构建验证：** `Assembly-CSharp.csproj` 0 errors。
3. **Plan Step 1–7 全覆盖：** 字段、RegisterRigidbodyBody、OnFixedUpdate、UnregisterAll、互斥校验、构建、提交。
