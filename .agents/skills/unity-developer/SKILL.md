---
name: unity-developer
description: Build, debug, refactor, and optimize Unity projects with C# scripts, scenes, prefabs, assets, render pipelines, UI, gameplay systems, editor tooling, builds, and performance workflows. Use when Codex is asked to fix Unity Console errors, modify Unity C# code, create or edit Unity gameplay/editor features, inspect scenes or prefabs, profile or optimize Unity performance, handle URP/HDRP/rendering/audio/Addressables issues, or verify Unity project changes with Unity Editor tools or local builds.
---

# Unity Developer

## Core Workflow

1. Inspect the Unity project before editing. Read nearby scripts, asmdefs, prefabs, scenes, ScriptableObjects, package versions, and current Console/build errors that affect the task.
2. Prefer existing project patterns over generic Unity advice. Match the repository's architecture, naming, serialization style, editor tooling, ECS/framework usage, asset layout, and build conventions.
3. Make scoped changes. Avoid broad refactors, package upgrades, render pipeline changes, asset metadata churn, or scene/prefab rewrites unless they are required for the request.
4. Verify with the most direct available path. Use Unity MCP tools for Console, scenes, prefabs, components, packages, tests, screenshots, and Editor state when available. Use local `dotnet build` for script compilation when appropriate.
5. Report the exact files changed, validation run, and any remaining Unity Console/build warnings that are outside the requested scope.

## Console And Compile Errors

- Read Unity Console first when the user mentions errors or asks to fix a bug.
- Map each compiler error to the smallest type/API mismatch or access issue before changing behavior.
- When fixing C# visibility, expose only the narrow method/property that represents a real public contract. Do not route through events, reflection, or temporary adapters unless the project already uses that pattern or the user explicitly asks.
- After editing, refresh Unity scripts when possible and read Console again. If Unity refresh is slow, run the relevant `.csproj` build and state what was verified.

## C# Implementation

- Keep Unity serialization rules in mind: public fields and `[SerializeField]` fields affect assets; renames can break serialized data unless migration is handled.
- Preserve ScriptableObject and prefab data compatibility. Avoid changing field types, names, or collection shapes without checking existing assets and migration requirements.
- For gameplay systems, check execution order, frame/update loop, object lifetime, pooling, and deterministic/network implications before changing behavior.
- For editor scripts, use `UnityEditor` APIs only inside editor assemblies or preprocessor guards.
- Add comments only for non-obvious Unity lifecycle, serialization, threading, or performance constraints.

## Assets, Scenes, And Prefabs

- Use structured Unity tools for scene, prefab, component, material, animation, UI, package, and asset operations when available.
- Before modifying prefabs or scenes, inspect the hierarchy and relevant components. Keep changes targeted to the requested object or asset.
- Do not hand-edit YAML scene or prefab files unless structured Unity tooling is unavailable and the change is small, understood, and validated.
- Avoid changing `.meta` files unless creating, moving, or deleting Unity assets requires it.

## Performance And Rendering

- Profile or inspect data before optimizing when the bottleneck is unclear.
- For CPU issues, check allocations, Update/FixedUpdate frequency, LINQ/reflection use, physics queries, jobs, and object pooling.
- For GPU/rendering issues, check render pipeline, materials/shaders, lights/shadows, post-processing, overdraw, batching, LOD, occlusion, and texture formats.
- For mobile/WebGL/console work, consider platform memory, shader variants, texture compression, input, screen ratio, and build settings.

## Testing And Verification

- Prefer the narrowest meaningful verification: relevant Unity tests, `dotnet build <project>.csproj`, Unity Console refresh, scene/prefab inspection, or a targeted play-mode/manual check.
- If tests or Unity refresh cannot be run, explain why and provide the best completed verification.
- Treat existing unrelated warnings as context, not as work to fix unless the user asks.
