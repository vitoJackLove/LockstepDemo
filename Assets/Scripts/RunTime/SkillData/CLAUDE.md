# SkillData

> 📍 位置: `Assets/Scripts/RunTime/SkillData/`
> 🔗 父文档: [RunTime CLAUDE.md](../CLAUDE.md)
> 📊 完善度: 🟡 中等

## 模块职责

这一层负责技能执行时的数据对象、命令缓存和回滚支持。

## 目录结构

```text
SkillData/
├── AttackSkillData.cs
├── BaseSkillData.cs
├── BaseSkillData.RollBack.cs
├── BaseSkillExecute.cs
├── BaseSkillExecute.RollBack.cs
├── ChargingExecuteSkill.cs
├── CommandCacheData.cs
├── CommandCacheData.RollBack.cs
├── DownExecuteSkill.cs
├── NormalSkillData.cs
├── UpExecuteSkill.cs
└── MonsterSkillData/
```

## 核心内容

- `BaseSkillData`：技能数据基类。
- `BaseSkillExecute`：技能执行基类。
- `CommandCacheData`：命令缓存和回滚信息。
- `MonsterSkillData`：怪物技能相关数据。

## 使用特点

- 这一层和 `SkillEditor` 的运行时轨道数据是配套关系。
- 多个文件的 `RollBack` 分片说明这里支持快照和重放。

## 注意事项

- 技能执行数据的字段变更通常会影响回滚兼容性。

---
<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-05-15
completeness: medium
-->
