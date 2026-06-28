# Pool 模块

> 📍 位置: `Assets/Scripts/RunTime/Pool/`
> 🔗 父文档: [根目录 CLAUDE.md../../../CLAUDE.md)
> 📊 完善度: 🔴 基础

## 模块职责

Pool 模块提供对象池功能，用于优化游戏性能，减少频繁的对象创建和销毁开销。

## 目录结构

```
Pool/
├── IPool.cs                    # 对象池接口
├── GameObjectFactory.cs        # GameObject 工厂
└── ...
```

## 核心概念

### IPool 接口

```csharp
public interface IPool<T>
{
    T Allocate();    // 从池中获取对象
    void Recycle(T obj); // 回收对象到池中
}
```

### 对象池原理

```
┌─────────────────────────────────────────────────────────────┐
│                    Object Pool Flow                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  首次请求                                                    │
│       │                                                     │
│       ▼                                                     │
│  ┌─────────────┐                                           │
│  │ Pool Empty? │ ──▶ 是 ──▶ 创建新对象 ──▶ 返回            │
│  └──────┬──────┘                                           │
│         │ 否                                               │
│         ▼                                                  │
│  从池中取出 ──▶ 复用 ──▶ 返回                               │
│                                                             │
│       │                                                     │
│       ▼                                                     │
│  对象使用完毕                                                │
│       │                                                     │
│       ▼                                                     │
│  回收对象 ──▶ 放回池中 ──▶ 等待复用                         │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## 核心实现

### GameObjectFactory

```csharp
public class GameObjectFactory
{
    // 同步创建
    public GameObject Create(string path);

    // 异步创建
    public Task<GameObject> CreateAsync(string path);

    // 回收
    public void Recycle(GameObject obj);
}
```

## 使用示例

```csharp
// 从池中获取对象
var bullet = FPoolHelper.Allocate<BulletEntity>();

// 使用完毕回收
FPoolHelper.Release(bullet);
```

## 注意事项

- **预加载**: 关键对象可预加载到池中
- **清理**: 场景切换时清理池
- **上限**: 池大小应有上限防止内存膨胀

---

<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-03-05
completeness: basic
-->
