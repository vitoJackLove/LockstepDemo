# Task 11 Report — MoveComponent 改绑 PhysicsBodyComponent.Motor

## Contract

- **STATUS:** DONE
- **Commits:** `4a2cedab7b5b7f25cc42d84bd6764a4be6f768f0`
- **Base:** `4d602c9a64ee2439c79969de774b648b0457fe57`
- **Tests:**
  - `dotnet build Assembly-CSharp.csproj -nologo -v:minimal` — 0 errors

---

## Deliverables

| File | Description |
|------|-------------|
| `Assets/Scripts/RunTime/ECS/Component/MoveComponent.cs` | Motor 来源改为 `PhysicsBodyComponent.Motor`，更新 XML 注释与错误消息 |
| `Assets/Scripts/RunTime/ECS/Entity/HeroEntity.cs` | 移除冗余 `SetData(PhysicsBodyData)`；保留 CC 层 Default→Hero 兜底 |

## Implementation Summary

### MoveComponent

- `OnStart`：`_motor = Entity.GetComponent<PhysicsBodyComponent>()?.Motor`
- 错误消息：`MoveComponent requires PhysicsBodyComponent with CharacterController mode. entityId=...`
- XML 注释：Motor 由 `PhysicsBodyComponent` 在 CharacterController 范式下创建
- 组件启动顺序：`HeroEntity.GetComponentTypes()` 中 `PhysicsBodyComponent` 在 `MoveComponent` 之前，`OnStart` 时 Motor 已注册

### HeroEntity OnInit

- 保留 `characterController.layer == Default` 时修正为 `FPCollisionLayer.Hero`
- 移除 `SetData(ComponentDataKey.PhysicsBodyData, ...)`；改由 `PhysicsBodyComponent.ResolveConfig()` 从 `BattleHeroData.HeroAssetsConfig` 读取（与 plan 一致，不再写 `KccBodyData` / 冗余 PhysicsBodyData）

### 全局残留检查

```
rg "KccRegistrationComponent" Assets/Scripts/RunTime
```

命中仅：

- `KccRegistrationComponent.cs`（Task 16 删除）

符合 plan Task 10 Step 4 预期（MoveComponent 已无引用）。

### 保留（Task 16）

- `KccRegistrationComponent.cs` / `KccBodySettings.cs` 未删除

## Build Verification

```
dotnet build Assembly-CSharp.csproj -nologo -v:minimal
```

Result: **Build succeeded** — 0 errors，37 warnings（项目既有警告）。

## Commit

```
refactor(kcc): wire MoveComponent to PhysicsBodyComponent
```

Hash: `4a2cedab7b5b7f25cc42d84bd6764a4be6f768f0`

## Concerns

1. **Hero 层修正写回 config 对象：** `characterController.layer` 就地修改共享 ScriptableObject 引用；与 plan 一致，资产未配 layer 时运行期兜底为 Hero。
2. **MonsterEntity 仍 SetData(PhysicsBodyData)：** Task 11 范围仅 Hero + MoveComponent；Monster 注入保留 Task 10 行为，`ResolveConfig` 优先读 runtime override。
3. **Rigidbody 范式英雄：** 若英雄配置为 `movementMode=Rigidbody`，`MoveComponent` 将报 Motor null 错误（符合 spec：仅 CC 范式绑定 Motor）。

## Self-Review

1. **范围合规：** 仅修改 `MoveComponent.cs` 与 `HeroEntity.cs`；无 `KccRegistrationComponent` 删除。
2. **构建验证：** `Assembly-CSharp.csproj` 0 errors。
3. **Plan Step 1–4 全覆盖：** MoveComponent 改引用、Hero 层兜底、移除冗余 SetData、构建、提交。
