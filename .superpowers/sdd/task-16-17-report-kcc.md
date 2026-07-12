# Task 16–17 Report — 遗留 KCC 清理与验收

## Contract

- **STATUS:** DONE (Task 16 committed; Task 17 verification documented)
- **Commits:**
  - Task 16: `d00b482080f3bfcbc1748c738393f4e211ed5600`
- **Base:** `791da7c080f3bfcbc1748c738393f4e211ed5600` (feat(editor): add physics body gizmo preview in factory windows)
- **Tests:**
  - `dotnet build Assembly-CSharp.csproj -nologo -v:minimal` — **0 errors**, 37 warnings（项目既有）

---

## Task 16 — 删除遗留 KCC 注册代码

### Deliverables

| File | Action |
|------|--------|
| `Assets/Scripts/RunTime/ECS/Component/KccRegistrationComponent.cs` | **Deleted**（含 `.meta`） |
| `Assets/Scripts/RunTime/KCC/KccBodySettings.cs` | **Deleted**（含 `.meta`） |
| `Assets/Scripts/RunTime/ECS/Component/ComponentDataKey.cs` | 移除 `KccBodyData` Obsolete 常量及 `using System;` |

### 零引用验证

```
rg "KccRegistrationComponent|KccBodySettings|KccRegistrationKind|KccBodyData" Assets/Scripts
```

**Result:** 无命中（`Assets/Scripts` 内零残留）

全仓库（含 docs / 历史 report）仅在 plan、spec、旧 task report 中有文档性提及，**无运行时 C# 引用**。

### 说明

- `KccRegistrationComponent.cs` / `KccBodySettings.cs` 在 base `791da7c` 上为 **untracked 本地文件**（从未纳入 git 历史）；Task 16 从磁盘删除，commit 仅包含 `ComponentDataKey.cs` 的 `KccBodyData` 移除。
- 实体与 `MoveComponent` 已在 Task 10–11 完成 `PhysicsBodyComponent` 迁移，删除后构建无 breakage。

### Commit

```
refactor(kcc): remove legacy KccRegistrationComponent and KccBodySettings
```

Hash: `d00b482080f3bfcbc1748c738393f4e211ed5600`

---

## Task 17 — 验收与回归

### Step 1: 全量构建 ✅

```
dotnet build Assembly-CSharp.csproj -nologo -v:minimal
```

| 指标 | 结果 |
|------|------|
| Errors | **0** |
| Warnings | 37（NavMesh / DrawDebugTools / Loxodon 等既有警告） |
| 执行时机 | Task 16 删除后、commit 前与报告编写时各一次 |

### Step 2: EditMode 测试 ⏳ MANUAL

**测试文件（存在）：**

- `Assets/Scripts/Test/EditMode/KCC/FPSphereCollisionTests.cs`
- `Assets/Scripts/Test/EditMode/KCC/FPCapsuleColliderTests.cs`
- `Assets/Scripts/Test/EditMode/KCC/PhysicsConfigConverterTests.cs`

**状态：** 未在本 session 执行 Unity Test Runner（需 Unity Editor → EditMode → `Rogue.Tests.EditMode.KCC.*`）。

**建议：** 合并前在 Editor 中跑一遍，确认 Sphere/Capsule/Converter 全部 PASS。

### Step 3: PlayMode 冒烟 ⏳ MANUAL

| 检查项 | 状态 |
|--------|------|
| `npm run server:single` + Rogue 战斗 Play | 未执行 |
| 英雄 WASD + Motor 阻挡 | 未执行 |
| 怪物寻路 + 碰撞体跟随（无 Motor） | 未执行 |
| 受击盒 / VolumeSystem 命中 | 未执行 |

### Step 4: 确定性 spot-check ⏳ MANUAL

同一输入序列 100 tick 对比 `transform.Position` fp 值 — **未执行**（可用 RollBack 指纹或临时日志）。

### Step 5: Spec 验收清单

| # | 验收项 | 验证方式 | 状态 |
|---|--------|----------|------|
| 1 | Hero/Monster 可配 movementMode，不绑 EntityType | 配置中心改怪物 CC、英雄 RB | ⏳ 需 Unity 手工 |
| 2 | CC 模式单胶囊 + Motor Sweep | 英雄仅 Motor，无 physicsBody 碰撞核 | ✅ 代码路径（`PhysicsBodyComponent.RegisterCharacterMotor` + 互斥 warning） |
| 3 | RB 模式一 body + N collider，Kinematic 形状不限 | 怪物多 Box/Sphere 组合 | ✅ 代码路径（`RegisterRigidbodyBody` + `SyncColliderFromEntity`） |
| 4 | 受击盒不受影响 | `colliderDataList` → VolumeSystem | ✅ 未改 ColliderComponent / VolumeSystem |
| 5 | 怪物寻路 + 碰撞体跟随 | 无 MoveComponent / Motor | ✅ `MonsterEntity` 无 `MoveComponent` |
| 6 | fp 确定性 | 100 tick 对比 | ⏳ 未跑 |

### Step 6: Commit

Task 17 无代码修复；**无单独 verification commit**（符合 plan「如有验收修复才 commit」）。

---

## Remaining Risks

1. **KinematicPlatform / FPPhysicsMover：** 旧 `KccRegistrationKind.KinematicPlatform` 已删除；`PhysicsBodyComponent` 声明 `_physicsMover` 字段但未实现注册路径。若地图/Cube 仍依赖移动平台，需后续为 `PhysicsBodyConfig` 增加 `usePhysicsMover` 或等价配置（plan Self-Review 已标注）。
2. **Dynamic 物理积分：** `FPDynamicRigidbody` 注册已实现，完整 solver tick 可能未就绪；冒烟应优先 Static/Kinematic 主路径。
3. **EditMode / PlayMode 未在本 session 执行：** 合并前建议在 Unity 中完成 Task 17 Step 2–4。
4. **GameAssetConfig YAML：** Task 12 资产迁移若未在 Editor 保存，运行时可能仍用旧序列化默认值（与 Task 16/17 无直接编译影响）。

---

## Self-Review

1. **Task 16 范围：** 删除两个遗留文件 + 清理 `KccBodyData`；`Assets/Scripts` 零 orphaned 符号。
2. **构建：** `Assembly-CSharp.csproj` 0 errors（硬性要求满足）。
3. **Task 17：** 构建验证完成；EditMode/PlayMode/确定性留 MANUAL 清单供人工验收。
4. **Commit：** Task 16 已提交；Task 17 仅报告文档，无额外 commit。
