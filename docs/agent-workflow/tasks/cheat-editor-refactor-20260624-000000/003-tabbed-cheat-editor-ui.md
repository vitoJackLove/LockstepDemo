# Subtask 003: Tabbed Cheat Editor UI

work_id: cheat-editor-refactor-20260624-000000
subtask_id: 003-tabbed-cheat-editor-ui
status: ready

## Original Request Summary

优化金手指界面，把筛选实体、增加 Buff、修改属性、添加子弹单独有自己的页签显示。

## Goal

将 `CheatEditorWindow` 的界面重组为清晰的页签结构，使实体选择、Buff 添加、属性修改和子弹创建分别独立显示，同时复用同一个运行世界和选中实体状态。

## Development Scope

- 在现有 Odin EditorWindow 基础上重组 UI，优先使用项目已有 Odin 属性/分组风格实现页签。
- 至少提供四个独立页签：
  - 筛选实体/选择实体，
  - 增加 Buff，
  - 修改属性，
  - 添加子弹。
- 各页签内只显示与当前功能直接相关的控件和状态，避免所有控件堆在同一长页面。
- 保留世界状态、刷新开关、操作结果等跨页签公共信息，或放在清晰的公共区域。
- Buff、属性和子弹页签应明确依赖当前选中实体，并在缺少选中实体时显示不可操作原因。
- 保持已有 editor-only smoke/agent 辅助入口可用，除非它们是纯 UI 布局相关并已同步迁移。

## Forbidden Scope

- 不改变子任务 002 已实现的 Buff/属性应用语义。
- 不重写运行时系统、子弹创建规则、Buff 规则或属性规则。
- 不修改场景、Prefab、资源配置、服务器代码或正式包 UI。
- 不把页签 UI 做成运行时功能。

## Acceptance Criteria

- 金手指窗口显示独立页签，实体选择、增加 Buff、修改属性、添加子弹不再混杂在同一连续表单中。
- 切换页签不会丢失当前运行世界状态、选中实体上下文和最近操作消息。
- Buff 页签能完成 Buff config id 输入和应用操作。
- 属性页签能展示并操作当前选中实体属性。
- 子弹页签保留既有 bullet config、位置、旋转、owner/source、运动参数等创建能力。
- 非 Play Mode、无世界、无选中实体时，各页签显示可理解的禁用/失败状态且无异常。

## Suggested Verification

- 在 Unity Editor 中打开 `Tool/金手指工具`，逐个切换四个页签，确认布局清晰且控件归属正确。
- 在 Play Mode 中先选择预测本地实体，再分别执行 Buff、属性和子弹操作，确认页签切换不破坏上下文。
- 在非 Play Mode 下打开窗口并切换页签，确认无异常。
- 检查 Unity Console 无新增错误。
- 可行时执行 Unity 脚本编译或 `dotnet build Roguelike_Master.sln -nologo`。

## Dependencies

- docs/agent-workflow/tasks/cheat-editor-refactor-20260624-000000/001-select-local-predicted-entity.md
- docs/agent-workflow/tasks/cheat-editor-refactor-20260624-000000/002-apply-cheats-to-predicted-and-local-entities.md

## Deliverables

- 开发总结，说明页签结构、公共状态位置和迁移的控件。
- 受影响文件清单。
- 验证记录，覆盖四个页签切换和各功能基本操作。

## Status Transition Log

- 2026-06-24: Created as pending.
- 2026-06-24: Marked ready after completion of docs/agent-workflow/tasks/cheat-editor-refactor-20260624-000000/002-apply-cheats-to-predicted-and-local-entities.md.
