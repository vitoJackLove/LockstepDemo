# Client Agent Test Entry

This entry lets an Agent enter the Unity client gameplay flow without manually clicking the startup UI.

## Prerequisite

Start the local frame-sync server first:

```powershell
npm run server:single
```

Or from `Server/`:

```powershell
cd Server
npm run start:single
```

The client uses the server settings serialized in `Assets/Scene/Launcher.unity`. The default is `127.0.0.1:8888`.

## Editor Menu

Use this Unity menu item:

```text
Tools/Agent/Run Client Enter Game Test
```

The menu entry opens `Assets/Scene/Launcher.unity`, enters Play Mode, creates `ClientAgentGameEntryRunner`, connects to the server, selects the first valid hero, loads `RougeBattle`, and creates the battle world.

## Agent Success Signal

The entry succeeds when the Unity Console contains:

```text
[AgentClientEntry] Enter game succeeded
```

Failures are logged with:

```text
[AgentClientEntry] Enter game failed
```

The failure log includes the stage and reason, such as missing server connection, timeout waiting for `PlayerConnect`, no hero config, scene load failure, or world creation failure.

## Command Line

The same entry can be started through Unity command line execution:

```powershell
Unity.exe -projectPath H:\UnityProject\Roguelike_Master -executeMethod ClientAgentGameEntryMenu.RunClientEnterGameTestBatch
```

The batch entry waits for the fixed success or failure log. It exits Unity with code `0` on success and code `1` on failure or timeout.

## Notes

- This is a client automation entry, not a server test.
- The server must already be running before invoking the entry.
- The entry is designed for one local client with `--max-players 1`.
- After the success log appears, Play Mode stays active so the Agent can continue testing gameplay internals.
