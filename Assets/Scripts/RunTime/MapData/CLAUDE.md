# MapData 模块

> 📍 位置: `Assets/Scripts/RunTime/MapData/`
> 🔗 父文档: [RunTime CLAUDE.md](../CLAUDE.md)
> 📊 完善度: 🟡 中等

## 模块职责

MapData 模块负责游戏地图数据的定义和管理，包括：
- **地形类型**: 不同地形块的属性定义
- **地图生成**: 地图生成算法和配置
- **噪声数据**: 用于程序化地图生成的噪声设置

## 目录结构

```
MapData/
├── MapTerrainType.cs           # 地形类型枚举
├── MapGenerateType.cs         # 地图生成类型
├── MapTerrainData.cs          # 地形数据
├── MapTerrainCellSetting.cs   # 地形块设置
├── TerrainNoiseDataSetting.cs # 噪声数据设置
└── ...
```

## 核心概念

### MapTerrainType - 地形类型

```csharp
public enum MapTerrainType
{
    Ground,      // 地面
    Wall,        // 墙壁
    Water,       // 水域
    Lava,        // 熔岩
    Obstacle,   // 障碍物
    // ...
}
```

### MapGenerateType - 生成类型

```csharp
public enum MapGenerateType
{
    Random,      // 随机生成
    PerlinNoise, // Perlin 噪声
    Cellular,   // 元胞自动机
    Room,       // 房间生成
    Grid,       // 网格布局
}
```

### MapTerrainData - 地形数据

```csharp
public class MapTerrainData
{
    public int Width;           // 地图宽度
    public int Height;          // 地图高度
    public MapTerrainType[,] Terrain; // 地形网格

    public bool IsWalkable(int x, int y);
    public MapTerrainType GetTerrain(int x, int y);
}
```

### MapTerrainCellSetting - 地形块设置

```csharp
public class MapTerrainCellSetting
{
    public MapTerrainType TerrainType;
    public bool IsWalkable;     // 是否可行走
    public bool IsTransparent;  // 是否透明
    public fp MoveCost;         // 移动消耗
}
```

### TerrainNoiseDataSetting - 噪声设置

```csharp
public class TerrainNoiseDataSetting
{
    public float Scale;        // 噪声缩放
    public float Threshold;    // 阈值
    public int Octaves;        // 叠加层数
    public float Persistance;  // 持续度
}
```

## 地图生成流程

```
┌─────────────────────────────────────────────────────────────┐
│                 Map Generation Flow                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. 初始化参数                                              │
│     ├── 地图尺寸 (Width x Height)                          │
│     ├── 生成类型                                            │
│     └── 种子 (Seed)                                        │
│                                                             │
│  2. 选择生成算法                                            │
│     ├── Random: 随机分布                                   │
│     ├── PerlinNoise: 平滑噪声                              │
│     ├── Cellular: 元胞自动机                               │
│     └── Room: 房间+走廊                                    │
│                                                             │
│  3. 生成地形数据                                            │
│     └── 生成 MapTerrainData                                │
│                                                             │
│  4. 后处理                                                  │
│     ├── 边界检查                                            │
│     ├── 寻路验证                                            │
│     └── 放置出生点                                          │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## 对外接口

### 创建地图

```csharp
var mapData = new MapTerrainData(width, height);
mapData.Generate(MapGenerateType.PerlinNoise, seed);

// 获取地形
var terrain = mapData.GetTerrain(x, y);
```

### 寻路检查

```csharp
if (mapData.IsWalkable(x, y))
{
    // 可以行走
}
```

---

<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-03-05
completeness: medium
-->
