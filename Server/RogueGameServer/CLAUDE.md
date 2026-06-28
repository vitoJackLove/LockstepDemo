# RogueGameServer

> 位置: `Server/RogueGameServer/`
> 父文档: [根目录 CLAUDE.md](../../CLAUDE.md)
> 完善度: 中等

## 模块职责

`RogueGameServer` 是独立 .NET 帧同步服务器。它监听 TCP 客户端、分配玩家索引、广播选英雄和加载开始消息、收集每个玩家的帧输入，并按服务器权威 tick 广播权威帧。它不运行 Unity 战斗模拟；客户端仍负责本地预测、权威校验和回滚。

## 目录结构

```text
RogueGameServer/
├── RogueGameServer.csproj        # .NET 7 控制台项目
├── Program.cs                    # 命令行入口和参数解析
├── generate-protobuf.ps1         # protobuf 生成辅助脚本
├── README.md                     # 服务端运行说明
└── Network/
    ├── NetworkServerOptions.cs   # 端口、最大玩家数、循环间隔等配置
    ├── FrameSyncServer.cs        # TCP 监听、房间状态、权威帧广播
    ├── FrameSyncConnection.cs    # 单连接输入缓存和包读写
    ├── NetworkPacketCodec.cs     # 服务端 protobuf codec
    ├── NetworkPacketStreamFramer.cs
    ├── NetworkPacketStreamUtility.cs
    └── BattleObserverEventMessage.cs
```

## 核心组件 / 类 / 系统

### `Program`
- **位置**: `Program.cs`
- **职责**: 解析 `--port/-p` 和 `--max-players/-m` 参数，创建 `FrameSyncServer`，等待 Enter 或 Ctrl+C 停止。

### `FrameSyncServer`
- **位置**: `Network/FrameSyncServer.cs`
- **职责**: 管理 TCP listener、accept 线程、game loop 线程、连接列表、玩家索引池和权威 tick。
- **关键流程**:
  - `AcceptClients()` 接入客户端并发送 `PlayerConnect`。
  - `GatherInputs()` 读取客户端包并缓存帧输入。
  - `TryBroadcastLoadStart()` 在所有玩家选英雄后广播加载开始。
  - `TryStartAuthorityFrames()` 在所有客户端准备后开始权威帧。
  - `TryBroadcastAuthorityFrame()` 等待每个连接提交对应 tick 的输入后广播权威帧。

### `FrameSyncConnection`
- **位置**: `Network/FrameSyncConnection.cs`
- **职责**: 封装单个 TCP 客户端、玩家索引、英雄选择状态、战斗 ready 状态和输入缓存。

### `NetworkPacketCodec`
- **位置**: `Network/NetworkPacketCodec.cs`
- **职责**: 服务端侧 protobuf 编解码，使用 `Rogue.Network.Proto` 生成类型。
- **共享协议**: `RogueGameServer.csproj` 通过 `Compile Include` 链接 `Assets/Scripts/RunTime/Server/Protobuf/Generated/NetworkPacketRuntime.cs`。

## 控制流

```text
dotnet run
  -> Program.ParseOptions()
  -> FrameSyncServer.Start()
  -> AcceptClients thread
  -> GameLoop thread
       -> GatherInputs()
       -> ProcessPacket()
       -> TryBroadcastAuthorityFrame()
  -> Stop()/Dispose()
```

## 对外接口

- 构建: `npm run server:build` 或 `dotnet build Server/RogueGameServer/RogueGameServer.csproj -nologo`
- 运行: `npm run server` 或 `cd Server && npm start`（默认 `0.0.0.0:8888`，最多 3 人）
- 单人测试: `npm run server:single`
- 默认端口和玩家数来自 `NetworkServerOptions`。

## 依赖关系

- **依赖**: .NET 7、`Google.Protobuf.dll`、Unity 侧生成的 `NetworkPacketRuntime.cs`。
- **被依赖**: Unity 客户端 `FrameSyncClientComponent` 和 `ServerCommandSystem` 按同一协议连接该服务端。

## 常见任务

### 修改网络协议
1. 修改 `.proto` 定义并重新生成 `NetworkPacketRuntime.cs`。
2. 同步更新 Unity 侧 `NetworkProtobufCodec`。
3. 同步更新服务端 `NetworkPacketCodec`。
4. 构建 Unity `Assembly-CSharp.csproj` 和服务端 `RogueGameServer.csproj`。

### 调整权威帧策略
1. 从 `FrameSyncServer.TryBroadcastAuthorityFrame()` 入手。
2. 检查 `MaxAuthorityFramesPerLoop`、`GameLoopSleepMilliseconds` 和每连接输入缓存。
3. 注意客户端预测窗口 `_forecastTick` 与服务端广播节奏的配合。

### 排查连接问题
1. 检查服务端端口和客户端 `FrameSyncClientComponent` 序列化字段。
2. 查看服务端控制台日志中的 `PlayerIndex`、`SelectHero`、`FrameCommand`。
3. 若只收到部分帧，检查客户端是否持续发送对应 tick 的 `FrameCommand`。

## 注意事项

- `bin/`、`obj/` 是构建产物，不应提交或手工维护。
- 服务端不会模拟完整战斗状态，它只聚合和广播输入/权威帧。
- 生成 protobuf 类型是 Unity 和 .NET server 的共享协议面，不要只改一侧。
- 线程退出通过 `_isRunning`、listener stop 和 join 控制，新增阻塞逻辑时要保证 `Stop()` 可退出。

<!-- USER_CONTENT_START -->
<!-- 在这里保留用户手写补充内容 -->
<!-- USER_CONTENT_END -->

## 更新日志

| 日期 | 变更 | 模式 | 备注 |
|------|------|------|------|
| 2026-06-16 | 初始创建独立帧同步服务端文档 | deep | 补齐 Unity 客户端之外的 server 边界 |

---
<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-06-16
completeness: medium
-->
