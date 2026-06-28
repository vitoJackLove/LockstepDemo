# 004-create-bullet-in-world Fix Attempt 1

work_id: cheat-editor-20260623-172031
subtask_id: 004-create-bullet-in-world
attempt: 1
test_report: docs/agent-workflow/reports/tests/cheat-editor-20260623-172031/004-test-attempt-1.md
related_task_doc: docs/agent-workflow/tasks/cheat-editor-20260623-172031/004-create-bullet-in-world.md
related_development_doc: docs/agent-workflow/reports/development/cheat-editor-20260623-172031/004-development.md

## 使用工具 / 技能

- codegraph：已按项目要求优先使用 CodeGraph 读取 `CheatEditorWindow`、现有 Buff/属性烟测入口、子弹创建按钮和相关运行时创建链路。
- unity-developer：已按 Unity 修复流程检查编辑器脚本、运行时子弹实体、Unity Console 和本地编译结果。

## CodeGraph 阶段查询

- 查询：`CheatEditorWindow RunAgentBuffSmoke RunAgentPropertySmoke TryRunBuffSmokeForAgent TryRunPropertySmokeForAgent CreateBulletInWorld BulletConfigId BulletControlCompinent BulletAssetsConfig`
- 读取/确认：`Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`
- 读取/确认：`Assets/Scripts/RunTime/ECS/Entity/BulletEntity.cs`
- 读取/确认：`Assets/Scripts/RunTime/ECS/Component/BulletControlCompinent.cs`
- 读取/确认：`Assets/Scripts/RunTime/Log/GameLog.cs`
- 读取/确认：`Assets/Scripts/RunTime/Log/GameLogChannel.cs`
- 读取/确认：`Assets/Scripts/RunTime/ECS/Entity/BaseEntity.ComponentData.cs`
- 确认调用关系：现有 Agent 验证模式通过 `Tools/Agent/Run Cheat Buff Smoke` / `Tools/Agent/Run Cheat Property Smoke` 菜单等待 Play Mode 运行世界，再调用窗口公开按钮或属性行方法执行核心验收。

## unity-developer 阶段检查

- 检查脚本：`Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`
- 检查运行时组件数据读取：`Assets/Scripts/RunTime/ECS/Entity/BaseEntity.ComponentData.cs`
- 检查子弹运行时初始化依赖：`Assets/Scripts/RunTime/ECS/Entity/BulletEntity.cs`、`Assets/Scripts/RunTime/ECS/Component/BulletControlCompinent.cs`
- 检查 Unity Console：当前仍有 1 条网络连接断开错误，和本次修复无关。

## 问题原因

测试失败不是编译错误或静态链路错误，而是缺少可由 Agent/测试工程师自动触发的子弹创建烟测入口。已有功能只提供手动窗口按钮 `CreateBulletInWorld()`，测试报告中没有产生 `[CheatEditorBulletCreate]` 日志，说明核心 Play Mode 创建路径未实际运行，导致有效配置、无效配置、重复指纹、运动数据和 Buff/属性回归等验收项没有被执行。

另一个潜在问题是自动化烟测若复用编辑器窗口残留筛选条件，`SelectEntity()` 在刷新后可能无法保留父实体选中项，导致子弹创建上下文误判为未选择实体。

## 修复方案

- 新增 `Tools/Agent/Run Cheat Bullet Smoke` 菜单入口，编号 2004，与 Buff/属性烟测入口保持一致。
- 新增子弹烟测等待流程：`StartBulletSmokeWaitForAgent()`、`TickBulletSmokeWaitForAgent()`、`StopBulletSmokeWaitForAgent()`，等待 Play Mode 运行世界创建。
- 新增 `TryRunBulletSmokeForAgent()`，通过窗口公开按钮 `CreateBulletInWorld()` 自动覆盖：
  - 查找带 `BulletControlCompinent` 的存活父实体。
  - 查找有效 `BulletAssetsConfig`。
  - 创建基础子弹并确认 `BulletEntity` 出现。
  - 重复创建并确认新实体指纹不同。
  - 启用 `MovementData` 创建运动子弹并确认运动数据写入。
  - 使用无效配置 ID 创建并确认仅产生失败提示且子弹数量不变。
- 新增辅助方法：`TryFindBulletSmokeTarget()`、`TryFindValidBulletConfigId()`、`FindLatestBulletEntity()`、`CountBulletEntities()`、`Fp3ToVector3()`、`ResetFiltersForAgentBulletSmoke()`。
- 在子弹烟测开始前重置筛选条件，避免窗口残留过滤器影响自动选中父实体。

## 修改的代码脚本路径

- `Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`

## 中文注释覆盖情况

- 新增常量、状态字段、菜单方法、等待流程方法、烟测方法、筛选重置方法、子弹目标/配置查找方法、子弹计数/查找方法和向量转换方法均已添加中文 XML 注释。
- 本次修改没有新增无注释的类、方法或属性。

## 修改的资源或配置路径

- 无。未修改子弹配置、技能时间线、场景、Prefab、服务器或协议文件。

## 功能影响范围

- 仅扩展 Editor-only 金手指窗口的 Agent 自动化验证入口。
- 手动 `Tool/金手指工具` 子弹创建按钮逻辑保持原有运行时创建链路。
- 新增菜单仅在 Unity Editor 中可用，不会进入玩家构建。
- 新增烟测会在 Play Mode 中真实创建数个 `BulletEntity`，用于测试验证。

## 自检命令和结果

- `dotnet build Assembly-CSharp-Editor.csproj -nologo -v:minimal`
  - 结果：成功，0 error，3 warning。
  - warning 为既有 Editor 警告：`RollBackDebugWindow.cs` Odin 过时属性、`SkillTimelineEditorWindow.DrawGui.cs` 过时方法、`ClipBlackBoardWindow.cs` 未赋值字段。
- `dotnet build Assembly-CSharp.csproj -nologo -v:minimal`
  - 结果：成功，0 warning，0 error。
- Unity MCP Console 读取：成功。
  - 当前仅见网络连接断开错误，判断与本次修复无关。

## 仍需测试工程师重点验证

1. 进入 Play Mode 后执行菜单 `Tools/Agent/Run Cheat Bullet Smoke`。
2. 确认 Console 出现 `[CheatEditorBulletSmoke] success`，并包含 parent、config、count、fingerprints、movementTick。
3. 确认 Console 出现 `[CheatEditorBulletCreate] success`，说明公开按钮路径被真实触发。
4. 确认无效配置分支不新增子弹且无未捕获异常。
5. 确认重复创建的指纹不同。
6. 执行已有 `Tools/Agent/Run Cheat Buff Smoke` 和 `Tools/Agent/Run Cheat Property Smoke`，回归已有金手指能力。
