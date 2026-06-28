# Event

> 📍 位置: `Assets/Scripts/RunTime/Event/`
> 🔗 父文档: [RunTime CLAUDE.md](../CLAUDE.md)
> 📊 完善度: 🟡 中等

## 模块职责

这一层描述战斗事件的条件和效果链。

## 目录结构

```text
Event/
├── EventCondition/
│   └── EventCondition.cs
└── EventEffect/
    ├── EventEffect.cs
    └── PropertyEffect.cs
```

## 核心对象

- `EventCondition`：事件触发前的条件判断基类。
- `EventEffect`：事件触发后的效果基类。
- `PropertyEffect`：属性修改类效果。

## 使用方式

- 条件负责判断是否可以进入效果阶段。
- 效果负责真正修改战斗状态或属性。
- 这一层通常和技能、Buff、战斗观察系统一起使用。

## 注意事项

- 条件和效果尽量保持无副作用拆分，便于组合和回放。

---
<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-05-15
completeness: medium
-->
