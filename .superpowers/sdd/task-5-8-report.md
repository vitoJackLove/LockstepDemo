# Task 5–8 Report: Hot Update Build Pipeline Steps & Orchestrator

**Date:** 2026-07-04  
**Scope:** Tasks 5, 6, 7, 8 from `docs/superpowers/plans/2026-07-04-hotupdate-publish-tools.md`

## Summary

Implemented Addressables build steps, preflight/AOT detection, player/manifest/summary steps, and `HotUpdateBuildPipeline` orchestrator with `_isRunning` concurrency lock.

## Files Created

| File | Task |
|------|------|
| `Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/ContentUpdateScope.cs` | 5 |
| `Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/AddressablesFullBuildStep.cs` | 5 |
| `Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/AddressablesContentUpdateStep.cs` | 5 |
| `Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/PreflightCheckStep.cs` | 6 |
| `Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/AotChangeDetectorStep.cs` | 6 |
| `Assets/Scripts/Test/EditMode/AotChangeDetectorStepTests.cs` | 6 |
| `Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/PlayerBuildStep.cs` | 7 |
| `Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/ManifestWriteStep.cs` | 7 |
| `Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/AssetChangeSummaryStep.cs` | 7 |
| `Assets/Scripts/Editor/HotUpdate/Pipeline/HotUpdateBuildPipeline.cs` | 8 |

## Verification

| Check | Result |
|-------|--------|
| `dotnet build Game.Editor.csproj` | ✅ Build succeeded (0 errors) |
| `dotnet build Game.Editor.Tests.csproj` | ✅ Build succeeded (0 errors) |
| Unity EditMode tests | ⚠️ Not run in this session (compile-only) |

## Pipeline Entry Points

- `HotUpdateBuildPipeline.RunFullPackage(context)` — Preflight → HybridCLR → Full Addressables → Player (optional) → Manifest
- `HotUpdateBuildPipeline.RunCodePatch(context)` — Preflight → AOT check → HybridCLR → Content Update (CodeOnly) → Manifest
- `HotUpdateBuildPipeline.RunResourcePatch(context)` — Preflight → Asset summary → Sync → Content Update (ResourceOnly) → Manifest

## Concerns / Follow-ups

1. **ContentUpdateScope filtering** — Phase 1 relies on Addressables state diff; strict group isolation (CodeOnly vs ResourceOnly) is documented but not enforced at entry level.
2. **AssetChangeSummaryStep** — Logs pre-sync group entry counts; post-sync diff deferred to `AddressablesSyncStep` GameLog output.
3. **ResourcePatch double sync** — `AssetChangeSummaryStep` snapshots before sync; pipeline then calls `AddressablesSyncStep` (matches plan order).
4. **Unity EditMode** — `AotChangeDetectorStepTests.HasAotChanges_WhenNoManifest_ReturnsFalse` should be run in Unity Test Runner for runtime confirmation.
