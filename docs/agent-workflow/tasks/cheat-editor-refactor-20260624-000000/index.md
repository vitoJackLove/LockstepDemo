# Cheat Editor Refactor Task Index

work_id: cheat-editor-refactor-20260624-000000
status: ready
original_request: 重构 Unity 编辑器金手指功能：给实体增加 Buff 和修改属性功能要同时作用到预测实体和本地实体，避免触发预测错误；实体筛选界面简化为用户只选中预测的本地实体；金手指界面按筛选实体、增加 Buff、修改属性、添加子弹独立页签显示。该功能仅限 Unity 编辑器，不进入正式包。

## Design Context

- Existing editor entry: `Assets/Scripts/Editor/CheatEditor/CheatEditorWindow.cs`
- Existing menu: `Tool/金手指工具`
- Existing completed baseline work: `docs/agent-workflow/tasks/cheat-editor-20260623-172031/`
- Relevant existing capabilities: entity listing/selection, Buff add, property editing, bullet creation, editor-only smoke helpers and operation messages.
- Runtime context to preserve: local prediction and authority/entity mapping can diverge; direct editor cheats on only one side may produce prediction mismatches.
- Useful runtime references for development investigation: `EntitySystem.ActorLocalEntity`, `EntitySystem.GetAuthorityHeroEntity`, `EntitySystem.HasLocalMappedEntity`, `EntitySystem.GetExecutingEntitiesForDebug`, `BaseEntity.GetPropertyDebugInfos()`, `TrySetProperty`, `TryChangePropertyValue`, `BuffComponent.CreateBuff(int buffConfigId)`.
- Scope boundary: editor tooling only. Do not add formal gameplay behavior, server/protocol changes, generated code edits, config edits, prefab/scene/resource edits, or player-build cheat surfaces.

## Subtasks

| Subtask | Status | Path | Summary |
| --- | --- | --- | --- |
| 001 | done | docs/agent-workflow/tasks/cheat-editor-refactor-20260624-000000/001-select-local-predicted-entity.md | Simplify entity selection so users select the predicted local entity and stale/filter-heavy flows are removed or hidden. |
| 002 | done | docs/agent-workflow/tasks/cheat-editor-refactor-20260624-000000/002-apply-cheats-to-predicted-and-local-entities.md | Refactor Buff and property cheat actions to apply consistently to the selected predicted entity and its mapped local/authority counterpart where required to avoid prediction errors. |
| 003 | ready | docs/agent-workflow/tasks/cheat-editor-refactor-20260624-000000/003-tabbed-cheat-editor-ui.md | Reorganize the cheat editor UI into independent tabs for entity selection, Buff add, property edit, and bullet creation. |
| 004 | pending | docs/agent-workflow/tasks/cheat-editor-refactor-20260624-000000/004-editor-only-guardrails-and-regression-verification.md | Verify editor-only isolation and regression behavior across selection, Buff, property, and bullet workflows. |

## Status Transition Log

- 2026-06-24: Created task index. Marked subtask 001 as ready and all later subtasks as pending.
- 2026-06-24: Subtask 001 completed and passed. Development report: docs/agent-workflow/reports/fixes/cheat-editor-refactor-20260624-000000/001-fix-attempt-2.md. Test report: docs/agent-workflow/reports/tests/cheat-editor-refactor-20260624-000000/001-test-attempt-3.md. Marked subtask 002 as ready.
- 2026-06-24: Subtask 002 completed and passed. Development report: docs/agent-workflow/reports/development/cheat-editor-refactor-20260624-000000/002-development.md. Test report: docs/agent-workflow/reports/tests/cheat-editor-refactor-20260624-000000/002-test-attempt-1.md. Marked subtask 003 as ready.
