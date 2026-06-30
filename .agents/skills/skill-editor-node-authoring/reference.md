# 技能编辑器节点参考

## 架构

```
SkillLineAsset
└── tracks: List<StandardTrack>
    └── taskClips: List<TaskClip>   // [SerializeReference]
        └── 各 Clip 子类（ScriptableObject 实例嵌在 asset 内）
```

编辑器通过反射扫描 `[ClipName]`、`[TrackName]`、`[TrackBindClip]` 构建菜单。`SkillTimelineFactory.CreateClip` 用 `ScriptableObject.CreateInstance(clipType)` 创建片段。

## 属性

| 属性 | 作用 | 用于 |
|------|------|------|
| `[ClipName("中文名")]` | 编辑器片段菜单显示名 | TaskClip 子类（必填） |
| `[TrackName(Name = "...")]` | 轨道显示名 | StandardTrack 子类 |
| `[TrackColor(r,g,b)]` | 轨道颜色 | StandardTrack 子类 |
| `[TrackBindClip(Types = new[] { typeof(A), typeof(B) })]` | 该轨道可创建的 Clip 类型 | StandardTrack 子类 |
| `[VariableName("标签")]` | 黑板/Inspector 字段名 | Clip 公有字段 |
| `[EditorVariable("标签")]` | 仅 Editor 预览用字段 | Clip 公有字段 |
| `[ClipStyleAttribute(...)]` | 自定义 Clip UI 样式（少用） | 可选 |

## TaskClip 生命周期

| 方法 | 调用时机 |
|------|----------|
| `OnRunTimeEnter` | 片段开始（运行时） |
| `RunTimeTick` | 片段持续期间每帧 |
| `OnRunTimeExit` | 片段结束或时间轴打断 |
| `EditorEnter/EditorTick/EditorExit` | 技能编辑器预览 |
| `RollBackEnter/RollBackExit` | 帧同步回滚重放 |
| `TakeSnapShot` / `RollBackTo` | 需在 Clip 内保存/恢复状态时 |

默认 `taskDuration = 100` 帧（Factory 创建时）。`taskStartID` 由编辑器根据时间轴位置设置。

## TrackType

- `TrackType.Logic`：影响战斗逻辑（Buff、移动、子弹、技能窗口）
- `TrackType.View`：纯表现（动画、特效、音频、相机）

## 目录约定

```
Assets/Scripts/RunTime/SkillEditor/
├── Base/
│   ├── BaseData/TaskClip.cs, StandardTrack.cs
│   └── Attribute/
└── RunTime/
    ├── Clip/
    │   ├── PlayAnimationClip.cs
    │   ├── PlayEffectClip.cs
    │   ├── BulletClip/
    │   ├── MovementClip/
    │   ├── SkillClip/
    │   ├── StateClip/
    │   ├── SkillAdditionClip/
    │   └── RollBack/
    └── Track/
        ├── AnimationTrack.cs
        ├── EffectTrack.cs
        └── ...
```

新 Clip 放语义最接近的子目录；新 Track 放 `RunTime/Track/`。

## 回滚实现要点

1. **Enter 改状态 → RollBackEnter 同样改**
2. **Exit 还原 → RollBackExit 同样还原**
3. **临时对象**：Enter 创建、Exit/RollBackExit 销毁
4. **无状态瞬时效果**（如一次性 CreateBuff）：通常只需 `OnRunTimeEnter`，可不写 RollBack

参考：`SkillBreakWindowClip`（开关 + Exit 还原）、`PlayEffectClip`（临时 GO + Tick 模拟）。

## 现有 Clip 一览

### 动画 / 表现
- `PlayAnimationClip`、`ChangeAnimatorParamClip`
- `PlayEffectClip`、`PlayAudioClip`、`CameraRotateClip`

### 移动
- `OpenMovementClip`、`LineMoveClip`、`LockTargetMoveClip`、`MoveToPositionClip`

### 子弹
- `CreateBulletClip`、`CreateFollowBulletClip`、`CreateMovementBulletClip`

### 技能逻辑
- `ExecuteSkillStateClip`、`SkillBreakWindowClip`、`SkillDeroveWindowClip`、`ClearAttackIndexClip`

### Buff / 状态 / 附加
- `PlayBuffClip`、`ChangeStateClip`、`SetSkillAdditionClip`

### 回滚
- `TakeSnapShotClip`

## 常见组件访问

```csharp
context.GetComponent<SkillComponent>()
context.GetComponent<BuffComponent>()
context.GetComponent<MoveComponent>()
context.GetSystem<EntitySystem>()
context.GetSystem<EntityViewSystem>()
GameEntry.DataTable.GetDataTable<T>(id)
```

## 反模式

- 在 Clip 里用 `float` 做位移/伤害等核心逻辑
- RunTime Clip 引用 `UnityEditor`
- 忘记 `[ClipName]` 导致 Factory 创建时 `GetCustomAttribute<ClipNameAttribute>()` 空引用
- 新建 Clip 但未加入任何 `TrackBindClip`，编辑器菜单看不到
- 有状态变更却不实现 RollBack 对称逻辑
