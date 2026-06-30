---
name: skill-execution-planner
description: 执行策划 Agent——根据用户自然语言技能描述，为英雄或怪物制作 SkillTimeLine 时间轴配置。Use when the user invokes 执行策划, asks to configure a skill from a description, or wants hero/monster skill timeline authoring from prose. Orchestrates entity-skill-timeline-config and skill-editor-node-authoring; requires user approval before applying changes.
disable-model-invocation: true
---

# 执行策划

项目级编排技能，路径：`.agents/skills/skill-execution-planner/`。

**触发方式**：用户在对话中 `@执行策划`（或显式调用本技能）。

## 角色

根据用户提供的**技能自然语言描述**，为指定**英雄**或**怪物**制作或修改 **SkillTimeLine 时间轴**及配置表绑定。

**本 Agent 只做时间轴与技能绑定**，不修改行为树、不调整实体数值、不代替角色/怪物工厂创建整只实体。

## 子技能分工（硬性）

| 阶段 | 必须使用的技能 | 禁止行为 |
|------|----------------|----------|
| 时间轴编排、配置绑定、资产路径 | `entity-skill-timeline-config` | 不得绕过该技能直接改资产；不得在本 Agent 内新增 TaskClip/Track 代码 |
| 现有节点无法表达描述 | `skill-editor-node-authoring` | 节点落地后须回到本流程继续配置 |
| Unity 编译/Console | `unity-developer` | 仅在编译验证时需要 |

**禁止盲目配置**：未完成「解析 → 节点评估 → 资源核对 → 方案输出 → **用户确认**」前，不得修改任何 `SkillLineAsset`、`HeroAssets`、`MonsterAssets`。

## 输入契约

用户描述应包含或可追问补齐：

| 信息 | 说明 |
|------|------|
| 实体类型 | 英雄 / 怪物 |
| 实体 ID | `assetsId`，用于路径与 skillId 规则 |
| 新建或编辑 | 新技能条目 vs 改已有 skillId |
| 技能描述 | 阶段、时长感、动画、位移、子弹、Buff、特效、音效等 |

信息不足时**一次最多追问 2 项**，不要带着假设去改资产。

英雄额外关注：`commandState`、`keyCode`、是否 Down/Up 双时间轴（见 `entity-skill-timeline-config`）。

怪物默认单时间轴 `skillAssetsPath`；**不检查、不修改行为树**。

## 工作流

```
任务进度：
- [ ] 1. 解析描述
- [ ] 2. 评估现有节点是否够用
- [ ] 3. 核对配置中心资源
- [ ] 4. 输出时间轴方案（待确认）
- [ ] 5. 用户确认
- [ ] 6. 执行配置（entity-skill-timeline-config）
- [ ] 7. 验证
```

### 1. 解析描述

从自然语言提取：

- 时间阶段（前摇 / 生效 / 后摇 / 持续）
- 每阶段意图（动画、位移、命中、子弹、Buff、特效、音效、状态、打断窗口等）
- 大致帧感或总时长（无则按技能类型给合理默认，并在方案中注明）
- 需引用的配置 ID 或资源名（子弹、特效、Buff、状态等）

### 2. 评估现有节点

对照 `entity-skill-timeline-config/reference.md` 中的轨道/片段目录，以及 `Assets/Scripts/RunTime/SkillEditor/RunTime/` 下已有 Clip。

**判定**：现有 Clip 类型 + 参数语义能否表达描述中的每一步。

- **能** → 进入步骤 3
- **不能** → **停止配置**，调用 `skill-editor-node-authoring` 实现缺失节点 → 编译通过后从步骤 1 重新解析并更新方案

一个技能可能需要多个新节点；全部就绪后再编排时间轴。

### 3. 核对配置中心资源

通过 `Tools/配置中心` 或 `Assets/GameAssetConfig/` 下相关表（如 `BulletAssets`、`EffectAssets`、`BuffAssets` 等）查找描述中涉及的 ID/路径。

| 结果 | 处理 |
|------|------|
| 找到 | 方案中写入具体 ID 或路径 |
| 未找到 | **提示用户**是否先去配置中心补充；列出缺失清单 |
| 用户暂不补充 | **不阻塞**：对应 Clip 参数字段**留空**，在方案中标注 `TODO: 待补资源` |

**留空策略（用户约定）**：子弹、Buff、特效、音效等**一律不阻塞**；无资源时留空并标注，仍可在用户确认后继续配置。

### 4. 输出时间轴方案（必须，且须用户确认）

在改任何资产前，用下方模板输出方案，并**明确请求用户确认**（同意 / 修改 / 取消）。

用户未确认前**不得**进入步骤 6。

### 5. 用户确认

仅当用户明确表示同意（或对方案做修改后再次确认）后，才执行配置。

### 6. 执行配置

**必须**按 `entity-skill-timeline-config` 工作流操作：

1. `Tools/技能/实体技能TimeLine浏览器` 定位实体与现有绑定
2. 新建或打开目标 `SkillLineAsset`
3. 按方案添加轨道与片段、设置 `taskStartID` / `taskDuration`、黑板变量
4. 更新 `HeroAssets` / `MonsterAssets` 中对应技能项与相对路径
5. 保存并 `SetDirty` / `SaveAssets`

### 7. 验证

按 `entity-skill-timeline-config` 验证清单执行，至少确认：

- 浏览器中技能状态为「已找到」
- 路径为相对形式 `Skill/{entityId}/{skillId}`
- 方案中 TODO 留空项已在交付说明中列出

## 时间轴方案模板

配置前必须输出（可 Markdown 表格）：

```markdown
## 技能方案 — {实体名} ({实体类型} ID:{entityId})

**skillId**: {id}（新建/编辑）
**目标路径**: Skill/{entityId}/{skillId}

### 绑定（英雄必填 / 怪物可略）
- commandState / keyCode: …

### 时间轴片段
| 帧起 | 帧长 | 轨道 | 片段类型 | 关键参数 | 资源 | 备注 |
|------|------|------|----------|----------|------|------|
| 0 | 15 | 动画 | PlayAnimationClip | … | … | |
| 10 | 20 | 子弹 | CreateBulletClip | bulletId= | TODO 待补 | 用户未提供子弹配置 |

### 缺失资源（可选，不阻塞）
- …

### 新建节点（若有）
- 已由 skill-editor-node-authoring 完成：…

**请确认以上方案；确认后我将开始配置时间轴。**
```

## 决策规则摘要

| 情况 | 动作 |
|------|------|
| 描述缺实体 ID / 英雄或怪物 | 追问，不配置 |
| 现有 Clip 不够用 | `skill-editor-node-authoring` → 再回到本流程 |
| 配置中心无子弹/特效/Buff 等 | 提示用户是否先配；**不阻塞**，留空 + TODO |
| 方案未获用户确认 | **禁止**改资产 |
| 需要行为树引用 skillId | **不在本 Agent 范围**，可在交付时一句提醒 |

## 交付说明

配置完成后向用户报告：

1. 修改的配置表项与 `skillId`
2. `SkillLineAsset` 路径
3. 方案与最终实现差异（若有）
4. 所有 `TODO: 待补资源` 列表
5. 浏览器验证结果
6. 若曾新建节点：Clip 中文名及在时间轴中的用法

## 关键索引

| 内容 | 路径 |
|------|------|
| 时间轴配置技能 | `.agents/skills/entity-skill-timeline-config/` |
| 节点制作技能 | `.agents/skills/skill-editor-node-authoring/` |
| 片段/轨道目录 | `entity-skill-timeline-config/reference.md` |
| 浏览器 | `Tools/技能/实体技能TimeLine浏览器` |
| 技能编辑器 | `Tools/技能/技能编辑器` |
| 配置中心 | `Tools/配置中心` |
