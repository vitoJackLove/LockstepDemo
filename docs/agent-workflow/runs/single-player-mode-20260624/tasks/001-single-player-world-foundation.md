---
work_id: single-player-mode-20260624
subtask_id: 001-single-player-world-foundation
phase: task_ready
---

# Task 001: Single Player World Foundation

## Goal

Make single-player mode a real local-only world path for RogueWorld initialization and fixed tick execution.

## Non-goals

- No server protocol changes.
- No generated protobuf changes.
- No dynamic UI creation.
- No unrelated cheat editor or gameplay refactors.

## Acceptance Criteria

- Single-player RogueWorld creates the self hero as LocalEntity and registers it as the actor without an authority entity.
- Single-player RogueWorld creates the initial monster as LocalEntity only.
- Single-player command frames use ActorLocalEntity.EntityId.
- Online RogueWorld keeps self local plus authority mirror, teammate authority entities, monster local plus authority mirror, and ServerCommandSystem initialization.
- BaseEntity.IsActor handles single-player without ActorAuthorityEntity.

## Verification

- Run `dotnet build Roguelike_Master.sln -nologo`.
- Run targeted source checks for single-player branches.
- Read Unity Console if Unity MCP is available.

## Review Focus

- No authority fallback in single-player.
- No accidental removal of online prediction/authority flow.
- World mode is owned by world creation/runtime, not UI implementation details.
