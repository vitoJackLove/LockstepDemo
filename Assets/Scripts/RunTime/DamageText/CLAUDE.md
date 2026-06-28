# DamageText

> 📍 位置: `Assets/Scripts/RunTime/DamageText/`
> 🔗 父文档: [RunTime CLAUDE.md](../CLAUDE.md)
> 📊 完善度: 🟡 中等

## 模块职责

这一层负责战斗飘字和跟随 UI 的表现逻辑。

## 目录结构

```text
DamageText/
├── EntityInfoBase.cs
├── RandomizeOffsetX.cs
└── UITextDamage.cs
```

## 核心对象

- `EntityInfoBase`：飘字 UI 的基类，负责跟随目标和 Canvas 刷新。
- `UITextDamage`：实际显示伤害文本的实现。
- `RandomizeOffsetX`：用于文本位置偏移的辅助逻辑。

## 行为特点

- 支持世界空间和屏幕空间。
- 通过生命周期计时自动回收显示状态。
- 结合随机偏移，避免多个飘字完全重叠。

## 注意事项

- 这类对象通常在高频战斗里被反复创建和激活，优先检查池化与复用。

---
<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-05-15
completeness: medium
-->
