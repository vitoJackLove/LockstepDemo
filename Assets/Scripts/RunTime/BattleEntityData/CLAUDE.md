# BattleEntityData

> 📍 位置: `Assets/Scripts/RunTime/BattleEntityData/`
> 🔗 父文档: [RunTime CLAUDE.md](../CLAUDE.md)
> 📊 完善度: 🟡 中等

## 模块职责

这一层承载战斗数值对象，负责实体属性的定义、读写和复用。

## 目录结构

```text
BattleEntityData/
├── BattleEntityData.cs
├── BattleHeroData.cs
├── BattleMonsterData.cs
├── PropertyData.cs
└── PropertyKey.cs
```

## 核心对象

- `BattleEntityData`：属性容器基类，内部用 `Dictionary<string, PropertyData>` 存属性。
- `BattleHeroData` / `BattleMonsterData`：具体实体属性集合。
- `PropertyKey`：属性键枚举。
- `PropertyData`：单个属性值的读写封装。

## 使用方式

- 初始化时由派生类实现 `InitProperty()`。
- 通过 `GetProperty` / `SetProperty` / `ChangeProperty` 访问数值。
- 该层对象实现 `IPool`，通常按对象池模式复用。

## 注意事项

- 新增属性先补 `PropertyKey`，再补 `PropertyData` 的初始化逻辑。
- 字符串键只是内部存储形式，调用侧尽量使用枚举键。

---
<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-05-15
completeness: medium
-->
