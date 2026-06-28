# FixedPoint

> 📍 位置: `Assets/Scripts/RunTime/FixedPoint/`
> 🔗 父文档: [RunTime CLAUDE.md](../CLAUDE.md)
> 📊 完善度: 🟡 中等

## 模块职责

这一层提供固定点数相关的数学扩展，服务于确定性战斗和帧同步。

## 目录结构

```text
FixedPoint/
├── Int3.cs
├── boolExtensions.cs
├── fpquaternion.cs
├── fpquaternionExtensions.cs
├── fpmath1.cs
└── fpmath1.Matrix.cs
```

## 核心内容

- `fpmath1`：项目自定义固定点数学工具集。
- `fpquaternion` / `fpquaternionExtensions`：固定点四元数支持。
- `Int3`：离散三维整数坐标辅助。

## 使用约定

- 核心战斗和回滚逻辑优先使用这里的 `fp` 工具。
- `float` 只适合表现层和编辑器辅助。

## 注意事项

- 这里的函数很多是项目定制实现，新增前先确认是否已经有对应工具。

---
<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-05-15
completeness: medium
-->
