# Server 模块

> 位置: `Assets/Scripts/RunTime/Server/`
> 父文档: [RunTime CLAUDE.md](../CLAUDE.md)
> 完善度: 完善

## 模块职责

`Assets/Scripts/RunTime/Server` 是 Unity 客户端侧的网络消息与 protobuf 编解码层。它定义运行时消息对象、将消息转换为 `Google.Protobuf` 生成类型，并提供 TCP 包体的长度前缀读写工具。独立 .NET 帧同步服务器位于 [Server/RogueGameServer](../../../../Server/RogueGameServer/CLAUDE.md)。

## 目录结构

```text
Server/
├── PlayerConnectMessage.cs        # 服务器分配玩家索引
├── SelectHeroMessage.cs           # 选英雄消息
├── GameStartMessage.cs            # 加载/战斗开始消息
├── FrameCommandMessage.cs         # 单帧输入与权威帧命令
└── Protobuf/
    ├── README.md                  # protobuf 工具说明
    ├── NetworkProtobufCodec.cs    # runtime <-> protobuf 转换
    ├── NetworkPacketStreamUtility.cs # TCP 长度前缀包读写
    └── Generated/
        └── NetworkPacketRuntime.cs # 生成的 protobuf C# 类型
```

## 核心组件 / 类 / 系统

### 消息对象
- **位置**: `PlayerConnectMessage.cs`、`SelectHeroMessage.cs`、`GameStartMessage.cs`、`FrameCommandMessage.cs`
- **职责**: 作为 Unity 运行时和观察者系统使用的消息模型，通常实现 `IObserverParams` 并暴露 `ObserverEventType`。
- **使用方**: `FrameSyncClientComponent` 收到包后反序列化为这些对象，再调用 `GameEntry.Observer.Notify()`。

### `NetworkProtobufCodec`
- **位置**: `Protobuf/NetworkProtobufCodec.cs`
- **职责**: 在运行时消息对象和 `Rogue.Network.Proto` 生成类型之间转换。
- **关键接口**:
  - `SerializePacket(BattleObserverEventEnum, IObserverParams)`
  - `TryDeserializePacket(byte[], out BattleObserverEventEnum, out IObserverParams)`
  - `TryGetMessageType(byte[], out BattleObserverEventEnum)`
- **定点数处理**: `fp3` 通过 `FixedVectorScale = 10000` 量化为 int，避免直接传浮点。

### `NetworkPacketStreamUtility`
- **位置**: `Protobuf/NetworkPacketStreamUtility.cs`
- **职责**: 对 TCP stream 进行长度前缀包读写，避免粘包/半包问题。
- **约束**: 包大小上限与 `NetworkProtobufCodec.MaxPacketBytes` 保持一致。

### `NetworkPacketRuntime.cs`
- **位置**: `Protobuf/Generated/NetworkPacketRuntime.cs`
- **职责**: 由 `.proto` 生成的 C# 类型，同时被 Unity 客户端和 `Server/RogueGameServer` 链接使用。
- **注意**: 这是生成代码，协议变更应从 `.proto` 或生成工具链开始，不要手工改生成文件。

## 数据流 / 控制流

```text
Unity runtime message
  -> NetworkProtobufCodec.ToProto(...)
  -> Proto.NetworkPacket { MessageType, Payload }
  -> NetworkPacketStreamUtility.WritePacket(...)
  -> TCP
  -> TryReadPacket(...)
  -> NetworkProtobufCodec.TryDeserializePacket(...)
  -> IObserverParams
  -> GameEntry.Observer.Notify(...)
```

## 对外接口

- `FrameSyncClientComponent.Send(content, messageType)` 使用本模块序列化并发送消息。
- `FrameSyncClientComponent.ReceiveData()` 使用本模块读取和反序列化服务端消息。
- `Server/RogueGameServer` 的 `NetworkPacketCodec` 使用同一份生成类型实现服务端侧协议。

## 依赖关系

- **依赖**: `Google.Protobuf`、`Unity.Mathematics.FixedPoint`、运行时消息枚举 `BattleObserverEventEnum`、命令数据 `CommandData`。
- **被依赖**: `FrameSyncClientComponent`、`ServerCommandSystem`、独立帧同步服务器、协议测试和 Protobuf 编辑器工具。

## 常见任务

### 新增网络消息
1. 新增或扩展运行时消息类，并保持 `ObserverEventType` 与业务事件一致。
2. 更新 `.proto` 定义并重新生成 `NetworkPacketRuntime.cs`。
3. 在 `NetworkProtobufCodec.CreatePayload()` 和 `ParsePayload()` 中加入转换逻辑。
4. 同步更新服务端 `NetworkPacketCodec`。
5. 增加或更新 protobuf round-trip 测试。

### 修改 `CommandData`
1. 同步修改运行时 `CommandData`、protobuf `CommandData`、客户端 codec、服务端 codec。
2. 保持定点数量化规则一致。
3. 检查回滚、快照和服务器权威帧聚合逻辑是否需要兼容新字段。

## 注意事项

- `BattleObserverEventEnum.None` 不会作为合法网络包类型发送。
- `MessageType` wire 值带有偏移，客户端和服务端必须保持一致。
- 不要绕开 `NetworkPacketStreamUtility` 直接写 TCP stream。
- 生成 protobuf 文件属于共享协议面，变更需要同时验证 Unity 和 .NET server。

<!-- USER_CONTENT_START -->
<!-- 在这里保留用户手写补充内容 -->
<!-- USER_CONTENT_END -->

## 更新日志

| 日期 | 变更 | 模式 | 备注 |
|------|------|------|------|
| 2026-06-16 | 深化 protobuf 包流、消息转换和服务端同步说明 | deep | 对齐当前 NetworkProtobufCodec 实现 |

---
<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-06-16
completeness: complete
-->
