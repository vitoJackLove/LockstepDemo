# Subtask 004: Create Bullet In World

work_id: cheat-editor-20260623-172031
subtask_id: 004-create-bullet-in-world
status: done

## Original Request Summary

Continue improving the Unity Editor-only cheat tool with the ability to create bullets and similar runtime entities in the world.

## Goal

Add editor-only controls that create a bullet entity in the currently running world using existing runtime entity creation patterns.

## Development Scope

- Extend the cheat editor window or editor-only helper classes.
- Provide inputs for at least:
  - bullet config id,
  - spawn position,
  - spawn rotation or direction,
  - optional parent/owner source using the currently selected entity.
- Reuse existing data and entity creation patterns from bullet skill clips:
  - `CreateBulletClip`,
  - `CreateFollowBulletClip`,
  - `CreateMovementBulletClip`,
  - `BulletAssetsConfig`,
  - `EntityCreateData.Create(...)`,
  - `EntitySystem.CreateDynamicEntity<BulletEntity>(...)`.
- Match the selected entity's update domain when using it as owner/context.
- Generate deterministic-enough debug fingerprints using the same style as existing bullet creation code, avoiding collisions for repeated editor actions in the same session.
- Report clear operation messages for unavailable world, invalid bullet config, missing selected owner when required, and failed creation.

## Forbidden Scope

- Do not create new bullet gameplay rules.
- Do not edit bullet asset configs.
- Do not modify skill timeline assets.
- Do not change server/protocol code.
- Do not expose bullet creation cheat in player builds.
- Do not bypass required runtime initialization for `BulletEntity`.

## Acceptance Criteria

- In Play Mode, entering a valid bullet config id and spawn parameters creates a bullet entity in the running world.
- When a selected entity is used as owner/parent/context, the bullet is created in the correct update domain and initialized consistently with existing bullet creation clips.
- Invalid config id or missing required context fails with a visible message and no exception.
- Repeated creation attempts do not obviously reuse the same live entity fingerprint.
- Existing Buff and property cheat actions continue to work after this addition.
- All changes remain editor-only.

## Suggested Verification

- Manual Unity Editor check using `Tool/金手指工具` in Play Mode.
- Create one bullet near the local hero or selected monster with a known valid bullet config id.
- Attempt creation with an invalid config id.
- Use the entity list refresh/filtering to confirm the new bullet entity appears if bullet entities are exposed by debug entity listing.
- Check Unity Console for absence of new errors.
- Compile through Unity script compilation or `dotnet build Roguelike_Master.sln -nologo` if feasible.

## Dependencies

- docs/agent-workflow/tasks/cheat-editor-20260623-172031/001-cheat-editor-baseline.md
- docs/agent-workflow/tasks/cheat-editor-20260623-172031/002-add-buff-to-entity.md
- docs/agent-workflow/tasks/cheat-editor-20260623-172031/003-edit-entity-properties.md

## Deliverables

- Development summary listing bullet creation path and affected files.
- Verification notes for valid config, invalid config, and repeated creation.
- Confirmation that no bullet asset configs, skill assets, runtime gameplay rules, server code, or protocol files were modified.

## Status Transition Log

- 2026-06-23 17:20:31: Created as pending.
- 2026-06-24: Marked ready after completion of docs/agent-workflow/tasks/cheat-editor-20260623-172031/003-edit-entity-properties.md.
- 2026-06-24: Marked done after main agent completion. Development report: docs/agent-workflow/reports/fixes/cheat-editor-20260623-172031/004-fix-attempt-1.md. Test report: docs/agent-workflow/reports/tests/cheat-editor-20260623-172031/004-test-attempt-2.md. PASSED: true.
