# 004-create-bullet-in-world Development Report

work_id: cheat-editor-20260623-172031
subtask_id: 004-create-bullet-in-world
subtask_doc: docs/agent-workflow/tasks/cheat-editor-20260623-172031/004-create-bullet-in-world.md

## 使用工具 / 技能

- codegraph：已按项目要求优先使用 CodeGraph 理解相关运行时代码和编辑器代码。
- unity-developer：已按 Unity 项目开发流程检查运行时创建链路、编辑器脚本、Unity Console 和编译结果。

## CodeGraph 阶段查询

- 查询：`Cheat editor window selected entity CreateBulletClip CreateFollowBulletClip CreateMovementBulletClip BulletAssetsConfig EntityCreateData.Create EntitySystem.CreateDynamicEntity BulletEntity`
- 读取：`Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`
- 读取：`Assets/Scripts/RunTime/SkillEditor/RunTime/Clip/BulletClip/CreateBulletClip.cs`
- 读取：`Assets/Scripts/RunTime/SkillEditor/RunTime/Clip/BulletClip/CreateFollowBulletClip.cs`
- 读取：`Assets/Scripts/RunTime/SkillEditor/RunTime/Clip/BulletClip/CreateMovementBulletClip.cs`
- 读取：`Assets/Scripts/RunTime/ECS/Entity/BulletEntity.cs`
- 读取：`Assets/Scripts/RunTime/WolrdData/EntityData/EntityCreateData.cs`
- 读取：`Assets/Scripts/RunTime/ECS/System/EntitySystem.cs`
- 读取：`Assets/Scripts/RunTime/ECS/Component/DisplacementComponent.cs`
- 读取：`Assets/Scripts/RunTime/RollBackData/FingerprintsGenerate.cs`
- 读取：`Assets/Scripts/RunTime/ECS/Component/BuffComponent.cs`
- 确认调用关系：现有子弹 Clip 均通过 `EntityCreateData.Create(...)` 与 `EntitySystem.CreateDynamicEntity<BulletEntity>(...)` 创建动态子弹实体，并沿用父实体的 `EntityUpdateType`。

## unity-developer 阶段检查

- 检查脚本：`Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`
- 检查运行时实体：`Assets/Scripts/RunTime/ECS/Entity/BulletEntity.cs`
- 检查运行时配置类型：`Assets/Scripts/RunTime/EntityAssetsConfig/BulletAssets.cs`
- 检查运行时动态实体创建：`Assets/Scripts/RunTime/ECS/System/EntitySystem.cs`
- 检查运动子弹自定义数据：`Assets/Scripts/RunTime/ECS/Component/DisplacementComponent.cs`
- 检查 Unity Console：当前有 1 条网络断开错误 `[Network][Error] Receive failed...`，与本次编辑器子弹创建改动无关。

## 开发结论

已在 Editor-only 金手指窗口中加入“子弹创建”功能。用户可在 Play Mode 输入子弹配置 ID、生成位置、生成旋转、父实体来源，并可选择启用运动参数。创建流程复用运行时已有的 `BulletAssetsConfig`、`EntityCreateData.Create(...)`、`MovementData.Create(...)` 和 `EntitySystem.CreateDynamicEntity<BulletEntity>(...)`，不会修改子弹玩法规则、子弹配置、技能时间线、服务器或协议代码。

## 修改的代码脚本路径

- `Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`

## 中文注释覆盖情况

- 新增字段均已添加中文 XML 注释，覆盖子弹配置、生成位置、生成旋转、父实体来源、运动参数和调试指纹缓存。
- 新增公开按钮方法 `CreateBulletInWorld()` 已添加中文注释。
- 新增辅助方法 `TryGetBulletCreationContext()`、`TryGetBulletConfig()`、`TryGetBulletParentEntity()`、`CreateBulletCustomData()`、`GenerateCheatBulletFingerprint()`、`TryConvertVector3ToFp3()`、`TryConvertFiniteInput()` 已添加中文注释和参数/返回值说明。
- 新增枚举 `BulletOwnerSource` 及其枚举值已添加中文注释。

## 修改的资源或配置路径

- 无。未修改 bullet asset config、skill timeline asset、server/protocol 文件或 Unity 场景/Prefab。

## 功能影响范围

- 仅影响 Editor-only 脚本 `Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`。
- 新增 UI 分组：`子弹创建`，不影响已有实体筛选、Buff 添加和属性修改入口。
- 子弹创建必须在 Play Mode 且当前世界有 EntitySystem 时执行。
- 子弹父实体来源支持：选中实体、本地主角、权威主角。
- 使用父实体的 `EntityUpdateType` 创建子弹，满足选中实体上下文的更新域一致性。
- 若启用运动，会传入 `MovementData` 给 `BulletEntity` 的 `DisplacementComponent`。

## 可能受影响的系统

- Editor 金手指窗口显示和按钮交互。
- 运行时动态实体系统，仅在编辑器按钮触发时创建 `BulletEntity`。
- 子弹父实体的 `BulletControlCompinent` 子弹登记列表。
- 可选运动数据会影响新建子弹的 `DisplacementComponent` 行为。

## 自检命令和结果

- `dotnet build Assembly-CSharp-Editor.csproj -nologo -v:minimal`
  - 结果：成功，0 error，3 warning。
  - 警告均为既有 Editor 警告：`RollBackDebugWindow.cs` Odin 过时属性、`SkillTimelineEditorWindow.DrawGui.cs` 过时方法、`ClipBlackBoardWindow.cs` 未赋值字段。
- Unity MCP Console 读取：成功读取 1 条日志。
  - 结果：仅发现一条网络连接被远程主机关闭错误，判断与本次改动无关。

## 未覆盖风险

- 未在真实 Play Mode 中手动点击创建有效子弹；本次只完成编译与静态链路验证。
- 具体有效 `BulletConfigId` 依赖运行时数据表内容，需要测试工程师在项目场景中选择已知有效 ID 验证。
- 指纹生成沿用现有 `FingerprintsGenerate.GenerateFingerprint(...)` 风格，并增加编辑器会话序号扰动和已知集合避重；仍建议在同一帧连续点击时观察实体列表指纹是否稳定区分。

## 交给测试工程师的建议

1. 进入 Play Mode，打开 `Tool/金手指工具`。
2. 选择一个带 `BulletControlCompinent` 的实体，建议先用“主角本地”或“主角权威”快捷选择。
3. 输入已知有效 `BulletConfigId`，设置生成位置和旋转，点击“创建子弹”，确认操作消息显示新建子弹 ID、父实体和更新域。
4. 勾选“启用运动”，设置运动帧数和速度，再次创建，观察子弹位移表现或实体列表中的 BulletEntity。
5. 输入无效子弹 ID，确认显示“未找到子弹配置”且 Console 无异常。
6. 在未选择实体且父实体来源为“选中实体”时点击创建，确认显示缺少选中实体提示。
7. 连续多次点击创建，刷新实体列表，确认新建 BulletEntity 不明显复用同一个实时指纹。
8. 回归 Buff 添加和属性修改按钮，确认已有金手指功能仍可使用。
