---
name: skill-editor-node-authoring
description: 通过用户描述制作技能编辑器节点（TaskClip 片段或 StandardTrack 轨道）。Use when the user asks to create, add, or implement a new skill timeline clip/track node, SkillEditor node, 技能编辑器节点, 时间轴片段, or describes skill behavior to implement as a timeline node in this lockstep Unity project.
disable-model-invocation: true
---

# 技能编辑器节点制作

项目级技能，路径：`.agents/skills/skill-editor-node-authoring/`。

与 `entity-skill-timeline-config` 分工：
- **本技能**：从用户描述 **实现新的 Clip/Track 代码**（运行时 + 编辑器注册）
- **entity-skill-timeline-config**：在已有节点类型上 **配置英雄/怪物时间轴资产**

## 适用场景

- 用户用自然语言描述「做一个 XXX 技能节点」
- 现有 Clip 目录里没有合适类型，需要新增 `TaskClip` 子类
- 需要新轨道类型或把新 Clip 注册到现有轨道
- 节点涉及帧同步、回滚、定点数战斗逻辑

## 工作流

### 1. 解析用户描述

从描述中提取并确认（缺信息时追问，一次最多 2 个问题）：

| 维度 | 需要明确 |
|------|----------|
| 节点类型 | Clip（片段）/ Track（轨道）/ 两者都要 |
| 中文显示名 | `[ClipName]` / `[TrackName]` 用的编辑器菜单名 |
| 所属轨道 | 动画、移动、子弹、Buff、特效、技能逻辑、状态、相机、音频、回滚、技能附加 |
| 参数字段 | 名称、类型、默认值、是否仅 Editor 预览 |
| 生命周期 | Enter 做什么、Tick 是否每帧更新、Exit 是否清理 |
| 回滚 | 是否改战斗状态；若改，需 `RollBackEnter/Exit` |
| TrackType | `Logic`（纯逻辑）或 `View`（表现） |

**优先复用**：先在 `Assets/Scripts/RunTime/SkillEditor/RunTime/Clip/` 搜索是否已有同类 Clip；能扩展字段就不要新建类。

### 2. 选择实现模板

```
用户描述
  ├─ 瞬时效果（Enter 一次）     → 参考 PlayBuffClip
  ├─ 窗口/开关（Enter 开 Exit 关）→ 参考 SkillBreakWindowClip、ChangeStateClip
  ├─ 持续表现（Tick 模拟）      → 参考 PlayEffectClip、PlayAnimationClip
  ├─ 位移/子弹/属性修改         → 参考对应子目录 Clip
  └─ 需要新轨道类别             → 新建 StandardTrack 子类 + TrackBindClip
```

### 3. 实现 TaskClip

**路径**：`Assets/Scripts/RunTime/SkillEditor/RunTime/Clip/{Category}/{Name}Clip.cs`

**最小结构**：

```csharp
[ClipName("用户可见中文名")]
public class ExampleClip : TaskClip
{
    [VariableName("参数说明")] public int exampleId;

    public override void OnRunTimeEnter(BaseEntity context, int fps)
    {
        base.OnRunTimeEnter(context, fps);
        // 运行时逻辑：用 fp/fp3，访问 context.GetComponent<T>()
    }

    public override void RollBackEnter(BaseEntity context, int fps)
    {
        base.RollBackEnter(context, fps);
        // 若 Enter 改了可回滚状态，这里 Mirror Enter
    }

    public override void OnRunTimeExit(BaseEntity context)
    {
        base.OnRunTimeExit(context);
        // 清理临时对象、还原开关
    }
}
```

**规则**：
- 继承 `TaskClip`，必须加 `[ClipName("...")]`
- 战斗逻辑用 `Unity.Mathematics.FixedPoint`（`fp`/`fp3`），避免运行时 `float` 漂移
- 字段用 `[VariableName("黑板标签")]`；仅 Editor 预览用 `[EditorVariable("...")]`
- 临时 GameObject/特效在 `OnRunTimeExit` / `RollBackExit` / `EditorExit` 中销毁
- 需要 Editor 预览时实现 `EditorEnter/EditorTick/EditorExit`
- 不要改 `TaskClip` 基类；不要放 `UnityEditor` 引用到 RunTime 程序集

### 4. 注册到轨道

在对应 Track 的 `[TrackBindClip(Types = new[] { typeof(...), typeof(NewClip) })]` 中加入新类型。

| 轨道类 | 文件 |
|--------|------|
| `SkillAnimationTrack` | `RunTime/Track/AnimationTrack.cs` |
| `SkillTrack` | `RunTime/Track/SkillTrack.cs` |
| `MovementTrack` | `RunTime/Track/MovementTrack.cs` |
| `BulletTrack` | `RunTime/Track/BulletTrack.cs` |
| `BuffTrack` | `RunTime/Track/BuffTrack.cs` |
| `StateTrack` | `RunTime/Track/StateTrack.cs` |
| `EffectTrack` | `RunTime/Track/EffectTrack.cs` |
| `AudioTrack` | `RunTime/Track/AudioTrack.cs` |
| `CameraTrack` | `RunTime/Track/CameraTrack.cs` |
| `SkillAdditionTrack` | `RunTime/Track/SkillAdditionTrack.cs` |
| `RollBackTrack` | `RunTime/Track/RollBackTrack.cs` |

若无合适轨道，新建 Track：

```csharp
[TrackName(Name = "轨道中文名")]
[TrackColor(r, g, b)]
[TrackBindClip(Types = new[] { typeof(NewClip) })]
public class NewTrack : StandardTrack
{
    public override TrackType TrackType => TrackType.Logic; // 或 View
}
```

### 5. 验证

```
- [ ] dotnet build Assembly-CSharp.csproj -nologo 无 error
- [ ] Unity Console 无编译错误（若 Editor 已连接）
- [ ] Tools/技能/技能编辑器 中右键轨道可看到新 Clip 中文名
- [ ] 新 Clip 能创建、保存 SkillLineAsset
- [ ] 若改战斗状态：Enter/Exit 与 RollBackEnter/RollBackExit 成对
- [ ] 未引入 UnityEditor 到 RunTime
```

### 6. 交付说明

向用户报告：
1. 新增/修改的文件路径
2. Clip 中文名与所属轨道
3. 参数字段含义
4. 如何在 `Tools/技能/技能编辑器` 中使用
5. 若需配置到具体英雄/怪物技能，提示使用 `entity-skill-timeline-config`

## 描述 → 实现 速查

| 用户说法 | 倾向实现 |
|----------|----------|
| 播放动画 | `PlayAnimationClip` 或扩展 |
| 放特效/音效 | `PlayEffectClip` / `PlayAudioClip` |
| 位移/冲刺/锁定移动 | `MovementTrack` 下现有或新 MovementClip |
| 发射子弹 | `BulletTrack` 下 Create*BulletClip |
| 加 Buff / 切状态 | `PlayBuffClip` / `ChangeStateClip` |
| 技能打断/派生窗口 | `SkillTrack` 下窗口类 Clip |
| 拍照/回滚调试 | `RollBackTrack` / `TakeSnapShotClip` |
| 相机震动/旋转 | `CameraRotateClip` 或扩展 |

完整目录见 [reference.md](reference.md)；描述解析示例见 [examples.md](examples.md)。

## 关键代码索引

| 模块 | 路径 |
|------|------|
| Clip 基类 | `Assets/Scripts/RunTime/SkillEditor/Base/BaseData/TaskClip.cs` |
| Track 基类 | `Assets/Scripts/RunTime/SkillEditor/Base/BaseData/StandardTrack.cs` |
| 属性 | `Assets/Scripts/RunTime/SkillEditor/Base/Attribute/` |
| 编辑器工厂 | `Assets/Scripts/Editor/SkillEditor/Editor/Factory/SkillTimelineFactory.cs` |
| 技能编辑器窗口 | `Assets/Scripts/Editor/SkillEditor/Editor/Window/` |
