# ECS 架构

> 📍 位置: `Assets/Scripts/RunTime/ECS/`
> 🔗 父文档: [RunTime CLAUDE.md](../CLAUDE.md)
> 📊 完善度: 🟡 中等

## 模块职责

ECS (Entity Component System) 模块是游戏的核心架构层，负责：
- **Entity (实体)**: 游戏中所有游戏对象的抽象基类
- **Component (组件)**: 存储实体数据，不包含逻辑
- **System (系统)**: 处理游戏逻辑，更新实体状态
- **View (视图)**: 实体在 Unity 场景中的表现层

## 目录结构

```
ECS/
├── Component/                    # 组件层 - 数据存储
│   ├── IComponentData.cs       # 组件数据接口
│   ├── ComponentData.cs         # 组件数据基类
│   ├── BaseComponent.cs        # 组件基类
│   ├── TransformComponent.cs    # 变换组件
│   ├── StateComponent.cs        # 状态组件
│   ├── HeroStateComponent.cs    # 英雄状态组件
│   ├── AiStateComponent.cs      # AI 状态组件
│   ├── SkillComponent.cs        # 技能组件 (多文件 partial)
│   ├── MoveComponent.cs         # 移动组件
│   ├── HitComponent.cs          # 伤害组件
│   ├── BuffComponent.cs         # Buff 组件
│   └── ...
├── Entity/                      # 实体层 - 对象抽象
│   ├── BaseEntity.cs           # 实体基类
│   ├── HeroEntity.cs           # 英雄实体
│   ├── MonsterEntity.cs        # 怪物实体
│   ├── BulletEntity.cs          # 子弹实体
│   ├── CubeEntity.cs            # 方块实体
│   ├── BuffEntity/              # Buff 实体
│   └── ...
├── System/                      # 系统层 - 逻辑处理
│   ├── ISystem.cs              # 系统接口
│   ├── BaseSystem.cs           # 系统基类
│   ├── EntitySystem.cs         # 实体管理系统
│   ├── BehaviourTreeSystem.cs  # 行为树系统
│   ├── SkillTimeLineSystem.cs  # 技能时间轴系统
│   ├── MapSystem.cs            # 地图系统
│   ├── CameraSystem.cs          # 相机系统
│   └── ...
├── View/                        # 视图层 - Unity 表现
│   ├── EntityView.cs           # 实体视图基类
│   ├── HeroEntityView.cs       # 英雄视图
│   ├── MonsterEntityView.cs    # 怪物视图
│   └── ...
└── Enum/                        # 枚举定义
    ├── CampEnum.cs             # 阵营枚举
    └── BattleExecuteTiming.cs  # 战斗执行时机
```

## 核心概念

### Entity (实体)

```csharp
public abstract partial class BaseEntity : ILifeCycle
{
    private int _entityId;              // 实体唯一ID
    private BaseWorld _baseWorld;        // 所属世界
    private EntityState _entityState;   // 实体状态
    private FTransform _fTransform;     // 定点数变换

    public int EntityId => _entityId;
    public EntityState EntityState => _entityState;
    public FTransform transform => _fTransform;
}
```

**生命周期**:
1. `OnInit(data)` - 初始化，创建组件
2. `OnStart(data)` - 开始，初始化视图
3. `OnUpdate(deltaTime)` - 每帧更新
4. `OnFixedUpdate(deltaTime, worldUpdateType)` - 固定帧更新
5. `DoEntityDead()` - 死亡处理
6. `OnDispose()` - 销毁，释放资源

**实体类型**:
- `HeroEntity` - 玩家英雄
- `MonsterEntity` - 怪物
- `BulletEntity` - 子弹/投射物
- `CubeEntity` - 场景方块

### Component (组件)

```csharp
public class HeroStateComponent : BaseComponent
{
    // 数据字段 (无逻辑)
    public fp MoveSpeed;
    public fp MaxHp;
    public fp CurrentHp;
}
```

**组件类型**:
| 组件 | 职责 |
|------|------|
| TransformComponent | 位置、旋转、缩放 |
| StateComponent | 实体状态管理 |
| SkillComponent | 技能系统 |
| MoveComponent | 移动逻辑 |
| HitComponent | 伤害计算 |
| BuffComponent | Buff 管理 |
| AiComponent | AI 控制 |

### System (系统)

```csharp
public class SkillTimeLineSystem : BaseSystem
{
    public override void OnInit(BaseWorld world) { }
    public override void OnStart() { }
    public override void OnUpdate(fp deltaTime) { }
    public override void OnFixedUpdate(fp deltaTime, WorldUpdateType worldUpdateType) { }
}
```

**核心系统**:
| 系统 | 职责 |
|------|------|
| EntitySystem | 实体创建/销毁/查询 |
| BehaviourTreeSystem | AI 行为树执行 |
| SkillTimeLineSystem | 技能时间轴 |
| MapSystem | 地图生成/管理 |
| CameraSystem | 相机控制 |
| BattleObserverSystem | 战斗事件观察 |

## 数据流

```
┌─────────────────────────────────────────────────────────────┐
│                        User Input                           │
└─────────────────────┬───────────────────────────────────────┘
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                    CommandSystem                            │
│              (输入命令收集与分发)                              │
└─────────────────────┬───────────────────────────────────────┘
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                      BaseWorld                              │
│           FixedUpdate(deltaTime)                            │
└───────┬─────────────────────┬───────────────────────┬────────┘
        ▼                     ▼                       ▼
┌───────────────┐    ┌───────────────┐    ┌───────────────┐
│  EntitySystem │    │ SkillSystem   │    │  AISystem     │
│  实体管理      │    │ 技能逻辑      │    │ AI 行为树     │
└───────┬───────┘    └───────┬───────┘    └───────┬───────┘
        ▼                     ▼                       ▼
┌─────────────────────────────────────────────────────────────┐
│                      Entities                               │
│   [Hero] [Monster] [Bullet] [Cube] ...                      │
│   每个实体包含多个 Component                                 │
└─────────────────────┬───────────────────────────────────────┘
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                      Views                                  │
│   [HeroView] [MonsterView] [BulletView] ...                │
│   Unity GameObject 表现层                                    │
└─────────────────────────────────────────────────────────────┘
```

## 对外接口

### 创建实体

```csharp
// 通过 EntitySystem 创建
var entity = world.GetSystem<EntitySystem>().CreateEntity<HeroEntity>(config, position);
```

### 获取组件

```csharp
var stateComp = entity.GetComponent<HeroStateComponent>();
```

### 获取系统

```csharp
var skillSystem = entity.GetSystem<SkillTimeLineSystem>();
```

## 帧同步机制

ECS 系统与帧同步深度集成：

1. **本地预测**: 本地先执行指令
2. **权威回滚**: 服务器指令驱动回滚
3. **快照系统**: 定期保存世界快照
4. **命令录制**: 记录所有输入命令

详见 [World 模块文档](../World/CLAUDE.md)

## 扩展指南

### 添加新实体

1. 继承 `BaseEntity`
2. 实现 `GetComponentTypes()` 返回所需组件类型
3. 实现 `EntityView()` 返回视图类型
4. 在 `EntityAssetsConfig` 中注册

### 添加新组件

1. 继承 `BaseComponent`
2. 实现组件数据和管理逻辑
3. 在实体类的 `GetComponentTypes()` 中注册

### 添加新系统

1. 继承 `BaseSystem`
2. 实现生命周期方法
3. 在 World 的 `GetSystemTypes()` 中注册

## 注意事项

- **定点数**: 核心逻辑使用 `Unity.Mathematics.fp` 避免浮点误差
- **无锁**: 游戏逻辑在主线程执行，无需线程安全
- **组件无状态**: 组件只存数据，逻辑在 System 中
- **Partial Class**: 大型类使用 partial 分离文件

---

<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-03-05
completeness: medium
-->
