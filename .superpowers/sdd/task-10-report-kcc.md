# Task 10 Report — 实体组件类型替换与 PhysicsBodyData 注入

## Contract

- **STATUS:** DONE
- **Commits:** `4d602c9a64ee2439c79969de774b648b0457fe57`
- **Base:** `f5adab247fbdbcd18a1127b2c8eca7ba60fa64a1`
- **Tests:**
  - `dotnet build Assembly-CSharp.csproj -nologo -v:minimal` — 0 errors

---

## Deliverables

| File | Description |
|------|-------------|
| `Assets/Scripts/RunTime/ECS/Entity/HeroEntity.cs` | `PhysicsBodyComponent` + `OnInit` 注入 `PhysicsEntityConfig` |
| `Assets/Scripts/RunTime/ECS/Entity/MonsterEntity.cs` | 同上；保留无 `MoveComponent` |
| `Assets/Scripts/RunTime/ECS/Entity/CubeEntity.cs` | `PhysicsBodyComponent` + 默认 Static Wall 盒体注入 |

## Implementation Summary

### GetComponentTypes 替换

- `HeroEntity` / `MonsterEntity` / `CubeEntity`：`typeof(KccRegistrationComponent)` → `typeof(PhysicsBodyComponent)`
- `MonsterEntity` 仍无 `MoveComponent`（寻路直写 Transform，符合 spec）

### OnInit — PhysicsBodyData 注入

**HeroEntity**

- 从 `HeroAssetsConfig` 构建 `PhysicsEntityConfig`（`movementMode`、`characterController`、`physicsBody`）
- `SetData(ComponentDataKey.PhysicsBodyData, ...)`
- 若 `characterController.layer == Default`，修正为 `FPCollisionLayer.Hero`

**MonsterEntity**

- 从 `MonsterAssetsConfig` 注入相同三字段的 `PhysicsEntityConfig`

**CubeEntity**

- 新增 `OnInit`：`PhysicsEntityConfig.CreateDefaultStaticBox(this)` → Static Rigidbody Wall 盒体

### 全局残留检查

```
rg "KccRegistrationComponent" Assets/Scripts/RunTime
```

命中仅：

- `KccRegistrationComponent.cs`（Task 16 删除）
- `MoveComponent.cs`（Task 11 改引用）

符合 plan Step 4 预期。

### 保留（Task 16）

- `KccRegistrationComponent.cs` / `KccBodySettings.cs` 未删除

## Build Verification

```
dotnet build Assembly-CSharp.csproj -nologo -v:minimal
```

Result: **Build succeeded** — 0 errors，37 warnings（项目既有警告）。

## Commit

```
refactor(kcc): switch entities to PhysicsBodyComponent
```

Hash: `4d602c9a64ee2439c79969de774b648b0457fe57`

## Concerns

1. **MoveComponent 仍引用 KccRegistrationComponent：** 英雄 `MoveComponent.OnStart` 仍从旧组件取 Motor；Task 11 需改绑 `PhysicsBodyComponent.Motor`，否则英雄移动在运行时 Motor 为 null。
2. **CubeEntity 半尺寸固定：** `CreateDefaultStaticBox` 使用 0.5³ 盒体，未按 `LocalScale` 定制；MapSystem 若需自定义可通过 `SetData(PhysicsBodyData)` 覆盖（plan Task 12/13 可细化）。
3. **Hero 层修正写回 config 对象：** `characterController.layer` 就地修改共享 ScriptableObject 引用；与 plan Task 11 一致，资产未配 layer 时运行期兜底为 Hero。

## Self-Review

1. **范围合规：** 仅修改三个实体文件；`KccRegistrationComponent` 文件保留。
2. **构建验证：** `Assembly-CSharp.csproj` 0 errors。
3. **Plan Step 1–5 全覆盖：** 组件替换、OnInit 注入、Cube 默认盒体、残留检查、提交。
