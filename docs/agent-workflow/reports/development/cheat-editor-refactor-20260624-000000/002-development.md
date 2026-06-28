# Development Report: Apply Cheats To Predicted And Local Entities

## 基本信息
- work_id: cheat-editor-refactor-20260624-000000
- subtask_id: 002-apply-cheats-to-predicted-and-local-entities
- 子任务文档: docs/agent-workflow/tasks/cheat-editor-refactor-20260624-000000/002-apply-cheats-to-predicted-and-local-entities.md
- 使用工具/技能: codegraph, unity-developer

## CodeGraph 阶段
查询过的关键符号、文件和调用关系：
- `Cheat editor selected local predicted entity add buff property edit BuffComponent CreateBuff TrySetProperty TryChangePropertyValue TryGetPropertyValue GetPropertyDebugInfos`
- `Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`
- `Assets/Scripts/RunTime/ECS/Entity/BaseEntity.Property.cs`
- `Assets/Scripts/RunTime/ECS/Component/BuffComponent.cs`
- `Assets/Scripts/RunTime/BattleEntityData/PropertyData.cs`
- `Assets/Scripts/RunTime/BattleEntityData/BattleEntityData.cs`
- `EntitySystem ActorLocalEntity ActorAuthorityEntity GetStableEntityIdentity GetDynamicLocalEntity GetDynamicAuthorityEntity EntityUpdateType AuthorityEntity LocalEntity stable entity identity`
- `EntitySystem.RegisterStaticEntityMap()` / `_entityMap`
- `EntitySystem.GetDynamicLocalEntity<T>()` / `GetDynamicAuthorityEntity<T>()`

结论：Buff 添加入口已经集中在 `CheatEditorWindow.ApplyBuffToSelectedEntity()` 调用 `BuffComponent.CreateBuff(int)`；属性编辑入口集中在 `EntityPropertyCheatRow` 内调用 `TrySetProperty`、`TryChangePropertyValue`、`TryGetPropertyValue`。运行时 Buff 和属性 API 已能表达本任务需要的 set/delta/min/max 语义，本次无需修改核心运行时计算逻辑。

## unity-developer 阶段检查
检查过的关键代码、资源或配置路径：
- `Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`
- `Assets/Scripts/RunTime/ECS/System/EntitySystem.cs`
- `Assets/Scripts/RunTime/ECS/System/EntitySystem.DynamicEntity.cs`
- `Assets/Scripts/RunTime/ECS/Component/BuffComponent.cs`
- `Assets/Scripts/RunTime/ECS/Entity/BaseEntity.Property.cs`
- `Assets/Scripts/RunTime/BattleEntityData/PropertyData.cs`
- Unity Console: 读取到 1 条现存 Error：`Assets/Scripts/RunTime/Singleton/Game.cs:133 System.NullReferenceException`，本次未改该运行时代码

## 开发结论
已在编辑器金手指窗口层实现 Buff 和属性操作的同步目标解析与双侧应用：
- 新增 `CheatEntityOperationTargets`，表示一次金手指操作实际写入的实体集合和摘要。
- 当前选中主角本地预测实体时，同步解析并写入 `EntitySystem.ActorAuthorityEntity`。
- 当前选中主角权威实体时，同步解析并写入 `EntitySystem.ActorLocalEntity`。
- 当前选中动态实体时，使用实体指纹解析本地动态实体和权威动态实体，无法解析任一侧时阻止操作并显示明确原因。
- 无法确认配对关系的调试静态实体保持单实体降级操作，避免猜测运行时映射规则。
- Buff 添加仍逐目标调用现有 `BuffComponent.CreateBuff(int buffConfigId)`，并预先校验所有目标均存活且拥有 `BuffComponent`。
- 属性 set、delta、set min、set max、设置最小边界、设置最大边界均先预读所有同步目标快照，再按同一 property key、value type 和请求值/增量逐目标调用现有属性 API。
- 属性烟测读取并恢复所有同步目标快照，避免自动化验证只恢复单侧。
- 操作结果消息包含同步目标摘要、影响实体数量和每侧实体的实际值/变化量。

## 修改的代码脚本路径
- `Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`

## 中文注释覆盖情况
本次新增或修改后形成的类、方法、属性均已补充中文 XML 注释，覆盖内容包括：
- `CheatEntityOperationTargets` 及其 `Entities`、`PrimaryEntity`、`Summary`、`CreateSingle()`、`CreatePaired()`、`BuildSummary()`
- `TryResolveCheatEntityOperationTargets()`
- `TryResolveDynamicCheatTargets()`
- `TryValidateOperationTarget()`
- `BuildEntityOperationLabel()`
- `TryReadTargetSnapshots()`
- `TryRestoreTargetSnapshots()`
- `GetSnapshotValue()`
- `BuildMutationSummary()`
- `EntityPropertyMutationResult`
- `EntityPropertyTargetSnapshot`

## 修改的资源或配置路径
- 无

## 功能影响范围
- Unity 编辑器菜单 `Tool/金手指工具` 中的 Buff 添加和属性修改操作。
- 默认本地预测实体选择流程继续沿用子任务 001 的简化选择结果。
- 不修改 Buff 运行时规则、属性夹取规则、回滚系统、预测系统、服务器协议、Prefab、场景或配置资源。

## 可能受影响的系统
- 编辑器金手指 Buff 操作
- 编辑器金手指属性修改操作
- Agent Buff / Property / Combined 烟测路径
- 本地预测实体与权威实体状态一致性验证

## 自检命令和结果
- CodeGraph: 已用于理解 `CheatEditorWindow`、`BaseEntity.Property`、`BuffComponent`、`PropertyData`、`EntitySystem` 相关关系。
- Unity MCP `validate_script` on `Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`: 0 errors，1 warning（字符串拼接 GC 提示，非本次新增编译错误）。
- `dotnet build Assembly-CSharp-Editor.csproj -nologo -v:minimal`: 成功，0 errors，3 warnings。
  - `Assets/Scripts/Editor/RollBack/RollBackDebugWindow.cs(62,9)` Odin 过时 API 警告。
  - `Assets/Scripts/Editor/SkillEditor/Editor/Window/SkillTimelineEditorWindow.DrawGui.cs(297,9)` Obsolete 警告。
  - `Assets/Scripts/Editor/SkillEditor/Editor/Window/ClipBlackBoardWindow.cs(28,45)` 字段未赋值警告。
- Unity Console error/warning 读取：1 条 Error，`Assets/Scripts/RunTime/Singleton/Game.cs:133 System.NullReferenceException`，本次未触碰该文件，需测试环境确认是否为进入 Play Mode 残留运行时状态。

## 未覆盖风险
- 未在真实 Play Mode 中手动执行 Buff/属性按钮验证双侧状态；当前完成的是编译和静态路径验证。
- 静态非主角实体没有公开通用本地/权威映射读取 API，本次只对主角静态实体和动态指纹实体做强配对；其他调试静态实体保持单侧降级并在消息中展示影响数量。
- 若某一侧写入过程中运行时实体在预读后立即失效，仍可能出现部分目标已写入后失败；窗口会报告失败侧，真实帧同步环境下建议在暂停或稳定帧进行验证。
- 当前仓库存在大量其他未提交/未跟踪改动，`Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs` 所在目录在 git 中显示为未跟踪状态；本次未回滚或处理其他 Agent/用户改动。

## 交给测试工程师的测试建议
- 在 Play Mode 打开 `Tool/金手指工具`，选中默认本地预测实体，对有效 Buff config id 执行添加，确认本地预测实体和关联权威实体均出现一致 Buff 状态或叠层。
- 对同一属性执行设置当前值、增减、设置为最小值、设置为最大值、设置最小边界、设置最大边界，确认操作消息显示 2 个实体且两侧属性快照一致刷新。
- 使用非法 Buff config id、缺少 BuffComponent 的实体、缺失属性键、NaN/Infinity 输入验证失败提示，无未捕获异常。
- 开启调试选择并选择动态实体，验证能通过指纹同时解析本地动态实体和权威动态实体；缺少任一侧时应阻止操作并提示具体缺失侧。
- 观察 Unity Console，确认没有新增预测校验错误、空引用或未捕获异常；同时确认现存 `Game.cs:133` 错误是否与当前测试流程有关。
