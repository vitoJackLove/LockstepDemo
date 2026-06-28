# Repository Guidelines

## Project Structure & Module Organization
This is a Unity 2023.2.20f1c1 roguelike project. Runtime gameplay code lives in `Assets/Scripts/RunTime`, with major systems under folders such as `World`, `GameSystem`, `BattleEntityData`, `SkillData`, `FixedPoint`, `Server`, and `Survivor`. Editor-only tooling belongs in `Assets/Scripts/Editor`; experiments and Unity test scripts are in `Assets/Scripts/Test`. Game content is organized under `Assets/Art`, `Assets/Prefabs`, `Assets/Scene` and `Assets/Scenes`, `Assets/Config`, and `Assets/GameAssetConfig`. Shared Unity packages are declared in `Packages/manifest.json`. The standalone .NET server is in `Server/RogueGameServer` and links generated protobuf runtime code from `Assets/Scripts/RunTime/Server/Protobuf/Generated`.

Do not edit generated or local cache folders such as `Library`, `Temp`, `obj`, `Logs`, or IDE cache directories.

## Build, Test, and Development Commands
- `dotnet build Roguelike_Master.sln -nologo`: compile generated C# projects outside Unity for quick syntax checks.
- `npm run server`: start the local frame-sync server (`127.0.0.1:8888`, max 3 players).
- `npm run server:single`: start the server for single-client / Agent tests (`--max-players 1`).
- `npm run server:build`: build the .NET 8 game server.
- `dotnet build Server/RogueGameServer/RogueGameServer.csproj -nologo`: build the game server directly.
- `dotnet run --project Server/RogueGameServer/RogueGameServer.csproj`: run the local game server directly.
- Unity Test Runner: run EditMode or PlayMode tests from the Unity Editor. For batch runs, use the Unity executable with `-batchmode -runTests -testPlatform EditMode`.
- `npm test` is currently a placeholder and should not be used as the project test command.

## Coding Style & Naming Conventions
Use C# conventions already present in the project: four-space indentation, `PascalCase` for types, methods, properties, and enum values, and `_camelCase` for private fields. Keep MonoBehaviour/editor code separated from deterministic runtime logic. Frame-sync and rollback code should prefer fixed-point types from `Unity.Mathematics.FixedPoint` over floating-point math. Preserve existing `.meta` files when moving assets.

## Testing Guidelines
Place Unity tests near `Assets/Scripts/Test` or in a feature-specific test folder when one exists. Name test files and classes after the behavior under test, for example `FrameSyncPathfindingTests.cs`. Cover deterministic systems, protobuf serialization, rollback behavior, and server/client protocol changes before merging.

## Commit & Pull Request Guidelines
Recent history uses both plain summaries and Conventional Commit prefixes such as `feat(survivor): ...`, `fix(survivor): ...`, and `docs: ...`. Prefer `type(scope): concise summary` for new commits. Pull requests should describe gameplay or protocol impact, list verification steps, link related issues, and include screenshots or video for visible Unity changes.

## Security & Configuration Tips
Do not commit secrets, machine-specific settings, or generated build output. Keep server protocol changes synchronized between `Server/RogueGameServer`, `Assets/Proto*`, and generated protobuf files.
