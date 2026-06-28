# Path

> 📍 位置: `Assets/Scripts/RunTime/Path/`
> 🔗 父文档: [RunTime CLAUDE.md](../CLAUDE.md)
> 📊 完善度: 🟡 中等

## 模块职责

这一层负责项目的 A* 寻路和帧同步路径包装。

## 目录结构

```text
Path/
├── AStarPathfinder.cs
├── AStarPathfinderExample.cs
├── FixedPointPath.cs
├── FrameSyncPathfinderWrapper.cs
├── FrameSyncPathfindingSystem.cs
├── GridNode.cs
├── IFrameSyncPathfinder.cs
├── MinHeap.cs
├── Test.cs
└── ...
```

## 核心内容

- `AStarPathfinder`：基于网格的 A* 实现。
- `GridNode` / `MinHeap`：路径搜索数据结构。
- `FrameSyncPathfindingSystem`：和帧同步逻辑配合的路径系统。
- `FrameSyncPathfinderWrapper`：对外包装接口。

## 使用特点

- 以网格和固定点数为主，目标是可预测、可复现。
- 支持世界坐标和网格坐标之间的转换。

## 注意事项

- 这里不是 Unity NavMesh Agent 方案，而是项目自定义路径系统。

---
<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-05-15
completeness: medium
-->
