# Helper 模块

> 📍 位置: `Assets/Scripts/RunTime/Helper/`
> 🔗 父文档: [根目录 CLAUDE.md../../../CLAUDE.md)
> 📊 完善度: 🟡 中等

## 模块职责

Helper 模块提供游戏开发中常用的辅助工具函数，包括：
- **路径帮助**: 资源路径管理
- **对象池帮助**: 对象池便捷操作

## 目录结构

```
Helper/
├── AssetsPathHelper.cs        # 资源路径帮助
├── FPoolHelper.cs            # 对象池帮助
└── ...
```

## 核心概念

### AssetsPathHelper - 资源路径

```csharp
public static class AssetsPathHelper
{
    // 获取资源路径
    public static string GetAssetPath(string assetName);

    // 获取 Prefab 路径
    public static string GetPrefabPath(string prefabName);

    // 获取配置表路径
    public static string GetDataTablePath(string tableName);
}
```

### FPoolHelper - 对象池

```csharp
public static class FPoolHelper
{
    // 从池中获取对象
    public static T Get<T>() where T : class, new();

    // 回收对象
    public static void Release<T>(T obj) where T : class;

    // 预加载
    public static void Preload<T>(int count) where T : class, new();
}
```

## 使用示例

### 资源路径

```csharp
// 获取资源完整路径
var path = AssetsPathHelper.GetPrefabPath("Hero_001");
var prefab = Resources.Load<GameObject>(path);
```

### 对象池

```csharp
// 获取
var entity = FPoolHelper.Get<BulletEntity>();

// 回收
FPoolHelper.Release(entity);

// 预加载
FPoolHelper.Preload<BulletEntity>(10);
```

---

<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-03-05
completeness: medium
-->
