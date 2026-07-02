# PVP 格斗 + GGPO 帧同步重构设计

> 日期：2026-06-30  
> 状态：v3 — **严格遵循 GGPO 原文设计**（GDMag 2012 / Tony Cannon）  
> 决策来源：用户确认（严格 1v1、无单机、方案 A、保留 TCP 服务端、保留技能编辑器、不动 ECS/View 表现层）

---

## 1. 目标与范围

### 1.1 目标

将现有 Roguelike 双实体 lockstep 框架重构为 **1v1 PVP 格斗** 框架，采用 **GGPO 方案 A（单模拟态 + 输入预测 + 回滚重放）**，删除怪物与 AI 行为树，保留技能时间轴编辑器及运行时。

### 1.2 范围内

| 类别 | 内容 |
|------|------|
| 世界 | `RogueWorld` → `PvpWorld`，`WorldModel.RogueLike` → `WorldModel.Pvp` |
| 帧同步 | **严格 GGPO**：投机执行、输入预测、快照回滚、Sync Test |
| 移动 | 逻辑层限制 XZ 平面位移 |
| 删除 | 怪物实体/配置/组件、行为树运行时与编辑器、MapSystem、FrameSyncPathfindingSystem、单机模式 |
| 服务端 | TCP + `RogueGameServer` **纯输入中继**（移除 lockstep 阻塞门闩） |
| 保留 | 技能编辑器、`SkillTimeLineSystem`、`HeroEntityView` 等 ECS/View 文件（不修改内容） |

### 1.3 范围外

- 不修改 `Assets/Scripts/RunTime/ECS/View/*` 中除删除 `MonsterEntityView` 以外的任何文件
- 不更换传输协议（保持 TCP）
- 不引入第三方 GGPO SDK

---

## 2. 架构总览

```text
┌─────────────────────────────────────────────────────────────┐
│                     PvpWorld (1v1)                          │
│  2 × HeroEntity（单实体/玩家，带 View）                      │
│  游戏循环对齐 GGPO Figure 7                                  │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
              ┌────────────────────────────┐
              │   GgpoFrameSyncPipeline     │  Facade
              │   （替代 idle + 双轨权威）   │
              └─────────────┬──────────────┘
                            │
         ┌──────────────────┼──────────────────┐
         ▼                  ▼                  ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│ 采样本地输入     │ │SynchronizeInputs │ │  AdvanceFrame   │
│ CommandSystem   │ │ GgpoInputSync    │ │ GgpoRollback    │
└─────────────────┘ │ 发本地+合并预测   │ │ 比对→回滚→快进  │
                    └────────┬─────────┘ └────────┬────────┘
                             │                    │
                             ▼                    │
                    ┌─────────────────┐           │
                    │ UpdateGameState  │◄──────────┘
                    │ 单模拟 + 快照     │  （重放时也调用）
                    └─────────────────┘
                             │
                             ▼
              ┌────────────────────────────┐
              │ RemoteInputReceiver           │
              │ ServerCommandSystem 改造      │
              │ 维护远端输入历史窗口           │
              └─────────────┬──────────────┘
                            │ TCP
                            ▼
              ┌────────────────────────────┐
              │ RogueGameServer             │
              │ MaxPlayers=2                │
              │ 收到输入 → 立即转发对端      │
              │ （不阻塞等待全员同 tick）    │
              └────────────────────────────┘
```

### 2.1 设计模式（C#）

| 模式 | 应用 |
|------|------|
| **Strategy** | `IEntitySyncPolicy` → 联机改用 `UnifiedEntitySyncPolicy`（单实体） |
| **Template Method** | `BaseWorld.FixedUpdate` 模板流程，PVP 分支走 GGPO 管道 |
| **Facade** | `GgpoFrameSyncPipeline` 封装输入同步 + 回滚对外接口 |
| **State** | `GgpoSyncTestMode` 开关，强制每帧走回滚路径（对应 GGPO sync test） |
| **Command** | 保留现有 `CommandData` / `CommandSystem`，按 tick 合并多玩家指令 |
| **Factory** | `GameSessionFactory` 仅保留 Online；`WorldSystem` 创建 `PvpWorld` |
| **Observer** | 保留 `ServerCommandSystem` 订阅 `FrameCommand` 事件 |

---

## 3. 世界层：PvpWorld

### 3.1 世界创建

- 新建 `PvpWorld.cs`（删除 `RogueWorld.cs`）
- 严格创建 **2 名 HeroEntity**，各自带 View Prefab
- 出生点（暂定，可配置化）：
  - Player1：`fp3(-3, 0, 0)`，朝向 +X
  - Player2：`fp3(3, 0, 0)`，朝向 -X
- 本地玩家：由 `PlayerData.IsSelf` 决定，绑定 `EntitySystem.RegisterLocalPlayer(hero)`
- 远端玩家：仅作为模拟中的对手实体，输入来自 `GgpoInputSynchronizer`

### 3.2 系统列表（PvpWorld.GetSystemTypes）

**保留：**

`BattleUISystem`, `EntityViewSystem`, `SkillTimeLineSystem`, `TouchSystem`, `CommandSystem`, `CameraSystem`, `VolumeSystem`, `EntitySystem`, `ServerCommandSystem`, `UIDamageTextSystem`, `BattleObserverSystem`

**移除：**

`BehaviourTreeSystem`, `FrameSyncPathfindingSystem`

**新增：**

`GgpoFrameSyncSystem`（或在 `BaseWorld` partial 中实现 Facade，系统层仅注册入口）

### 3.3 BaseWorld 清理

- 删除 `_behaviourTreeRoot` 及创建逻辑
- 删除 `_mapRoot` 若仅服务 MapSystem（MapSystem 删除后一并移除）
- `WorldModel` / `WorldSystem` 分支改为 `PvpWorld`

---

## 4. GGPO 帧同步（严格按原文）

> 参考：GDMag 2012 — *Good Game Peace Out*（Tony Cannon）  
> 核心：**不在远端输入到齐前阻塞模拟**；本地玩家始终即时且正确；远端真实输入到达后比对预测，从首个错误帧回滚并重放至当前帧。

### 4.0 与 lockstep / v2 双轨的根本区别

| 概念 | 传统 lockstep / v2 双轨 | **严格 GGPO** |
|------|-------------------------|---------------|
| 模拟推进 | 等全员同 tick 输入 | **每帧必推进**，远端用预测 |
| 本地手感 | 常受 RTT 影响 | **本地输入零延迟** |
| 回滚触发 | 服务端收齐才校验 | **远端真实输入到达**即比对 |
| `_authorityTick` | 存在 | **删除** |
| 服务端 | 聚合权威帧、阻塞 tick | **输入中继**，不阻塞 |
| 双实体 Local/Authority | 常见 | **单模拟态** |

### 4.1 GGPO 三前提（原文 Criteria）

1. 模拟状态 **仅由**「上一状态 + 玩家输入」确定性推导  
2. 模拟更新 **独立于** 采样手柄、渲染、音频  
3. 可 **save / load** 完整模拟状态并按需重执行  

### 4.2 预测算法（原文内置策略）

对远端玩家 tick `N+1` 的预测：

> **未来输入 = 该远端玩家已收到的最近一次真实输入**

- 首包未到前：空指令  
- 预测错误次数 ≈ 输入 **变化次数**  

### 4.3 远端输入历史窗口

`GgpoRemoteInputBuffer` 维护每名远端玩家最近 **W** 帧真实输入（默认 **8** 帧）：

- 网络包到达 → 按 tick 写入窗口  
- `SynchronizeInputs`：窗口内有真实值则用真实值，否则 Predict  
- 记录每 tick **实际用于模拟的远端输入**（含预测），供 `AdvanceFrame` 比对  

### 4.4 游戏循环（对齐 Figure 7）

```text
每 FixedUpdate:

  if (正在回滚快进) { RollbackResimulateStep(); return; }

  localInput = CommandSystem.GetCommand()

  merged = GgpoInputSynchronizer.SynchronizeInputs(localTick+1, localInput)
      → 发送本地输入到服务端
      → 合并：本地真实 + 远端(窗口真实 | 预测)
      → 记录预测快照

  localTick++
  TakeLocalSnapShot(localTick)
  UpdateGameState(merged)

  GgpoRollbackController.AdvanceFrame(localTick)
      → 新到达的远端真实输入 vs 当时预测
      → 不一致：Load(firstErrorTick-1) → 重算到 localTick
```

### 4.5 新组件

| 组件 | 对应 GGPO API | 职责 |
|------|---------------|------|
| `GgpoInputSynchronizer` | `ggpo_synchronize_inputs` | 发本地、合并预测/真实、记录预测快照 |
| `GgpoRemoteInputBuffer` | input window | 远端真实输入历史 |
| `GgpoRollbackController` | `ggpo_advance_frame` | 比对、回滚、快进 |
| `GgpoFrameSyncPipeline` | Figure 7 总线 | Facade，挂接 BaseWorld.FixedUpdate |

### 4.6 删除的现有逻辑

| 删除 | 原因 |
|------|------|
| `AuthorityUpdateWorld` | 无第二模拟轨 |
| `_authorityTick` | 无权威帧门闩 |
| `VerifyForecast`（双实体指纹） | 改为输入预测比对 |
| `DualEntitySyncPolicy` | 单模拟态 |
| `LogicUpdate` / `AuthorityUpdate` 分离 | 合并为 `UpdateGameState` |

**命令执行：** 统一 `ExecuteSimulation(List<CommandData>)` 按 EntityId 分发。

### 4.7 Session Profile

```csharp
RequiresRollback => true
RequiresLocalSnapshot => true
RequiresAuthoritySnapshot => false
EntitySyncPolicy => UnifiedEntitySyncPolicy
InputHistoryWindow => 8
SimulatePacketLoss => false
```

### 4.8 Sync Test（原文 sync test）

- 每帧强制走 rollback 路径，验证快照与 CPU 预算  
- 目标：每视频帧可额外重算 **4~5** 次逻辑步（覆盖 ~80ms 延迟）

---

## 5. 服务端改造（TCP — GGPO 输入中继）

### 5.1 配置

- `NetworkServerOptions.DefaultMaxPlayers`：`3` → **`2`**
- `Program.cs` / npm 脚本默认 `--max-players 2`

### 5.2 主路径：即时输入转发（对齐 GGPO 语义）

收到某玩家 `FrameCommand(tick=N)` 后：

1. 存入 `FrameSyncConnection`（现有）
2. **立即** 向对端广播该玩家的 `(PlayerIndex, tick, CommandData)`  
3. **不等待** 另一玩家同 tick 输入到齐  
4. **不阻塞** game loop 推进

客户端 `ServerCommandSystem` 改造为 **RemoteInputReceiver**：

- 按 tick 写入 `GgpoRemoteInputBuffer`  
- **不** 以「全员齐全 List」作为模拟推进条件  

### 5.3 移除 lockstep 权威帧（`TryBroadcastAuthorityFrame`）

| 项 | 处理 |
|----|------|
| `TryBroadcastAuthorityFrame` | **删除或停用** — 非 GGPO 语义 |
| `_serverTick` 阻塞等待 | **删除** — 服务端不做模拟 tick |
| `GameLoop` | 仅 `GatherInputs` + 转发，无权威帧广播 |

> TCP 保留；GGPO 原文用 UDP，此处为 **传输层适配**，客户端行为仍严格按 Figure 7。

### 5.4 可选后续：quality_report（非首版）

原文 `quality_report` / `frame_advantage` 公平性同步可 Phase 2 加入，首版不阻塞 PVP 核心。

---

## 6. 移动约束（XZ 平面）

修改 **`CommandMoveComponent`**（逻辑组件，非 View）：

```csharp
// Movement 内：
// moveDir.y = 0
// 归一化后在 XZ 平面位移
// Position 更新后强制 position.y 保持不变（或固定为 0）
```

`CommandSystem.UpdateInputUv` 已设 `y=0`，在 `Movement` 内二次钳制保证确定性。

---

## 7. 删除清单

### 7.1 运行时 C#（删除文件）

**怪物：**

- `ECS/Entity/MonsterEntity.cs`
- `ECS/View/MonsterEntityView.cs`（用户明确要求删除）
- `ECS/Component/MonsterSkillComponent.cs`, `MonsterHitComponent.cs`
- `EntityAssetsConfig/MonsterAssets.cs`
- `BattleEntityData/BattleMonsterData.cs`
- `SkillData/MonsterSkillData/*`
- `Enum/MonsterBehaviourTreeState.cs`
- `ECS/Component/EnemyEetectionComponent.cs`
- `ECS/Component/AiComponent.cs`（若无其他引用）

**行为树：**

- 整个 `UnityBehaviourTree/` 目录
- `BehaviourTreeData/`
- `ECS/System/BehaviourTreeSystem.cs`

**地图 / 寻路：**

- `ECS/System/MapSystem.cs`
- `Path/FrameSyncPathfindingSystem.cs`
- `ECS/Component/PathFindingComponent.cs`（若无引用）

**单机：**

- `GameSession/Profile/SinglePlayerGameSessionProfile.cs`
- `GameSession/Transport/LocalLoopbackFrameSyncTransport.cs`
- `GameSession/EntitySync/DualEntitySyncPolicy.cs`

### 7.2 编辑器（删除）

- `Editor/UnityBehaviourTreeEditor/` 整个目录
- `Editor/MonsterFactory/` 整个目录

### 7.3 配置与引用清理

- `DataTableHelper` 移除 `MonsterAssets` 表名
- `EntityType` 移除 `MonsterEntity`
- `EntitySystem` 移除 `_monsterEntityList`、双实体映射逻辑（保留动态实体接口供子弹等）
- `GameStartUpViewModel` 移除单机模式 UI 与分支
- `GameSessionModeType` 移除 `SinglePlayer` 枚举值
- `ConfigCenterWindow` 等编辑器入口移除 Monster / BehaviourTree 引用

### 7.4 保留但需改签名（非 View）

| 文件 | 改动 |
|------|------|
| `BattleUISystem` | `RegisterActor(HeroEntity, HeroEntity)` 注册双方英雄血条 |
| `BattleInfoViewModel` | 改为双 Hero 数据源（**UI ViewModel，非 ECS/View**） |
| `VolumeSystem` | 移除 `Authority` 分支中对双实体类型的特殊判断，简化为单模拟 |
| `EntitySystem.RollBack` | 适配单实体快照路径 |

---

## 8. 数据流（严格 GGPO — 1v1）

```text
Client A (localTick 每帧 +1，从不等待)          Server              Client B
─────────────────────────────────────────────────────────────────────────
tick=N: 本地真实输入 → Simulate
        远端B = Predict(上一真实)
        Snapshot(N)
        Send P1@N ──────────────────► 转发 ──────────────────────► 写入 Buffer
                                              ◄────────────────── Send P2@N
        ◄──────────────── 转发 P2@N ────────
AdvanceFrame:
  Buffer 收到 P2@N 真实值
  若 Predict@N ≠ Real@N → Rollback(N) → Resimulate(N..localTick)
  若一致 → 继续

延迟表现：B 的起手动画前若干 ms 可能被跳过；A 的本地操作始终即时。
```

---

## 9. 测试与验证

| 项 | 方法 |
|----|------|
| 确定性 | `GgpoSyncTestMode` 本地跑 1000 帧无异常 |
| 回滚 | 模拟丢包/延迟注入，确认位置/HP 收敛 |
| 1v1 联机 | 双客户端 + `dotnet run` 服务端，对打 + 观察 rollback 日志 |
| 编译 | `dotnet build Assembly-CSharp.csproj` + `dotnet build Server/RogueGameServer` |
| XZ 约束 | 斜向输入后 `position.y` 不变 |

---

## 10. 实施顺序（Implementation Plan 预览）

1. **Phase 1 — 清理与重命名**：删除怪物/行为树/地图/单机，重命名 WorldModel，创建 `PvpWorld` 骨架  
2. **Phase 2 — 实体简化**：单实体 1v1 创建，UnifiedEntitySyncPolicy，移除 DualEntity  
3. **Phase 3 — GGPO 核心**：GgpoInputSynchronizer、RollbackController、BaseWorld 单轨 FixedUpdate  
4. **Phase 4 — 服务端**：MaxPlayers=2，输入即时转发，**移除 TryBroadcastAuthorityFrame**  
5. **Phase 5 — 移动/UI/联调**：XZ 钳制，BattleUISystem 双英雄，Sync Test，端到端验证  

---

## 11. 待确认默认值（非阻塞，可按推荐值实施）

| 项 | 推荐默认值 |
|----|-----------|
| 出生点 | P1 `(-3,0,0)`，P2 `(3,0,0)` |
| 输入历史窗口 `InputHistoryWindow` | 8 帧 |
| 是否删除整个 `Path/` 目录 | **仅删 FrameSyncPathfindingSystem 及测试**；A* 工具类暂留 |
| `GameAssetConfig` 中 Monster 资产 | 删除 ScriptableObject 资产文件 |
| `CardGame` WorldModel 枚举 | 保留不动（WorldSystem default 仍 false） |

---

## 12. 审批

请确认本设计文档。批准后我将：

1. 按 Phase 1–5 编写详细 Implementation Plan（`writing-plans`）
2. 开始分阶段实施

如出生点、预测窗口、Path 目录删除范围需调整，请在审批时一并说明。
