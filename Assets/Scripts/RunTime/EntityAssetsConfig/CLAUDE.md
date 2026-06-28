# EntityAssetsConfig 模块

> 📍 位置: `Assets/Scripts/RunTime/EntityAssetsConfig/`
> 🔗 父文档: [RunTime CLAUDE.md](../CLAUDE.md)
> 📊 完善度: 🟡 中等

## 模块职责

EntityAssetsConfig 模块负责游戏中所有实体资源配置的管理，包括：
- **实体配置**: 英雄、怪物、子弹、方块等实体属性
- **资源映射**: 实体类型与 Prefab 资源的对应关系
- **状态配置**: 实体状态相关配置

## 目录结构

```
EntityAssetsConfig/
├── IAssetsConfig.cs              # 配置表接口
├── EntityAssetsConfig.cs         # 实体配置基类
├── HeroAssets.cs                # 英雄资源配置
├── MonsterAssets.cs              # 怪物资源配置
├── BulletAssets.cs               # 子弹资源配置
├── CubeAssets.cs                 # 方块资源配置
├── EffectAssets.cs              # 特效配置
├── DamageTextAssets.cs          # 伤害数字配置
├── StateAssets.cs               # 状态配置
├── BuffAsstes.cs                # Buff 配置
├── BuffConditionAssets.cs       # Buff 条件配置
├── BuffEffectAssets.cs          # Buff 效果配置
├── EntityCampConfig.cs          # 阵营配置
├── CubeType.cs                  # 方块类型
└── OperationMethod.cs           # 操作方法枚举
```

## 核心概念

### IAssetsConfig 接口

```csharp
public interface IAssetsConfig
{
    Type GetDataTableType();                    // 获取数据类型
    EntityAssetsConfig GetDataTable(int id);   // 获取指定配置
    List<EntityAssetsConfig> GetAllDataTable(); // 获取所有配置
}
```

### EntityAssetsConfig 基类

```csharp
public class EntityAssetsConfig
{
    public int assetsId;        // 资源ID
    public string assetsPath;   // 资源路径 (Prefab)
    public string assetsName;   // 资源名称
    // ... 其他属性
}
```

## 配置类型

### HeroAssets - 英雄配置

```csharp
public class HeroAssets : EntityAssetsConfig
{
    public fp MoveSpeed;        // 移动速度
    public fp MaxHp;           // 最大生命值
    public fp Attack;          // 攻击力
    public fp AttackSpeed;     // 攻击速度
    // ...
}
```

### MonsterAssets - 怪物配置

```csharp
public class MonsterAssets : EntityAssetsConfig
{
    public fp MoveSpeed;
    public fp MaxHp;
    public fp Attack;
    public int AIType;          // AI 类型
    // ...
}
```

### Buff 配置

```csharp
// BuffAsstes - Buff 基础配置
// BuffConditionAssets - Buff 触发条件
// BuffEffectAssets - Buff 效果实现
```

## 对外接口

### 获取实体配置

```csharp
// 通过 ID 获取
var heroConfig = HeroAssets.GetDataTable(heroId);

// 获取所有配置
var allMonsters = MonsterAssets.GetAllDataTable();
```

### 资源配置查找

```csharp
// 根据实体 ID 获取 Prefab 路径
var path = entityConfig.assetsPath;
var prefab = Resources.Load<GameObject>(path);
```

## 使用示例

```csharp
// 创建英雄实体
var heroConfig = HeroAssets.GetDataTable(heroId);
var entity = world.GetSystem<EntitySystem>().CreateEntity<HeroEntity>(
    heroConfig,
    position,
    isNeedView: true
);
```

## 扩展指南

### 添加新实体配置

1. 继承 `EntityAssetsConfig`
2. 添加实体特有的属性字段
3. 在 DataTableComponent 中注册配置表
4. 使用 Excel/JSON 表格配置数据

---

<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-03-05
completeness: medium
-->
