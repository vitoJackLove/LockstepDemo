# GameEntry 模块

> 位置: `Assets/Scripts/RunTime/GameEntry/`
> 父文档: [RunTime CLAUDE.md](../CLAUDE.md)
> 完善度: 完善

## 模块职责

`GameEntry` 是 Unity 场景进入游戏运行时后的启动入口，负责注册并初始化运行时组件，把 Unity 生命周期转发给全局 `Game` 单例体系，并打开启动 UI。它不承载具体战斗逻辑；战斗世界由 `GameSystem.WorldSystem` 创建，实体和帧同步逻辑在 `World` / `ECS` / `Server` 模块内处理。

## 目录结构

```text
GameEntry/
├── GameEntry.cs                    # MonoBehaviour 入口，驱动 Update/FixedUpdate/LateUpdate
├── GameEntry.Component.cs          # 静态组件入口和初始化顺序
├── GameEntryRunTime.cs             # RuntimeComponent 注册与获取
└── Component/
    ├── RunTimeComponent.cs         # 组件基类，Awake 时注册到 GameEntryRunTime
    ├── ResourceComponent.cs        # Addressables / Editor 资源加载
    ├── DataTableComponent.cs       # GameAssetConfig 数据表加载与查询
    ├── UIComponent.cs              # Loxodon UI 分组和窗口加载
    ├── SceneComponent.cs           # 场景加载
    ├── FrameSyncClientComponent.cs # TCP 帧同步客户端
    ├── ObserverComponent.cs        # 事件分发入口
    ├── CameraComponent.cs          # 相机入口
    ├── CanvasComponent.cs          # Canvas 入口
    └── GameSettingComponent.cs     # 全局设置入口
```

## 核心组件 / 类 / 系统

### `GameEntry`
- **位置**: `Assets/Scripts/RunTime/GameEntry/GameEntry.cs`
- **职责**: 在 `Start()` 中异步调用 `Init()`，初始化组件、注册 UI 分组并打开 `GameStartUpWindow`。
- **生命周期转发**: `Update()` / `FixedUpdate()` / `LateUpdate()` 分别调用 `Game.Update()` / `Game.FixedUpdate()` / `Game.LateUpdate()`；`OnDestroy()` 和 `OnApplicationQuit()` 调用 `Game.Close()`。

### `GameEntry.Component`
- **位置**: `Assets/Scripts/RunTime/GameEntry/GameEntry.Component.cs`
- **职责**: 暴露 `GameEntry.Resource`、`GameEntry.UI`、`GameEntry.DataTable`、`GameEntry.TcpClient` 等静态入口。
- **初始化顺序**: 先从 `GameEntryRunTime` 获取组件，再依次初始化 `GameSetting`、`Resource`、`UI`、`Scene`、`DataTable`、`Observer`、`Camera`、`Canvas`，最后初始化 `TcpClient`。

### `RunTimeComponent`
- **位置**: `Assets/Scripts/RunTime/GameEntry/Component/RunTimeComponent.cs`
- **职责**: 运行时组件基类，`Awake()` 自动注册到 `GameEntryRunTime`。
- **扩展点**: 新组件通常重写 `Init()`、`InitAsync()` 或 `Shutdown()`。

### `ResourceComponent`
- **位置**: `Assets/Scripts/RunTime/GameEntry/Component/ResourceComponent.cs`
- **职责**: 统一资源加载入口。
- **加载模式**: 当前只支持 `ResourceMode.Addressables = 0` 和 `ResourceMode.Editor = 2`；`ResourceMode.Resource` / `Resources.Load` 模式已移除。
- **接口**: 优先使用 `AsyncLoadAsset<T>(string path)`；同步 `LoadAsset<T>()` 已标记 `Obsolete`，仅用于旧调用点。
- **路径约定**: 运行时路径通过 `AssetsPathHelper` 生成，Addressables 模式要求地址已被编辑器工具同步到 Addressables 配置。

### `DataTableComponent`
- **位置**: `Assets/Scripts/RunTime/GameEntry/Component/DataTableComponent.cs`
- **职责**: 加载 `DataTableHelper.DataTableNames` 中声明的 ScriptableObject 配置，并通过 `GetDataTable<T>()` / `GetAllDataTable<T>()` 提供查询。
- **注意**: `Init()` 内部会调用 `InitAsync()`，而 `GameEntry.InitOptionalComponent()` 也会 `await DataTable.InitAsync()`；调整初始化流程时要避免重复加载或竞态。

### `FrameSyncClientComponent`
- **位置**: `Assets/Scripts/RunTime/GameEntry/Component/FrameSyncClientComponent.cs`
- **职责**: 使用 `TcpClient` 连接帧同步服务器，收发 protobuf 包，并把收到的消息排队回主线程后交给 `GameEntry.Observer`。
- **线程模型**: 后台线程负责阻塞读取网络包，`Update()` 负责派发主线程事件。

## 控制流

```text
Unity Scene
  -> GameEntry.Start()
  -> InitOptionalComponent()
  -> DataTable / Resource / UI / Scene / Network 初始化
  -> UI.OpenUIWindow<GameStartUpWindow>()
  -> Game.Update / FixedUpdate 转发
  -> GameStartUpViewModel 创建 WorldSystem 世界
```

## 对外接口

- `GameEntry.Resource.AsyncLoadAsset<T>(path)`: 异步加载 Addressables 或 Editor 资产。
- `GameEntry.DataTable.GetDataTable<T>(id)`: 获取 `GameAssetConfig` 中的配置项。
- `GameEntry.UI.OpenUIWindow<T>()`: 打开 UI 窗口。
- `GameEntry.TcpClient.Send(content, messageType)`: 发送帧同步消息。
- `GameEntry.Observer.Notify(eventType, payload)`: 分发运行时事件。

## 依赖关系

- **依赖**: `Cysharp.Threading.Tasks`、Addressables、Loxodon UI、Unity 场景和组件生命周期、运行时 `DataTable` / `Server` 模块。
- **被依赖**: `World` 初始化、`EntityViewSystem` 资源加载、UI ViewModel、帧同步客户端、配置表访问都通过 `GameEntry` 的静态组件入口接入。

## 常见任务

### 新增运行时组件
1. 在 `Component/` 下创建继承 `RunTimeComponent` 的类。
2. 确保组件挂在 `GameEntryRunTime` 所在 GameObject 或其注册体系可发现的位置。
3. 在 `GameEntry.Component.cs` 添加静态属性。
4. 在 `InitOptionalComponent()` 中按依赖顺序获取并初始化。

### 新增配置表
1. 新建或扩展实现 `IAssetsConfig` 的 ScriptableObject 配置。
2. 将资产放到 `Assets/GameAssetConfig/`。
3. 在 `DataTableHelper.DataTableNames` 中登记资产名。
4. 确保 Addressables 配置包含该资产，或在 Editor 模式下使用 AssetDatabase 路径加载。

### 调整资源加载
- 默认走 Addressables；不要新增 `Resources.Load` 分支。
- Editor 模式仅用于编辑器下直接加载 `AssetDatabase` 路径，运行时构建需要 Addressables 地址可解析。

## 注意事项

- `GameEntry` 是启动协调层，不应放入战斗规则、实体行为或回滚逻辑。
- 网络接收在后台线程执行，不要在 `ReceiveData()` 中直接触碰 Unity 对象。
- `ResourceMode.Editor` 包在 `#if UNITY_EDITOR` 中，构建环境不能依赖该分支。
- 修改初始化顺序前先检查 `UIComponent`、`DataTableComponent`、`FrameSyncClientComponent` 的依赖。

<!-- USER_CONTENT_START -->
<!-- 在这里保留用户手写补充内容 -->
<!-- USER_CONTENT_END -->

## 更新日志

| 日期 | 变更 | 模式 | 备注 |
|------|------|------|------|
| 2026-06-16 | 深化 GameEntry 启动、资源加载、网络客户端说明 | deep | 同步 ResourceMode.Resource 移除后的约束 |

---
<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-06-16
completeness: complete
-->
