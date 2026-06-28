# Editor 工具模块

> 位置: `Assets/Scripts/Editor/`
> 父文档: [根目录 CLAUDE.md](../../../CLAUDE.md)
> 完善度: 中等

## 模块职责

`Assets/Scripts/Editor` 放置 Unity Editor 专用工具，覆盖配置表编辑、技能时间轴编辑、行为树编辑、protobuf 生成、Addressables 同步、地图/Buff/动画辅助等内容。该目录代码只应服务编辑器工作流，不应被运行时程序集依赖。

## 目录结构

```text
Editor/
├── Addressables/              # Runtime 资源 Addressables 同步和构建工具
├── AnimationToolWindow/       # 骨骼曲线和动画测量工具
├── Art/                       # 低模树等美术生成工具
├── BuffGenerateEditor/        # Buff 制作窗口
├── ConfigCenter/              # 配置中心窗口
├── MapGengerateEditor/        # 地图生成窗口
├── MonsterFactory/            # 怪物制作相关编辑器工具
├── ProtobufConverter/         # C# / protobuf 转换和生成工具
├── RollBack/                  # 预测回滚调试窗口
├── SkillEditor/               # 技能时间轴编辑器
├── SuperScrollView/           # SuperScrollView 自定义 Inspector
└── UnityBehaviourTreeEditor/  # 行为树编辑器窗口和 UI Toolkit 资源
```

## 子模块索引

| 子模块 | 路径 | 职责 | 完善度 | 文档 |
|--------|------|------|--------|------|
| Addressables | `./Addressables/` | 同步运行时资产到 Addressables group，构建运行时内容 | 基础 | 目录型 |
| ConfigCenter | `./ConfigCenter/` | ScriptableObject 配置资产集中编辑 | 基础 | 目录型 |
| ProtobufConverter | `./ProtobufConverter/` | C# 到 protobuf 转换、生成 Google.Protobuf 源码 | 中等 | [README.md](./ProtobufConverter/README.md) |
| SkillEditor | `./SkillEditor/` | 技能时间轴编辑窗口、轨道/片段 UI、编辑器事件 | 基础 | 目录型 |
| UnityBehaviourTreeEditor | `./UnityBehaviourTreeEditor/` | 行为树可视化编辑器和节点脚本模板 | 基础 | 目录型 |
| RollBack | `./RollBack/` | 预测回滚配置和双世界线调试窗口 | 基础 | 目录型 |
| MapGengerateEditor | `./MapGengerateEditor/` | 地图地形和噪声生成工具 | 基础 | 目录型 |
| BuffGenerateEditor | `./BuffGenerateEditor/` | Buff 制作工具窗口 | 基础 | 目录型 |

## 核心组件 / 类 / 系统

### `RuntimeAddressablesConfigurator`
- **位置**: `Addressables/RuntimeAddressablesConfigurator.cs`
- **菜单**: `Tools/Addressables/Sync Runtime Assets`、`Tools/Addressables/Build Runtime Content`
- **职责**: 将运行时加载所需资源同步到 Addressables，并构建运行时内容。
- **关联运行时**: `ResourceComponent` 的 Addressables 模式依赖这些地址可解析。

### `SkillTimelineEditorWindow`
- **位置**: `SkillEditor/Editor/Window/SkillTimelineEditorWindow.*.cs`
- **菜单**: `Tool/技能编辑器`
- **职责**: 编辑技能时间轴资产，配合运行时 `Assets/Scripts/RunTime/SkillEditor` 的 Track/Clip/Data 类型。

### `BehaviourTreeEditorWindow`
- **位置**: `UnityBehaviourTreeEditor/Editor/BehaviourTreeEditorWindow.cs`
- **菜单**: `Window/AI/BehaviourTree`
- **职责**: 可视化编辑行为树、黑板和节点脚本模板。

### `ConfigCenterWindow`
- **位置**: `ConfigCenter/ConfigCenterWindow.cs`
- **菜单**: `Tools/配置中心`
- **职责**: 集中管理配置资产，常与 `Assets/GameAssetConfig` 和 `DataTableComponent` 配套使用。

### Protobuf 工具
- **位置**: `ProtobufConverter/`
- **菜单**: `Tools/C# to Protobuf Converter`、`Tools/Protobuf Converter/*`
- **职责**: 生成 `.proto` 或 Google.Protobuf C# 源码，并提供协议转换测试入口。

## 数据流 / 控制流

```text
Editor Window / MenuItem
  -> 修改 ScriptableObject / prefab / generated source
  -> AssetDatabase 保存和刷新
  -> Runtime 模块通过 GameEntry.Resource / DataTable / SkillTimeline / BehaviourTree 使用
```

## 对外接口

- Unity 菜单项是主要入口：`Tools/*`、`Tool/*`、`Window/AI/*`、`Assets/Create/*`。
- 编辑器工具通常通过 `AssetDatabase`、`EditorWindow`、`OdinEditorWindow`、UI Toolkit 操作项目资产。
- 运行时不应直接引用 `UnityEditor` 或本目录类型。

## 依赖关系

- **依赖**: `UnityEditor`、Odin Inspector、Addressables Editor、UI Toolkit、运行时数据类型和 ScriptableObject 资产。
- **被依赖**: 内容制作流程、配置表维护、技能/行为树资产编辑、协议生成流程。

## 常见任务

### 新增编辑器工具
1. 放入 `Assets/Scripts/Editor/<Feature>/`，避免运行时程序集引用。
2. 使用清晰的 `MenuItem` 路径，优先放在现有 `Tools/` 或 `Tool/` 分组下。
3. 修改资产时使用 `Undo.RecordObject`、`EditorUtility.SetDirty` 和 `AssetDatabase.SaveAssets`。
4. 涉及 `.meta` 或资产移动时保持 Unity 资产引用稳定。

### 更新运行时资源加载
1. 修改运行时资源路径或新资源后，同步检查 `RuntimeAddressablesConfigurator`。
2. 确认 `Assets/Scripts/Libraries/AddressableAssetsData` 或当前 Addressables 配置包含新地址。
3. 避免回退到 `Resources.Load` 模式。

## 注意事项

- 不要把编辑器工具类移动到 `RunTime`，否则会污染构建程序集。
- `ProtobufConverter` 可能生成共享协议文件，修改后必须同步 Unity 客户端和 `Server/RogueGameServer`。
- 大量工具依赖项目约定资产路径，改路径前先搜索对应菜单/窗口和运行时加载点。
- `MapGengerateEditor` 保留历史拼写，文档和代码中不要仅为纠正拼写而改目录。

<!-- USER_CONTENT_START -->
<!-- 在这里保留用户手写补充内容 -->
<!-- USER_CONTENT_END -->

## 更新日志

| 日期 | 变更 | 模式 | 备注 |
|------|------|------|------|
| 2026-06-16 | 初始创建 Editor 工具总览 | deep | 补齐编辑器边界文档 |

---
<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-06-16
completeness: medium
-->
