# Subtask 003: Edit Entity Properties

work_id: cheat-editor-20260623-172031
subtask_id: 003-edit-entity-properties
status: done

## Original Request Summary

Continue improving the Unity Editor-only cheat tool with the ability to modify entity attributes.

## Goal

Complete and harden the editor-only property editing workflow so developers can reliably set and adjust selected entity properties during Play Mode.

## Development Scope

- Work from the existing property row implementation in `CheatEditorWindow`.
- Keep using entity property APIs such as `GetPropertyDebugInfos()`, `TrySetProperty`, `TryChangePropertyValue`, and `TryGetPropertyValue` where appropriate.
- Ensure property rows have clear behavior for:
  - setting current value,
  - applying a delta to current value,
  - setting to min,
  - setting to max.
- If the current UI cannot modify min/max property bounds and the codebase exposes a safe API for doing so, add editor-only controls for that; otherwise document that only current value modification is supported by existing runtime APIs.
- Add validation and messages for stale entity references, missing property keys, non-finite input values, and values clamped by `PropertyData`.
- Refresh the displayed current/min/max values after every successful or failed operation.

## Forbidden Scope

- Do not change core property math or clamping semantics unless a bug is found and explicitly documented for escalation.
- Do not add cheat-only APIs to runtime builds.
- Do not modify entity config assets to simulate property changes.
- Do not change production UI.

## Acceptance Criteria

- In Play Mode, selected entity property rows display current, min, and max values.
- Setting a current value updates the selected entity and refreshes the row display.
- Applying a delta updates the selected entity and reports the actual applied change when clamping occurs.
- Min/max quick actions still work according to the existing property API.
- Invalid/stale entity or missing property cases fail with visible messages and no exception.
- The implementation is editor-only and does not change formal gameplay behavior outside direct editor cheat actions.

## Suggested Verification

- Manual Unity Editor check on local hero and at least one monster entity.
- Modify `Hp`, `Attack`, `Speed`, or another available property where present.
- Test set, delta, min, and max actions.
- Confirm visible game/debug state reflects changed values where applicable.
- Check Unity Console for absence of new errors.
- Compile through Unity script compilation or `dotnet build Roguelike_Master.sln -nologo` if feasible.

## Dependencies

- docs/agent-workflow/tasks/cheat-editor-20260623-172031/001-cheat-editor-baseline.md

## Deliverables

- Development summary listing property edit behavior and affected files.
- Verification notes for set, delta, min, max, and failure cases.
- If min/max bounds cannot be directly edited with existing APIs, document that limitation clearly.

## Status Transition Log

- 2026-06-23 17:20:31: Created as pending.
- 2026-06-24: Marked ready after completion of docs/agent-workflow/tasks/cheat-editor-20260623-172031/002-add-buff-to-entity.md.
- 2026-06-24: Marked done after main agent completion. Development report: docs/agent-workflow/reports/fixes/cheat-editor-20260623-172031/003-fix-attempt-1.md. Test report: docs/agent-workflow/reports/tests/cheat-editor-20260623-172031/003-test-attempt-2.md. PASSED: true.
