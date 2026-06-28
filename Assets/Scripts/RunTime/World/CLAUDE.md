# World 模块

> 📍 位置: `Assets/Scripts/RunTime/World/`
> 🔗 父文档: [RunTime CLAUDE.md](../CLAUDE.md)
> 📊 完善度: 🟡 中等

## 模块职责

World 模块是游戏世界的核心管理器，负责：
- **世界生命周期**: 世界的创建、初始化、更新、销毁
- **系统管理**: 注册、更新、调度所有系统
- **帧同步**: 实现客户端预测与服务器权威同步
- **回滚机制**: 支持指令回滚与状态恢复
- **快照系统**: 保存和恢复世界状态

## 目录结构

```
World/
├── BaseWorld.cs                 # 世界基类
├── BaseWorld.Config.cs          # 世界配置
├── BaseWorld.RollBack.cs        # 回滚机制
├── RogueWorld.cs               # Roguelike 游戏世界
├── SurvivorWorld.cs            # Survivor 模式世界
├── GameTimeType.cs             # 游戏时机枚举
├── WolrdTypeEnum.cs            # 世界类型枚举
└── ...
```

## 核心概念

### 世界生命周期

```
┌─────────────────────────────────────────────────────────────┐
│                    World Lifecycle                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Constructor ──▶ Init ──▶ Enter ──▶ Start ──▶ Update      │
│       │             │          │         │         │       │
│       │             │          │         │         │       │
│     创建         系统初始化    资源加载   游戏开始   循环更新  │
│                                                             │
│                          │                                 │
│                          ▼                                 │
│                   Pause / Shutdown                        │
│                          │                                 │
│                          ▼                                 │
│                       Dispose                              │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### BaseWorld

```csharp
public abstract partial class BaseWorld
{
    private uint _worldId;              // 世界ID
    private string _sceneName;          // 场景名
    private uint _localTick;            // 本地帧号

    // 根节点
    private Transform _worldRoot;       // 世界根节点
    private Transform _entityRoot;       // 实体根节点
    private Transform _mapRoot;         // 地图根节点
    private Transform _skillTimeLineRoot; // 技能时间轴根
    private Transform _behaviourTreeRoot; // 行为树根

    // 系统字典
    private Dictionary<Type, BaseSystem> _systemDic;
}
```

### 游戏时机 (GameTimeType)

```csharp
public enum GameTimeType
{
    WaitStart,   // 等待开始
    Start,       // 游戏进行中
    WaitStop,    // 等待结束
    Paused,      // 暂停
}
```

## 帧同步机制

### 核心流程

```
┌─────────────────────────────────────────────────────────────┐
│                   Frame Sync Flow                           │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────┐                                           │
│  │ Input       │ ──▶ 获取玩家输入                            │
│  └──────┬──────┘                                           │
│         ▼                                                   │
│  ┌─────────────┐                                           │
│  │ Command     │ ──▶ 生成命令数据                           │
│  └──────┬──────┘                                           │
│         ▼                                                   │
│  ┌─────────────────────────────────────────┐               │
│  │ FixedUpdate (每帧调用)                  │               │
│  │ 1. 获取本地命令                          │               │
│  │ 2. 记录并发送命令                        │               │
│  │ 3. 拍摄本地快照                         │               │
│  │ 4. 本地逻辑更新                         │               │
│  │ 5. 权威逻辑更新                         │               │
│  └────────────────────┬──────────────────┘               │
│                       ▼                                     │
│         ┌─────────────┴─────────────┐                     │
│         │                           │                     │
│    本地更新                     权威更新                   │
│    (预测)                      (纠正)                     │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 回滚机制

```csharp
// 回滚更新模式
if (_isStartRollBackUpdate)
{
    // 高速回滚刷新
    for (int i = 0; i < _rollBackSpeed; i++)
    {
        RollBackLocalUpdateWorld(deltaTime);
    }
}
```

**回滚流程**:
1. 收到服务器权威帧
2. 比对本地帧与服务器帧
3. 如果有差异，执行回滚
4. 从快照恢复状态
5. 重放之后的命令

### 快照系统

```csharp
// 拍摄快照
public virtual void TakeLocalSnapShot()
{
    // 保存当前帧的所有实体状态
}
```

## 世界类型

### RogueWorld

Roguelike 模式世界，特点：
- 随机地图生成
- 房间/关卡系统
- Boss 战斗
- 永久死亡

### SurvivorWorld

Survivor (类似 Vampire Survivors) 模式：
- 自动战斗
- 敌人波次生成
- 经验/升级系统
- 武器/技能组合

## 对外接口

### 创建世界

```csharp
var createData = new CreateWorldData
{
    WorldId = 1,
    WorldRoot = root,
    SceneName = "RogueBattle"
};

var world = new RogueWorld(createData);
```

### 获取系统

```csharp
var entitySystem = world.GetSystem<EntitySystem>();
var skillSystem = world.GetSystem<SkillTimeLineSystem>();
```

### 更新世界

```csharp
// 每帧调用
world.Update(deltaTime);

// 固定帧调用 (游戏逻辑)
world.FixedUpdate(deltaTime);
```

## 数据结构

### RollBackData

```csharp
public class RollBackData
{
    public uint Tick;              // 帧号
    public List<CommandData> Commands; // 执行的命令
    // ... 其他状态数据
}
```

### WorldLineRecorder

```csharp
public class WorldLineRecorder
{
    // 记录每一帧的命令和状态
    // 用于回滚和重放
}
```

## 扩展指南

### 添加新世界类型

1. 继承 `BaseWorld`
2. 实现 `GamePreparation()` 方法
3. 实现 `GetSystemTypes()` 返回所需系统
4. 在 World 创建工厂中注册

### 添加回滚支持

1. 实体实现 `IRollBack` 接口
2. 在 `RollBackData` 中保存必要数据
3. 实现 `OnRollBack()` 恢复状态

## 注意事项

- **定点数**: 使用 `fp` 而非 `float` 保证确定性
- **单线程**: 逻辑在主线程执行，无需锁
- **帧独立**: 确保每帧计算结果一致
- **命令录制**: 所有输入必须录制以支持回滚

---

<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-03-05
completeness: medium
-->
