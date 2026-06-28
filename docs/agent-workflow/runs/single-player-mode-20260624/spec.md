---
work_id: single-player-mode-20260624
report_type: spec
phase: spec_ready
---

# Spec: Single Player World Foundation

## Problem

The current single-player toggle only changes the entry flow and skips part of the online tick loop. RogueWorld still creates AuthorityEntity mirrors for the self hero and monster, and BaseWorld single-player input still targets ActorAuthorityEntity. That means single-player is still modeled as a client predicting against a local authority mirror, which is not a single-player game architecture.

## Required Behavior

- Single-player mode creates one gameplay entity path for the self hero and initial monster: LocalEntity only.
- Single-player mode does not create AuthorityEntity mirrors during RogueWorld initialization.
- Single-player mode does not register local-to-authority static entity maps.
- Single-player mode does not send input to the network/server command path.
- Single-player mode does not run authority update, snapshot verification, or rollback replay.
- Online mode keeps the existing prediction/authority entity graph and frame-sync flow.
- The UI toggle remains prefab-authored and bound through the startup window; no dynamic toggle creation.

## Architecture Boundary

Online mode remains:

1. local input command
2. record and send input
3. local snapshot
4. local prediction update
5. authority command/update
6. forecast verification and rollback if needed

Single-player mode becomes:

1. local input command
2. record local command for debugging/replay continuity
3. local gameplay update only

## Acceptance Criteria

- RogueWorld single-player initialization does not call CreateServerEntity for the self hero.
- RogueWorld single-player initialization does not create any EntityUpdateType.AuthorityEntity for the initial monster.
- BaseWorld single-player command EntityId is ActorLocalEntity.EntityId.
- BaseWorld no longer depends on the UI ViewModel file for session mode decisions.
- BaseEntity.IsActor does not null-reference when no ActorAuthorityEntity exists.
- dotnet build completes successfully or reports only pre-existing unrelated issues.
