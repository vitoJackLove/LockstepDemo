# Subtask 001: Select Local Predicted Entity

work_id: cheat-editor-refactor-20260624-000000
subtask_id: 001-select-local-predicted-entity
status: done

## Original Request Summary

重构金手指工具：实体筛选界面太麻烦，用户只用选中预测的本地实体即可。

## Goal

简化 `CheatEditorWindow` 的实体选择工作流，让用户主要选择当前运行世界中的预测本地实体，并为后续 Buff/属性双侧应用提供稳定的选中实体上下文。

## Development Scope

- 修改 Unity 编辑器金手指窗口的实体选择相关 UI 和数据刷新逻辑。
- 优先定位并展示预测本地实体，参考现有运行世界和 `EntitySystem` 暴露的本地/权威实体信息。
- 将当前繁琐筛选项移除、隐藏、折叠或降级为调试辅助，默认工作流不再要求用户按实体类型、更新域、配置 ID、实体 ID 组合筛选。
- 保留足够的实体基础信息展示，能让用户确认当前选中的是本地预测实体以及其映射/关联实体状态。
- 处理 Play Mode 未运行、世界不存在、预测本地实体不存在、实体已失效等状态，并显示明确操作消息。
- 确保后续 Buff、属性和子弹页签能复用同一个“当前选中预测本地实体”上下文。

## Forbidden Scope

- 不修改正式运行时选择/锁敌/实体映射规则。
- 不修改 Unity 场景、Prefab、资源配置或服务器代码。
- 不为了编辑器 UI 增加会进入正式包的 cheat API。
- 不删除已有核心调试能力导致后续任务无法验证实体列表；如需保留完整列表，应作为隐藏/调试辅助而非默认主流程。

## Acceptance Criteria

- 打开 `Tool/金手指工具` 后，实体选择区域默认围绕预测本地实体展示，不再要求用户使用多条件筛选才能选中目标。
- Play Mode 中存在预测本地实体时，用户能一键/默认选中该实体，并看到实体 ID、配置 ID、类型、更新域、存活状态等关键信息。
- Play Mode 未运行或世界/预测本地实体不可用时，界面无异常，并显示明确原因。
- 刷新数据后，选中实体失效时能清理或重新解析选中状态，不保留危险的悬空引用。
- Buff、属性、子弹功能仍可读取当前选中实体上下文。

## Suggested Verification

- 在 Unity Editor Play Mode 中打开 `Tool/金手指工具`，确认默认显示并选中预测本地实体。
- 停止 Play Mode 后刷新窗口，确认无异常且提示不可用原因。
- 在实体死亡、切场景或重开 Play Mode 后刷新，确认选中状态不会指向旧实体。
- 检查 Unity Console 无新增错误。
- 可行时执行 Unity 脚本编译或 `dotnet build Roguelike_Master.sln -nologo` 做语法检查。

## Dependencies

- None.

## Deliverables

- 开发总结，说明实体选择默认流程和任何保留的调试筛选入口。
- 受影响文件清单，预期主要为 `Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`。
- 验证记录，覆盖 Play Mode、非 Play Mode、实体不可用和刷新失效场景。

## Status Transition Log

- 2026-06-24: Created as ready.
- 2026-06-24: Marked done after main agent completion. Development report: docs/agent-workflow/reports/fixes/cheat-editor-refactor-20260624-000000/001-fix-attempt-2.md. Test report: docs/agent-workflow/reports/tests/cheat-editor-refactor-20260624-000000/001-test-attempt-3.md. PASSED: true.
