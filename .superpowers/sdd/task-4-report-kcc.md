# Task 4 Report — FPCapsuleCollision Sphere 相交检测

## Contract

- **STATUS:** DONE
- **Commits:** `1ca06b1ecd032a1db54564e2c63b075dc04d12fd`
- **Tests:**
  - `dotnet build Assembly-CSharp.csproj -nologo -v:minimal` — 0 errors
  - `CapsuleIntersectsSphere_Overlapping_ReturnsTrue` — 逻辑已实现，待 Unity Test Runner 验证
  - `CapsuleIntersectsSphere_Separated_ReturnsFalse` — 逻辑已实现，待 Unity Test Runner 验证
- **Concerns:** 见下方

---

## Deliverables

| File | Description |
|------|-------------|
| `Assets/Scripts/RunTime/KCC/Physics/FPCapsuleCollision.cs` | 胶囊碰撞工具：新增 `FPShapeType.Sphere` 分支及 `CapsuleIntersectsSphere` / `CapsulePenetratesSphere` |
| `Assets/Scripts/RunTime/KCC/Physics/FPMathKCC.cs` | KCC 数学辅助：含 `ClosestPointOnSegmentToPoint` 供胶囊-球体最近点计算 |
| `Assets/Scripts/Test/EditMode/KCC/FPSphereCollisionTests.cs` | 追加分离 case 测试 `CapsuleIntersectsSphere_Separated_ReturnsFalse` |

## Implementation Summary

### OverlapCapsule / TryComputePenetration

- `switch` 新增 `case FPShapeType.Sphere:`，分别调用 `CapsuleIntersectsSphere` 与 `CapsulePenetratesSphere`。
- 胶囊轴线段到球心的最近距离 `dist` 与 `capsule.Radius + sphere.Radius` 比较判定重叠/穿透。
- 穿透方向：球心指向线段最近点；零距离退化时使用 `FPMathKCC.WorldUp`。

### Raycast

- `case FPShapeType.Sphere:` 使用射线-球体二次方程，取 `[0, distance]` 内最近正根。
- 命中法线为 `normalize(point - sphere.Center)`。

### FPMathKCC.ClosestPointOnSegmentToPoint

- 基于已有 `ClosestPointOnSegment` 实现，返回点到线段最近点距离及最近点坐标。

## Build Verification

```
dotnet build Assembly-CSharp.csproj -nologo -v:minimal
```

Result: **Build succeeded** — 0 errors，0 warnings。

## Commit

```
feat(kcc): add sphere intersection to FPCapsuleCollision
```

## Concerns

1. **Unity Test Runner 未在本 Task 执行：** 分离/重叠 case 逻辑与 plan 一致，需在 EditMode 中运行 `FPSphereCollisionTests` 做最终验收。
2. **FPCapsuleCollision.cs.meta / FPMathKCC.cs.meta 未纳入 commit：** plan 仅指定 `.cs` 源文件；Unity 聚焦项目后会自动生成 meta。
3. **Raycast 仅覆盖 Sphere：** Box/Capsule/Plane 已有实现；Capsule-Capsule Raycast 仍走 `default` 返回 false（与 plan 范围一致）。

## Self-Review

1. **范围合规：** 仅修改 plan 列出的 3 个文件，未触及 ECS/World 等其他未提交改动。
2. **算法一致性：** 胶囊-球体相交采用标准线段-点最近距离 + 半径和判定，与 `CapsulePenetratesCapsule` 模式一致。
3. **定点数：** 全部使用 `fp` / `fp3`，无浮点 API。
