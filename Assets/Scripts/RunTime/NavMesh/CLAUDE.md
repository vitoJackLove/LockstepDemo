# NavMesh

> 📍 位置: `Assets/Scripts/RunTime/NavMesh/`
> 🔗 父文档: [RunTime CLAUDE.md](../CLAUDE.md)
> 📊 完善度: 🟢 基础

## 模块职责

这一层是项目自带的路径插值和调试辅助，不是 Unity 原生 NavMesh 系统。

## 目录结构

```text
NavMesh/
├── AIPathVisualizerGizmos.cs
├── PathInterpolator.cs
└── VectorMath.cs
```

## 核心内容

- `PathInterpolator`：路径插值。
- `VectorMath`：向量运算辅助。
- `AIPathVisualizerGizmos`：路径可视化调试。

## 注意事项

- 这个目录更多是辅助工具，不要和 `Path/` 中的主寻路系统混淆。

---
<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-05-15
completeness: basic
-->
