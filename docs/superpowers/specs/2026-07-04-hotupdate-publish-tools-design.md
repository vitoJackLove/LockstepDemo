# 热更发布工具重设计

> 日期：2026-07-04  
> 状态：已确认（方案 A）  
> 依赖：[Addressables 重配置设计](./2026-07-02-addressables-reconfiguration-design.md)、[热更代码迁入 Addressables 设计](./2026-07-03-hotupdate-addressables-design.md)  
> 模式：统一 EditorWindow「热更发布中心」+ 共享 Pipeline 内核

## 1. 背景与目标

### 1.1 现状

运行时热更架构已完成迁移：

- 代码热更：`HybridCLR` + Addressables（`HotUpdate-Code-Local` / `HotUpdate-Code-Remote`）
- 资源热更：Addressables Group + Content Update（Local 首包 + Remote OTA）
- 启动顺序：`PreBootstrapAddressablesAsync` → `LoadHotUpdateAssembliesAsync` → 业务 Init

Editor 工具链仍分散在 `Tools/HybridCLR/` 与 `Tools/Addressables/`，共 9+ 个独立 MenuItem，操作者需手工串联 5 步以上流程，且「构建远程更新」需手动选择 content state 文件。

### 1.2 目标

1. **单一入口**：`Tools/发布/热更发布中心` EditorWindow，替代所有分散的热更/Addressables 构建 MenuItem。
2. **三个主工具**：首包构建、代码热更打包、资源热更打包，各一键完成。
3. **共享 Pipeline**：Window 与后续 CLI（Phase 2）共用 `HotUpdateBuildPipeline`，避免双轨逻辑。
4. **自动 manifest**：记录 content state 路径、版本、hash，消除手选 content state。
5. **开发测试内聚**：本地 HTTP / 环境配置迁入 Window「开发测试」Tab，不再单独占 MenuItem。

### 1.3 非目标

- 不修改运行时热更加载逻辑（`GameFramework/Bootstrap/HotUpdate/*` 保持不变）。
- 不实现 App 运行中无重启的代码热载（HybridCLR 限制）。
- Phase 1 不实现 CDN 自动上传（工具 6 仅设计预留接口）。
- Phase 1 不实现 `-executeMethod` CLI（工具 8 预留目录与接口）。

---

## 2. 方案决策

| 决策 | 选择 | 理由 |
|------|------|------|
| 主入口形式 | **A：统一 EditorWindow** | 流程可视化、日志集中、适合团队协作 |
| 底层架构 | Pipeline + Step 编排 | Window 薄 UI，逻辑可复用于 CI |
| 菜单收敛 | 仅保留 1 个 MenuItem | 降低认知负担 |
| content state | 读/写 `HotUpdateManifest.json` | 消除 OpenFilePanel 手选 |
| 本地测试 | Window「开发测试」Tab | 合并现有 3 个 HTTP/配置 MenuItem |

---

## 3. 整体架构

```text
┌─────────────────────────────────────────────────────────────┐
│           热更发布中心 (HotUpdatePublishWindow)               │
│  Tab: [发布] [开发测试] [历史记录]                              │
├─────────────────────────────────────────────────────────────┤
│  工具1 首包构建  │  工具2 代码热更  │  工具3 资源热更           │
│  [一键构建]      │  [打包代码]      │  [打包资源]               │
├─────────────────────────────────────────────────────────────┤
│  构建日志面板（滚动 + 导出）                                   │
│  高级选项（折叠）：平台 / Development / CDN URL / 分组策略      │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│              HotUpdateBuildPipeline                          │
│  PreflightCheck → HybridClr → AddressablesSync → Build/Update│
│  → [PlayerBuild] → ManifestWrite                             │
└──────────────────────────┬──────────────────────────────────┘
                           │
           ┌───────────────┼───────────────┐
           ▼               ▼               ▼
   Assets/HotUpdate/Code   ServerData/    Build/
   (DLL .bytes)            {Target}/      HotUpdateManifest.json
```

---

## 4. EditorWindow 设计

### 4.1 菜单入口

```text
Tools/发布/热更发布中心
```

仅此一个对外 MenuItem。Window 最小尺寸建议 520×640，支持 Dock。

### 4.2 Tab：发布

#### 公共控件（三个工具共享）

| 控件 | 类型 | 默认 | 说明 |
|------|------|------|------|
| 构建平台 | EnumPopup | `EditorUserBuildSettings.activeBuildTarget` | 与 HybridCLR / Addressables 一致 |
| 版本号 | TextField | `PlayerSettings.bundleVersion` | 写入 manifest；首包可选同步到 PlayerSettings |
| 远程 CDN 根 URL | TextField | 读 `AddressablesContentSettings.DefaultRemoteBaseUrl` | 不含 BuildTarget；构建时同步 Profile |
| Development Build | Toggle | 读 `EditorUserBuildSettings.development` | 影响 HybridCLR CompileDll 与 Player Build |
| 构建前检查 | Button | — | 调用 `PreflightCheckStep`，结果展示在日志区 |

#### 工具 1：首包构建

**按钮文案**：`构建首包（代码 + 资源 + 应用）`

**触发 Pipeline**：`HotUpdateBuildPipeline.RunFullPackageAsync(context)`

**步骤序列**：

1. `PreflightCheckStep`（FullPackage 模式）
2. `HybridClrBuildStep`（Generate 提示 + CompileDll + 同步 DLL 到 `Assets/HotUpdate/Code/`）
3. `AddressablesSyncStep`
4. `AddressablesFullBuildStep`（`BuildPlayerContent`）
5. `PlayerBuildStep`（IL2CPP Player）
6. `ManifestWriteStep`（type=`full`，写入 content state 路径）

**完成后展示**：

- Player 产物路径（`.exe` / `.apk` 等）
- `ServerData/{BuildTarget}/` 路径与总大小
- manifest 中记录的 `contentStatePath`

**高级选项（折叠）**：

- 输出目录（默认 `Build/` 下按平台+版本命名）
- 构建场景列表（默认当前 Editor Build Settings）
- 跳过 Player Build（仅生成 Addressables 首包内容，供 CI 分步时使用）

#### 工具 2：代码热更打包

**按钮文案**：`打包代码热更`

**触发 Pipeline**：`HotUpdateBuildPipeline.RunCodePatchAsync(context)`

**步骤序列**：

1. `PreflightCheckStep`（CodePatch 模式）
2. `AotChangeDetectorStep` — 若检测到 AOT 层变更 → **阻断**，日志提示改走工具 1
3. `HybridClrBuildStep`（仅 CompileDll + 同步 `Game.Runtime.dll.bytes` + manifest.json hash）
4. `AddressablesSyncStep`（仅热更代码相关路径）
5. `AddressablesContentUpdateStep`（groups=`HotUpdate-Code-Remote` only）
6. `ManifestWriteStep`（type=`code`）

**UI 约束提示**（HelpBox，常显）：

> 代码热更需用户**重启 App** 后生效。若修改了 `GameFramework` 或 AOT 元数据，请使用「首包构建」。

**高级选项**：

- 强制跳过 AOT 变更检测（仅高级用户，默认关闭）

#### 工具 3：资源热更打包

**按钮文案**：`打包资源热更`

**触发 Pipeline**：`HotUpdateBuildPipeline.RunResourcePatchAsync(context)`

**步骤序列**：

1. `PreflightCheckStep`（ResourcePatch 模式）
2. `AddressablesSyncStep`
3. `AssetChangeSummaryStep` — 按 Group 统计新增/移动/删除条目，写入日志
4. `AddressablesContentUpdateStep`（groups=所有 `Can Change` 组，**排除** `HotUpdate-Code-*`）
5. `ManifestWriteStep`（type=`resource`）

**高级选项**：

- 分组策略：`全部 Can Change`（默认）/ 指定 Group 多选 / 指定 Label
- 构建前预览变更列表（只读 TreeView）

### 4.3 Tab：开发测试

合并现有 `AddressablesLocalHotUpdateEnvironment` 能力：

| 控件 | 行为 |
|------|------|
| 一键配置本地环境 | 写 ContentSettings、Profile URL、PlayerPrefs |
| 启动 / 停止本地 HTTP | 服务 `ServerData/` 根目录 |
| 本地端口 | 默认 8080，可编辑 |
| ForceLocalOnly 快捷开关 | 写 ContentSettings，用于断网首包测试 |
| 模拟 OTA 流程说明 | HelpBox：先工具 2/3 构建 → 启 HTTP → Play |

**环境状态面板**（只读）：

- 当前平台、ServerData 是否存在、HTTP 是否运行、PlayerPrefs URL 覆盖

### 4.4 Tab：历史记录

读取 `Build/HotUpdateManifest.json` 的 `patches` 数组，列表展示：

| 列 | 内容 |
|----|------|
| 时间 | `generatedAtUtc` |
| 类型 | full / code / resource |
| 版本 | patch version |
| 平台 | buildTarget |
| 产物 | ServerData 相对路径链接（Reveal in Explorer） |

---

## 5. Pipeline 内核设计

### 5.1 核心类型

```csharp
// 构建模式
enum HotUpdateBuildMode { FullPackage, CodePatch, ResourcePatch }

// 上下文（Window 与 CLI 共用）
class HotUpdateBuildContext
{
    BuildTarget BuildTarget;
    string Version;
    string RemoteBaseUrl;
    bool DevelopmentBuild;
    HotUpdateBuildMode Mode;
    IReadOnlyList<string> ResourceGroupFilter;  // null = 默认策略
    bool SkipPlayerBuild;                       // 仅 FullPackage
    bool ForceSkipAotChangeCheck;               // 仅 CodePatch
    IProgress<string> Log;                        // 日志回调
    CancellationToken Cancellation;
}

// 结果
class HotUpdateBuildResult
{
    bool Success;
    string ErrorMessage;
    IReadOnlyList<string> Logs;
    string OutputPlayerPath;          // FullPackage
    string ServerDataPath;
    string ManifestPath;
}
```

### 5.2 Step 清单

| Step | Full | Code | Resource | 说明 |
|------|:----:|:----:|:--------:|------|
| `PreflightCheckStep` | ✓ | ✓ | ✓ | 环境校验 |
| `AotChangeDetectorStep` | — | ✓ | — | 比对 AOT dll hash / GameFramework 变更 |
| `HybridClrBuildStep` | ✓ | ✓ | — | CompileDll + 拷贝到 `Assets/HotUpdate/Code/` |
| `AddressablesSyncStep` | ✓ | ✓ | ✓ | 原 `RuntimeAddressablesConfigurator.SyncRuntimeAssets` |
| `AddressablesFullBuildStep` | ✓ | — | — | `BuildPlayerContent` |
| `AddressablesContentUpdateStep` | — | ✓ | ✓ | `ContentUpdateScript.BuildContentUpdate` |
| `AssetChangeSummaryStep` | — | — | ✓ | 变更摘要 |
| `PlayerBuildStep` | ✓ | — | — | `BuildPipeline.BuildPlayer` |
| `ManifestWriteStep` | ✓ | ✓ | ✓ | 读写 manifest |

### 5.3 content state 自动定位

`AddressablesContentUpdateStep` **禁止**使用 `EditorUtility.OpenFilePanel`。

定位顺序：

1. `Build/HotUpdateManifest.json` → `contentStatePath`（上次 full build 写入）
2. 若不存在 → `Assets/Scripts/Libraries/AddressableAssetsData/*/addressables_content_state.bin` 下最新文件
3. 仍不存在 → 失败，日志提示「请先执行首包构建」

Full build 完成后，`ManifestWriteStep` 将本次 content state 绝对路径写入 manifest。

### 5.4 PreflightCheckStep 规则

| 检查项 | Full | Code | Resource | 失败级别 |
|--------|:----:|:----:|:--------:|----------|
| AddressableAssetSettings 存在 | ✓ | ✓ | ✓ | Error |
| `AddressablesRemoteBaseUrl` Profile 变量 | ✓ | ✓ | ✓ | Warn |
| HybridCLR Settings 可用 | ✓ | ✓ | — | Error |
| HotUpdateDlls 输出目录存在 | ✓ | ✓ | — | Error |
| AOT strip 目录存在 | ✓ | — | — | Error |
| content state 存在 | — | ✓ | ✓ | Error |
| AOT / GameFramework 变更 | — | ✓ | — | Error（可 ForceSkip） |
| `Assets/HotUpdate/Code/Game.Runtime.dll.bytes` 存在 | ✓ | ✓ | — | Warn |

### 5.5 AotChangeDetectorStep

检测信号（任一命中即阻断 CodePatch）：

1. `GameFramework.csproj` 对应源文件 git diff（可选，Editor 下用 `HybridCLRData/AssembliesPostIl2CppStrip` 目录 mtime/hash 与 manifest 记录比对更简单）
2. `Assets/HotUpdate/Code/AOT/*.dll.bytes` 与上次 manifest 中 `aotMetadataHashes` 不一致
3. HybridCLR `SettingsUtil.AOTAssemblyNames` 列表长度变化

manifest full 记录中增加：

```json
"aotMetadataHashes": { "mscorlib.dll.bytes": "sha256:...", ... }
```

---

## 6. HotUpdateManifest 格式

路径：`Build/HotUpdateManifest.json`（项目根下 `Build/`，不进 Version Control 亦可，但建议 `.gitignore` 仅 ignore 产物不 ignore manifest 模板）

```json
{
  "appVersion": "1.0.0",
  "buildTarget": "StandaloneWindows64",
  "lastFullBuildUtc": "2026-07-04T02:00:00Z",
  "contentStatePath": "D:/UnityProject/lockstep/Assets/Scripts/Libraries/AddressableAssetsData/.../addressables_content_state.bin",
  "remoteBaseUrl": "https://cdn.example.com/addressables",
  "aotMetadataHashes": {
    "mscorlib.dll.bytes": "sha256:abc..."
  },
  "gameRuntimeHash": "sha256:def...",
  "patches": [
    {
      "type": "full",
      "version": "1.0.0",
      "generatedAtUtc": "2026-07-04T02:00:00Z",
      "playerOutputPath": "Build/StandaloneWindows64/Roguelike.exe",
      "serverDataPath": "ServerData/StandaloneWindows64"
    },
    {
      "type": "code",
      "version": "1.0.1-code",
      "generatedAtUtc": "2026-07-04T03:00:00Z",
      "gameRuntimeHash": "sha256:...",
      "bundleCount": 1
    },
    {
      "type": "resource",
      "version": "1.0.1-res",
      "generatedAtUtc": "2026-07-04T04:00:00Z",
      "changedGroups": ["Battle-Entity", "Config"],
      "bundleCount": 12
    }
  ]
}
```

patch 版本号规则：

- full：与 `appVersion` 一致
- code：`{appVersion}-code` 或自动递增 `-code.N`
- resource：`{appVersion}-res` 或自动递增 `-res.N`

---

## 7. 待删除 / 收敛清单

### 7.1 删除的 MenuItem

| 原路径 | 处置 |
|--------|------|
| `Tools/HybridCLR/正式入口/同步 DLL 到 Addressables 热更目录` | 删除 MenuItem |
| `Tools/HybridCLR/正式入口/编译并同步 DLL 到 Addressables 热更目录` | 删除 MenuItem |
| `Tools/Addressables/同步运行时资源` | 删除 MenuItem |
| `Tools/Addressables/构建本地内容` | 删除（重复） |
| `Tools/Addressables/构建运行时内容` | 删除 MenuItem |
| `Tools/Addressables/构建远程更新` | 删除 MenuItem |
| `Tools/Addressables/配置本地热更测试环境` | 删除 MenuItem |
| `Tools/Addressables/启动本地热更 HTTP 服务` | 删除 MenuItem |
| `Tools/Addressables/停止本地热更 HTTP 服务` | 删除 MenuItem |
| `Tools/Addressables/禁用构建布局报告` | 删除 MenuItem |

### 7.2 文件重构

| 原文件 | 新归属 | 说明 |
|--------|--------|------|
| `Editor/HybridCLR/HybridCLRDllCopyTool.cs` | `Editor/HotUpdate/Pipeline/Steps/HybridClrBuildStep.cs` | 去掉 MenuItem，保留拷贝/manifest 逻辑 |
| `Editor/Addressables/RuntimeAddressablesConfigurator.cs` | `Editor/HotUpdate/Pipeline/Steps/AddressablesSyncStep.cs` + `AddressablesFullBuildStep.cs` + `AddressablesContentUpdateStep.cs` | 拆分 Step；`AddressablesBuildLayoutGuard` 保留为 internal |
| `Editor/Addressables/AddressablesLocalHotUpdateEnvironment.cs` | `Editor/HotUpdate/Dev/LocalDevEnvironment.cs` | 去掉 MenuItem |
| `Editor/Addressables/AddressablesContentSettingsEditor.cs` | 保留，简化 | 移除操作按钮，仅保留配置字段；状态只读展示可保留或改为「打开发布中心」链接 |

### 7.3 保留不动

| 路径 | 原因 |
|------|------|
| `GameFramework/Bootstrap/HotUpdate/*` | 运行时加载 |
| `AddressablesContentSettings.cs` | 运行时 + Editor 配置 |
| `Editor/Addressables/HotUpdateAddressablesResolverTests.cs` | EditMode 测试 |
| `RunTime/Helper/AddressablesGroupResolver.cs` | Group 解析 |

---

## 8. 新目录结构

```text
Assets/Scripts/Editor/HotUpdate/
├── HotUpdatePublishWindow.cs
├── Pipeline/
│   ├── HotUpdateBuildPipeline.cs
│   ├── HotUpdateBuildContext.cs
│   ├── HotUpdateBuildMode.cs
│   ├── HotUpdateBuildResult.cs
│   ├── HotUpdateManifest.cs
│   └── Steps/
│       ├── PreflightCheckStep.cs
│       ├── AotChangeDetectorStep.cs
│       ├── HybridClrBuildStep.cs
│       ├── AddressablesSyncStep.cs
│       ├── AddressablesFullBuildStep.cs
│       ├── AddressablesContentUpdateStep.cs
│       ├── AssetChangeSummaryStep.cs
│       ├── PlayerBuildStep.cs
│       └── ManifestWriteStep.cs
├── Dev/
│   └── LocalDevEnvironment.cs
└── Internal/
    └── AddressablesBuildLayoutGuard.cs   // 从 RuntimeAddressablesConfigurator 拆出
```

---

## 9. 扩展工具（Phase 2+，本阶段仅预留）

### 9.1 CDN 上传

- 接口：`ICdnUploader.UploadAsync(serverDataPath, remoteBaseUrl, buildTarget)`
- 首版可实现 `LocalFolderUploader`（复制到指定目录）供内网测试
- Window 高级选项预留「上传目标」下拉，Phase 1 禁用

### 9.2 CLI 入口

```text
Assets/Scripts/Editor/HotUpdate/Cli/HotUpdateBuildCli.cs
```

```bash
Unity -batchmode -quit -executeMethod Rogue.Editor.HotUpdate.HotUpdateBuildCli.FullPackage \
  --buildTarget StandaloneWindows64 --version 1.0.0
```

与 Pipeline 共用 `HotUpdateBuildContext`，日志写 stdout。

### 9.3 构建报告导出

- 日志面板「导出」→ 写入 `Build/Logs/hotupdate-{timestamp}.log`
- manifest patch 条目追加 `logPath` 字段

---

## 10. 错误处理

| 错误 | 行为 |
|------|------|
| Preflight Error | 阻断构建，Window 日志区红色高亮 |
| AOT 变更阻断 CodePatch | 阻断，HelpBox 提示改首包 |
| content state 缺失 | 阻断 Code/Resource Patch |
| Addressables 构建失败 | 阻断，输出 `result.Error` |
| Player Build 失败 | 阻断 FullPackage，保留已生成的 ServerData |
| 用户取消（Cancel 按钮） | 通过 `CancellationToken` 停止后续 Step；已写入文件不自动回滚 |
| HybridCLR 目录缺失 | 阻断，提示先执行 HybridCLR Generate |

构建过程中 Window 显示 ProgressBar + 当前 Step 名称；禁止连续双击触发并发构建。

---

## 11. 测试计划

| 用例 | 步骤 | 预期 |
|------|------|------|
| 打开发布中心 | MenuItem | Window 正常打开，三按钮可见 |
| 首包构建（Editor 内 Skip Player） | 工具 1 + Skip Player Build | ServerData 生成，manifest 写入 contentStatePath |
| 代码热更 | 改 Game.Runtime → 工具 2 | 仅 HotUpdate-Code-Remote bundle 更新 |
| AOT 阻断 | 改 AOT 元数据 → 工具 2 | 阻断并提示首包 |
| 资源热更 | 改 Prefab → 工具 3 | 变更摘要正确，不含代码组 |
| content state 自动定位 | 删除 manifest → 工具 3 | 回退到 AddressableAssetsData 最新 bin |
| 本地 OTA 测试 | 开发测试 Tab | HTTP 启停正常，Play 可拉 catalog |
| EditMode 回归 | 现有 Resolver 测试 | 仍通过 |
| 旧 MenuItem 清理 | 搜索 MenuItem | 无 `Tools/Addressables/*` 构建项残留 |

---

## 12. 实施阶段建议

### Phase 1（本次）

1. 创建 Pipeline + Step 骨架与 manifest 读写
2. 迁移 HybridCLR / Addressables 逻辑到 Step
3. 实现 `HotUpdatePublishWindow`（发布 + 开发测试 Tab）
4. 删除旧 MenuItem，简化 ContentSettings Inspector
5. 手动验收测试计划中的用例

### Phase 2

1. 历史记录 Tab
2. CDN 上传接口 + LocalFolderUploader
3. CLI `HotUpdateBuildCli`
4. 构建日志导出

---

## 13. 风险与缓解

| 风险 | 缓解 |
|------|------|
| Player Build 耗时长阻塞 Editor | 异步构建 + Cancel；可选 Skip Player |
| content state 路径因 Unity 版本变化 | manifest 存绝对路径 + 回退搜索最新 bin |
| 并发构建导致 Addressables 状态损坏 | UI 构建锁 + 单例 Pipeline 运行标志 |
| 团队习惯旧 MenuItem | CHANGELOG / 发布中心 HelpBox 说明迁移 |
| AOT 检测误报 | ForceSkip 开关 + manifest hash 精确比对 |

---

## 14. 决策记录

| 日期 | 决策 | 选择 |
|------|------|------|
| 2026-07-04 | 主入口 | 方案 A：统一 EditorWindow |
| 2026-07-04 | content state | manifest 自动定位，禁止手选 |
| 2026-07-04 | 代码/资源 OTA 分离 | 不同 Step 过滤 Addressables Group |
| 2026-07-04 | CDN / CLI | Phase 2，Phase 1 仅预留接口 |

---

<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-07-04
status: approved_option_a
-->
