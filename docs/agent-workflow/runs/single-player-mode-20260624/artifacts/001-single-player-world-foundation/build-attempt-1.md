---
work_id: single-player-mode-20260624
subtask_id: 001-single-player-world-foundation
attempt: 1
report_type: build
status: done
---

# Build Attempt 1

## Changes

- Added world-scoped session mode to `CreateWorldData` and `BaseWorld`.
- Passed `GameSessionMode.Current` into `WorldSystem.CreateWorldChannel`.
- Changed `RogueWorld` single-player initialization to create only LocalEntity hero and monster.
- Changed single-player command frames to use `ActorLocalEntity`.
- Made `BaseEntity.IsActor` safe when no authority actor exists.

## Build Status

Done.

## Notes

An initial build failed because a new script file was not included in the generated Unity `.csproj`. The mode types were moved into existing `CreateWorldData.cs`, and the build then passed.
