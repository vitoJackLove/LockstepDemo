# RunTime 运行时总索引

> 📍 位置: `Assets/Scripts/RunTime/`
> 🔗 父文档: [根目录 CLAUDE.md](../../../CLAUDE.md)
> 📊 完善度: 🟡 中等

## 运行时职责

这一层放置所有游戏运行时代码，覆盖入口、世界管理、ECS、技能、AI、寻路、碰撞、数据和 UI。

## 总体调用链

`GameEntry` -> `GameSystem.WorldSystem` -> `BaseWorld` -> `ECS` / `SkillData` / `UnityBehaviourTree` / `Path`

## 顶层目录索引

| 目录 | 职责 | 文档 |
|------|------|------|
| `ECS/` | 实体、组件、系统、视图 | [CLAUDE.md](./ECS/CLAUDE.md) |
| `World/` | 世界生命周期、帧同步、回滚 | [CLAUDE.md](./World/CLAUDE.md) |
| `GameEntry/` | Unity 启动入口与运行时组件 | [CLAUDE.md](./GameEntry/CLAUDE.md) |
| `GameSystem/` | 世界频道分发与模式创建 | [CLAUDE.md](./GameSystem/CLAUDE.md) |
| `Singleton/` | 全局单例基础设施 | [CLAUDE.md](./Singleton/CLAUDE.md) |
| `Pool/` | 对象池接口与工厂 | [CLAUDE.md](./Pool/CLAUDE.md) |
| `UI/` | 窗口、绑定、表现层 | [CLAUDE.md](./UI/CLAUDE.md) |
| `Server/` | 帧同步消息与 protobuf | [CLAUDE.md](./Server/CLAUDE.md) |
| `SkillData/` | 技能执行数据、回滚与命令缓存 | [CLAUDE.md](./SkillData/CLAUDE.md) |
| `SkillEditor/` | 技能时间轴运行时数据 | [CLAUDE.md](./SkillEditor/CLAUDE.md) |
| `UnityBehaviourTree/` | 行为树资产和节点运行时 | [CLAUDE.md](./UnityBehaviourTree/CLAUDE.md) |
| `Path/` | A*、路径包装与帧同步寻路 | [CLAUDE.md](./Path/CLAUDE.md) |
| `PrimitiveAndDectect/` | 几何体和碰撞检测 | [CLAUDE.md](./PrimitiveAndDectect/CLAUDE.md) |
| `FixedPoint/` | 定点数数学辅助 | [CLAUDE.md](./FixedPoint/CLAUDE.md) |
| `BattleEntityData/` | 属性表、属性值与战斗数值 | [CLAUDE.md](./BattleEntityData/CLAUDE.md) |
| `DataTable/` | 数据表索引 | [CLAUDE.md](./DataTable/CLAUDE.md) |
| `Event/` | 条件与效果链 | [CLAUDE.md](./Event/CLAUDE.md) |
| `DamageText/` | 飘字 UI 逻辑 | [CLAUDE.md](./DamageText/CLAUDE.md) |
| `NavMesh/` | 路径插值与调试辅助 | [CLAUDE.md](./NavMesh/CLAUDE.md) |
| `BaseVolume/` | 体积和命中体 | 目录型 |
| `WolrdData/` | 世界创建、命令与实体数据 | [CLAUDE.md](./WolrdData/CLAUDE.md) |
| `WolrdContent/` | 世界内容常量 | [CLAUDE.md](./WolrdContent/CLAUDE.md) |

## Important Conventions

- 固定点数是核心战斗逻辑的默认数值类型。
- 运行时类大量使用 `partial`，新增行为时先找同名拆分文件。
- 资源和数据入口优先走已有组件，不要在运行时层随手引入新的全局静态。
- 保留历史拼写 `Wolrd*`，不要为了文档统一而改目录名。

## Notes

- `Library/`、`Temp/`、`obj/`、`node_modules/` 不属于运行时文档范围。

---
<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-05-15
coverage: 82%
mode: deep
-->
