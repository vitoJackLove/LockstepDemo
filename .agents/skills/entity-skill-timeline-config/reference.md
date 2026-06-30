# 实体技能 TimeLine 参考

## 路径约定

### 配置表存储（相对路径）

```
Skill/{entityId}/{skillId}
```

示例：英雄 ID `1001`、技能 ID `100101` → 配置写 `Skill/1001/100101`。

### 编辑器资产路径（绝对路径）

```
Assets/Prefabs/Battle/Skill/{entityId}/{skillId}.asset
```

由 `SkillTimelinePathUtility.ResolveEditorAssetPath` 自动补全前缀与 `.asset` 后缀。

### 关联 Prefab 路径（快速创建生成）

| 类型 | 路径模板 |
|------|----------|
| 英雄 View | `Assets/Prefabs/Battle/Hero/{id}/{id}View.prefab` |
| 英雄 Logic | `Assets/Prefabs/Battle/Hero/{id}/{id}Logic.prefab` |
| 怪物 View | `Assets/Prefabs/Battle/Monster/{id}/{id}View.prefab` |
| 怪物 Logic | `Assets/Prefabs/Battle/Monster/{id}/{id}Logic.prefab` |
| 技能 Launcher | `Assets/Prefabs/Battle/Skill/{id}/{skillId}.prefab` |
| 状态 TimeLine | `Assets/Prefabs/Battle/State/{id}/{id}Hit.asset` |
| 行为树（怪物） | `Assets/Prefabs/Battle/Tree/{id}/{id}.asset` |

配置表中 `assetsPath` 字段（View 引用）使用更短形式，如 `Hero/{id}/{id}View`。

## 默认技能 ID 规则

### 英雄（entityId = N）

| skillId | 用途 | keyCode | commandState |
|---------|------|---------|--------------|
| N×100+1 | 普攻第 1 段 | Attack | OnlyDown |
| N×100+2 | 普攻第 2 段 | Attack | OnlyDown |
| N×100+3 | 普攻第 3 段 | Attack | OnlyDown |
| N×100+10 | 技能 | SKill | OnlyDown |
| N×100+20 | 翻滚 | Roll | OnlyDown |

普攻额外字段：`attackIndex`（1/2/3）、`attackIndexCacheTick`（默认 10）。

### 怪物（entityId = N）

| skillId | 用途 |
|---------|------|
| N×100+1 | 技能 1 |
| N×100+2 | 技能 2 |

## HeroSkillConfig 字段说明

| 字段 | 说明 |
|------|------|
| `skillId` | 技能唯一 ID，运行时字典键 |
| `commandState` | `DownUp` / `OnlyUp` / `OnlyDown`，决定按下/抬起时间轴 |
| `skillDownAssetsPath` | 按下阶段 SkillTimeLine 相对路径 |
| `skillUpAssetsPath` | 抬起阶段 SkillTimeLine 相对路径 |
| `maxDownTick` | DownUp 模式下按下最长 tick |
| `keyCode` | `CommandType` 输入绑定 |
| `attackIndexCacheTick` | 普攻段数缓存 tick（Attack 专用） |
| `attackIndex` | 普攻段序号（Attack 专用） |

## MonsterSkillConfig 字段说明

| 字段 | 说明 |
|------|------|
| `skillId` | 技能 ID，`MonsterSkillComponent.ExecuteSkill(skillId)` 使用 |
| `skillAssetsPath` | SkillTimeLine 相对路径 |

## SkillLineAsset 结构

```csharp
SkillLineAsset
├── tracks: List<StandardTrack>      // 轨道列表
├── blackBoardVariable               // 黑板变量
├── duration: int                    // 总时长（帧）
└── fps: int                         // 帧率（快速创建默认 30）
```

保存时 `SkillTimelineFactory.Save` 会根据所有片段重新计算 `duration`。

## 轨道类型（StandardTrack 派生）

| 轨道 | 类名 | 典型用途 |
|------|------|----------|
| 动画 | `SkillAnimationTrack` | 播放角色动画 |
| 技能逻辑 | `SkillTrack` | 技能状态、打断窗口 |
| 移动 | `MovementTrack` | 位移、锁定目标移动 |
| 子弹 | `BulletTrack` | 创建/发射子弹 |
| Buff | `BuffTrack` | 附加 Buff |
| 状态 | `StateTrack` | 切换实体状态 |
| 特效 | `EffectTrack` | 播放特效 |
| 音频 | `AudioTrack` | 播放音效 |
| 相机 | `CameraTrack` | 相机旋转等 |
| 技能附加 | `SkillAdditionTrack` | 技能加成参数 |
| 回滚 | `RollBackTrack` | 快照/回滚相关 |

## 片段类型（TaskClip 派生，按目录）

### 动画 / 表现

- `PlayAnimationClip` — 播放动画
- `ChangeAnimatorParamClip` — 修改 Animator 参数
- `PlayEffectClip` — 播放特效
- `PlayAudioClip` — 播放音频
- `CameraRotateClip` — 相机旋转

### 移动

- `OpenMovementClip` — 开启移动
- `LineMoveClip` — 直线移动
- `LockTargetMoveClip` — 锁定目标移动
- `MoveToPositionClip` — 移动到指定位置

### 子弹

- `CreateBulletClip` — 创建子弹
- `CreateFollowBulletClip` — 创建追踪弹
- `CreateMovementBulletClip` — 创建移动弹

### 技能逻辑

- `ExecuteSkillStateClip` — 执行技能状态
- `SkillBreakWindowClip` — 技能打断窗口
- `SkillDeroveWindowClip` — 技能派生窗口
- `ClearAttackIndexClip` — 清除普攻段索引

### Buff / 状态

- `PlayBuffClip` — 播放 Buff
- `ChangeStateClip` — 切换状态
- `SetSkillAdditionClip` — 设置技能附加

### 回滚

- `TakeSnapShotClip` — 拍摄快照

已有片段/轨道定义位于 `Assets/Scripts/RunTime/SkillEditor/RunTime/`（**本技能只读参考，禁止修改**；新增节点见 `skill-editor-node-authoring`），编辑器工厂在 `Assets/Scripts/Editor/SkillEditor/Editor/Factory/SkillTimelineFactory.cs`。

## 运行时加载链

**英雄**

1. `HeroAssetsConfig.initSkillList` → `SkillComponent` 初始化
2. 玩家输入 → `CommandType` 匹配 → 加载 down/up SkillTimeLine
3. `SkillTimelineLauncher` 驱动轨道片段执行

**怪物**

1. `MonsterAssetsConfig.monsterSkillList` → `MonsterSkillData.Create`
2. 行为树 / AI → `MonsterSkillComponent.ExecuteSkill(skillId)`
3. `MonsterSkillData` 加载 `skillAssetsPath` 对应时间轴

## Addressables 同步

运行时资源加载走 Addressables。新增或移动 Skill 资产后：

1. `Tools/Addressables/Sync Runtime Assets`
2. 必要时 `Tools/Addressables/Build Runtime Content`

快速创建窗口在创建流程末尾会自动调用同步（若项目配置如此）。
