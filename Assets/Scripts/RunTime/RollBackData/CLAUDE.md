# RollBackData 模块

> 📍 位置: `Assets/Scripts/RunTime/RollBackData/`
> 🔗 父文档: [根目录 CLAUDE.md../../../CLAUDE.md)
> 📊 完善度: 🟡 中等

## 模块职责

RollBackData 模块负责帧同步中的回滚机制实现，用于：
- **回滚数据**: 存储回滚所需的游戏状态
- **指纹生成**: 生成实体唯一标识用于回滚比对
- **行记录器**: 记录游戏帧的执行历史

## 目录结构

```
RollBackData/
├── RollBackData.cs             # 回滚数据基类
├── IRollBack.cs               # 回滚接口
├── GameRollBackContent.cs     # 回滚内容
├── WorldLineRecorder.cs       # 行记录器
├── FingerprintsGenerate.cs    # 指纹生成
└── RollBackType.cs           # 回滚类型枚举
```

## 核心概念

### RollBackType - 回滚类型

```csharp
public enum RollBackType
{
    NoRollBack = 0,        // 无回滚
    Property = 1 << 0,     // 属性回滚
    Position = 1 << 1,     // 位置回滚
    State = 1 << 2,        // 状态回滚
    Skill = 1 << 3,        // 技能回滚
    Buff = 1 << 4,         // Buff 回滚
    // ...
}
```

### RollBackData - 回滚数据

```csharp
public class RollBackData : IPool
{
    private RollBackType _rollBackType;

    // 添加回滚类型
    public void AttachRollBackType(RollBackType type);

    // 移除回滚类型
    public void RemoveRollBackType(RollBackType type);

    // 检查是否包含
    public bool CheckType(RollBackType type);
}
```

### IRollBack 接口

```csharp
public interface IRollBack
{
    // 记录回滚数据
    void RecordRollBack(RollBackData data);

    // 执行回滚
    void OnRollBack(RollBackData data);
}
```

### WorldLineRecorder - 行记录器

```csharp
public class WorldLineRecorder
{
    // 记录帧数据
    public void Record(uint tick, List<CommandData> commands);

    // 获取指定帧数据
    public List<CommandData> GetFrameData(uint tick);

    // 清除历史记录
    public void ClearHistory(uint fromTick);
}
```

## 回滚机制流程

```
┌─────────────────────────────────────────────────────────────┐
│                   RollBack Flow                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. 帧执行                                                  │
│     └── 每帧执行命令，记录状态                              │
│                                                             │
│  2. 服务器同步                                              │
│     └── 收到服务器权威帧                                    │
│                                                             │
│  3. 状态比对                                                │
│     ├── 本地帧 vs 服务器帧                                  │
│     └── 比对实体指纹                                        │
│                                                             │
│  4. 触发回滚 (如有差异)                                     │
│     ├── 保存当前帧                                          │
│     ├── 回滚到服务器帧                                      │
│     └── 重放后续命令                                        │
│                                                             │
│  5. 快照恢复                                                │
│     └── 从快照恢复世界状态                                  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## 指纹机制

### FingerprintsGenerate

```csharp
// 生成实体唯一指纹
public static int GenerateFingerprints(BaseEntity entity)
{
    // 结合 EntityId、ConfigId、创建时间等
    return hash(EntityId, ConfigId, CreateTime);
}
```

### 指纹用途

1. **快速比对**: 判断实体是否发生变化
2. **增量同步**: 只同步变化的部分
3. **冲突检测**: 检测多端状态不一致

## 对外接口

### 实体回滚

```csharp
// 在实体中实现 IRollBack
public class HeroEntity : BaseEntity, IRollBack
{
    public void RecordRollBack(RollBackData data)
    {
        data.AttachRollBackType(RollBackType.Position);
        // 记录位置
    }

    public void OnRollBack(RollBackData data)
    {
        // 恢复位置
    }
}
```

### 行记录器

```csharp
// 记录帧
recorder.Record(tick, commands);

// 回滚
recorder.RollBackTo(tick);
```

---

<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-03-05
completeness: medium
-->
