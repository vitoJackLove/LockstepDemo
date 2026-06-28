# Development Report: Select Local Predicted Entity

## 基本信息
- work_id: cheat-editor-refactor-20260624-000000
- subtask_id: 001-select-local-predicted-entity
- 子任务文档: docs/agent-workflow/tasks/cheat-editor-refactor-20260624-000000/001-select-local-predicted-entity.md
- 使用工具/技能: codegraph, unity-developer

## CodeGraph 阶段
查询过的关键符号、文件和关系：
- `CheatEditorWindow entity selection local predicted entity EntitySystem local authority predicted runtime world`
- `Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`
- `Assets/Scripts/RunTime/ECS/System/EntitySystem.cs`
- `EntitySystem.ActorLocalEntity`
- `EntitySystem.ActorAuthorityEntity`
- `EntitySystem.GetExecutingEntitiesForDebug()`
- `EntitySystem.RegisterActor()`
- `EntitySystem.GetStableEntityIdentity()`
- `EntitySystem.GetDynamicLocalEntity()` / `GetDynamicLocalOrCachedEntity()` / `GetDynamicAuthorityEntity()`

结论：金手指窗口只被自身菜单入口使用；当前 Buff、属性、子弹功能都通过 `SelectedEntityView` 和 `TryGetSelectedEntity()` 复用同一个选中实体上下文。`EntitySystem` 已暴露 `ActorLocalEntity` 和 `ActorAuthorityEntity`，本任务无需修改正式运行时映射规则。

## unity-developer 阶段检查
检查过的关键代码、资源或配置路径：
- `Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`
- `Assets/Scripts/RunTime/ECS/System/EntitySystem.cs`
- Unity Console: 当前读取到 0 条 error/warning
- 未修改 Unity 场景、Prefab、ScriptableObject、服务器代码或资源配置

## 开发结论
已将 `CheatEditorWindow` 的默认实体选择流程改为围绕当前运行世界的本地预测实体：
- 窗口刷新时默认自动解析并选中 `EntitySystem.ActorLocalEntity`。
- 顶部新增本地预测实体摘要，展示实体 ID、指纹、配置 ID、更新域、存活状态和关联权威实体摘要。
- Play Mode 未运行、世界不存在、实体系统不存在、本地预测实体不存在或实体失效时会清空危险旧选择，并显示明确原因。
- 原完整实体列表、多条件筛选、权威主角/怪物快捷选择保留为折叠调试入口；默认不会让用户必须通过复杂筛选选择目标。
- 新增 `AllowDebugEntitySelection` 调试开关，默认关闭；关闭时 Buff、属性和子弹仍稳定使用本地预测实体上下文。

## 修改的代码脚本路径
- `Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`

## 中文注释覆盖情况
本次新增或修改后形成的字段、方法和关键行为均已补充中文 XML 注释或中文用途说明，包括：
- `AllowDebugEntitySelection`
- `_localPredictedEntityInfo`
- `_localPredictedEntityState`
- `RefreshSelectedEntityReference(EntitySystem entitySystem)`
- `TrySelectLocalPredictedEntity(...)`
- `RefreshLocalPredictedEntityState(...)`
- `BuildLocalPredictedEntityInfo(...)`
- 调试筛选字段和当前实体上下文字段的用途注释

## 修改的资源或配置路径
- 无

## 功能影响范围
- 影响 Unity 编辑器菜单 `Tool/金手指工具` 中的实体选择区域和刷新逻辑。
- Buff 添加、属性修改、子弹创建继续复用 `SelectedEntityView` / `TryGetSelectedEntity()`，默认目标变为本地预测实体。
- 调试实体列表仍可展开使用，但不再是默认主流程。

## 可能受影响的系统
- Unity 编辑器金手指窗口实体选择 UI
- 金手指 Buff 操作
- 金手指属性修改操作
- 金手指子弹创建父实体上下文
- Agent 菜单烟测中依赖选中实体的流程

## 自检命令和结果
- Unity MCP `validate_script` on `Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`: 0 errors, 1 warning（字符串拼接 GC 提示，非编译错误）
- `dotnet build Assembly-CSharp-Editor.csproj -nologo -v:minimal`: 成功，0 errors，3 warnings
  - `Assets/Scripts/Editor/RollBack/RollBackDebugWindow.cs(62,9)` Odin 过时 API 警告
  - `Assets/Scripts/Editor/SkillEditor/Editor/Window/SkillTimelineEditorWindow.DrawGui.cs(297,9)` Obsolete 警告
  - `Assets/Scripts/Editor/SkillEditor/Editor/Window/ClipBlackBoardWindow.cs(28,45)` 字段未赋值警告
- Unity Console error/warning 读取：0 条

## 未覆盖风险
- 未在真实 Play Mode 中打开窗口进行人工 UI 验证。
- 当前仓库中 `Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs` 处于未跟踪目录状态，无法通过 `git diff` 与基线直接比较；本次仅在该文件内做目标变更。
- 若后续任务希望默认实体不是主角本地预测实体，而是其他“本地预测实体”候选，需要再扩展 `EntitySystem` 的编辑器只读查询入口。

## 交给测试工程师的测试建议
- 在 Unity Editor Play Mode 中打开 `Tool/金手指工具`，确认顶部默认显示并选中本地预测实体。
- 确认本地预测实体摘要包含 EntityId、Fingerprints、ConfigId、EntityType、UpdateType、State 和关联权威实体状态。
- 停止 Play Mode 后刷新窗口，确认无异常且提示 `请先进入 Play Mode`。
- 在世界尚未创建或本地预测实体未注册时打开窗口，确认不会保留旧选中实体。
- 让选中实体失效、切场景或重进 Play Mode 后刷新，确认 Buff/属性/子弹操作不会使用旧引用。
- 展开调试实体筛选，确认默认关闭调试选择时权威/怪物按钮会提示需要开启调试选择；开启后仍能作为辅助选择完整实体列表。
