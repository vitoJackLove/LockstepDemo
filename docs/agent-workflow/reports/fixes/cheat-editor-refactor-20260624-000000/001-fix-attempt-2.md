# Fix Report: Select Local Predicted Entity

## 基本信息
- work_id: cheat-editor-refactor-20260624-000000
- subtask_id: 001-select-local-predicted-entity
- attempt: 2
- 对应测试失败文档: docs/agent-workflow/reports/tests/cheat-editor-refactor-20260624-000000/001-test-attempt-2.md
- 关联开发文档: docs/agent-workflow/reports/development/cheat-editor-refactor-20260624-000000/001-development.md
- 关联子任务文档: docs/agent-workflow/tasks/cheat-editor-refactor-20260624-000000/001-select-local-predicted-entity.md
- 关联历史修复文档: docs/agent-workflow/reports/fixes/cheat-editor-refactor-20260624-000000/001-fix-attempt-1.md
- 使用工具/技能: codegraph, unity-developer

## CodeGraph 阶段
查询过的关键符号、文件和调用关系：
- `WorldSystem CurrentRunWorld CreateWorldChannel`
- `ClientAgentGameEntryRunner RunAsync WaitForGameStartOrLocalReady WaitForEditorRunWorldReady`
- `CheatEditorWindow TryGetCurrentWorld TryGetCurrentEntitySystem TryRunPropertySmokeForAgent TryFindBuffSmokeTarget TryFindBulletSmokeTarget`
- `TryRunPropertySmokeForAgent -> TryGetCurrentEntitySystem -> TryGetCurrentWorld`
- `WaitForEditorRunWorldReady` 仅由 `ClientAgentGameEntryRunner.RunAsync()` 调用，影响 `Tools/Agent/Run Client Enter Game Test` 自动化入口。
- `TryFindBuffSmokeTarget` / `TryFindBulletSmokeTarget` 仅由金手指 Agent 烟测路径调用，适合做面向本地预测实体的最小修复。

结论：第 2 次失败的直接原因在于 Agent 入口成功日志早于编辑器侧稳定读取运行世界；同时 `WorldSystem.CurrentRunWorld` 在编辑器读取时只返回缓存字段，缓存丢失时不会从仍存活的世界频道恢复，导致组合烟测在 Play Mode 中得到“暂无运行世界”。

## unity-developer 阶段检查
检查过的关键代码、资源或配置路径：
- `Assets/Scripts/RunTime/GameSystem/WorldSystem.cs`
- `Assets/Scripts/RunTime/Agent/ClientAgentGameEntryRunner.cs`
- `Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`
- `Assets/Scripts/Editor/Agent/CLIENT_AGENT_TEST_ENTRY.md` 所述本地 8888 帧同步服务与 Agent 菜单入口流程
- Unity Console `GameLogChannel.AgentTest` 日志
- Unity 菜单 `Tools/Agent/Run Client Enter Game Test`
- Unity 菜单 `Tools/Agent/Run Cheat Combined Smoke`

未修改 Unity 场景、Prefab、ScriptableObject、资源配置或服务器代码。

## 问题原因
测试第 2 轮中，`Tools/Agent/Run Client Enter Game Test` 已能输出 `[AgentClientEntry] Enter game succeeded`，但金手指组合烟测随后通过 `CheatEditorWindow.TryGetCurrentWorld()` 读取 `WorldSystem.Instance.CurrentRunWorld` 时得到空引用，最终报错：

`[CheatEditorCombinedSmoke] failed: 等待 Play Mode 运行世界超时`

定位后确认有两个问题：
- `WorldSystem.CurrentRunWorld` 编辑器只读入口过度依赖 `_currentRunWorld` 缓存；当缓存为空但 `worldChannels` 中仍有存活 `BaseWorld` 时，编辑器工具无法恢复运行世界引用。
- `ClientAgentGameEntryRunner.RunAsync()` 在创建世界后立即输出成功，没有显式等待 `WorldSystem.CurrentRunWorld` 对编辑器工具可见，因此外部自动化看到成功日志后立刻运行金手指烟测时存在时序风险。

本地端到端验证上述修复后，又暴露组合烟测仍会尝试 `FirstMonster` 属性目标；当前任务已将默认选择锁定为本地预测实体，调试实体选择默认关闭，因此该烟测目标与新默认工作流不一致。

## 修复方案
- 在 `WorldSystem.CurrentRunWorld` 中加入编辑器调试可读的恢复逻辑：当 `_currentRunWorld` 为空时，从 `worldChannels` 中查找仍存活的世界并回填缓存。
- 在 `ClientAgentGameEntryRunner.RunAsync()` 创建世界成功后、输出 `Enter game succeeded` 前新增 `WaitForEditorRunWorldReady()`，确保编辑器侧能读到当前运行世界；成功时输出 AgentTest 日志，失败时在 `EditorWorldReady` 阶段报错。
- 调整金手指 Agent 组合烟测目标：
  - 属性烟测只验证 `ActorLocalEntity`，避免默认流程被怪物调试目标拦截。
  - Buff 和 Bullet 烟测优先使用存活的 `ActorLocalEntity`，只有本地主角不可用时才回退扫描其他实体。
  - 新增 `IsAgentSmokeTargetUsable(BaseEntity entity)` 统一判断烟测目标是否存活可用。

## 修改的代码脚本路径
- `Assets/Scripts/RunTime/GameSystem/WorldSystem.cs`
- `Assets/Scripts/RunTime/Agent/ClientAgentGameEntryRunner.cs`
- `Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`

## 中文注释覆盖情况
本次新增或修改后形成的属性、方法均已补充中文 XML 注释，说明用途、关键参数和返回值：
- `WorldSystem.CurrentRunWorld`
- `ClientAgentGameEntryRunner.WaitForEditorRunWorldReady()`
- `CheatEditorWindow.IsAgentSmokeTargetUsable(BaseEntity entity)`

本次调整的既有 Agent 烟测方法周边原有中文注释仍保留，未新增无注释类或属性。

## 修改的资源或配置路径
- 无

## 功能影响范围
- 影响 Unity 编辑器和 Agent 自动化读取当前运行世界的稳定性。
- 影响 `Tools/Agent/Run Client Enter Game Test` 的成功日志时机：现在成功日志会等待编辑器侧运行世界可见后输出。
- 影响 `Tools/Agent/Run Cheat Combined Smoke` 的目标选择：默认围绕本地预测实体执行属性、Buff、子弹烟测。
- 不影响正式服务器协议、正式运行时实体映射规则、场景资源或 Prefab。

## 自检命令和结果
- Unity MCP `validate_script` on `Assets/Scripts/RunTime/GameSystem/WorldSystem.cs`: 0 errors，1 warning（Unity MCP 静态提示：`String concatenation in Update() can cause garbage collection issues`，非本次编译错误）。
- Unity MCP `validate_script` on `Assets/Scripts/RunTime/Agent/ClientAgentGameEntryRunner.cs`: 0 errors，0 warnings。
- Unity MCP `validate_script` on `Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`: 0 errors，1 warning（Unity MCP 静态提示：`String concatenation in Update() can cause garbage collection issues`，非本次编译错误）。
- `dotnet build Assembly-CSharp.csproj -nologo -v:minimal`: 成功，0 warnings，0 errors。
- `dotnet build Assembly-CSharp-Editor.csproj -nologo -v:minimal`: 成功，0 warnings，0 errors。
- Unity Console error/warning 读取：0 条。
- 启动本地服务：`dotnet run --project Server/RogueGameServer/RogueGameServer.csproj -- --port 8888 --max-players 1`，服务成功监听 `0.0.0.0:8888`。
- Unity 菜单 `Tools/Agent/Run Client Enter Game Test`: 通过，AgentTest 日志包含：
  - `[AgentClientEntry] Runtime world ready for editor tools. scene=RougeBattle, localTick=0, authorityTick=0`
  - `[AgentClientEntry] Enter game succeeded`
- Unity 菜单 `Tools/Agent/Run Cheat Combined Smoke`: 通过，AgentTest 日志包含：
  - `[CheatEditorPropertySmoke] target=ActorLocal entity=HeroEntity #1 key=Hp success: set/delta/min/max/bounds passed`
  - `[CheatEditorBuffSmoke] success: target=HeroEntity #1, buff=4001, count 0->1->1, layer 1->2`
  - `[CheatEditorBulletSmoke] success: parent=HeroEntity #1, config=3001, count 0->1->2->3`
  - `[CheatEditorCombinedSmoke] ... success: CheatEditorPropertySmoke=success, CheatEditorBuffSmoke=success, CheatEditorBulletSmoke=success`
- Unity MCP `manage_editor stop`: 已退出 Play Mode。
- 本轮启动的本地 8888 服务会话已停止。

## 仍需测试工程师重点验证的点
- 重新执行测试第 2 轮复现步骤，确认 `Enter game succeeded` 后组合烟测不再出现“等待 Play Mode 运行世界超时”。
- 确认 `Tool/金手指工具` 在真实 Play Mode 中默认显示并选中本地预测实体。
- 确认本地预测实体摘要包含 EntityId、Fingerprints、ConfigId、EntityType、UpdateType、State 和关联权威实体状态。
- 确认 Buff、属性、子弹功能在默认调试选择关闭时均复用本地预测实体上下文。
- 停止 Play Mode 后刷新窗口，确认不会保留旧运行世界或旧实体引用。

