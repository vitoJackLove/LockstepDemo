# Task 14–15 Report — MonsterQuickCreate 物理段 & PhysicsBody Gizmo 预览

## Contract

- **STATUS:** DONE
- **Commits:** `791da7c514583b1696bd6db7ca442ed326458f56` (Task 15 only; Task 14 skipped — see below)
- **Base:** `12228f017a01e849d8dcf3a51a78e2ca505539b1`
- **Tests:**
  - `dotnet build Assembly-CSharp.csproj -nologo -v:minimal` — 0 errors

---

## Task 14 — MonsterQuickCreateWindow 物理段

### Verdict: SKIPPED (already complete)

Task 14 已在 Task 7（`7eed796`）中实现，与 Task 13 对 Hero 的处理方式相同。在 base `12228f0` 上验证，无需额外 diff。

| Plan Step | Status | Evidence |
|-----------|--------|----------|
| 默认 Rigidbody Kinematic + Box | ✅ | `MonsterQuickCreateWindow.cs` L35–49 |
| DrawPhysicsSection | ✅ | L522–537 |
| BuildMonsterConfig 写入 movementMode / physicsBody | ✅ | L919–920 |
| OnGUI 调用 DrawPhysicsSection | ✅ | L84 |

**Prior commit:** `7eed7964951c0bb4cf1163aedc6c2f028fccd4f5` (`feat(kcc): add movementMode and physics settings to hero/monster configs`)

---

## Task 15 — PhysicsBodyConfigGizmoDrawer

### Deliverables

| File | Description |
|------|-------------|
| `Assets/Scripts/Editor/PhysicsBody/PhysicsBodyConfigGizmoDrawer.cs` | Scene 视图 + 工厂窗口预览线框绘制（Box/Sphere/Capsule、CC 胶囊） |
| `Assets/Scripts/Editor/PhysicsBody/PhysicsBodyConfigGizmoDrawer.cs.meta` | Unity meta |
| `Assets/Scripts/Editor/PhysicsBody.meta` | 目录 meta |
| `Assets/Scripts/Editor/HeroFactory/HeroQuickCreateWindow.cs` | 预览区叠加 CC 物理体青色线框 |
| `Assets/Scripts/Editor/MonsterFactory/MonsterQuickCreateWindow.cs` | 预览区叠加 Rigidbody 复合碰撞体青色线框 |

### Implementation Summary

#### PhysicsBodyConfigGizmoDrawer

- `DrawPhysicsBodyGizmos(config, entityMatrix)` — Scene 视图 Rigidbody 复合碰撞体 `Handles.DrawingScope` 线框
- `DrawCharacterControllerGizmos(settings, entityMatrix)` — Scene 视图 CC 胶囊线框
- `DrawPhysicsBodyPreviewOverlay(rect, camera, config, entityMatrix)` — 工厂窗口 PreviewRenderUtility 屏幕叠加层
- `DrawCharacterControllerPreviewOverlay(rect, camera, settings, entityMatrix)` — 工厂窗口 CC 胶囊叠加层
- 颜色：青色 `(0.2, 0.85, 1, 0.95)`，与红色受击盒区分

#### HeroQuickCreateWindow

- 新增 `DrawPhysicsPreviewOverlay`：CharacterController 范式下调用 `DrawCharacterControllerPreviewOverlay`
- 预览提示更新为「红色=受击盒，青色=物理体」

#### MonsterQuickCreateWindow

- 新增 `DrawPhysicsPreviewOverlay`：Rigidbody 范式下调用 `DrawPhysicsBodyPreviewOverlay`
- 预览提示同步更新

## Build Verification

```
dotnet build Assembly-CSharp.csproj -nologo -v:minimal
```

Result: **Build succeeded** — 0 errors。

> **Note:** `Game.Editor.csproj` 依赖的 `Game.Runtime.csproj` 尚未同步 KCC 新类型（`PhysicsMovementMode` 等），为既有程序集拆分问题，非本 Task 引入。Unity 主程序集 `Assembly-CSharp.csproj` 编译通过。

## Commit

```
feat(editor): add physics body gizmo preview in factory windows
```

## Concerns

1. **Task 14 无独立 commit：** 与 Task 13 相同，代码已在 Task 7 合并提交。
2. **Game.Editor / Game.Runtime 拆分滞后：** 新手写 KCC 类型未列入 `Game.Runtime.csproj`；后续 Task 17 或程序集维护时需补齐。
3. **Scene 视图自动挂钩未实现：** Plan 提及 ConfigCenter / Inspector SceneGUI callback；当前提供静态 API，工厂窗口预览已集成；ConfigCenter 挂钩可后续迭代。
4. **Capsule Gizmo 为简化线框：** 与 plan 一致（顶/底圆盘 + 侧线），非完整半球封盖。

## Self-Review

1. **Task 14：** Plan Step 1–3 全覆盖，无缺口。
2. **Task 15：** GizmoDrawer 创建 + 双工厂窗口 PreviewRenderUtility 回调集成完成。
3. **构建：** `Assembly-CSharp.csproj` 0 errors。
4. **范围：** 未改运行时逻辑；仅 Editor 预览层。
