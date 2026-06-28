# Roguelike Unity Project

## Project Overview

这是一个 Unity Roguelike 项目，核心是固定点数驱动的帧同步战斗、ECS 运行时、行为树 AI、技能时间轴和回滚/快照体系。

## Tech Stack

- Unity
- ECS / partial class
- FixedPoint (`Unity.Mathematics.FixedPoint`)
- Loxodon.Framework
- UniTask
- Google.Protobuf
- TheKiwiCoder / Unity Behaviour Tree

## Repository Map

```text
Assets/
├── Scripts/
│   ├── RunTime/                 # 运行时主代码
│   │   ├── CLAUDE.md            # 运行时总索引
│   │   ├── ECS/                 # 实体 / 组件 / 系统
│   │   ├── World/               # 世界生命周期与帧同步
│   │   ├── GameEntry/           # 启动入口与运行时组件
│   │   ├── GameSystem/          # 世界分发器
│   │   ├── Singleton/           # 单例基础设施
│   │   ├── Pool/                # 对象池
│   │   ├── UI/                  # UI 框架与窗口
│   │   ├── Server/              # 网络消息与 protobuf 编解码
│   │   ├── SkillEditor/         # 技能编辑器运行时
│   │   ├── UnityBehaviourTree/  # 行为树运行时
│   │   ├── Path/                # A* 与帧同步寻路
│   │   ├── PrimitiveAndDectect/ # 几何体与碰撞检测
│   │   ├── FixedPoint/          # 固定点数学
│   │   └── ...
│   ├── Editor/                  # 编辑器扩展与内容制作工具
│   └── Libraries/               # 第三方库
├── GameAssetConfig/             # 运行时 ScriptableObject 配置资产
├── Prefabs/                     # 战斗、UI、技能、行为树等 Prefab/资产
├── Art/                         # 角色、怪物、动画、美术源资产
├── Packages/                    # 包依赖
├── ProjectSettings/             # Unity 项目设置
├── Server/RogueGameServer/      # 独立 .NET 帧同步服务器
└── .claude/                     # 文档规范与辅助文件
```

## Architecture

### Runtime Flow

1. `GameEntry` 完成运行时组件初始化。
2. `GameSystem.WorldSystem` 创建并管理 `BaseWorld` 实例。
3. `BaseWorld` 注册系统、创建根节点、驱动 `Update` / `FixedUpdate`。
4. `ECS` 层管理实体、组件和系统。
5. `SkillData`、`SkillEditor`、`UnityBehaviourTree`、`Path`、`PrimitiveAndDectect` 为战斗逻辑提供支撑。
6. `FrameSyncClientComponent` 与 `Server/RogueGameServer` 通过共享 protobuf 协议交换帧同步消息。

### Key Patterns

- `partial class` 拆分大型实体、系统、技能类。
- `Singleton<T>` 管理全局入口和基础设施。
- 对象池贯穿 `CreateData` / `FPoolHelper` / 运行时临时对象。
- 核心战斗逻辑使用 `fp` / `fp3` / `fpquaternion`，避免浮点漂移。
- 运行时资源加载优先走 Addressables；`ResourceComponent` 仅保留 Addressables 和 Editor 加载模式。

## Directory Index

### Core Runtime

| 模块 | 路径 | 职责 | 文档 |
|------|------|------|------|
| Runtime 总索引 | `Assets/Scripts/RunTime/` | 运行时入口与总览 | [CLAUDE.md](./Assets/Scripts/RunTime/CLAUDE.md) |
| ECS | `Assets/Scripts/RunTime/ECS/` | 实体、组件、系统、视图 | [CLAUDE.md](./Assets/Scripts/RunTime/ECS/CLAUDE.md) |
| World | `Assets/Scripts/RunTime/World/` | 世界生命周期、帧同步、回滚 | [CLAUDE.md](./Assets/Scripts/RunTime/World/CLAUDE.md) |
| GameEntry | `Assets/Scripts/RunTime/GameEntry/` | 启动入口、运行时组件 | [CLAUDE.md](./Assets/Scripts/RunTime/GameEntry/CLAUDE.md) |
| GameSystem | `Assets/Scripts/RunTime/GameSystem/` | 世界分发与模式创建 | [CLAUDE.md](./Assets/Scripts/RunTime/GameSystem/CLAUDE.md) |
| Singleton | `Assets/Scripts/RunTime/Singleton/` | 单例基础设施 | [CLAUDE.md](./Assets/Scripts/RunTime/Singleton/CLAUDE.md) |
| Pool | `Assets/Scripts/RunTime/Pool/` | 对象池 | [CLAUDE.md](./Assets/Scripts/RunTime/Pool/CLAUDE.md) |
| UI | `Assets/Scripts/RunTime/UI/` | 窗口、数据绑定、UI 效果 | [CLAUDE.md](./Assets/Scripts/RunTime/UI/CLAUDE.md) |
| Server | `Assets/Scripts/RunTime/Server/` | 网络消息与 protobuf | [CLAUDE.md](./Assets/Scripts/RunTime/Server/CLAUDE.md) |
| SkillEditor | `Assets/Scripts/RunTime/SkillEditor/` | 技能时间轴运行时数据 | [CLAUDE.md](./Assets/Scripts/RunTime/SkillEditor/CLAUDE.md) |
| SkillData | `Assets/Scripts/RunTime/SkillData/` | 技能执行数据与回滚 | [CLAUDE.md](./Assets/Scripts/RunTime/SkillData/CLAUDE.md) |
| UnityBehaviourTree | `Assets/Scripts/RunTime/UnityBehaviourTree/` | 运行时行为树 | [CLAUDE.md](./Assets/Scripts/RunTime/UnityBehaviourTree/CLAUDE.md) |
| Path | `Assets/Scripts/RunTime/Path/` | A* 与帧同步寻路 | [CLAUDE.md](./Assets/Scripts/RunTime/Path/CLAUDE.md) |
| PrimitiveAndDectect | `Assets/Scripts/RunTime/PrimitiveAndDectect/` | 几何体与碰撞检测 | [CLAUDE.md](./Assets/Scripts/RunTime/PrimitiveAndDectect/CLAUDE.md) |
| FixedPoint | `Assets/Scripts/RunTime/FixedPoint/` | 固定点数学工具 | [CLAUDE.md](./Assets/Scripts/RunTime/FixedPoint/CLAUDE.md) |

### Tools and Services

| 模块 | 路径 | 职责 | 文档 |
|------|------|------|------|
| Editor Tools | `Assets/Scripts/Editor/` | Unity 编辑器工具、配置、技能、行为树、protobuf、Addressables 辅助 | [CLAUDE.md](./Assets/Scripts/Editor/CLAUDE.md) |
| RogueGameServer | `Server/RogueGameServer/` | 独立 .NET 7 帧同步服务器 | [CLAUDE.md](./Server/RogueGameServer/CLAUDE.md) |

### Data and Content

| 模块 | 路径 | 职责 | 文档 |
|------|------|------|------|
| EntityAssetsConfig | `Assets/Scripts/RunTime/EntityAssetsConfig/` | 实体资源配置 | [CLAUDE.md](./Assets/Scripts/RunTime/EntityAssetsConfig/CLAUDE.md) |
| BattleEntityData | `Assets/Scripts/RunTime/BattleEntityData/` | 战斗属性数据 | [CLAUDE.md](./Assets/Scripts/RunTime/BattleEntityData/CLAUDE.md) |
| DataTable | `Assets/Scripts/RunTime/DataTable/` | 表名索引与数据表访问 | [CLAUDE.md](./Assets/Scripts/RunTime/DataTable/CLAUDE.md) |
| Event | `Assets/Scripts/RunTime/Event/` | 事件条件与效果 | [CLAUDE.md](./Assets/Scripts/RunTime/Event/CLAUDE.md) |
| DamageText | `Assets/Scripts/RunTime/DamageText/` | 伤害飘字 UI | [CLAUDE.md](./Assets/Scripts/RunTime/DamageText/CLAUDE.md) |
| MapData | `Assets/Scripts/RunTime/MapData/` | 地图生成数据 | [CLAUDE.md](./Assets/Scripts/RunTime/MapData/CLAUDE.md) |
| RollBackData | `Assets/Scripts/RunTime/RollBackData/` | 回滚记录与指纹 | [CLAUDE.md](./Assets/Scripts/RunTime/RollBackData/CLAUDE.md) |
| SnapShotData | `Assets/Scripts/RunTime/SnapShotData/` | 世界快照 | [CLAUDE.md](./Assets/Scripts/RunTime/SnapShotData/CLAUDE.md) |
| BaseVolume | `Assets/Scripts/RunTime/BaseVolume/` | 命中体 / 体积数据 | 目录型 |
| WolrdData | `Assets/Scripts/RunTime/WolrdData/` | 世界创建数据与命令数据 | [CLAUDE.md](./Assets/Scripts/RunTime/WolrdData/CLAUDE.md) |
| WolrdContent | `Assets/Scripts/RunTime/WolrdContent/` | 世界内容常量与状态 | [CLAUDE.md](./Assets/Scripts/RunTime/WolrdContent/CLAUDE.md) |

## Development Notes

- 保持 `RunTime` 下的目录名不变，包含历史拼写 `WolrdData` 和 `WolrdContent`。
- 新增实体优先走 `ECS` + `EntityAssetsConfig` + `World.GetSystemTypes()`。
- 新增技能逻辑优先确认是否属于 `SkillData`、`SkillEditor` 还是 `UnityBehaviourTree`。
- 网络消息优先放入 `Server`，protobuf 编解码继续沿用现有消息类型。
- 协议变更必须同步 Unity 客户端、`Assets/Proto*`、生成的 protobuf 文件和 `Server/RogueGameServer`。
- 编辑器工具放在 `Assets/Scripts/Editor`，不要让运行时程序集依赖 `UnityEditor`。

## Dependencies

- `com.danielmansson.mathematics.fixedpoint`
- `com.unity.addressables`
- `com.unity.timeline`
- `com.unity.visualscripting`
- `Google.Protobuf`

## Build and Run

- `dotnet build Roguelike_Master.sln -nologo`: 快速编译 Unity 生成的 C# 工程。
- `dotnet build Assembly-CSharp.csproj -nologo -v:minimal`: 快速检查运行时主程序集。
- `npm run server`: 启动本地帧同步服务器（默认 `127.0.0.1:8888`，最多 3 人）。
- `npm run server:single`: 启动单人 / Agent 测试服务器。
- `npm run server:build`: 构建独立 .NET 8 帧同步服务器。
- `dotnet build Server/RogueGameServer/RogueGameServer.csproj -nologo`: 直接构建帧同步服务器。
- `dotnet run --project Server/RogueGameServer/RogueGameServer.csproj`: 直接启动本地帧同步服务器。
- Unity Test Runner: 在 Editor 中运行 EditMode / PlayMode 测试；批处理可使用 Unity `-batchmode -runTests`。

## Build Settings

- Platform: Windows
- API Compatibility: .NET Standard 2.1
- Script Backend: IL2CPP

---
<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-06-16
coverage: 86%
mode: deep
-->
