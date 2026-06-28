---
work_id: single-player-mode-20260624
subtask_id: 001-single-player-world-foundation
attempt: 1
report_type: review
approved: false
---

# Review Attempt 1

## Result

Rejected.

## Findings

- `ClientGameEntryFlow` still read `GameSessionMode.Current` directly instead of receiving the desired session mode as an explicit parameter.
- Rollback handling still ran before the single-player fixed-frame branch, and `StartRollBack` could be triggered externally in a single-player world, eventually dereferencing `ActorAuthorityEntity`.
