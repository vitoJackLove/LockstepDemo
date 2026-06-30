---
name: entity-skill-timeline-config
description: Configures hero and monster SkillTimeLine bindings in the lockstep Unity project. Use when binding SkillLineAsset to HeroAssets/MonsterAssets, creating entity skills, editing skill timeline assets, fixing missing skill assets, or working with EntitySkillTimelineBrowser, SkillTimelineEditor, HeroFactory, or MonsterFactory. Does NOT create or modify skill editor nodes (TaskClip/StandardTrack code); use skill-editor-node-authoring for that.
disable-model-invocation: true
---

# 实体技能 TimeLine 配置

项目级技能，路径：`.agents/skills/entity-skill-timeline-config/`。

## 范围约束（必须遵守）

**本技能只做「配置与资产编排」，不做「节点代码开发」。**

### 允许

- 绑定/修改 `HeroAssets`、`MonsterAssets` 中的技能项
- 创建、移动、复制 `SkillLineAsset` 资产文件
- 在 `Tools/技能/技能编辑器` 中用**已有** Clip/Track 类型编排时间轴（拖片段、改参数、黑板变量）
- 用浏览器、快速创建窗口、配置中心排查缺失路径

### 禁止

- **不得新增** `TaskClip` / `StandardTrack` 子类或任何技能编辑器节点 C# 代码
- **不得修改** 已有 Clip/Track 实现（含 `[ClipName]`、`[TrackBindClip]`、`OnRunTimeEnter` 等）
- **不得修改** `Assets/Scripts/RunTime/SkillEditor/` 下运行时节点逻辑以「实现新效果」
- **不得修改** `Assets/Scripts/Editor/SkillEditor/` 下编辑器工厂/节点注册逻辑

若用户需求超出已有节点能力（例如「做一个全新类型的技能节点」），**停止本技能流程**，改用手动调用 `skill-editor-node-authoring`。

## 适用场景

- 为英雄/怪物新增或修改技能绑定
- 编辑 `SkillLineAsset` 时间轴内容（用已有轨道/片段、黑板变量）
- 排查「未配置 / 缺失」SkillTimeLine 资产
- 通过快速创建窗口批量生成实体技能骨架

## 核心概念

| 层级 | 资产/类型 | 路径 |
|------|-----------|------|
| 配置表 | `HeroAssets.asset` / `MonsterAssets.asset` | `Assets/GameAssetConfig/` |
| 英雄技能项 | `HeroSkillConfig`（`initSkillList`） | 含按下/抬起双路径 |
| 怪物技能项 | `MonsterSkillConfig`（`monsterSkillList`） | 单一路径 `skillAssetsPath` |
| 时间轴资产 | `SkillLineAsset` | `Assets/Prefabs/Battle/Skill/{entityId}/{skillId}.asset` |
| 配置中存储 | 相对路径 | `Skill/{entityId}/{skillId}`（无 `Assets/` 前缀、无 `.asset` 后缀） |

路径解析由 `SkillTimelinePathUtility.ResolveEditorAssetPath` 完成，前缀 `Assets/Prefabs/Battle/`。

## 编辑器入口

| 菜单 | 用途 |
|------|------|
| `Tools/技能/实体技能TimeLine浏览器` | 浏览所有英雄/怪物技能绑定，一键编辑/定位 |
| `Tools/技能/技能编辑器` | 打开 SkillTimeLine 编辑器 |
| `Tools/角色工厂/快速创建角色基础数据` | 新建英雄 + 默认技能资产与配置 |
| `Tools/怪物工厂/快速创建怪物基础数据` | 新建怪物 + 默认技能资产与配置 |
| `Tools/配置中心` | 直接编辑 `HeroAssets.asset` / `MonsterAssets.asset` |
| `Assets/Create/技能编辑器/SkillTimeLineAsset` | 手动创建空 SkillLineAsset |

## 工作流

### 1. 先定位实体与现有绑定

1. 打开 `Tools/技能/实体技能TimeLine浏览器`。
2. 在左侧树选择目标英雄或怪物。
3. 查看技能表格：技能 ID、绑定类型、配置路径、FPS、时长、轨道数、资产状态。
4. 状态为「已找到」才可正常编辑；「未配置」或「缺失」需补资产或修正路径。

优先用浏览器「编辑」「定位」按钮，不要手工拼绝对路径。

### 2. 编辑已有 SkillTimeLine

1. 在浏览器点击「编辑」，或调用 `SkillTimelineEditorWindow.OpenWindowWithConfigPath(configPath)`。
2. 在时间轴中添加/调整**已有类型**的轨道与片段，配置黑板变量与片段参数。
3. 若菜单里没有所需片段类型 → **不要写代码**，告知用户需 `skill-editor-node-authoring` 先实现节点。
4. 保存后确认 `duration` 与 `fps` 合理（快速创建默认 `fps=30`，`duration=60`）。
5. 回到浏览器点「刷新」，确认状态变为「已找到」。

### 3. 为已有实体新增技能

**英雄**

1. 在 `HeroAssets.asset` 对应 `HeroAssetsConfig.initSkillList` 追加 `HeroSkillConfig`。
2. 填写 `skillId`（建议 `{entityId}*100 + N`，避免与现有 ID 冲突）。
3. 设置 `commandState`、`keyCode`，以及 `skillDownAssetsPath` / `skillUpAssetsPath`（见下方差异表）。
4. 创建 `SkillLineAsset` 到 `Assets/Prefabs/Battle/Skill/{entityId}/{skillId}.asset`。
5. 配置路径写相对形式：`Skill/{entityId}/{skillId}`。
6. 若运行时走 Addressables，同步 Addressables 配置（`Tools/Addressables/Sync Runtime Assets`）。

**怪物**

1. 在 `MonsterAssets.asset` 对应 `MonsterAssetsConfig.monsterSkillList` 追加 `MonsterSkillConfig`。
2. 填写 `skillId` 与 `skillAssetsPath`（相对路径 `Skill/{entityId}/{skillId}`）。
3. 创建对应 `SkillLineAsset` 资产。
4. 怪物技能由行为树或 AI 通过 `skillId` 触发，无需 `CommandType` 绑定。

### 4. 新建实体（推荐快速创建）

**英雄**（`Tools/角色工厂/快速创建角色基础数据`）默认技能 ID：

- `{id}01`–`{id}03`：普攻三段（`CommandType.Attack`）
- `{id}10`：技能（`CommandType.SKill`）
- `{id}20`：翻滚（`CommandType.Roll`）

**怪物**（`Tools/怪物工厂/快速创建怪物基础数据`）默认技能 ID：

- `{id}01`、`{id}02`：两个怪物技能

快速创建会自动：创建 SkillLineAsset、Skill Launcher Prefab、写入配置表。完成后用浏览器验证。

### 5. 手动创建空 SkillLineAsset

1. `Assets/Create/技能编辑器/SkillTimeLineAsset` 或在 Project 窗口右键创建。
2. 放到 `Assets/Prefabs/Battle/Skill/{entityId}/` 下，文件名建议 `{skillId}.asset`。
3. 在配置表写入相对路径并保存。

## 英雄 vs 怪物差异

| 字段/行为 | 英雄 `HeroSkillConfig` | 怪物 `MonsterSkillConfig` |
|-----------|------------------------|---------------------------|
| 路径字段 | `skillDownAssetsPath` + `skillUpAssetsPath` | `skillAssetsPath` |
| 执行类型 | `commandState`（DownUp / OnlyUp / OnlyDown） | 无（单时间轴） |
| 输入绑定 | `keyCode`（Attack / SKill / Roll 等） | 无 |
| 普攻段数 | `attackIndex`、`attackIndexCacheTick` | 无 |
| 运行时组件 | `SkillComponent` | `MonsterSkillComponent` |
| 触发方式 | 玩家输入 → 命令系统 | 行为树 / AI 调用 `ExecuteSkill(skillId)` |

`commandState == DownUp` 时，浏览器会分别显示「按下」「抬起」两行绑定，需确保两条路径各自有资产（若业务只需要一段，改用 OnlyDown / OnlyUp）。

## 验证清单

完成配置后逐项确认：

```
- [ ] 配置表路径为相对路径 Skill/{entityId}/{skillId}，非 Assets/ 绝对路径
- [ ] SkillLineAsset 存在于 Assets/Prefabs/Battle/Skill/{entityId}/{skillId}.asset
- [ ] 浏览器中该技能状态为「已找到」，FPS/时长/轨道数符合预期
- [ ] 英雄：commandState 与 down/up 路径匹配；keyCode 与输入逻辑一致
- [ ] 怪物：skillId 与行为树/AI 引用一致
- [ ] 修改 HeroAssets / MonsterAssets 后已 SetDirty 并 SaveAssets
- [ ] 若涉及运行时加载，Addressables 已同步
- [ ] 未新增/修改任何 TaskClip、StandardTrack 或 SkillEditor 节点代码
```

## 常见问题

**浏览器显示「缺失」**

- 检查配置路径拼写与 skillId 是否一致。
- 确认 `.asset` 文件是否在 `Assets/Prefabs/Battle/Skill/` 下。
- 用 `SkillTimelinePathUtility.TryResolveAsset` 逻辑：相对路径 → `Assets/Prefabs/Battle/{path}.asset`。

**编辑器打不开**

- `OpenWindowWithConfigPath` 需要非空配置路径且资产存在。
- 查看 Console 中 `未找到 SkillTimeLine 资产` 警告。

**技能运行时无效果**

- 英雄：检查 `keyCode`、`commandState` 与 `SkillComponent` 加载路径。
- 怪物：检查 `MonsterSkillComponent` 是否从 `monsterSkillList` 初始化，行为树是否调用正确 `skillId`。
- 时间轴是否含必要片段（动画、命中、子弹等），见 [reference.md](reference.md)。
- 若现有 Clip 类型无法满足需求：不要在本技能内扩展代码，转 `skill-editor-node-authoring`。

## 关键代码索引

| 模块 | 路径 |
|------|------|
| 浏览器窗口 | `Assets/Scripts/Editor/EntitySkillTimeline/` |
| 路径解析 | `Assets/Scripts/Editor/EntitySkillTimeline/SkillTimelinePathUtility.cs` |
| 技能编辑器 | `Assets/Scripts/Editor/SkillEditor/` |
| 英雄快速创建 | `Assets/Scripts/Editor/HeroFactory/HeroQuickCreateWindow.cs` |
| 怪物快速创建 | `Assets/Scripts/Editor/MonsterFactory/MonsterQuickCreateWindow.cs` |
| 配置类型 | `Assets/Scripts/RunTime/EntityAssetsConfig/HeroAssets.cs`, `MonsterAssets.cs` |
| 运行时轨道/片段（只读参考，本技能禁止修改） | `Assets/Scripts/RunTime/SkillEditor/` |
| 新节点实现（不在本技能范围） | `.agents/skills/skill-editor-node-authoring/` |

## 附加资源

- 轨道与片段完整目录、默认 ID 规则、路径模板：见 [reference.md](reference.md)
