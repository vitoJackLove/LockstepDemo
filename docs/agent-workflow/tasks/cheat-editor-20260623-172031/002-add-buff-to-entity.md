# Subtask 002: Add Buff To Entity

work_id: cheat-editor-20260623-172031
subtask_id: 002-add-buff-to-entity
status: done

## Original Request Summary

Continue improving the Unity Editor-only cheat tool with the ability to add Buffs to entities.

## Goal

Add an editor-only cheat action that applies a Buff configuration to the currently selected runtime entity.

## Development Scope

- Extend the existing cheat editor window under editor-only code.
- Add input/control for a Buff config id.
- Apply the Buff to the selected entity by reusing existing runtime behavior, primarily `BuffComponent.CreateBuff(int buffConfigId)` when the selected entity has or can validly access the component.
- Validate these cases with clear operation messages:
  - not in Play Mode,
  - no current world,
  - no selected entity,
  - selected entity no longer exists or is dead,
  - invalid or missing Buff config id,
  - selected entity cannot receive Buffs because required component/system is unavailable.
- Refresh selected entity state after the operation.

## Forbidden Scope

- Do not implement new Buff gameplay rules.
- Do not edit Buff asset contents or generated data.
- Do not change server/protocol behavior.
- Do not make the cheat action available in player builds.
- Do not silently create Buffs through a divergent path if an existing component method should own Buff creation.

## Acceptance Criteria

- In Play Mode, selecting a valid entity and entering a valid Buff config id applies the Buff.
- Reapplying the same Buff uses existing layer/increase behavior instead of creating duplicate inconsistent state.
- Invalid Buff config ids fail gracefully with a visible operation message and no exception.
- Missing selected entity or non-Play Mode usage fails gracefully with a visible operation message and no exception.
- The implementation remains under editor-only compilation/folder boundaries.
- Existing entity selection and property editing behavior is not regressed.

## Suggested Verification

- Manual Unity Editor check with `Tool/金手指工具` during Play Mode.
- Apply a known valid Buff config id to the local hero or first monster and observe expected effect/state where visible.
- Apply an invalid Buff config id and confirm failure message.
- Check Unity Console for absence of new errors.
- Compile through Unity script compilation or `dotnet build Roguelike_Master.sln -nologo` if feasible.

## Dependencies

- docs/agent-workflow/tasks/cheat-editor-20260623-172031/001-cheat-editor-baseline.md

## Deliverables

- Development summary listing affected editor-only files.
- Manual verification or test notes covering success and failure cases.
- Confirmation that formal build/runtime/server/protocol surfaces were not changed.

## Status Transition Log

- 2026-06-23 17:20:31: Created as pending.
- 2026-06-23: Marked ready after completion of docs/agent-workflow/tasks/cheat-editor-20260623-172031/001-cheat-editor-baseline.md.
- 2026-06-24: Marked done after main agent completion. Development report: docs/agent-workflow/reports/fixes/cheat-editor-20260623-172031/002-fix-attempt-1.md. Test report: docs/agent-workflow/reports/tests/cheat-editor-20260623-172031/002-test-attempt-2.md. PASSED: true.
