# 场景配置（SceneAssets）设计

> 日期：2026-07-12  
> 状态：已确认

## 目标

在配置中心增加场景配置，支持：

1. 竞技场模板：自动生成初始地板 + 四面空气墙
2. 额外碰撞体列表：自定义静态阻挡
3. 运行时在世界启动时生成物理碰撞体，视觉 Prefab 可选

## 架构

```text
SceneAssets.asset (GameAssetConfig)
    └── SceneAssetsConfig[]  (按 sceneName 索引)
            ├── arenaTemplate
            └── extraColliderPieces[]

SceneEnvironmentSystem.SpawnEnvironmentAsync(sceneName)
    ├── 查 SceneAssetsConfig
    ├── 模板 → 5 个 CubeEntity
    └── 列表 → N 个 CubeEntity
```

## 数据结构

- `SceneAssets` / `SceneAssetsConfig` / `SceneArenaTemplate` / `SceneColliderPiece`
- 空气墙：`Rigidbody + Static + Wall` 层
- 地板：`Rigidbody + Static + Default` 层

## 运行时

- `RogueWorld.GamePreparation` 最先调用 `SceneEnvironmentSystem`
- 复用 `CubeEntity`，通过 `EntityCreateData.EntityData` 注入 `PhysicsEntityConfig`
- `viewPrefabPath` 非空时异步加载到 `MapRoot`

## 配置中心

- `DataTableHelper` 注册 `SceneAssets`
- 自动扫描 `Assets/GameAssetConfig/SceneAssets.asset`

## 验收

1. 配置中心可编辑 SceneAssets
2. RougeBattle 进入后生成地板 + 空气墙
3. KccPhysicsDebugView 可见碰撞体
4. 英雄被墙阻挡、站在地板上
