# SnapShotData 模块

> 📍 位置: `Assets/Scripts/RunTime/SnapShotData/`
> 🔗 父文档: [根目录 CLAUDE.md../../../CLAUDE.md)
> 📊 完善度: 🟡 中等

## 模块职责

SnapShotData 模块负责世界状态的快照管理，用于：
- **状态保存**: 保存世界的完整或部分状态
- **状态恢复**: 从快照恢复世界状态
- **回滚支持**: 支持帧同步回滚机制

## 目录结构

```
SnapShotData/
└── BaseSnapShotData.cs         # 快照基类
```

## 核心概念

### BaseSnapShotData - 快照基类

```csharp
public abstract class BaseSnapShotData
{
    public uint Tick;           // 快照帧号

    // 序列化快照
    public abstract byte[] Serialize();

    // 反序列化恢复
    public abstract void Deserialize(byte[] data);

    // 拍摄快照
    public abstract void TakeSnapShot(BaseWorld world);

    // 应用快照
    public abstract void ApplySnapShot(BaseWorld world);
}
```

### 快照类型

| 类型 | 说明 |
|------|------|
| FullSnapShot | 完整快照，保存所有状态 |
| DeltaSnapShot | 增量快照，只保存变化部分 |
| KeyFrameSnapShot | 关键帧快照，定期保存 |

## 快照策略

### 关键帧间隔

```
Tick: 0    1    2    3    4    5    6    7    8    9   10
      │    │    │    │    │    │    │    │    │    │    │
      K────I────I────I────K────I────I────I────K────I────I
      K = 关键帧 (完整快照)
      I = 增量帧 (变化部分)
```

### 快照存储

```csharp
// 快照管理器
public class SnapShotManager
{
    private Dictionary<uint, BaseSnapShotData> _snapshots;

    // 保存快照
    public void SaveSnapShot(uint tick, BaseSnapShotData data);

    // 获取快照
    public BaseSnapShotData GetSnapShot(uint tick);

    // 清理过期快照
    public void ClearOldSnapShots(uint minTick);
}
```

## 对外接口

### 拍摄快照

```csharp
// 在 FixedUpdate 中拍摄
if (tick % SnapShotInterval == 0)
{
    TakeLocalSnapShot();
}
```

### 应用快照

```csharp
// 回滚时应用
var snapShot = GetSnapShot(rollbackTick);
snapShot.ApplySnapShot(world);
```

## 使用场景

1. **帧同步回滚**: 从快照恢复状态
2. **观战系统**: 重放游戏过程
3. **录像回放**: 录像功能支持
4. **调试工具**: 回放调试游戏

---

<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-03-05
completeness: medium
-->
