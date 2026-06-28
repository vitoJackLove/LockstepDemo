# PrimitiveAndDectect

> 📍 位置: `Assets/Scripts/RunTime/PrimitiveAndDectect/`
> 🔗 父文档: [RunTime CLAUDE.md](../CLAUDE.md)
> 📊 完善度: 🟡 中等

## 模块职责

这一层提供几何体抽象和相交检测工具，服务于战斗命中、技能判定和调试。

## 目录结构

```text
PrimitiveAndDectect/
├── Base/
├── GJK/
├── Primitive/
├── PrimitiveEnum.cs
├── PrimitiveExtension.cs
├── PrimitiveInfo.cs
└── IntersectionDetection.cs
```

## 核心内容

- `BasePrimitive` 及其派生几何体。
- `IntersectionDetection`：大量碰撞检测和相交判断函数。
- `GJKDetecotor`：更复杂的凸体检测实现。
- `PrimitiveExtension`：矩阵、向量和几何辅助函数。

## 使用特点

- 以固定点数几何计算为主。
- 同时支持 2D / 3D 的命中和空间检测。

## 注意事项

- 这里的算法复杂且高度依赖几何假设，修改前先确认输入坐标系和形状约束。

---
<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-05-15
completeness: medium
-->
