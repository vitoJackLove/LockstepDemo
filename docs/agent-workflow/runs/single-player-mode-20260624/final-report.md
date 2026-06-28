---
work_id: single-player-mode-20260624
report_type: final
phase: done
---

# Final Report

## Summary

Single-player mode now uses a local-only RogueWorld path for startup entities and fixed-frame execution. The self hero and initial monster are LocalEntity only in single-player; no AuthorityEntity mirrors or static local-to-authority maps are created.

## Completed Changes

- Added session mode to `CreateWorldData` and `BaseWorld`.
- Made `ClientGameEntryFlow` accept an explicit `GameSessionModeType`, defaulting non-UI callers to Online.
- Passed the startup toggle's selected mode explicitly from `GameStartUpViewModel`.
- Updated `RogueWorld` single-player hero and monster creation to skip AuthorityEntity creation and static mapping.
- Updated single-player command frames to target `ActorLocalEntity`.
- Made actor identity checks work without `ActorAuthorityEntity`.
- Blocked rollback replay and externally triggered rollback in single-player worlds.

## Verification

- `dotnet build Roguelike_Master.sln -nologo`: passed with 0 errors and existing warnings.
- Targeted source scans confirmed single-player branches exit before authority entity creation and rollback/server paths.
- Unity Console read returned 0 entries.
- Independent second-round review approved the final state.
