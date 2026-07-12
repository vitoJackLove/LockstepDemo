# Task 3 Report — FPCapsuleCollider 实现

## Contract

- **STATUS:** DONE
- **Commits:** `86060a73b84298613a745e437d34b6c2807ba54d`
- **Tests:**
  - `dotnet build Assembly-CSharp.csproj -nologo -v:minimal` — 0 errors
  - `CapsuleCollider_GetCapsuleShape_MatchesDimensions` — 逻辑已实现，待 Unity Test Runner 验证
- **Concerns:** 见下方

---

## Deliverables

| File | Description |
|------|-------------|
| `Assets/Scripts/RunTime/KCC/Physics/FPCapsuleCollider.cs` | `IFPCollider` 胶囊实现：`SyncFromTransform`、`SetWorldPose`、`GetCapsuleShape` |
| `Assets/Scripts/RunTime/KCC/Physics/FPCapsuleCollider.cs.meta` | Unity 资产 GUID |
| `Assets/Scripts/Test/EditMode/KCC/FPCapsuleColliderTests.cs` | EditMode 单元测试（1 用例） |
| `Assets/Scripts/Test/EditMode/KCC/FPCapsuleColliderTests.cs.meta` | 测试资产 GUID |

## Implementation Summary

- 遵循 `FPBoxCollider` / `FPSphereCollider` 模式：`FPCollisionWorld.AllocateColliderId()`、Y 轴缩放影响高度、X/Z 轴 `max(abs(scale))` 影响半径。
- 构造函数支持 `yOffset` 叠加到局部中心；`directionAxis` 在构造时设定，`SyncFromTransform` 不覆盖。
- `ShapeType` 返回 `FPShapeType.Capsule`；`GetCapsuleShape()` 返回局部 `Center`、世界 `Rotation`、尺寸与方向轴。
- 全部使用 `fp` / `fp3` / `fpquaternion`，无 Unity Physics API；公开 API 附中文 XML 注释。

## Build Verification

```
dotnet build Assembly-CSharp.csproj -nologo -v:minimal
```

Result: **Build succeeded** — 0 errors，37 warnings（均为项目既有第三方/样本警告）。

## Commit

```
feat(kcc): add FPCapsuleCollider
```

## Concerns

1. **csproj 刷新：** 生成 `Game.Runtime.csproj` / `KCC.EditModeTests.csproj` 尚未包含新文件条目；需 Unity 聚焦/刷新资产后自动纳入。`Assembly-CSharp.csproj` 构建已通过，新类语法与 `FPSphereCollider` 同模式。
2. **Center 空间约定：** `GetCapsuleShape().Center` 返回局部 `_localCenter`（含 yOffset），与 `FPBoxCollider`/`FPSphereCollider` 使用世界 `_position` 不同；与 plan 及 `FPCapsuleCollision.BuildShapeCapsule` 局部空间约定一致，后续 compound body 注册时需注意。
3. **测试覆盖：** 当前仅验证尺寸与方向轴；`SyncFromTransform` 缩放行为可在后续任务补测。

## Self-Review

1. **范围合规：** 仅新增 FPCapsuleCollider + 测试 + meta，未修改 Task 4 相交逻辑。
2. **API 一致性：** 与 plan 及现有 collider 命名/行为一致；中文 XML 覆盖公开 API。
3. **测试：** `SetUp` 重置 `FPCollisionWorld`，与 `FPSphereCollisionTests` 一致。
