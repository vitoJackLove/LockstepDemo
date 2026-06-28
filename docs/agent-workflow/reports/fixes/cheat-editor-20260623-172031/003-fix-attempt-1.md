# Subtask 003 Fix Attempt 1: Edit Entity Properties

work_id: cheat-editor-20260623-172031
subtask_id: 003-edit-entity-properties
attempt: 1
test_failure_doc: docs/agent-workflow/reports/tests/cheat-editor-20260623-172031/003-test-attempt-1.md

## 已使用工具 / 技能

- codegraph
- unity-developer
- Unity MCP: validate_script、refresh_unity、read_console、run_tests、execute_menu_item、manage_editor
- 本地命令: dotnet build Assembly-CSharp-Editor.csproj -nologo -v:minimal

## CodeGraph 阶段

查询过的关键符号、文件或调用关系：

- CheatEditorWindow
- EntityPropertyCheatRow
- EntityPropertySnapshot
- RunAgentBuffSmoke
- TryRunBuffSmokeForAgent
- RunAgentPropertySmoke
- TryRunPropertySmokeForAgent
- TryRunPropertySmokeForTarget
- TryRunPropertyFailureSmoke
- BaseEntity.TrySetProperty
- BaseEntity.TryChangePropertyValue
- BaseEntity.TryGetPropertyValue
- PropertyData.TryChangePropertyValue
- Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs
- Assets/Scripts/RunTime/ECS/Entity/BaseEntity.Property.cs
- Assets/Scripts/RunTime/BattleEntityData/PropertyData.cs

## unity-developer 阶段检查

检查过的关键代码、资源或配置路径：

- Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs
- Assets/Scripts/Editor/Agent/CLIENT_AGENT_TEST_ENTRY.md
- docs/agent-workflow/tasks/cheat-editor-20260623-172031/003-edit-entity-properties.md
- docs/agent-workflow/reports/development/cheat-editor-20260623-172031/003-development.md
- docs/agent-workflow/reports/tests/cheat-editor-20260623-172031/003-test-attempt-1.md
- Unity Console

## 问题原因

测试失败不是属性编辑代码编译错误，而是缺少可稳定执行的 Play Mode 自动验收入口。测试报告要求覆盖本地主角和怪物实体的设置、增减、最小值、最大值和边界编辑，但上一轮只完成静态/编译验证，无法确认真实 Play Mode 路径。

## 修复方案

- 在 `CheatEditorWindow` 增加 `Tools/Agent/Run Cheat Property Smoke` 菜单。
- 新增属性烟测等待流程，沿用 Buff 烟测的 Play Mode 世界等待模式，并全部输出到 `GameLogChannel.AgentTest`。
- 新增属性烟测主体：
  - 对本地主角和首个怪物分别选择实体。
  - 通过 `EntityPropertyCheatRow` 公开按钮方法执行 `设置`、`增减`、`最小值`、`最大值`、`设最小`、`设最大`。
  - 每一步刷新后重新查找属性行，避免继续使用过期行对象。
  - 操作后尝试恢复原始 current/min/max 快照，降低自动化验证对后续调试的影响。
  - 覆盖 NaN 输入、缺失属性、空实体引用失败提示。
- 将属性行快照读取方法开放为 editor-only 的窄测试入口 `TryReadSnapshot(...)`，不修改运行时属性 API。
- 修复过程中发现一次本地文本替换导致 `CheatEditorWindow.cs` 中文编码损坏，已重建该 editor-only 脚本并通过编译恢复。

## 修改的代码脚本路径

- Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs

## 中文注释覆盖情况

本次新增或重建后的类、方法、属性均有中文 XML 注释，覆盖：

- AgentPropertySmokeLogPrefix
- AgentPropertySmokeTimeoutSeconds
- _agentPropertySmokeDeadlineTime
- _isAgentPropertySmokeWaiting
- RunAgentPropertySmoke
- StartPropertySmokeWaitForAgent
- TickPropertySmokeWaitForAgent
- StopPropertySmokeWaitForAgent
- TryRunPropertySmokeForAgent
- TryRunPropertySmokeForTarget
- TryRunPropertyFailureSmoke
- TryGetFirstEditablePropertyRow
- TryRefreshAndFindPropertyRow
- TryRequireOperationMessage
- TryRestorePropertySnapshot
- EntityPropertyCheatRow.TryReadSnapshot
- EntityPropertyCheatRow.EntityPropertySnapshot

## 修改的资源或配置路径

- 无。

## 功能影响范围

- 仅影响 Unity Editor 下 `Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`。
- 新增 Agent 自动化菜单不会进入正式玩家 UI。
- 未修改运行时属性数学、PropertyData 夹取语义、配置资产、场景、Prefab、服务器和协议。

## 自检命令和结果

- Unity MCP validate_script Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs
  - 结果：0 error，1 warning。
  - warning：`String concatenation in Update() can cause garbage collection issues`，为工具性能提示。
- dotnet build Assembly-CSharp-Editor.csproj -nologo -v:minimal
  - 结果：Build succeeded，0 error，3 warning。
  - warning 来自既有编辑器文件：RollBackDebugWindow、SkillTimelineEditorWindow.DrawGui、ClipBlackBoardWindow。
- Unity MCP refresh_unity(scope=scripts, compile=request, wait_for_ready=true)
  - 结果：成功，editor ready；期间 MCP 连接恢复一次。
- Unity MCP read_console(errors/warnings)
  - 结果：0 log entries。
- Unity MCP run_tests(mode=EditMode)
  - 结果：21 total，21 passed，0 failed，0 skipped。
- 真实 Play Mode 尝试：
  - 已启动 `dotnet run --project Server/RogueGameServer/RogueGameServer.csproj -- --port 8888 --max-players 1`。
  - 已触发 `Tools/Agent/Run Client Enter Game Test`。
  - Console 出现 `[AgentClientEntry] Enter game succeeded`。
  - 触发 `Tools/Agent/Run Cheat Property Smoke` 后，CheatEditor 快照仍显示 `Play Mode 中暂无运行世界`，属性烟测没有拿到 `WorldSystem.CurrentRunWorld`，因此真实属性按钮烟测仍需测试工程师继续确认。
  - 已退出 Play Mode 并停止本地服务端。

## 仍需测试工程师重点验证的点

- 在 Unity Play Mode 中触发 `Tools/Agent/Run Cheat Property Smoke`，确认出现 `[CheatEditorPropertySmoke] success`。
- 如果仍出现 `Play Mode 中暂无运行世界`，需要先确认 `WorldSystem.CurrentRunWorld` 与 Agent 入场创建世界的生命周期是否一致。
- 手工打开 `Tool/金手指工具`，选择本地主角和首个怪物，执行属性 `设置`、`增减`、`最小值`、`最大值`、`设最小`、`设最大`。
- 验证 NaN / Infinity、缺失属性、实体失效提示可见且 Console 无异常。
