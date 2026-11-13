# __NAME__

Space Engineers 2 prototype plugin. TODO: one line on what the player gets.

## Before you run it

This is a prototype plugin, not part of the game. A plugin is code that runs inside Space
Engineers 2 with the same access to your PC as any program you start, and it is not signed or
checked by anyone. Only run it if you trust the person who gave you this zip, and back up any
save you care about first.

## Try it

1. Unzip anywhere.
2. Double-click `launch.cmd`. It finds Space Engineers 2 through Steam and starts it with the plugin.
   If it can't find Steam, run `.\find-game.ps1 -SteamDir "<folder containing steam.exe>"` once in PowerShell.
3. Load a world. TODO: what to do in game to see it.

Prefer starting from Steam? Add this to the game's Launch Options instead:
`-plugins:"C:\full\path\to\__NAME__.dll"`

## What it does

TODO: short bullets, player-facing.

## Keys

TODO: one line per key (default key - what it does - its id).

To rebind, create `keys.json` next to `__NAME__.dll`, e.g. `{"toggle": "Keyboard::F6"}`.

## Known limits

- Prototype: single player only unless stated otherwise.
- Built against Space Engineers 2 __GAMEVERSION__ (Steam). A game update can break it.

## Logs

- Plugin: `%APPDATA%\SpaceEngineers2\__SHORT__\`
- Game: `%APPDATA%\SpaceEngineers2\Temp\Logs\` (search for "Plugin NOT loaded" if nothing happens)

## Third-party

Uses [Harmony](https://github.com/pardeike/Harmony) (`0Harmony.dll`), MIT licence - see
`0Harmony-LICENSE.txt`.

## Build from source

`run-game.ps1` builds, deploys a copy to `bin\Deploy` and launches the game.
`package.ps1` makes the shareable zip in `dist\`.
