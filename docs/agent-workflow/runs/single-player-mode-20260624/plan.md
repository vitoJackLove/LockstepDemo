---
work_id: single-player-mode-20260624
report_type: plan
phase: plan_ready
---

# Plan

## Subtask 001: Single Player World Foundation

Scope:

- Add a world-scoped session mode to world creation data and BaseWorld.
- Pass GameSessionMode.Current into world creation from ClientGameEntryFlow/WorldSystem.
- Update RogueWorld entity creation to branch by session mode.
- Update BaseWorld single-player tick to target ActorLocalEntity.
- Update EntitySystem/BaseEntity actor checks so a single-player actor can exist without authority.
- Verify with dotnet build and targeted source checks.

Non-goals:

- Do not change protobuf protocol.
- Do not change server behavior.
- Do not refactor the whole ECS update model.
- Do not dynamically create or modify the UI toggle at runtime.

Risk:

- Some editor tooling assumes ActorAuthorityEntity exists. Runtime must not depend on that in single-player; editor-only tools can continue to report missing authority entities.
