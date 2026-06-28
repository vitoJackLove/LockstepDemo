# Subtask 005 Fix Attempt 1

work_id: cheat-editor-20260623-172031

subtask_id: 005-editor-only-guardrails-and-verification

attempt: 1

对应测试失败文档路径: docs/agent-workflow/reports/tests/cheat-editor-20260623-172031/005-test-attempt-1.md

关联开发文档路径: docs/agent-workflow/reports/development/cheat-editor-20260623-172031/005-development.md

关联子任务文档路径: docs/agent-workflow/tasks/cheat-editor-20260623-172031/005-editor-only-guardrails-and-verification.md

## 已使用工具/技能

- codegraph
- unity-developer
- Unity MCP: `validate_script`、`refresh_unity`、`execute_menu_item`、`read_console`
- 本地命令: `dotnet build Assembly-CSharp.csproj -nologo -v:minimal`、`dotnet build Assembly-CSharp-Editor.csproj -nologo -v:minimal`

## CodeGraph 阶段查询

- `CheatEditorWindow RunAgentBuffSmoke RunAgentPropertySmoke RunAgentBulletSmoke StartBuffSmokeWaitForAgent TryRunBuffSmokeForAgent TryRunPropertySmokeForAgent TryRunBulletSmokeForAgent GameLogChannel AgentTest CheatEditorPropertySmoke CheatEditorBuffSmoke CheatEditorBulletSmoke`
- `Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`
- `Assets/Scripts/RunTime/Log/GameLog.cs`
- `Assets/Scripts/RunTime/Log/GameLogChannel.cs`

CodeGraph 结论:

- 现有 Buff、Property、Bullet smoke 都位于 Editor-only `CheatEditorWindow` 内，并分别输出独立前缀日志。
- 测试失败不是静态隔离或编译错误，而是三项 smoke 分散触发时 Console 读取容易被旧日志、超时和旧 success 混淆。
- `GameLogChannel.AgentTest` 是符合项目规则的 Agent 验证日志频道。

## unity-developer 阶段检查路径

- `Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`
- `Assets/Scripts/RunTime/Log/GameLog.cs`
- `docs/agent-workflow/reports/tests/cheat-editor-20260623-172031/005-test-attempt-1.md`
- `docs/agent-workflow/reports/development/cheat-editor-20260623-172031/005-development.md`
- `docs/agent-workflow/tasks/cheat-editor-20260623-172031/005-editor-only-guardrails-and-verification.md`
- 历史修复/测试文档：
  - `docs/agent-workflow/reports/fixes/cheat-editor-20260623-172031/003-fix-attempt-1.md`
  - `docs/agent-workflow/reports/fixes/cheat-editor-20260623-172031/004-fix-attempt-1.md`
  - `docs/agent-workflow/reports/tests/cheat-editor-20260623-172031/004-test-attempt-2.md`

## 问题原因

测试失败的核心原因是组合 Play Mode 验收缺少一个可唯一识别本轮执行结果的统一入口。原有三个菜单分别输出 `CheatEditorPropertySmoke`、`CheatEditorBuffSmoke`、`CheatEditorBulletSmoke`，测试读取 Console 时遇到分项查询超时，且旧日志窗口中仍有历史 Buff/Bullet success 和旧回滚错误，导致无法稳定确认“本轮”属性、Buff、子弹三项均完成。

另外，当前 Unity Console 中历史回滚错误数量较多，会淹没 AgentTest smoke 日志，进一步增加测试读取不稳定性。

## 修复方案

- 新增 Editor-only 菜单 `Tools/Agent/Run Cheat Combined Smoke`。
- 新增组合烟测前缀 `[CheatEditorCombinedSmoke]`。
- 新增组合 smoke runId，格式为递增序号加编辑器时间戳，用于测试工程师过滤本轮日志。
- 新增组合等待流程：
  - `StartCombinedSmokeWaitForAgent()`
  - `TickCombinedSmokeWaitForAgent()`
  - `StopCombinedSmokeWaitForAgent()`
  - `GenerateAgentCombinedSmokeRunId()`
  - `TryRunCombinedSmokeForAgent(...)`
- 组合 smoke 复用现有三个核心 smoke 方法，按顺序执行：
  - `TryRunPropertySmokeForAgent(true)`
  - `TryRunBuffSmokeForAgent(true)`
  - `TryRunBulletSmokeForAgent(true)`
- 组合 smoke 启动时通过反射清理 Unity Console 旧日志，仅作用于 Agent 组合验收菜单，避免旧回滚错误和旧 success 混淆本轮结果。
- 组合 smoke 输出单条汇总 success 日志，明确包含：
  - `CheatEditorPropertySmoke=success`
  - `CheatEditorBuffSmoke=success`
  - `CheatEditorBulletSmoke=success`
  - `runId`
  - `worldState`
  - `entityCount`
  - `operation`
- 如果没有运行世界或某一步失败，会输出带相同 `runId` 的 pending/failed 日志，便于测试确定失败原因。

## 修改的代码脚本路径

- `Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`

## 中文注释覆盖情况

本次新增或修改后形成的常量、字段、菜单方法和 helper 方法均已添加中文 XML 注释，覆盖：

- `AgentCombinedSmokeLogPrefix`
- `AgentCombinedSmokeTimeoutSeconds`
- `_agentCombinedSmokeDeadlineTime`
- `_isAgentCombinedSmokeWaiting`
- `_agentCombinedSmokeSequence`
- `_agentCombinedSmokeRunId`
- `RunAgentCombinedSmoke()`
- `StartCombinedSmokeWaitForAgent(...)`
- `TickCombinedSmokeWaitForAgent()`
- `StopCombinedSmokeWaitForAgent()`
- `GenerateAgentCombinedSmokeRunId()`
- `ClearConsoleForAgentCombinedSmoke()`
- `TryRunCombinedSmokeForAgent(...)`

## 修改的资源或配置路径

无。

## 功能影响范围

- 仅影响 Unity Editor 下的金手指 Agent 自动化验收入口。
- 不改变手动 `Tool/金手指工具` UI 行为。
- 不新增 gameplay 规则，不修改 Buff/属性/子弹运行时创建逻辑。
- 不修改服务器、协议、生成代码、场景、Prefab 或资产。
- 正式玩家构建不包含该菜单入口，因为脚本位于 `Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`。

## 自检命令和结果

- Unity MCP `validate_script Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`
  - 结果：成功，0 error，1 个既有 GC warning。
- `dotnet build Assembly-CSharp-Editor.csproj -nologo -v:minimal`
  - 结果：成功，0 error，3 个既有 Editor warning。
- `dotnet build Assembly-CSharp.csproj -nologo -v:minimal`
  - 结果：成功，0 warning，0 error。
- Unity MCP `refresh_unity(mode=if_dirty, scope=scripts, compile=request, wait_for_ready=true)`
  - 结果：成功；过程中 MCP 断连后自动恢复，编辑器 ready。
- Unity MCP `execute_menu_item Tools/Agent/Run Cheat Combined Smoke`
  - 结果：菜单触发成功。
- Unity MCP `read_console(filter_text="CheatEditorCombinedSmoke")`
  - 结果：可稳定读到本轮 runId 日志。
  - 当前没有运行世界时读到：
    - `[CheatEditorCombinedSmoke] runId=1-76026281 start: waiting for runtime world...`
    - `[CheatEditorCombinedSmoke] runId=1-76026281 pending: Play Mode 中暂无运行世界`
    - `[CheatEditorCombinedSmoke] runId=1-76026281 failed: 等待 Play Mode 运行世界超时`
- Unity MCP `read_console(action=clear)`
  - 结果：已清理本次“无运行世界”验证产生的预期超时日志，避免干扰下一轮测试。
- `git diff --check -- Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs Assets/Scripts/RunTime/ECS/Entity/BaseEntity.Property.cs Assets/Scripts/RunTime/ECS/System/EntitySystem.cs`
  - 结果：通过，仅输出 Git 行尾提示。

## 仍需测试工程师重点验证

1. 启动本地服务端并进入 Play Mode 运行世界。
2. 执行菜单 `Tools/Agent/Run Cheat Combined Smoke`。
3. 过滤 Console 文本 `CheatEditorCombinedSmoke`，确认出现同一 `runId` 的最终汇总 success：
   - `CheatEditorPropertySmoke=success`
   - `CheatEditorBuffSmoke=success`
   - `CheatEditorBulletSmoke=success`
4. 确认 `GameLogChannel.AgentTest` 没有本轮 `Error`。
5. 如果失败，直接使用同一 `runId` 的 failed 日志定位失败步骤：`property`、`buff`、`bullet` 或等待运行世界超时。
