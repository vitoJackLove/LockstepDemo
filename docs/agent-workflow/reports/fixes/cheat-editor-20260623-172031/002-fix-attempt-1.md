# Fix Report: Add Buff To Entity

## 基本信息

- work_id: `cheat-editor-20260623-172031`
- subtask_id: `002-add-buff-to-entity`
- attempt: `1`
- 对应测试失败文档: `docs/agent-workflow/reports/tests/cheat-editor-20260623-172031/002-test-attempt-1.md`
- 关联任务文档: `docs/agent-workflow/tasks/cheat-editor-20260623-172031/002-add-buff-to-entity.md`
- 关联开发文档: `docs/agent-workflow/reports/development/cheat-editor-20260623-172031/002-development.md`
- 已使用工具/技能: `codegraph`, `unity-developer`

## CodeGraph 阶段

- 查询 `CheatEditorWindow ApplyBuffToSelectedEntity TryGetSelectedEntityReadyForBuff TryGetBuffConfig BuffComponent CreateBuff ClientAgentGameEntryMenu RunClientEnterGameTestBatch AgentClientEntry Enter game succeeded`。
- 确认 `ApplyBuffToSelectedEntity()` 仍通过 `TryGetSelectedEntityReadyForBuff(...)`、`TryGetBuffConfig(...)` 后调用 `BuffComponent.CreateBuff(BuffConfigId)`。
- 确认 `BuffComponent.CreateBuff(int)` 的重复 Buff 路径会调用 `BuffEntity.IncreaseLayer()`。
- 读取 `BuffEntity.cs`，确认可通过公开属性 `Layer`、`ParentEntity`、`ConfigId` 观察 Buff 创建与叠层结果。
- 读取 `EntitySystem.cs`，确认可通过 `GetExecutingEntitiesForDebug()` 遍历当前执行实体和动态 BuffEntity。

## Unity Developer 阶段检查

- `Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`
- `Assets/Scripts/Editor/Agent/CLIENT_AGENT_TEST_ENTRY.md`
- `Assets/Scripts/Editor/Agent/ClientAgentGameEntryMenu.cs`
- `Assets/Scripts/RunTime/ECS/Component/BuffComponent.cs`
- `Assets/Scripts/RunTime/ECS/Entity/BuffEntity/BuffEntity.cs`
- `Assets/Scripts/RunTime/ECS/System/EntitySystem.cs`

## 问题原因

测试失败不是编译错误或静态代码缺陷，而是关键 Play Mode 验收路径没有可稳定执行的自动化入口。原实现只能通过人工窗口点击验证“有效 Buff 添加”和“重复添加走叠层”，测试工程师无法在本轮确认验收通过。

## 修复方案

在 Editor-only 金手指窗口中新增 Agent 专用烟测菜单 `Tools/Agent/Run Cheat Buff Smoke`，该入口复用窗口真实操作方法 `ApplyBuffToSelectedEntity()`，不绕过金手指按钮逻辑。烟测会等待 Play Mode 运行世界创建，自动查找带 `BuffComponent` 的存活实体和有效 Buff 配置，执行有效添加、重复添加叠层检查、无效 Buff ID 失败提示检查，并输出固定前缀 `[CheatEditorBuffSmoke]` 的 success/failed 日志。

## 修改的代码脚本路径

- `Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`

## 具体修改

- 新增 `RunAgentBuffSmoke()` 菜单入口。
- 新增 `StartBuffSmokeWaitForAgent(...)`、`TickBuffSmokeWaitForAgent()`、`StopBuffSmokeWaitForAgent()`，支持在运行世界尚未 ready 时提前触发菜单并等待。
- 新增 `TryRunBuffSmokeForAgent(...)`，复用 `ApplyBuffToSelectedEntity()` 验证有效添加、重复叠层和无效 ID 失败提示。
- 新增 `TryFindBuffSmokeTarget(...)`、`TryFindValidBuffConfigId(...)`、`FindBuffEntity(...)`、`CountBuffEntities(...)`，用于自动查找目标实体、配置和验证 BuffEntity 状态。

## 中文注释覆盖情况

本次新增的常量、静态字段、菜单方法、等待流程方法、烟测方法和辅助查询方法均已添加中文 XML 注释，包含用途、关键参数和返回值含义。未新增类，未修改运行时类。

## 修改的资源或配置路径

- 无。未修改 Buff 资源、场景、Prefab、协议、服务器行为或生成数据。

## 功能影响范围

- 仅影响 Unity Editor 下 `Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`。
- 原用户入口 `Tool/金手指工具` 和 `添加Buff` 按钮逻辑保持不变。
- 新增 `Tools/Agent/Run Cheat Buff Smoke` 仅用于 Agent/测试自动化，不进入玩家构建。

## 自检命令和结果

- `dotnet build Assembly-CSharp-Editor.csproj -nologo -v:minimal`
  - 结果: 通过，0 error，3 个既有 warning。
- Unity MCP `refresh_unity(mode=if_dirty, scope=scripts, compile=request, wait_for_ready=true)`
  - 结果: 成功，Unity 断线重连后 editor ready。
- `dotnet build Roguelike_Master.sln -nologo`
  - 结果: 通过，0 error，3 个既有 warning。
- Unity MCP `execute_menu_item("Tools/Agent/Run Cheat Buff Smoke")`
  - 结果: 菜单可执行；在无运行世界时输出 `[CheatEditorBuffSmoke] waiting for runtime world...` 和 pending 日志。
- Unity MCP `execute_menu_item("Tools/Agent/Run Client Enter Game Test")`
  - 结果: 本轮真实运行验证未进入世界。第一次流程停在加载阶段；第二次因服务端房间已被前一次客户端占用，服务端输出 `Reject client: server is full`，Unity Console 输出 `[AgentClientEntry] Enter game failed. stage=PlayerConnect, reason=Timed out after 30 seconds.`，随后 Buff 烟测等待世界超时。

## 仍需测试工程师重点验证的点

- 使用干净的 Unity Play Mode 和新启动的本地服务端执行:
  - `Tools/Agent/Run Client Enter Game Test`
  - `Tools/Agent/Run Cheat Buff Smoke`
- 期望 Console 出现 `[AgentClientEntry] Enter game succeeded` 后，再出现 `[CheatEditorBuffSmoke] success: ...`。
- 若手动验证，打开 `Tool/金手指工具`，选择带 `BuffComponent` 的存活实体，输入有效 Buff ID，点击两次 `添加Buff`，确认只保留一致 BuffEntity 且 `Layer` 增加。
- 输入 `0`、负数或不存在 Buff ID，确认窗口操作结果显示“添加Buff失败”且无异常。
