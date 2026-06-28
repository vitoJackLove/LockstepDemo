# GameSystem

> 📍 位置: `Assets/Scripts/RunTime/GameSystem/`
> 🔗 父文档: [RunTime CLAUDE.md](../CLAUDE.md)
> 📊 完善度: 🟡 中等

## 模块职责

这一层负责世界频道的创建、切换、更新和销毁，是 `World` 的外部调度器。

## 目录结构

```text
GameSystem/
├── WorldModel.cs
└── WorldSystem.cs
```

## 核心对象

- `WorldModel`：世界模式枚举，当前包含 `RogueLike` 和 `CardGame`。
- `WorldSystem`：世界管理单例，负责创建 `BaseWorld`、分发 `Update` / `FixedUpdate`、销毁世界。

## 流程

1. `CreateWorldChannel` 生成世界根节点。
2. 创建 `CreateWorldData` 并填充英雄列表与场景名。
3. 根据 `WorldModel` 选择具体世界实现。
4. 调用世界初始化、进入和启动流程。
5. 运行时由 `Update` / `FixedUpdate` 驱动当前世界。

## 注意事项

- 这里是世界创建入口，不要把具体战斗逻辑塞进来。
- 世界列表是多实例的，处理生命周期时要留意并发更新和销毁时机。

---
<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-05-15
completeness: medium
-->
