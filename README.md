# 项目介绍

## 概述

本项目是一款基于 **帧同步（Frame Sync）** 的 动作游戏框架，核心特色是 **客户端预测 + 权威校验 + 回滚重放** 的联机同步方案。逻辑层使用 **定点数（Fixed-Point）** 与 **固定时间步长** 保证多端确定性，支持在延迟与丢包环境下仍能保持流畅的本地操作反馈，并在收到权威结果后自动校正偏差。

**技术栈：** Unity 2023.2 · ECS · 定点数 `fp`/`fp3` · Loxodon UI · UniTask · Google.Protobuf · 行为树 · 技能时间轴

---

## 一、端到端帧同步流程

### 1.1 整体架构图

```text
┌─────────────────────────────────────────────────────────────────────────┐
│                         启动与会话建立                                    │
├─────────────────────────────────────────────────────────────────────────┤
│  Unity GameEntry.Start()                                                 │
│    → InitOptionalComponent()（Resource / UI / DataTable / TcpClient）   │
│    → GameStartUpWindow（选英雄、单机/联机切换）                          │
│    → WorldSystem.CreateWorldChannel() → RogueWorld                       │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
          ┌─────────────────────────┴─────────────────────────┐
          ▼                                                   ▼
┌──────────────────────┐                         ┌──────────────────────────┐
│  单机模式             │                         │  联机模式                 │
│  LocalLoopback       │                         │  TcpFrameSyncTransport   │
│  Transport           │                         │  ↔ FrameSyncClient       │
│  （本地回环注入指令）  │                         │  ↔ RogueGameServer       │
└──────────────────────┘                         └──────────────────────────┘
          │                                                   │
          └─────────────────────────┬─────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                    每逻辑帧 FixedUpdate（BaseWorld）                      │
├─────────────────────────────────────────────────────────────────────────┤
│  1. _localTick++                                                         │
│  2. CommandSystem.GetCommand() → RecodeAndSendCommand() → 发送 FrameCommand│
│  3. TakeLocalSnapShot()（联机模式）                                      │
│  4. LogicUpdateWorld(Local)   ← 本地预测                                 │
│  5. AuthorityUpdateWorld()    ← 权威追帧 + VerifyForecast              │
│  或：RollBackLocalUpdateWorld(RollBack) ← 回滚重放模式                   │
└─────────────────────────────────────────────────────────────────────────┘
```



### 1.2 三种世界更新类型

逻辑帧内区分三种更新语义，贯穿整条预测–权威–回滚链路：


| 类型            | 枚举值                         | 说明                                 |
| ------------- | --------------------------- | ---------------------------------- |
| **Local**     | `WorldUpdateType.Local`     | 本地预测：用本地输入推进一帧，拍摄本地快照              |
| **Authority** | `WorldUpdateType.Authority` | 权威推进：用服务器下发的该帧指令推进权威世界，拍摄权威快照并触发校验 |
| **RollBack**  | `WorldUpdateType.RollBack`  | 回滚重放：从回滚点起，用**本地录制的指令**逐帧重放        |


所有系统与组件的 `OnFixedUpdate(deltaTime, WorldUpdateType)` 都会根据更新类型分支，保证**同一套逻辑**在「预测」「权威」「回滚」三种场景下语义一致且可重放。

### 1.3 主循环关键代码（BaseWorld.FixedUpdate）

每固定帧的核心入口在 `BaseWorld.FixedUpdate`：

```csharp
// Assets/Scripts/RunTime/World/BaseWorld.cs
public void FixedUpdate(fp deltaTime)
{
    // 回滚重放模式：跳过正常预测，只做重放
    if (SessionProfile.RequiresRollback && _isStartRollBackUpdate)
    {
        for (int i = 0; i < _rollBackSpeed; i++)
            RollBackLocalUpdateWorld(deltaTime);
        return;
    }

    _localTick++;
    CommandData data = GetSystem<CommandSystem>().GetCommand();

    RecodeAndSendCommand(data);              // 录制 + 发送
    if (SessionProfile.RequiresLocalSnapshot)
        TakeLocalSnapShot();                 // 拍本地快照
    LogicUpdateWorld(deltaTime, data);       // 本地预测
    AuthorityUpdateWorld(deltaTime);         // 权威追帧
}
```

**指令录制与发送**（`RecodeAndSendCommand`）：

```csharp
// Assets/Scripts/RunTime/World/BaseWorld.cs
data.Tick = _localTick;
data.EntityId = actorEntity.EntityId;
GetSystem<CommandSystem>().RecodeCommand(data);           // 本地录制，供回滚重放
GetSystem<ServerCommandSystem>().SendPlayerInput(data);   // 经 Transport 发往服务器
```



### 1.4 本地预测（LogicUpdateWorld）

```csharp
// Assets/Scripts/RunTime/World/BaseWorld.RollBack.cs
private void LogicUpdateWorld(fp deltaTime, CommandData data)
{
    if (data != null)
        ExecuteLogicCommand(data);   // 分发到各 System.OnExecuteLocalCommand
    foreach (var system in _systemDic.Values)
        system.OnFixedUpdate(deltaTime, WorldUpdateType.Local);
}
```



### 1.5 权威追帧（AuthorityUpdateWorld）

本地帧始终领先权威 `_forecastTick` 帧，实现「边预测边等权威」：

```csharp
// Assets/Scripts/RunTime/World/BaseWorld.RollBack.cs
private void AuthorityUpdateWorld(fp deltaTime)
{
    if (_localTick - _authorityTick < _forecastTick)
        return;   // 预测窗口未满，暂不推进权威

    _authorityTick++;
    List<CommandData> commandDataList =
        GetSystem<ServerCommandSystem>().GetServerCommand(_authorityTick);

    if (commandDataList != null)
    {
        TakeAuthoritySnapShot();                    // 拍权威快照
        ExecuteServerCommand(commandDataList);      // 分发到各 System.OnExecuteServerCommand
        foreach (var system in _systemDic.Values)
            system.OnFixedUpdate(deltaTime, WorldUpdateType.Authority);
        VerifyForecast(_authorityTick, deltaTime);  // 比对本地 vs 权威
        AuthorityUpdateWorld(deltaTime);            // 递归追帧（一次 FixedUpdate 可追多帧）
    }
    else
    {
        _authorityTick--;   // 权威指令未到，回退等待
    }
}
```



### 1.6 网络层：客户端 ↔ 服务器



#### 客户端发送（ServerCommandSystem）

```csharp
// Assets/Scripts/RunTime/ECS/System/ServerCommandSystem.cs
public void SendPlayerInput(CommandData commandData)
{
    commandData.ClientSeq = ++_clientSeq;
    FrameCommandMessage commandMessage = new FrameCommandMessage { CommandData = commandData };
    GameEntry.FrameSyncTransport?.Send(BattleObserverEventEnum.FrameCommand, commandMessage);
}
```



#### 客户端接收（FrameSyncClientComponent）

- 后台线程通过 `NetworkPacketStreamUtility.TryReadPacket` 读取 TCP 包
- `NetworkProtobufCodec.TryDeserializePacket` 反序列化
- 主线程 `Update()` 中通过 `GameEntry.Observer.Notify` 分发
- `ServerCommandSystem.OnNotify` 将权威帧指令写入 `_serverCommands[tick]`



#### 服务器权威帧广播（FrameSyncServer）

```csharp
// Server/RogueGameServer/Network/FrameSyncServer.cs
private bool TryBroadcastAuthorityFrame()
{
    uint nextServerTick = _serverTick + 1;
    // 等待所有连线玩家提交 nextServerTick 的输入
    foreach (FrameSyncConnection sourceConnection in snapshot)
    {
        if (!sourceConnection.TryGetInput(nextServerTick, out CommandData? commandData))
            return false;   // 任一玩家未到，暂不广播
        frameInputs.Add((sourceConnection.PlayerIndex, commandData));
    }
    _serverTick = nextServerTick;
    byte[] authorityPacket = NetworkPacketCodec.SerializeFrameCommand(_serverTick, frameInputs);
    BroadcastPacket(authorityPacket);   // 广播给所有客户端
    return true;
}
```

**协议格式：**

```text
[4-byte little-endian 包长度][NetworkPacket protobuf bytes]
```

协议源文件：`Assets/Proto/network_packet_runtime.proto`  
生成 C#：`Assets/Scripts/RunTime/Server/Protobuf/Generated/NetworkPacketRuntime.cs`（Unity 客户端与 .NET 服务器共用）

**服务器房间流程：**

1. 客户端连接 → 服务器发送 `PlayerConnect`（分配 PlayerIndex）
2. 客户端发送 `SelectHero` → 服务器转发；全员选完后广播 `GameStart`（加载阶段）
3. 客户端准备完毕 → 服务器 `TryStartAuthorityFrames()` 开始权威 tick
4. 每 tick 聚合所有玩家 `FrameCommand` → 广播权威帧



### 1.7 会话模式差异


| 配置项       | 单机 `SinglePlayerGameSessionProfile` | 联机 `OnlineGameSessionProfile`           |
| --------- | ----------------------------------- | --------------------------------------- |
| Transport | `LocalLoopbackFrameSyncTransport`   | `TcpFrameSyncTransport`                 |
| 快照        | 关闭                                  | 开启（本地 + 权威）                             |
| 回滚        | 关闭                                  | 开启                                      |
| 预测窗口      | 0                                   | 默认 2（可由 RollBack 配置覆盖）                  |
| 实体策略      | 统一实体                                | 双实体（ActorLocal / ActorAuthority）        |
| 模拟丢包      | 关闭                                  | 可通过 `GameRollBackContent.lossPacket` 注入 |


---



## 二、回滚机制（RollBack）



### 2.1 回滚触发（VerifyForecast）

```csharp
// Assets/Scripts/RunTime/World/BaseWorld.RollBack.cs
private void VerifyForecast(uint tick, fp deltaTime)
{
    RollBackType rollBackType = RollBackType.NoRollBack;
    foreach (var system in _systemDic.Values)
        rollBackType |= system.VerifyForecast(tick);   // EntitySystem 比对硬/软快照

    if (rollBackType != RollBackType.NoRollBack)
        StartRollBack(tick, rollBackType);
}
```

- **静态实体**：比对该帧硬快照与软快照字符串
- **动态实体**：比对数量、指纹与内容，不一致标记硬回滚



### 2.2 回滚类型（RollBackType）


| 类型             | 说明                    |
| -------------- | --------------------- |
| `NoRollBack`   | 无需回滚                  |
| `SoftRollBack` | 用权威软快照覆盖本地（可插值、非关键状态） |
| `HardRollBack` | 用权威硬快照覆盖，可能伴随实体创建/销毁  |


均为 `[Flags]`，可组合。

### 2.3 回滚步骤（StartRollBack）

```csharp
// Assets/Scripts/RunTime/World/BaseWorld.RollBack.cs
public void StartRollBack(uint rollBackTick, RollBackType rollBackType)
{
    RollBack(rollBackTick, rollBackType);              // 各 System 还原到权威状态
    RemoveLocalSnapShot(rollBackTick, rollBackType);   // 删除出错帧之后的本地快照

    if ((rollBackType & RollBackType.HardRollBack) == RollBackType.HardRollBack)
    {
        _isStartRollBackUpdate = true;
        _rollBackEndTick = _localTick;
        _localTick = rollBackTick;   // 本地帧拉回回滚点
    }
}
```



### 2.4 回滚重放（RollBackLocalUpdateWorld）

```csharp
// Assets/Scripts/RunTime/World/BaseWorld.RollBack.cs
private void RollBackLocalUpdateWorld(fp deltaTime)
{
    if (_localTick > _rollBackEndTick)
    {
        _isStartRollBackUpdate = false;   // 重放完成，恢复正常预测
        return;
    }

    CommandData selfCommandData = GetSystem<ServerCommandSystem>()
        .GetServerCommand(_localTick, actorEntityId);
    ExecuteLogicCommand(selfCommandData);
    foreach (var system in _systemDic.Values)
        system.OnFixedUpdate(deltaTime, WorldUpdateType.RollBack);
    _localTick++;
}
```

### 2.5 演示视频

以下视频展示两种回滚在实际战斗中的表现，可配合 `Tools/调试/预测回滚` 与 `Tools/调试/双世界线查看器` 对照理解。

| 类型 | 场景说明 | 视频 |
| ---- | -------- | ---- |
| **软回滚** | 队友攻击怪物时，本地软快照与权威不一致，仅用权威软快照覆盖本地（位置/Buff 等可插值状态），无需重放输入 | [软回滚-队友攻击怪物.mp4](软回滚-队友攻击怪物.mp4) |
| **硬回滚** | 预测 5 帧 + 模拟 20% 丢包率下触发硬回滚：本地帧拉回出错点，删除后续快照，并用录制的输入逐帧重放至当前预测帧 | [硬回滚-预测5帧模拟百分之20的丢包率.mp4](硬回滚-预测5帧模拟百分之20的丢包率.mp4) |

> 在 GitHub 上点击链接可直接播放；本地克隆仓库后也可直接打开上述 `.mp4` 文件。

---



## 三、快照与确定性



### 3.1 快照层级

- **按帧存储**：本地快照按 `_localTick`，权威快照按 `_authorityTick`
- **实体级**：每个实体有硬快照（Hard）与软快照（Soft）；组件/行为树/技能通过 `TakeSnapShot(hardWriter, softWriter)` 写入
- **动态实体**：额外记录该帧动态实体列表（数量 + 指纹），回滚时做创建/销毁/覆盖



### 3.2 确定性保障


| 手段     | 实现位置                                                         |
| ------ | ------------------------------------------------------------ |
| 固定时间步长 | `fpmath1.LogicDeltaTime`（如 0.033s）                           |
| 定点数    | `Unity.Mathematics.FixedPoint` 的 `fp`/`fp3`/`fpquaternion`   |
| 确定性寻路  | `FrameSyncPathfindingSystem` + `AStarPathfinder`（纯逻辑 A*，无随机） |
| 指令驱动   | 每帧状态仅由「该帧指令 + 上一帧状态」决定                                       |


---



## 四、项目启动方式



### 4.1 环境需求

- **Unity**：2023.2.20f1c1（Windows）
- **.NET**：7.0 Runtime（帧同步服务器）
- **脚本后端**：IL2CPP · API Compatibility .NET Standard 2.1



### 4.2 Unity 客户端

1. 用 Unity Hub 打开项目根目录 `D:\UnityProject\roguelike`
2. 打开含 `GameEntry` 的启动场景（通常为主场景）
3. 点击 **Play** 进入游戏

**启动链路：**

```text
GameEntry.Start()
  → InitOptionalComponent()
      GameSetting → Resource → UI → Scene → DataTable
      → Observer → Camera → Canvas → TcpClient（联机模式）
  → ConfigureSession()（单机 / 联机 Transport）
  → OpenUIWindow<GameStartUpWindow>()
  → 选英雄 → LoadScene → WorldSystem.CreateWorldChannel()
  → RogueWorld.WorldSystemInit() → WorldEnter() → GameStart()
```

**GameEntry 生命周期转发：**

```csharp
// Assets/Scripts/RunTime/GameEntry/GameEntry.cs
Update()      → Game.Update()       → WorldSystem.Update()
FixedUpdate() → Game.FixedUpdate()  → WorldSystem.FixedUpdate() → BaseWorld.FixedUpdate()
LateUpdate()  → Game.LateUpdate()   → WorldSystem.LateUpdate()
```

**单机 vs 联机切换：** 在 `GameStartUpWindow` 中切换模式，ViewModel 会调用 `GameEntry.ConfigureSession()` 切换 `GameSessionFactory.CreateSinglePlayer()` 或 `CreateOnline(TcpClient)`。

### 4.3 帧同步服务器（Server/RogueGameServer）

服务器为独立 .NET 8 控制台项目，**不执行 Unity 战斗模拟**，只负责聚合玩家输入并按 tick 广播权威帧。

#### 前置条件：安装 .NET SDK

若 PowerShell 提示 `dotnet : 无法将"dotnet"项识别为 cmdlet...`，说明本机未安装 .NET SDK 或未加入 PATH。

**方式一：winget 安装（推荐）**

```powershell
winget install Microsoft.DotNet.SDK.8 --accept-package-agreements --accept-source-agreements
```

**方式二：** 从 [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download) 下载并安装 **.NET 8 SDK**。

安装完成后 **关闭并重新打开 PowerShell/终端**，再执行 `dotnet --version` 确认（应显示 `8.x.x`）。

若仍提示找不到命令，可临时使用完整路径：

```powershell
& "C:\Program Files\dotnet\dotnet.exe" --version
```

**构建：**

```powershell
cd D:\UnityProject\lockstep
npm run server:build
```

**启动（默认端口 8888，最多 3 人）：**

```powershell
npm run server
```

或在 `Server` 目录：

```powershell
cd D:\UnityProject\lockstep\Server
npm start
```

**单人 / Agent 测试：**

```powershell
npm run server:single
```

或直接运行已构建的可执行文件：

```powershell
.\Server\RogueGameServer\bin\Debug\net8.0\RogueGameServer.exe --port 8888 --max-players 3
```

**命令行参数：**


| 参数              | 简写   | 说明                                           |
| --------------- | ---- | -------------------------------------------- |
| `--port`        | `-p` | 监听端口（默认见 `NetworkServerOptions.DefaultPort`） |
| `--max-players` | `-m` | 最大玩家数                                        |


按 **Enter** 或 **Ctrl+C** 停止服务器。

**客户端连接配置：** `FrameSyncClientComponent` 序列化字段 `serverIp = "127.0.0.1"`、`port = 8888`，需与服务器一致。

**联机测试步骤：**

1. 终端 A：`npm run server` 或 `npm run server:single`
2. Unity Editor：Play → 取消「单机模式」→ 选英雄 → 开始
3. （可选）再开一个 Unity 实例或 Build 客户端，连同一服务器做多人测试



### 4.4 快速编译检查（无需 Unity）

```powershell
# 编译整个解决方案
dotnet build Roguelike_Master.sln -nologo

# 仅检查运行时主程序集
dotnet build Assembly-CSharp.csproj -nologo -v:minimal
```



### 4.5 Agent 自动化入口（Editor）

菜单 **Tools/Agent/Run Client Game Entry** 可通过 Editor Agent 自动完成启动流程（见 `Assets/Scripts/Editor/Agent/ClientAgentGameEntryMenu.cs`）。

---



## 五、项目内置工具介绍

所有编辑器工具位于 `Assets/Scripts/Editor/`，**仅供 Editor 使用**，运行时程序集不应引用。

### 5.1 战斗与帧同步调试


| 工具          | 菜单路径           | 职责                                          |
| ----------- | -------------- | ------------------------------------------- |
| **预测回滚窗口**  | `Tools/调试/预测回滚`    | 配置 `forecastTick`、模拟丢包率 `lossPacket`，调试回滚行为 |
| **双世界线查看器** | `Tools/调试/双世界线查看器` | 并排对比本地预测世界线与权威世界线                           |
| **金手指工具**   | `Tools/调试/金手指工具`   | 运行时作弊：Buff / 属性 / 子弹等调试                     |




### 5.2 内容制作


| 工具            | 菜单路径                      | 职责                                                              |
| ------------- | ------------------------- | --------------------------------------------------------------- |
| **技能编辑器**     | `Tools/技能/技能编辑器`              | 编辑技能时间轴资产（Track / Clip），配合 `Assets/Scripts/RunTime/SkillEditor` |
| **实体技能浏览器**   | `Tools/技能/实体技能TimeLine浏览器`     | 浏览英雄/怪物技能绑定，一键打开技能编辑器                                      |
| **行为树编辑器**    | `Window/AI/BehaviourTree` | 可视化编辑 AI 行为树、黑板与节点脚本模板                                          |
| **Buff 制作工具** | `Tools/Buff/Buff制作工具`           | 制作 Buff 条件与效果配置                                                 |
| **配置中心**      | `Tools/配置中心`              | 集中管理 `Assets/GameAssetConfig` 下的 ScriptableObject 配置            |
| **角色工厂**      | `Tools/角色工厂/快速创建角色基础数据`   | 快速创建英雄基础配置                                                      |
| **怪物工厂**      | `Tools/怪物工厂/快速创建怪物基础数据`   | 快速创建怪物基础配置                                                      |
| **子弹工厂**      | `Tools/子弹工厂/快速配置子弹`       | 快速创建/编辑子弹配置与 View Prefab                                         |
| **地图生成**      | `Tools/地图/MapGenerate`        | 程序化地图地形与噪声生成                                                    |




### 5.3 资源与协议


| 工具                        | 菜单路径                                                                 | 职责                           |
| ------------------------- | -------------------------------------------------------------------- | ---------------------------- |
| **Addressables 同步**       | `Tools/Addressables/Sync Runtime Assets`                             | 将运行时资源同步到 Addressables Group |
| **Addressables 构建**       | `Tools/Addressables/Build Runtime Content`                           | 构建 Addressables 运行时内容        |
| **C# → Protobuf**         | `Tools/C# to Protobuf Converter`                                     | C# 类型转 `.proto` 定义           |
| **Protobuf 进阶转换**         | `Tools/Protobuf Converter/Advanced Converter`                        | 进阶 protobuf 转换               |
| **Protobuf 反射转换**         | `Tools/Protobuf Converter/Reflection Converter`                      | 反射式 protobuf 转换              |
| **生成 Google.Protobuf 源码** | `Tools/Protobuf Converter/Generate Google.Protobuf Sources`          | 从 `.proto` 生成 C# 源码          |
| **Protobuf 往返测试**         | `Tools/Protobuf Converter/Run Google.Protobuf Class Conversion Test` | 验证序列化/反序列化正确性                |


> **注意：** 修改 `.proto` 或生成文件后，需同步更新 Unity 客户端 `NetworkProtobufCodec`、`.NET` 服务器 `NetworkPacketCodec`，并重新构建两侧。



### 5.4 美术与动画辅助


| 工具           | 菜单路径                               | 职责           |
| ------------ | ---------------------------------- | ------------ |
| **低模树生成**    | `Tools/Art/Generate Low Poly Tree` | 程序化生成低多边形树模型 |
| **骨骼世界坐标曲线** | `Tools/骨骼世界坐标曲线`                   | 骨骼动画曲线测量与辅助  |




### 5.5 日志与 Agent


| 工具               | 菜单路径                            | 职责                            |
| ---------------- | ------------------------------- | ----------------------------- |
| **Log Channels** | `Tools/Log Channels`            | 管理 `GameLogChannel` 开关，过滤调试输出 |
| **Agent 烟雾测试**   | `Tools/Agent/Run Cheat * Smoke` | Agent 自动化 Buff / 属性 / 子弹等烟雾测试 |




### 5.6 资产创建快捷方式

- `Assets/Create/技能编辑器/SkillTimeLineAsset` — 创建技能时间轴资产

---



## 六、核心模块一览


| 模块                                      | 路径                                            | 职责                                          |
| --------------------------------------- | --------------------------------------------- | ------------------------------------------- |
| **GameEntry**                           | `Assets/Scripts/RunTime/GameEntry/`           | Unity 启动入口、运行时组件（Resource / UI / TcpClient） |
| **WorldSystem**                         | `Assets/Scripts/RunTime/GameSystem/`          | 世界频道创建、Update / FixedUpdate 分发              |
| **BaseWorld**                           | `Assets/Scripts/RunTime/World/`               | 帧循环、本地/权威/回滚三种更新、快照、回滚触发                    |
| **GameSession**                         | `Assets/Scripts/RunTime/GameSession/`         | 单机/联机 Profile、Transport 抽象                  |
| **CommandSystem / ServerCommandSystem** | `Assets/Scripts/RunTime/ECS/System/`          | 指令录制、发送、按帧获取权威指令                            |
| **FrameSyncClientComponent**            | `Assets/Scripts/RunTime/GameEntry/Component/` | TCP 连接、protobuf 收发、主线程事件分发                  |
| **EntitySystem**                        | `Assets/Scripts/RunTime/ECS/System/`          | 实体快照、硬/软回滚、预测校验                             |
| **RogueGameServer**                     | `Server/RogueGameServer/`                     | 独立 .NET 帧同步中继服务器                            |
| **NetworkProtobufCodec**                | `Assets/Scripts/RunTime/Server/Protobuf/`     | 运行时消息 ↔ protobuf 转换                         |
| **Path**                                | `Assets/Scripts/RunTime/Path/`                | 确定性 A* 寻路                                   |
| **SkillEditor / SkillData**             | `Assets/Scripts/RunTime/`                     | 技能时间轴执行与回滚                                  |
| **UnityBehaviourTree**                  | `Assets/Scripts/RunTime/UnityBehaviourTree/`  | 行为树运行时（支持 tick / snapshot / rollback）       |


---



## 七、目录结构速查

```text
roguelike/
├── Assets/
│   ├── Scripts/
│   │   ├── RunTime/          # 运行时主代码（ECS / World / Server / UI …）
│   │   └── Editor/           # 编辑器工具（技能 / 行为树 / 回滚调试 …）
│   ├── GameAssetConfig/      # ScriptableObject 配置资产
│   ├── Prefabs/              # 战斗、UI、技能 Prefab
│   ├── Art/                  # 美术资源
│   └── Proto/                # protobuf 协议定义
├── Server/
│   └── RogueGameServer/      # 独立 .NET 7 帧同步服务器
└── Packages/                 # Unity 包依赖
```

---



## 八、总结

本项目通过 **帧同步 + 本地预测 + 权威校验 + 快照回滚重放**，在保证**确定性**与**可重放性**的前提下，实现了：

- **低延迟体验**：本地立即按输入推进，无需等服务器每帧确认
- **自动纠错**：权威帧到达后比对快照，一旦不一致则回滚到出错帧并用录制输入重放
- **可测试性**：可注入丢包率（`lossPacket`）、预测帧数（`forecastTick`），通过 `Tools/调试/预测回滚` 与 `Tools/调试/双世界线查看器` 验证回滚正确性
- **演示视频**：见 [§2.5 演示视频](#25-演示视频)（软回滚 / 硬回滚实战录屏）
- **单机/联机统一管线**：同一套 `BaseWorld.FixedUpdate` 逻辑，通过 `IGameSessionProfile` 切换是否启用快照与回滚

适合作为 Roguelike、格斗、MOBA 等需要强一致性与手感反馈的联机游戏底层框架。

---

*最后更新：2026-06-27*