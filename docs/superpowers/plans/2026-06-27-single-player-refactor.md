# 单机模式重构实施计划

> **Goal:** 用本地回环帧同步统一单机/联机管道，单机禁用快照，修复金手指 Buff 兼容。

**Architecture:** IGameSessionProfile + IFrameSyncTransport + IEntitySyncPolicy + 统一 BaseWorld.FixedUpdate

## Global Constraints

- 联机保留快照与回滚；单机 ForecastTick=0，不生成快照
- 新增代码需中文 XML 注释
- 不修改 FrameSyncServer

## 任务清单

- [x] P1: GameSession 模块（Profile/Transport/EntitySync/Factory）
- [x] P2: GameEntry.ConfigureSession + ServerCommandSystem 走 Transport
- [x] P3: BaseWorld 统一 FixedUpdate，Profile 控制快照/回滚
- [x] P4: RogueWorld / EntitySystem / ViewModel / Agent 改造
- [ ] P4b: CheatEditorWindow 恢复后应用 TryResolve 补丁（见 docs/recovery/cheat-editor-window-restore.md）
