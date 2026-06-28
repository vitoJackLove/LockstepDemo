# Subtask 004: Editor Only Guardrails And Regression Verification

work_id: cheat-editor-refactor-20260624-000000
subtask_id: 004-editor-only-guardrails-and-regression-verification
status: pending

## Original Request Summary

该金手指功能是 Unity 编辑器功能，不能进入正式包；重构后需要确认 Buff、属性、实体选择和子弹功能没有回归。

## Goal

对本次金手指重构做最终隔离检查和回归验证，确保所有能力仍为编辑器专用，并覆盖预测本地实体选择、Buff/属性双侧应用、页签 UI 和子弹创建。

## Development Scope

- 检查所有新增或修改代码位于编辑器目录、编辑器程序集或 `UNITY_EDITOR` 保护下。
- 确认没有修改正式包场景、Prefab、资源配置、服务器、协议或生成代码。
- 回归验证四个页签工作流：
  - 选择预测本地实体，
  - 增加 Buff，
  - 修改属性，
  - 添加子弹。
- 回归验证非 Play Mode、无世界、无选中实体、实体失效、非法输入等失败路径。
- 检查现有 agent smoke/日志辅助是否仍能用于自动化或手动验证；如需要轻微维护，仅限编辑器测试/辅助代码。
- 汇总任何无法自动验证的手动步骤和残余风险。

## Forbidden Scope

- 不新增新功能。
- 不扩大正式运行时代码改动。
- 不修改 gameplay 规则来让验证通过。
- 不修改资源、Prefab、场景、服务器或协议。

## Acceptance Criteria

- 编辑器金手指入口仍为 `Tool/金手指工具`，功能不进入 player build。
- 四个页签均可打开，基础状态显示正常。
- 预测本地实体选择流程可用且不依赖复杂筛选。
- Buff 添加和属性修改作用范围符合子任务 002 的双侧一致性要求。
- 子弹创建能力未因 UI 重组回归。
- 非 Play Mode 和失败输入路径无未捕获异常。
- Unity Console 无新增相关错误。
- 验证报告明确列出已执行命令/手动步骤、结果和未覆盖项。

## Suggested Verification

- Unity Editor Play Mode 手动验证 `Tool/金手指工具` 四个页签。
- 手动添加 Buff、修改属性、创建子弹，并观察操作消息和 Console。
- 停止 Play Mode 后重复打开/切换页签，确认失败提示正常。
- 可行时执行 Unity 脚本编译或 `dotnet build Roguelike_Master.sln -nologo`。
- 检查 git diff，确认没有不该出现的资源、场景、Prefab、服务器或生成代码改动。

## Dependencies

- docs/agent-workflow/tasks/cheat-editor-refactor-20260624-000000/001-select-local-predicted-entity.md
- docs/agent-workflow/tasks/cheat-editor-refactor-20260624-000000/002-apply-cheats-to-predicted-and-local-entities.md
- docs/agent-workflow/tasks/cheat-editor-refactor-20260624-000000/003-tabbed-cheat-editor-ui.md

## Deliverables

- 最终验证报告路径，建议位于 `docs/agent-workflow/reports/tests/cheat-editor-refactor-20260624-000000/`。
- 若发现问题，提供失败复现和建议回退到对应子任务修复。
- 确认 editor-only 隔离和禁止范围未被破坏。

## Status Transition Log

- 2026-06-24: Created as pending.
