# Troubleshooting: symptom → cause

Run `scripts/logs.ps1 -Short <Short>` first. Most rows below were each a lost debugging session.

| Symptom | Likely cause | Fix |
|---|---|---|
| No plugin log file at all | Plugin not loaded. Game log says "Plugin NOT loaded", or the `-plugins:` arg was split on a space | Quote the whole arg: `"-plugins:C:\path with spaces\X.dll"`. Check the class is public, non-abstract, implements `IPlugin` |
| Two plugin log files per run | Normal. The game starts a bootstrapper, then relaunches itself with the same args | Read the newest. Never bind ports/pipes/files in the ctor; start them once `VRageCore.HasValue` |
| Black screen at main menu | (a) Avalonia object created in a static initializer or the ctor, (b) `AllocConsole`, (c) Harmony patch installed from the ctor | Build brushes/controls on the UI thread; log to file; patch lazily from the pump |
| "TCP port in use" / requests go nowhere | Both processes bound the port | Start the listener lazily after `VRageCore.HasValue`; set `ExclusiveAddressUse = true` |
| Build error "file is locked by SpaceEngineers2" | Game holds the DLL it loaded | Build to `bin\Debug`, run from the `bin\Deploy` copy (`run-game.ps1` does this). Close the game before redeploying |
| `Feature "X": OFF - <reason>` in the log | A hook or member that feature needs is missing on this game build (the reason names it) | `api-search.ps1 -Type/-Text` for the new name; add it as `"New\|Old"` in `Game\`, or fix the patch's parameter names. Run `compat-check.ps1` to see every prototype at once |
| `OFF - uses T.Member, which changed in this game build` / `MissingFieldException` / `MissingMethodException` | Compiled code touches a game member whose type or signature changed (e.g. field → property, or a value got wrapped). Compiles fine against the new build, fails with the old DLL | Read it with `Bind.ReadAs<T>(obj, "Member")` so one DLL works on both builds, or rebuild for the new build |
| `Feature "X" switched OFF: ...` mid-game | That feature's patch code threw (first exception is in the log in full) | Fix the bug the logged exception points at; the game itself was protected |
| Build error "Game code outside the Game\ adapter" | A non-`Game\` file has a `using Keen.*` (other than Mathematics) | Move the game-facing code into `Game\` (or a bridge there) and pass plain data to the logic |
| `X has N overloads - pass args or pick` | The target name is overloaded | `args: new[] { typeof(bool) }`, `pick: m => ...`, or `allOverloads: true` |
| Patch installs but never fires | Wrong overload, method inlined, or the logic runs on the other scene | Log inside the patch once. Check client vs server. Pick overloads by parameter types |
| "PATCH TARGET MISSING" in log | Game update renamed/moved it | `api-search.ps1 -Type` / `-Text` to find the new name |
| Game freezes for seconds | Unbudgeted work on the engine thread | Stopwatch-slice every engine-thread loop (~8 ms), resume from a cursor |
| Server component reads null/zero on the client | It's `[ServerOnly]` | Read it from `Sessions.Server` |
| Action from server session silently does nothing | Some network stories need the client narrator (e.g. block placement) | Call with the **client** session's entity |
| Value read fine, but writing it has no effect | Value was snapshotted into native/Havok at creation | Apply at creation (prefix the Init), then respawn/reload |
| Character spins / input sticks | Injected `ControlData.Rotation` delta persists | Withdraw the delta next tick |
| Ship sinks at exactly 9.8 m/s² while "hovering" | GravityMultiplier = 2 trap | Hover via `CustomGravity`, not thrust force |
| Image is blank / striped / wrong colours | Custom Avalonia bitmap bugs | Packed `Width*4` stride, R,G,B,A order, fill before assigning Source, new bitmap per update |
| Timing constants off by a ratio | Client tick is not 60 Hz | Measure real dt |
| `async void` compile warning CS1685 | Game2.Client ships its own `AsyncVoidMethodBuilder` | Use `async Task` |
| Things appear for a while after load | Grids stream blocks in for ~30 s | Baseline after a settle window |
| Plugin stopped loading after a Steam update ("Plugin NOT loaded") | Compiled against a different build's DLLs | Rebuild (`run-game.ps1`); run `compat-check.ps1` first to see what changed |

## Log locations

- Plugin: `%APPDATA%\SpaceEngineers2\<Short>\`
- Game: `%APPDATA%\SpaceEngineers2\Temp\Logs\SpaceEngineers2_*.log` (loader lines, exceptions; `[Process Tag Added] : PluginLoaded` confirms load)

## Debugging technique that works

When a picture looks wrong, split the pipeline: dump the raw data to a file from inside the game,
render it with a tiny standalone console tool that links the **same** source file, and also dump
the final buffer. That tells "bad data / bad renderer / bad display" apart in one step. Count
failures that don't throw (NaN, empty results) separately from exceptions.
