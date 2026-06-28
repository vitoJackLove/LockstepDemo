# SkillEditor

> 📍 位置: `Assets/Scripts/RunTime/SkillEditor/`
> 🔗 父文档: [RunTime CLAUDE.md](../CLAUDE.md)
> 📊 完善度: 🟡 中等

## 模块职责

这一层保存技能时间轴编辑器的运行时数据、轨道定义、剪辑定义和回滚支持。

## 目录结构

```text
SkillEditor/
├── Base/
│   ├── Attribute/
│   ├── BaseData/
│   ├── GraphOwner/
│   └── SkillTimeLineAssetsFactory/
└── RunTime/
    ├── BaseClipVariable/
    ├── Clip/
    ├── Driver/
    ├── SkillTimeLineData/
    ├── Track/
    ├── Variable/
    └── VariableType/
```

## 核心内容

- `SkillTimelineLauncher`：技能时间轴运行时入口。
- `SkillTimeLineData`：时间轴数据容器。
- `Track` / `Clip`：轨道与片段定义。
- `BaseClipVariable`：片段参数变量封装。
- `Attribute`：编辑器绘制和绑定标记。

## 使用特点

- 这个目录是“编辑器资产在运行时的配套实现”。
- 运行时 tick、回滚和时间轴驱动都已经拆到独立文件。

## 注意事项

- 修改轨道和片段时要同步考虑运行时和回滚文件。

---
<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-05-15
completeness: medium
-->
