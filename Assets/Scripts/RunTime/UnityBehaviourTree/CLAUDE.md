# UnityBehaviourTree

> 📍 位置: `Assets/Scripts/RunTime/UnityBehaviourTree/`
> 🔗 父文档: [RunTime CLAUDE.md](../CLAUDE.md)
> 📊 完善度: 🟡 中等

## 模块职责

这一层是项目自己的行为树运行时，不是编辑器插件层。

## 目录结构

```text
UnityBehaviourTree/
├── Actions/
├── Composites/
├── Conditions/
├── Decorators/
├── BehaviourTree.cs
├── BehaviourTreeBuilder.cs
├── BehaviourTreeInstance.cs
├── Blackboard.cs
├── Context.cs
└── ...
```

## 核心内容

- `BehaviourTree`：树资产和运行时入口。
- `BehaviourTreeInstance`：实例化与执行状态。
- `Blackboard`：黑板数据。
- `Node` 系列：根节点、组合节点、装饰节点、条件节点、行动节点。

## 使用特点

- 支持 tick、snapshot 和 rollback。
- 节点按照 `Composite / Decorator / Condition / Action` 拆分。

## 注意事项

- 这里的树是运行时代码；编辑器窗口在 `Assets/Scripts/Editor/UnityBehaviourTreeEditor/`。

---
<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-05-15
completeness: medium
-->
