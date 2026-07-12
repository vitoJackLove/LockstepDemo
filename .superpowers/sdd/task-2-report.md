# Task 2 Report — FPSphereCollider 实现

## Contract

- **STATUS:** DONE
- **Commits:** `d996bba84eff803b39a1745ead3b53e1e1553cf3`
- **Tests:**
  - `dotnet build Assembly-CSharp.csproj -nologo -v:minimal` — 0 errors
  - `SphereCollider_SyncFromTransform_ScalesRadius` — 逻辑已实现，待 Unity Test Runner 验证
  - `CapsuleIntersectsSphere_Overlapping_ReturnsTrue` — **预期仍 FAIL**（Task 4 才实现 OverlapCapsule Sphere 分支）
- **Concerns:** 见下方

---

## Deliverables

| File | Description |
|------|-------------|
| `Assets/Scripts/RunTime/KCC/Physics/FPSphereCollider.cs` | `IFPCollider` 球体实现：`SyncFromTransform`、`SetWorldPose`、`GetSphereShape` |
| `Assets/Scripts/RunTime/KCC/Physics/FPSphereCollider.cs.meta` | Unity 资产 GUID |
| `Assets/Scripts/Test/EditMode/KCC/FPSphereCollisionTests.cs` | EditMode 单元测试（2 用例） |
| `Assets/Scripts/Test/EditMode/KCC/KCC.EditModeTests.asmdef` | 测试程序集，引用 `Game.Runtime` + Test Runner |

## Implementation Summary

- 遵循 `FPBoxCollider` 模式：`FPCollisionWorld.AllocateColliderId()`、缩放半径 `max(abs(scale)) * baseRadius`、局部中心经旋转缩放变换到世界空间。
- `ShapeType` 返回 `FPShapeType.Sphere`；非球形状接口返回 `default`。
- 全部使用 `fp` / `fp3` / `fpquaternion`，无 Unity Physics API。

## Build Verification

```
dotnet build Assembly-CSharp.csproj -nologo -v:minimal
```

Result: **Build succeeded** — 0 errors，37 warnings（均为项目既有第三方/样本警告）。

补充：`Game.Runtime.csproj` 在本地临时加入 `FPSphereCollider.cs` 条目后 `dotnet build Game.Runtime.csproj` 亦 0 errors，确认新类可编译。（`*.csproj` 为 gitignore，需 Unity 刷新后自动纳入。）

## Commit

```
feat(kcc): add FPSphereCollider
```

## Concerns

1. **csproj 刷新：** Unity 实例已打开，batchmode 无法重生成 csproj；`FPSphereCollider` 与 `KCC.EditModeTests` 需 Unity 聚焦/刷新资产后才会写入生成 csproj。代码已按 `FPBoxCollider` 同模式验证可编译。
2. **Task 4 依赖：** `CapsuleIntersectsSphere_Overlapping_ReturnsTrue` 在 `FPCapsuleCollision.OverlapCapsule` 增加 `FPShapeType.Sphere` 分支前会持续失败，符合 brief 预期。
3. **父目录 .meta：** `Assets/Scripts/Test/` 新建目录链尚无 folder `.meta`，Unity 首次导入时会自动生成。

## Self-Review

1. **范围合规：** 仅新增 FPSphereCollider + 测试 + asmdef，未修改 Task 4 相交逻辑。
2. **API 一致性：** 与 plan 及 `FPBoxCollider` 命名/行为一致；中文 XML 注释覆盖公开 API。
3. **测试：** 首测覆盖缩放半径与世界中心 X 分量；第二测为 Task 4 前置占位。
