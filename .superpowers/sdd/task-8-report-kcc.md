# Task 8 Report — PhysicsBodyComponent CharacterController 分支

## Contract

- **STATUS:** DONE
- **Commits:** `cc1db641e392252e1d17b0a48a421f9bfa18382c`
- **Base:** `7eed7964951c0bb4cf1163aedc6c2f028fccd4f5`
- **Tests:**
  - `dotnet build Assembly-CSharp.csproj -nologo -v:minimal` — 0 errors
- **Concerns:** 见下方

---

## Deliverables

| File | Description |
|------|-------------|
| `Assets/Scripts/RunTime/ECS/Component/PhysicsBodyComponent.cs` | 新增 `PhysicsEntityConfig` DTO + `PhysicsBodyComponent`（CC 分支 + 骨架） |
| `Assets/Scripts/RunTime/ECS/Component/PhysicsBodyComponent.cs.meta` | Unity meta |
| `Assets/Scripts/RunTime/ECS/Component/ComponentDataKey.cs` | 新增 `PhysicsBodyData`；`KccBodyData` 标记 `[Obsolete]` |

## Implementation Summary

### ComponentDataKey

- 新增 `PhysicsBodyData = "PhysicsBodyData"`。
- 保留 `KccBodyData` 并标记 `[Obsolete("Use PhysicsBodyData")]`（迁移期兼容 `KccRegistrationComponent`）。

### PhysicsEntityConfig

- 运行时 DTO：`MovementMode` / `CharacterController` / `PhysicsBody`。
- `CreateDefaultStaticBox(BaseEntity)`：Cube 等无资产配置实体默认 Static Rigidbody + Wall 盒体（Rigidbody 注册留 Task 9）。

### PhysicsBodyComponent

- **OnStart：** `ResolveConfig()` → `CharacterController` 时 `RegisterCharacterMotor()`。
- **ResolveConfig 优先级：**
  1. `Entity.GetData<PhysicsEntityConfig>(ComponentDataKey.PhysicsBodyData)` 运行时注入
  2. `HeroEntity` → `BattleHeroData.HeroAssetsConfig`
  3. `MonsterEntity` → `BattleMonsterData.MonsterAssetsConfig`
  4. 默认 `CreateDefaultStaticBox`
- **RegisterCharacterMotor：** `PhysicsConfigConverter.ToCharacterMotorDimensions` → `FPKinematicCharacterMotor` 注册。
- **OnDispose：** `Unregister` + `UnbindEntity`。

### 未改动（按 plan 范围）

- **MoveComponent：** 仍引用 `KccRegistrationComponent.Motor`（Task 11 改接 `PhysicsBodyComponent`）。
- **KccRegistrationComponent：** 保留，Task 16 删除。
- **实体 GetComponentTypes：** 仍用 `KccRegistrationComponent`（Task 10 替换）。

## Build Verification

```
dotnet build Assembly-CSharp.csproj -nologo -v:minimal
```

Result: **Build succeeded** — 0 errors，37 warnings（项目既有警告）。

## Commit

```
feat(kcc): add PhysicsBodyComponent with CharacterController branch
```

Hash: `cc1db641e392252e1d17b0a48a421f9bfa18382c`

## Concerns

1. **ResolveConfig 未使用 `Entity.Config`：** `BaseEntity.Config` 为 `protected`，组件无法直接访问；改用 `HeroEntity`/`MonsterEntity` + `Battle*Data.*AssetsConfig`，语义等价于 plan 的资产配置读取。
2. **PhysicsBodyComponent 尚未挂到实体：** Task 10 将把 `KccRegistrationComponent` 替换为 `PhysicsBodyComponent`；当前运行时仍走旧组件。
3. **Rigidbody / Static 默认分支未注册碰撞体：** `CreateDefaultStaticBox` 仅构造配置；实际 `FPCollisionWorld` 注册在 Task 9。
4. **KccBodyData Obsolete：** `KccRegistrationComponent` 仍引用该常量，编译会产生 CS0618 警告（预期，Task 16 清理）。

## Self-Review

1. **范围合规：** 仅 CC 分支 + 骨架，无 Rigidbody 实现、无 MoveComponent/实体 wiring。
2. **构建验证：** `Assembly-CSharp.csproj` 0 errors。
3. **KccRegistrationComponent 保留：** 符合 Task 16 前不删除要求。
