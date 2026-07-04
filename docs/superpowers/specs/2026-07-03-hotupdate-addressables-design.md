# 热更代码迁入 Addressables 设计

> 日期：2026-07-03  
> 状态：已确认  
> 依赖：[Addressables 重配置设计](./2026-07-02-addressables-reconfiguration-design.md)  
> 模式：本地首包打底 + 远程 OTA 代码热更 + Agent/CI 离线可跑

## 1. 背景与目标

### 1.1 现状

- HybridCLR 热更 DLL 与 AOT 元数据存放在 `StreamingAssets/GameHotUpdate/*.dll.bytes`。
- Player 启动时 `HybridClrAotMetadataLoader` / `HybridClrHotUpdateAssemblyLoader` 通过 `File.ReadAllBytes` 读取。
- 热更加载发生在 `GameEntry.Init()` 的 **第一步**，早于 `AddressablesBootstrapComponent.InitializeAsync()`。
- 远程更新代码需整包替换 `StreamingAssets/GameHotUpdate`，无法复用现有 Addressables Catalog / Content Update 管线。

### 1.2 目标（用户确认：选项 C）

1. **统一打包管线**：热更 DLL 纳入 Addressables 同步 / 构建 / Content Update 工具链。
2. **远程 OTA**：`Game.Runtime.dll` 可通过 Catalog 更新从 CDN 拉取；**重启 App 后**加载新版本（HybridCLR 不支持同会话卸载重载热更程序集）。
3. **离线可玩**：首包 Local Bundle 包含基线热更代码 + AOT 元数据；无 CDN 时正常启动。
4. **Agent/CI 离线**：`ClientAgentGameEntryMode`、CI 批跑、本地 Player 测试不依赖网络；Catalog 检查可跳过或超时回退本地。
5. **入口最小改动**：保留 AOT 壳 + `IGameHotUpdateBootstrap` 回调架构；仅调整启动顺序与 Loader 数据源。

### 1.3 非目标

- 不实现 App 运行中无重启的热更代码热载。
- 不改变 AOT 程序集 `GameFramework` 的热更边界（AOT 层变更仍需重打 Player 包）。
- 不修改 `AssetsPathHelper` 业务资源 Address 契约。
- 热更 DLL 不走 `ResourceComponent`（避免与业务资源加载耦合，且热更阶段 `ResourceComponent` 尚未 Init）。

---

## 2. 启动顺序（核心变更）

### 2.1 现状

```
GameEntry.Start
  → LoadHotUpdateAssembliesAsync()     // File / StreamingAssets
  → InitOptionalComponent()            // Addressables.InitializeAsync 在此
  → HotUpdateBootstrap.RegisterUiGroups()
  → InitService() / OpenStartForm()
```

### 2.2 目标

```
GameEntry.Start
  → PreBootstrapAddressablesAsync()    // 新增：幂等 Initialize + 可选 Catalog 更新
  → LoadHotUpdateAssembliesAsync()     // Addressables 读 TextAsset.bytes
  → InitOptionalComponent()            // Addressables 已初始化，跳过重复 Init
  → HotUpdateBootstrap.RegisterUiGroups()
  → InitService() / OpenStartForm()
```

### 2.3 PreBootstrap 职责

| 步骤 | 说明 |
|------|------|
| `Addressables.InitializeAsync()` | 必须；幂等，多次调用安全 |
| `ApplyRemoteBaseUrl()` | 仅使用 `AddressablesContentSettings` / `PlayerPrefs`，**不依赖** `FrameSyncTransport`（此时尚未 ConfigureTransport） |
| `TryUpdateCatalogsAsync()` | 按策略表（§5）决定是否执行；失败或超时回退本地 Catalog |

### 2.4 不变部分

- Editor：`EditorAotMetadataLoader` + `EditorHotUpdateAssemblyLoader`（复用 AppDomain 内 `Game.Runtime`）。
- Player 服务注册：`ActivateHotUpdateServices` 反射注册 `GameHotUpdateBootstrap`、`NetworkProtobufCodec`。
- `HotUpdateRuntimeInitialize`（Editor BeforeSceneLoad）保持不变。

---

## 3. Addressables Group 与资产布局

### 3.1 新增 Group

| Group | 资产范围 | Local Build | Remote Path | IncludeInBuild | Content Update | Label |
|-------|----------|-------------|-------------|----------------|----------------|-------|
| **HotUpdate-Code-Local** | `Assets/HotUpdate/Code/AOT/*.dll.bytes` | Local.BuildPath | — | true | **Static** | `hotupdate-aot` |
| **HotUpdate-Code-Remote** | `Assets/HotUpdate/Code/Game.Runtime.dll.bytes` | Local + Remote | Remote.LoadPath | true（首包基线） | **Can Change** | `hotupdate-runtime` |

说明：

- **AOT 元数据**（约 21 个 `*.dll.bytes`）：Static，随 Player 包发布；仅当 HybridCLR `patchAOTAssemblies` / 泛型桥接变更时需发新包。
- **Game.Runtime.dll.bytes**：Can Change；Content Update 可单独推送；首包必须含 Local 副本保证离线启动。

### 3.2 目录与 Address 约定

```
Assets/HotUpdate/Code/
├── AOT/
│   ├── mscorlib.dll.bytes
│   ├── System.dll.bytes
│   └── ...（patchAOTAssemblies 对应文件）
├── Game.Runtime.dll.bytes
└── manifest.json                    // 可选：buildTarget、hash、生成时间
```

Address 格式（与项目现有 `Assets/...` 全路径一致）：

| 用途 | Address |
|------|---------|
| 热更主程序集 | `Assets/HotUpdate/Code/Game.Runtime.dll.bytes` |
| AOT 元数据 | `Assets/HotUpdate/Code/AOT/{Name}.dll.bytes` |
| 清单（可选） | `Assets/HotUpdate/Code/manifest.json` |

Unity 资产类型：全部作为 **TextAsset**（`.bytes` 扩展名）注册 Addressables。

### 3.3 AddressablesGroupResolver 扩展

在 `ResolveGroupName()` 增加：

- `Assets/HotUpdate/Code/AOT/` → `HotUpdate-Code-Local`
- `Assets/HotUpdate/Code/Game.Runtime.dll.bytes` → `HotUpdate-Code-Remote`

### 3.4 RuntimeAddressablesConfigurator 扩展

- `GroupDefinitions` 增加上述两组。
- `EnumerateRuntimeAssetPaths()` 增加 `Assets/HotUpdate/Code` 扫描。
- AOT 子目录与主 DLL 分 Group 注册（通过路径规则或 Resolver）。

---

## 4. 运行时加载实现

### 4.1 新增 / 替换 Loader（Player）

| 类 | 职责 |
|----|------|
| `AddressablesAotMetadataLoader` | `LoadAssetsAsync<TextAsset>("hotupdate-aot")` 或按 Address 列表加载；`RuntimeApi.LoadMetadataForAOTAssembly` |
| `AddressablesHotUpdateAssemblyLoader` | `LoadAssetAsync<TextAsset>(hotUpdateAddress)` → `Assembly.Load(bytes)` |

**Factory 变更**（`GameHotUpdateEntryFactory`）：

```csharp
#if UNITY_EDITOR
    // 不变
#else
    return new GameHotUpdateEntry(
        new AddressablesAotMetadataLoader(),
        new AddressablesHotUpdateAssemblyLoader(),
        usedEditorAssembly: false);
#endif
```

删除或保留 `HybridClr*Loader` 为 internal 备用（建议删除，避免双轨）。

### 4.2 加载 API 约束

- 直接使用 `UnityEngine.AddressableAssets.Addressables`，**不经过** `ResourceComponent`。
- 加载完成后 `Addressables.Release(handle)`（AOT 元数据加载后可 Release；热更 Assembly 加载后 bytes 可 Release，程序集常驻内存）。
- 失败时 Error 日志含 Address、Status、OperationException（Development Build 与 Editor 全级别；Release 至少 Warn）。

### 4.3 GameHotUpdateEntryOptions 扩展

| 字段 | 类型 | 说明 |
|------|------|------|
| `HotUpdateAssemblyAddress` | string | 默认 `Assets/HotUpdate/Code/Game.Runtime.dll.bytes` |
| `AotMetadataLabel` | string | 默认 `hotupdate-aot` |
| `UseAddressables` | bool | Player 固定 true；Editor false（由 Factory 决定，可不暴露） |

移除或废弃 `StreamingAssetsFolderName`（Player 路径）；Editor 不使用该字段。

### 4.4 GameHotUpdateEntryComponent 序列化

Launcher 场景组件字段调整为：

```yaml
enabledOnStart: 1
hotUpdateAssemblyName: Game.Runtime          # Editor 用
hotUpdateAssemblyAddress: Assets/HotUpdate/Code/Game.Runtime.dll.bytes
aotMetadataLabel: hotupdate-aot
autoDiscoverAotAssemblies: 1                 # Addressables 下改为 Label 发现，语义保留
```

---

## 5. Catalog 更新与离线策略

### 5.1 策略表

| 场景 | PreBootstrap Catalog 更新 | 远程 URL | 热更 DLL 来源 |
|------|---------------------------|----------|---------------|
| 正常 Player（有 CDN 配置） | 开启，超时回退本地 | `DefaultRemoteBaseUrl` 或 Dev PlayerPrefs | 更新后 Remote；失败则 Local 基线 |
| `ForceLocalOnly = true` | **跳过** | 空 | 仅 Local 首包 |
| `ClientAgentGameEntryMode.IsRunning` | **跳过** | 空 | 仅 Local 首包 |
| CI / 批处理（`-batchmode`） | **跳过**（见 §5.2） | 空 | 仅 Local 首包 |
| Editor Play | 不适用（Editor Loader） | — | AppDomain |

### 5.2 AddressablesContentSettings 扩展

在现有 ScriptableObject 上增加：

| 字段 | 类型 | 默认 | 说明 |
|------|------|------|------|
| `EnableHotUpdateCatalogCheck` | bool | true | 是否在 PreBootstrap 阶段检查 Catalog（与业务资源 Catalog 检查共用开关或独立，**推荐独立**以便 Agent 只关代码热更检查） |
| `SkipCatalogCheckInBatchMode` | bool | true | `Application.isBatchMode` 时跳过 Catalog 更新 |

现有字段复用：

- `ForceLocalOnly`：强制本地，Agent 测试场景可勾选或通过代码设置。
- `CatalogUpdateTimeoutSeconds`：PreBootstrap 与 InitOptionalComponent 共用。

### 5.3 Agent / CI 保证

1. **ClientAgentGameEntryRunner** 已在 `Start` 设置 `ClientAgentGameEntryMode.IsRunning = true`；PreBootstrap 检测后跳过 Catalog 更新。
2. **CI 构建**：Player 包构建前执行 Addressables Local Build，确保 `HotUpdate-Code-*` 组 IncludeInBuild；CI 运行加 `-batchmode` 触发 `SkipCatalogCheckInBatchMode`。
3. **无 CDN URL**：`DefaultRemoteBaseUrl` 为空时，`ShouldCheckCatalog()` 返回 false（与现有 Addressables 设计一致）。
4. **验收**：断网启动 Player / Agent PlayMode 测试，日志应出现本地加载热更 DLL，无 Catalog 超时阻塞。

---

## 6. Editor 工具链

### 6.1 HybridCLRDllCopyTool 变更

| 菜单 | 行为 |
|------|------|
| 正式入口/拷贝 DLL 到 StreamingAssets | **改为**「同步 DLL 到 Addressables 热更目录」 |
| 正式入口/编译并拷贝 DLL | **改为**「编译并同步 DLL 到 Addressables 热更目录」 |

拷贝目标：

```
HybridCLRData/HotUpdateDlls/{Target}/Game.Runtime.dll
  → Assets/HotUpdate/Code/Game.Runtime.dll.bytes

HybridCLRData/AssembliesPostIl2CppStrip/{Target}/*.dll
  → Assets/HotUpdate/Code/AOT/{Name}.dll.bytes
```

完成后提示执行 `Tools/Addressables/同步运行时资源`（或工具内自动调用 Sync）。

**废弃** `StreamingAssets/GameHotUpdate`（迁移完成后从首包移除；过渡期可在文档注明一次性清理）。

### 6.2 发布流程

#### 首包（Local + 远程基线）

1. HybridCLR Generate/AotDlls + CompileDll  
2. Tools/HybridCLR/正式入口/编译并同步 DLL 到 Addressables 热更目录  
3. Tools/Addressables/同步运行时资源  
4. Tools/Addressables/构建运行时内容  
5. Unity Build Player（IL2CPP）

#### 仅代码 OTA（不发新 Player）

1. 修改 `Game.Runtime` 代码  
2. HybridCLR CompileDll + 同步 DLL  
3. Tools/Addressables/构建远程更新（Content Update）  
4. 上传 `ServerData/[BuildTarget]` 与 catalog 到 CDN  
5. **用户重启 App** → PreBootstrap Catalog 更新 → 加载新 `Game.Runtime.dll.bytes`

---

## 7. 入口层文件变更清单

| 文件 | 变更类型 | 说明 |
|------|----------|------|
| `GameEntry.cs` | **修改** | `Init()` 插入 `PreBootstrapAddressablesAsync()` |
| `GameEntry.Component.cs` | **修改** | 提取 PreBootstrap；InitOptionalComponent 内 Addressables 幂等 |
| `AddressablesBootstrapComponent.cs` | **修改** | 拆分 `EnsureInitializedAsync`、`ShouldCheckCatalogForHotUpdate` |
| `AddressablesContentSettings.cs` | **修改** | 新增 Agent/CI 相关开关 |
| `GameHotUpdateEntryFactory.cs` | **修改** | Player 使用 Addressables Loader |
| `GameHotUpdateEntryOptions.cs` | **修改** | Address / Label 配置 |
| `GameHotUpdateEntryComponent.cs` | **修改** | 序列化字段 |
| `AddressablesAotMetadataLoader.cs` | **新增** | |
| `AddressablesHotUpdateAssemblyLoader.cs` | **新增** | |
| `GameHotUpdatePathUtility.cs` | **删除或仅 Editor** | Player 不再用文件路径 |
| `HybridClrAotMetadataLoader.cs` | **删除** | 由 Addressables 版替代 |
| `HybridClrHotUpdateAssemblyLoader.cs` | **删除** | 同上 |
| `AddressablesGroupResolver.cs` | **修改** | 新 Group 规则 |
| `RuntimeAddressablesConfigurator.cs` | **修改** | 新 Group + 扫描路径 |
| `HybridCLRDllCopyTool.cs` | **修改** | 拷贝目标目录 |
| `Assets/Scene/Launcher.unity` | **修改** | GameHotUpdateEntryComponent 字段 |

**不需修改**：`GameHotUpdateEntry.cs`（模板方法）、`IGameHotUpdateBootstrap`、`GameHotUpdateBootstrap`、`HotUpdateRuntimeInitialize`、`NetworkPacketCodecProvider`。

---

## 8. manifest 与版本校验

### 8.1 manifest.json（Addressable TextAsset）

```json
{
  "generatedAtUtc": "2026-07-03T12:00:00Z",
  "buildTarget": "StandaloneWindows64",
  "hotUpdateAssembly": "Game.Runtime.dll.bytes",
  "hotUpdateAssemblyHash": "sha256:...",
  "aotMetadataCount": 21
}
```

### 8.2 启动校验（可选，推荐 Phase 2）

- PreBootstrap 加载 manifest，与本地 `Application.version` 或内置 baseline hash 比对，不匹配时 Warn。
- AOT 元数据 count 与 `patchAOTAssemblies` 不一致时 Error 并提示重打 Player 包。

Phase 1 可仅记录日志，不阻断启动。

---

## 9. 错误处理

| 错误 | 行为 |
|------|------|
| Addressables 初始化失败 | 阻断启动，Dialog/LogError |
| Catalog 更新超时 | Warn，继续本地 Catalog |
| 热更主 DLL Address 加载失败 | 阻断启动（无法进入游戏） |
| 单个 AOT 元数据缺失 | Warn + 跳过（与现 HybridClr 行为一致） |
| Bootstrap 未注册 | 抛 `InvalidOperationException`（现有逻辑） |

---

## 10. 测试计划

| 用例 | 环境 | 预期 |
|------|------|------|
| Editor Play 进入 Launcher | Editor | 复用 AppDomain Game.Runtime，无 Addressables DLL 加载 |
| Player 离线首启 | 断网 Player | PreBootstrap 跳过/失败 Catalog；Local 加载热更 DLL；进入 GameStartUp |
| Agent 自动进战斗 | ClientAgentGameEntryMode | 无 Catalog 网络请求；30s 内 Enter game succeeded |
| CI batchmode | `-batchmode -runTests` | 无 Catalog 阻塞；EditMode/PlayMode 相关测试通过 |
| 远程代码 OTA | CDN 有新 catalog + DLL | 重启后 PreBootstrap 更新 Catalog；加载新 Game.Runtime；玩法变更生效 |
| AOT 元数据-only 本地 | 删 Remote 仅留 Local | 正常启动 |

---

## 11. 迁移步骤（一次性）

1. 创建 `Assets/HotUpdate/Code/` 目录结构。  
2. 实现 Addressables Loader + PreBootstrap 启动顺序。  
3. 扩展 Addressables 配置器与 Group。  
4. 修改 HybridCLRDllCopyTool 输出路径。  
5. 执行同步 + Addressables Build + Player Build。  
6. 验证离线 Player / Agent。  
7. 删除 `Assets/StreamingAssets/GameHotUpdate/`（及 Build 产物中同名目录）。  
8. 更新 README / CLAUDE 中热更流程说明。

---

## 12. 风险与缓解

| 风险 | 缓解 |
|------|------|
| 启动顺序回归 | Development Build 日志 + Agent 集成测试 |
| 首包未含 Local 热更 DLL | IncludeInBuild=true；构建 CI 检查 bundle 存在 |
| Catalog 更新阻塞 Agent | IsRunning + batchmode + ForceLocalOnly 三重跳过 |
| 新 DLL 与旧 AOT 元数据不兼容 | manifest hash；文档强调 AOT 变更需发新包 |
| TextAsset 大小限制 | Game.Runtime.dll 通常 &lt; 10MB，可接受；极大时考虑自定义 ResourceProvider（非本阶段） |

---

## 13. 决策记录

| 决策 | 选择 | 理由 |
|------|------|------|
| 分发通道 | Addressables 替代 StreamingAssets | 统一 CDN OTA |
| 启动顺序 | PreBootstrap → 热更 → 业务 Init | Addressables 依赖 |
| 离线 | 首包 Local IncludeInBuild | 选项 C 要求 |
| Agent/CI | 跳过 Catalog + 仅 Local DLL | 无网络依赖 |
| 代码 OTA 生效时机 | 重启 App | HybridCLR 限制 |
| 热更加载 API | 直接 Addressables，不用 ResourceComponent | 避免循环依赖 |

---

<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-07-03
status: pending_review
-->
