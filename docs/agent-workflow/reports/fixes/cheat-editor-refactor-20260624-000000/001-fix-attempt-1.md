# Fix Report: Select Local Predicted Entity

## 基本信息
- work_id: cheat-editor-refactor-20260624-000000
- subtask_id: 001-select-local-predicted-entity
- attempt: 1
- 对应测试失败文档: docs/agent-workflow/reports/tests/cheat-editor-refactor-20260624-000000/001-test-attempt-1.md
- 关联开发文档: docs/agent-workflow/reports/development/cheat-editor-refactor-20260624-000000/001-development.md
- 关联子任务文档: docs/agent-workflow/tasks/cheat-editor-refactor-20260624-000000/001-select-local-predicted-entity.md
- 使用工具/技能: codegraph, unity-developer

## CodeGraph 阶段
查询过的关键符号、文件和调用关系：
- `ClientAgentGameEntryRunner GameStart Timed out after 30 seconds AgentClientEntry Run Client Enter Game Test FrameSyncClientComponent`
- `ClientGameEntryFlow LoadBattleSceneAndCreateWorldAsync GameStartMessage SelectHeroMessage FrameSyncClientComponent TcpClient Send GameStart server GameStart`
- `Server GameStartMessage SelectHeroMessage PlayerConnectMessage max players RogueGameServer GameStart`
- `Assets/Scripts/RunTime/Agent/ClientAgentGameEntryRunner.cs`
- `Assets/Scripts/RunTime/Agent/ClientGameEntryFlow.cs`
- `Assets/Scripts/RunTime/Agent/ClientAgentGameEntryMode.cs`
- `Assets/Scripts/RunTime/Agent/ClientAgentGameEntryBootstrap.cs`
- `Assets/Scripts/Editor/Agent/ClientAgentGameEntryMenu.cs`
- `Assets/Scripts/RunTime/UI/ViewMode/GameStartUpViewModel.cs`

结论：测试失败点发生在 Agent 真实客户端入口的 `GameStart` 阶段。`ClientAgentGameEntryRunner` 在发送 `GameStartMessage` 后只等待服务器广播 `_hasGameStart`，但单人本地自动化入口中已经完成 `PlayerConnect` 和自身 `SelectHero` 确认时，服务器可能不再回传该事件，导致 30 秒超时，无法进入真实 Play Mode 世界验证金手指窗口。

## unity-developer 阶段检查
检查过的关键代码、资源或配置路径：
- `Assets/Scripts/RunTime/Agent/ClientAgentGameEntryRunner.cs`
- `Assets/Scripts/RunTime/Agent/ClientGameEntryFlow.cs`
- `Assets/Scripts/Editor/Agent/ClientAgentGameEntryMenu.cs`
- `Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`
- Unity Console error/warning
- 本地帧同步服务器端口 8888 状态

## 问题原因
`ClientAgentGameEntryRunner.RunAsync()` 在 `Request load start` 后调用 `SendGameStart()`，随后通过 `WaitUntil(() => _hasGameStart, ..., "GameStart")` 硬等待服务器广播。测试环境中单人本地服务已能分配玩家索引并回传选英雄消息，但没有在 30 秒内回传 `GameStart`，Runner 因此输出：

`[AgentClientEntry] Enter game failed. stage=GameStart, reason=Timed out after 30 seconds.`

这阻塞了后续 `ClientGameEntryFlow.LoadBattleSceneAndCreateWorldAsync()`，导致金手指窗口真实 Play Mode 验证无法进行。

## 修复方案
在 `ClientAgentGameEntryRunner` 中新增 Agent 专用等待方法 `WaitForGameStartOrLocalReady(int heroId)`：
- 优先接受服务器 `GameStart` 广播，保持原有成功路径。
- 如果运行世界已经存在，也视为可继续。
- 如果本机玩家索引已分配、当前英雄选择已被 SelectHero 回包确认，即 `HasSelfPlayer(heroId)` 为 true，则记录 AgentTest Info 日志并继续本地战斗世界创建。
- 若以上条件都不满足，仍按原逻辑在超时后输出失败。

该修复只影响 Agent 自动化入口，不修改正式 UI、服务器协议、运行时实体映射或金手指窗口选择规则。

## 修改的代码脚本路径
- `Assets/Scripts/RunTime/Agent/ClientAgentGameEntryRunner.cs`
- `Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`（保留开发阶段改动，本次未为失败点继续修改）

## 中文注释覆盖情况
本次新增方法已添加中文 XML 注释，说明用途、参数和返回值：
- `WaitForGameStartOrLocalReady(int heroId)`

本次未新增类或属性。

## 修改的资源或配置路径
- 无

## 功能影响范围
- 影响 `Tools/Agent/Run Client Enter Game Test` 自动化进入游戏流程。
- 解除单人本地自动化入口对服务器 `GameStart` 回包的硬依赖，使真实 Play Mode 世界能够创建，便于验证金手指窗口默认选中本地预测实体。
- 不影响正式玩家点击 `GameStartUpViewModel` 的开始游戏流程。

## 自检命令和结果
- Unity MCP `validate_script` on `Assets/Scripts/RunTime/Agent/ClientAgentGameEntryRunner.cs`: 0 errors, 0 warnings。
- `dotnet build Assembly-CSharp.csproj -nologo -v:minimal`: 成功，0 errors，13 warnings。警告来自既有运行时代码，例如 `EntityInfoBase.camera` 隐藏成员、`FindObjectOfType` 过时、未使用字段等。
- Unity MCP `refresh_unity`: 成功，脚本刷新后编辑器 ready。
- Unity Console 清理后执行 Agent 入口，读取 `AgentClientEntry` 日志：出现 `[AgentClientEntry] GameStart fallback: self player is ready...` 和 `[AgentClientEntry] Enter game succeeded`。
- Unity MCP `manage_editor stop`: 已退出 Play Mode。
- Unity Console error/warning 读取：0 条。

补充说明：尝试新启动 `dotnet run --project Server/RogueGameServer/RogueGameServer.csproj -- --port 8888 --max-players 1` 时端口 8888 已被占用，说明已有服务监听；未杀未知进程，直接复用现有监听环境完成 Agent 入口验证。

## 仍需测试工程师重点验证的点
- 重新执行 `Tools/Agent/Run Client Enter Game Test`，确认不再出现 `stage=GameStart` 超时。
- 在 Agent 入口成功进入 Play Mode 后打开 `Tool/金手指工具`，确认默认显示并选中本地预测实体。
- 验证本地预测实体摘要包含 EntityId、Fingerprints、ConfigId、EntityType、UpdateType、State 和关联权威实体状态。
- 验证 Buff、属性、子弹操作仍复用同一个本地预测实体上下文。
- 如果后续多人自动化验证需要严格等待服务器开始广播，应单独增加模式开关，不应复用本单人 Agent fallback。
