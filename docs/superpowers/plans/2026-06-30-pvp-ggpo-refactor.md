# PVP + 严格 GGPO 重构 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将项目重构为 1v1 PVP 格斗框架，采用严格 GGPO 帧同步（单模拟态、输入预测、回滚重放），删除怪物/AI/单机/地图寻路。

**Architecture:** `PvpWorld` + `GgpoFrameSyncPipeline`（SynchronizeInputs → UpdateGameState → AdvanceFrame）；TCP 服务端改为输入中继；删除双实体与 `_authorityTick`。

**Tech Stack:** Unity 2023.2, C# fixed-point ECS, TCP `RogueGameServer`, protobuf

## Global Constraints

- 严格 1v1，无单机模式
- 严格 GGPO（Figure 7），删除 lockstep 权威帧门闩
- 保留 TCP + RogueGameServer（relay 模式）
- 不动 `ECS/View/*`（除删除 MonsterEntityView）
- 保留技能编辑器
- XZ 平面移动
- C# 设计模式 + 中文注释
- `WorldModel.Pvp`

---

## Phase 1: 清理与重命名

### Task 1: WorldModel 与 PvpWorld 骨架

**Files:**
- Modify: `Assets/Scripts/RunTime/GameSystem/WorldModel.cs`
- Modify: `Assets/Scripts/RunTime/GameSystem/WorldSystem.cs`
- Create: `Assets/Scripts/RunTime/World/PvpWorld.cs`
- Delete: `Assets/Scripts/RunTime/World/RogueWorld.cs`

### Task 2: 删除怪物/行为树/地图/单机相关源码

**Files:** 见 spec §7.1–7.2

### Task 3: 清理引用（DataTable、EntityType、GameStartUpViewModel、GameSessionFactory）

---

## Phase 2: 单实体 1v1

### Task 4: EntitySystem 简化（双实体移除、1v1 双 Hero 创建）

### Task 5: UnifiedEntitySyncPolicy 联机默认；删除 DualEntitySyncPolicy

### Task 6: BattleUISystem / BattleInfoViewModel 双 Hero

---

## Phase 3: GGPO 核心

### Task 7: 新建 `Assets/Scripts/RunTime/FrameSync/` 模块

- `GgpoRemoteInputBuffer.cs`
- `GgpoInputSynchronizer.cs`
- `GgpoRollbackController.cs`
- `GgpoFrameSyncPipeline.cs`

### Task 8: 重写 `BaseWorld.RollBack.cs` + `BaseWorld.FixedUpdate` GGPO 循环

### Task 9: ServerCommandSystem → 远端输入写入 Buffer

### Task 10: ExecuteSimulation 统一命令路径

---

## Phase 4: 服务端

### Task 11: `FrameSyncServer` 输入即时转发，移除 TryBroadcastAuthorityFrame

### Task 12: MaxPlayers=2 默认值

---

## Phase 5: 收尾

### Task 13: CommandMoveComponent XZ 钳制

### Task 14: GgpoSyncTestMode 开关

### Task 15: `dotnet build` 验证
