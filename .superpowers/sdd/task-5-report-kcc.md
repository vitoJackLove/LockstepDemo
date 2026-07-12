# Task 5 Report — 物理枚举与配置类

## Contract

- **STATUS:** DONE
- **Commits:** `922441ebf14ec1d5c877e9e03f9c997dedf89ba0`
- **Base:** `1ca06b1`
- **Tests:**
  - `dotnet build Assembly-CSharp.csproj -nologo -v:minimal` — 0 errors
- **Concerns:** 见下方

---

## Deliverables

| File | Description |
|------|-------------|
| `Assets/Scripts/RunTime/KCC/Physics/PhysicsMovementMode.cs` | `PhysicsMovementMode`、`PhysicsBodyType`、`PhysicsShapeType` 枚举 |
| `Assets/Scripts/RunTime/KCC/Physics/CharacterControllerSettings.cs` | Character Controller 范式 Authoring 配置（Odin LabelText） |
| `Assets/Scripts/RunTime/KCC/Physics/PhysicsBodyConfig.cs` | `PhysicsBodyConfig` + `PhysicsColliderSetting`（ShowIf 按形状切换字段） |

## Implementation Summary

### 枚举（PhysicsMovementMode.cs）

- `PhysicsMovementMode`：`CharacterController` / `Rigidbody`，对齐 Unity CC 与 Rigidbody 范式。
- `PhysicsBodyType`：`Static` / `Kinematic` / `Dynamic`，实体级刚体类型。
- `PhysicsShapeType`：`Box` / `Sphere` / `Capsule`，复合碰撞体形状。

### CharacterControllerSettings

- 单一胶囊配置：`radius`、`height`、`center`、`stepOffset`、`slopeLimit`、`skinWidth`、`layer`。
- 默认 `layer = FPCollisionLayer.Hero`。
- 全部字段带 Odin `[LabelText]` 中文标签。

### PhysicsBodyConfig / PhysicsColliderSetting

- `PhysicsBodyConfig`：`bodyType` + `List<PhysicsColliderSetting> colliders`。
- `PhysicsColliderSetting`：通用字段（key、shape、localOffset、localEuler、layer、isTrigger）+ 形状专用字段。
- `[ShowIf("shape", PhysicsShapeType.*)]` 控制盒体/球体/胶囊尺寸字段显示。

## Build Verification

```
dotnet build Assembly-CSharp.csproj -nologo -v:minimal
```

Result: **Build succeeded** — 0 errors，37 warnings（项目既有警告，与本次变更无关）。

## Commit

```
feat(kcc): add physics movement mode and body config types
```

Hash: `922441ebf14ec1d5c877e9e03f9c997dedf89ba0`

## Concerns

1. **与现有 `FPShapeType` 并存：** 运行时定点形状枚举 `FPShapeType` 保留；新 `PhysicsShapeType` 专供 Authoring/配置层，Task 6 `PhysicsConfigConverter` 负责转换。
2. **配置类使用 float/Vector3：** 按 plan 设计为 Unity Inspector Authoring 层；定点转换在后续 Task 6 实现。
3. **未触及 ECS/World：** 仅新增配置类型，实体集成在后续 Task 进行。

## Self-Review

1. **范围合规：** 仅新增 plan 指定的 3 个 `.cs` 文件及对应 `.meta`。
2. **代码一致性：** 与 plan Step 1–3 源码一致，含中文 XML 文档与 Odin 属性。
3. **构建验证：** `Assembly-CSharp.csproj` 编译通过，无新增错误。
