# 描述 → 节点实现示例

## 示例 1：瞬时 Buff

**用户描述**：「做一个节点，在技能开始时给自身加一个 Buff，参数是 Buff ID。」

**判定**：瞬时逻辑 → `BuffTrack` → 已有 `PlayBuffClip`，无需新建。

**回复**：项目已有 `[ClipName("创建Buff")] PlayBuffClip`，字段 `buffId`。在技能编辑器 Buff 轨道添加「创建Buff」片段即可。

---

## 示例 2：技能期间禁止移动

**用户描述**：「技能释放后 30 帧内不能移动，结束后恢复。」

**判定**：窗口开关 → `MovementTrack` → 参考 `OpenMovementClip` 或新建。

**实现草案**：

```csharp
[ClipName("禁止移动窗口")]
public class DisableMoveWindowClip : TaskClip
{
    [VariableName("禁止移动")] public bool disableMove = true;
    [VariableName("结束时恢复")] public bool restoreOnExit = true;

    public override void OnRunTimeEnter(BaseEntity context, int fps)
    {
        base.OnRunTimeEnter(context, fps);
        context.GetComponent<MoveComponent>()?.SetMoveEnabled(!disableMove);
    }

    public override void RollBackEnter(BaseEntity context, int fps)
    {
        base.RollBackEnter(context, fps);
        OnRunTimeEnter(context, fps);
    }

    public override void OnRunTimeExit(BaseEntity context)
    {
        base.OnRunTimeExit(context);
        if (restoreOnExit)
            context.GetComponent<MoveComponent>()?.SetMoveEnabled(true);
    }

    public override void RollBackExit(BaseEntity context)
    {
        base.RollBackExit(context);
        OnRunTimeExit(context);
    }
}
```

**注册**：在 `MovementTrack.cs` 的 `TrackBindClip` 中加入 `typeof(DisableMoveWindowClip)`。

---

## 示例 3：播放挂点特效

**用户描述**：「在角色脚下播放一个特效，持续整个片段，Editor 里能预览 prefab。」

**判定**：持续表现 → `EffectTrack` → 参考 `PlayEffectClip`。

**字段建议**：
- `[VariableName("特效ID")] int id` — 运行时走 `EffectAssetsConfig`
- `[EditorVariable("预览预制体")] GameObject prefab` — 仅 Editor
- `[VariableName("位置偏移")] Vector3 offset`

**生命周期**：Enter 实例化；Tick 驱动 `ParticleSystem.Simulate`；Exit 销毁。

---

## 示例 4：新轨道类型

**用户描述**：「加一个『输入』轨道，用来放监听玩家按键的节点。」

**判定**：需新 Track + 新 Clip。

**Track**：

```csharp
[TrackName(Name = "输入")]
[TrackColor(0.2f, 0.6f, 1f)]
[TrackBindClip(Types = new[] { typeof(ListenCommandClip) })]
public class InputTrack : StandardTrack
{
    public override TrackType TrackType => TrackType.Logic;
}
```

**Clip**：`ListenCommandClip` 继承 `TaskClip`，`[ClipName("监听指令")]`，字段 `CommandType commandType`。

---

## 解析 checklist（Agent 内部用）

```
用户描述: "..."
- 节点类型: Clip / Track / 两者
- 中文名:
- 目标轨道:
- 字段列表:
- Enter/Tick/Exit 行为:
- 需要 RollBack: 是/否
- 复用现有: 是(哪个) / 否(新建)
- 修改文件:
```
