# Singleton 模块

> 📍 位置: `Assets/Scripts/RunTime/Singleton/`
> 🔗 父文档: [RunTime CLAUDE.md](../CLAUDE.md)
> 📊 完善度: 🔴 基础

## 模块职责

Singleton 模块提供单例模式的实现，用于游戏中需要全局唯一访问的核心系统。

## 目录结构

```
Singleton/
├── Singleton.cs                 # 单例基类
├── ISingletonAwake.cs          # 唤醒接口
├── ISingletonUpdate.cs         # 更新接口
├── ISingletonFixedUpdate.cs    # 固定更新接口
└── ISingletonLateUpdate.cs    # 晚期更新接口
```

## 核心概念

### Singleton<T> 基类

```csharp
public class Singleton<T> where T : class, new()
{
    private static T _instance;
    private static readonly object _lock = new object();

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new T();
                    }
                }
            }
            return _instance;
        }
    }
}
```

### 生命周期接口

```csharp
// 唤醒 (类似 MonoBehaviour Awake)
public interface ISingletonAwake
{
    void OnSingletonAwake();
}

// 每帧更新 (类似 MonoBehaviour Update)
public interface ISingletonUpdate
{
    void OnSingletonUpdate(fp deltaTime);
}

// 固定帧更新 (类似 MonoBehaviour FixedUpdate)
public interface ISingletonFixedUpdate
{
    void OnSingletonFixedUpdate(fp deltaTime);
}

// 晚期更新 (类似 MonoBehaviour LateUpdate)
public interface ISingletonLateUpdate
{
    void OnSingletonLateUpdate(fp deltaTime);
}
```

## 使用示例

```csharp
public class GameManager : Singleton<GameManager>, ISingletonAwake, ISingletonUpdate
{
    public void OnSingletonAwake()
    {
        // 初始化
    }

    public void OnSingletonUpdate(fp deltaTime)
    {
        // 每帧更新
    }
}

// 使用
GameManager.Instance.DoSomething();
```

## 扩展指南

### 添加新的单例系统

1. 继承 `Singleton<T>`
2. 实现需要的生命周期接口
3. 在对应系统中注册更新

---

<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-03-05
completeness: basic
-->
