---
name: prototype-engineers
description: >
  Builds playable Space Engineers 2 gameplay prototypes as in-process C# game plugins (loaded with
  the game's -plugins: argument) for the retail Steam version of the game, end to end: finds the
  Steam install on this machine, scaffolds a project that builds first time, reverse-engineers the
  game API by decompiling the shipped DLLs, implements with Harmony hooks and custom UI/HUD, builds,
  deploys, launches the game, reads the logs, iterates, and packages a standalone zip anyone with
  the game can run. Use when the user wants to prototype, mock up, test or pitch an SE2 feature or
  mechanic in the real game ("prototype X in SE2", "make a plugin that...", "can we try Y in game",
  "hack together Z", "SE2 mod/plugin", "add a terminal tab / HUD bar / new ship behaviour"), or to
  continue, debug or package an existing SE2 plugin prototype.
---

# PrototypeEngineers: SE2 prototype plugins

Goal: go from "what if the game did X" to the user playing X in the real game in one sitting,
with Claude doing the research, code, build and launch, and the user only playing and giving
feedback. Prototypes prove a feeling or a mechanic. They are not production code, but they
must be solid enough that someone else can load the zip and see the idea.

Only the **retail game installed through Steam** is supported: every project builds against, and
launches, the Steam install of Space Engineers 2 (appid 1133870).

Paths in this file are relative to this skill's folder (`<skill>`). Scripts are PowerShell;
run them with `powershell -NoProfile -ExecutionPolicy Bypass -File <script> ...`.

## Before anything: is this skill current? (every session, a few seconds)

Run `scripts/check-skill-update.ps1` (read-only, it only fetches) and act on its first line:
- **UP TO DATE** or **CHECK FAILED** → carry on.
- **NOT A CLONE** → carry on; tell the user once that this copy can't check for updates and that
  installing from the skill's repository (its README) fixes that.
- **BEHIND n** → tell the user how far behind it is and the new commit titles, and ask whether to
  update now. Yes → run the two commands the script printed (`git pull`, then the repo's
  `install.ps1`, which relinks the skill and refreshes the helper agents), then **re-read this
  SKILL.md** before going on, because the steps may have changed. No → carry on with the
  installed version.
- **LOCAL CHANGES** (with BEHIND) → don't pull; tell the user their own edits to the skill would
  conflict and let them decide.

## 0. Environment (once per machine, ~1 minute)

1. **Game (first run finds Steam)**: `scripts/find-game.ps1`. On the first run it discovers the
   Steam folder (Windows registry, then Steam's default install folders) and remembers it in
   `%LOCALAPPDATA%\prototype-engineers\steam-path.txt`; later runs reuse it. From there it reads
   every Steam library in `libraryfolders.vdf` (any drive) and the SE2 app manifest. It prints one
   line, `Steam|<Game2 folder>|<version>`, or:
   - **STEAM NOT FOUND** → ask the user where Steam is installed, then run
     `scripts/find-game.ps1 -SteamDir "<folder containing steam.exe>"` (remembered from then on).
   - **GAME NOT FOUND** → Space Engineers 2 isn't installed in that Steam; ask the user to install it
     from Steam.
   - Steam moved or a library changed → `scripts/find-game.ps1 -Rediscover`.
   Never assume a drive letter, and never point a project at a game copy outside Steam.
2. **.NET SDK 9+**: `dotnet --list-sdks`. Missing → ask the user to install it from
   https://dotnet.microsoft.com/download (don't install system software for them).
3. **ilspycmd** (decompiler): `ilspycmd --version`, else `dotnet tool install -g ilspycmd`
   (tell the user you're installing it).
4. Build the API index once: `scripts/api-search.ps1 -Game2Dir <dir> -Type 'ThrustComponent$'`
   (first run indexes the game's ~75 DLLs in ~10 s, cached per game version under
   `%LOCALAPPDATA%\prototype-engineers\api-cache\`).
5. **Helper agents**: `scripts/install-agents.ps1 -Check`. Missing or out of date → run it without
   `-Check` (copies `agents\*.md` to `%USERPROFILE%\.claude\agents`); they're usable from the next
   session. Not installed is fine - see "Cheaper helpers" below for the fallback.

## Cheaper helpers (save tokens on the bulky, checkable work)

Decompiled code and game logs are huge; reading them in the main conversation is where most of a
prototype's tokens go. Hand those jobs to cheap agents that return a short, checkable answer:

| Agent (model) | Hand it | Example prompt |
|---|---|---|
| `pe-api-scout` (Haiku) | One concrete question about game code (section 3) | "Game2 dir: <dir>. Where is per-tick thrust computed, and is it server-only? Signatures + evidence." |
| `pe-log-reader` (Haiku) | Reading logs after every run (section 5) | "Short: AmmoHud. Last run - load status, features, exceptions." |
| `pe-build-fixer` (Sonnet) | Compile errors once you've written the code (section 5) | "Project: <dir>. Make it build; don't change behaviour." |
| `pe-reviewer` (Sonnet) | Reviewing the plugin's code against the section 4/4b traps (section 5) | "Project: <dir>. New since last launch: Game\Dock.cs, the tab UI. Review." |

- If the agent type isn't available (not installed yet), use the Agent tool with
  `subagent_type: general-purpose`, `model` as in the table, and a prompt that starts with the body
  of `agents/<name>.md` followed by the question.
- Several independent questions → several scouts in one message, so they run in parallel.
- **Keep for yourself:** pinning down the prototype, choosing hooks and patch types, client vs
  server and thread decisions, Avalonia UI, anything from section 4, and diagnosing *why* a
  feature misbehaves. A cheap model's wrong call there costs an in-game test loop - far more
  than the tokens saved.
- **Trust but check:** confirm a scout's signature by grepping the cited `.cs` line before you
  patch it; review the build-fixer's change list. If an answer is vague or contradicts the cookbook,
  ask a sharper follow-up (SendMessage) or check it yourself - don't escalate a cheap agent to a
  bigger model for the same question.

### When handing off is worth it

Every helper starts by loading its own instructions and tools before it reads anything. Measured
on real runs: **~15k tokens for a dedicated agent** (~8.6k of that is shared cache when several
start together or one after another), **~57-62k for the general-purpose fallback**. That sounds
expensive, but count it in what it costs, not in tokens. A Haiku token costs ~1/40 of an Opus
token and a Sonnet token ~1/2 (API prices; plan usage limits weigh models roughly alike):

| Spin-up | Raw tokens | Main-model (Opus) equivalent |
|---|---|---|
| `pe-api-scout` / `pe-log-reader` (Haiku) | ~15k | ~0.4k |
| General-purpose fallback on Haiku | ~62k | ~1.5k |
| `pe-build-fixer` / `pe-reviewer` (Sonnet) | ~16.5k | ~8k |
| General-purpose fallback on Sonnet | ~62k | ~31k |

The hidden cost is on the other side. Everything you read yourself **stays in the main context and
is re-read on every later turn** of the session. A typical scout run pulls in ~50k tokens of
decompiled code. Inline on Opus, that's ~$0.25 to write it once plus ~$0.01 on every later turn.
On Haiku the whole run, spin-up included, costs about $0.02.

Rules:
- **Delegate** when the job would pull more than ~3k tokens into the main context: a lookup that
  takes more than one search, reading a decompiled type, a log of more than ~100 lines, more than
  a couple of compile errors, a review of more than one file.
- **Do it yourself** when it's one grep or `-Type` search in a known place, a log tail under ~50
  lines, or a one-line fix. Do the same when you'd have to re-read the same material to check
  the answer anyway.
- **Haiku helpers are close to free to start, so use them freely.** On a Sonnet helper the
  spin-up is real (~8k Opus-equivalent), so hand it jobs worth several thousand tokens of your
  own work, not a single error.
- **Install the agents** (`install-agents.ps1`). The general-purpose fallback costs ~4× as much to
  start, and on Sonnet that's ~31k Opus-equivalent, more than a small job saves.
- **Follow-ups go to the same helper** (SendMessage). Its context is already loaded, so a second
  question costs no new spin-up. Independent questions go out in one message so the cache is shared.
- A helper's answer costs main context too (~0.5-2k tokens), so ask for signatures and line
  references, not file dumps.

## 1. Pin down the prototype (short)

From the pitch, decide and state in 3-5 lines — ask only if genuinely ambiguous:
- **What the player does and feels** (the thing to prove).
- **The smallest build that proves it**. Cut everything else: no settings UI unless tuning is the
  point, single player only, one ship type is fine.
- **Where it plugs in**: which game system (thrust, character, terminal, HUD, production...) and
  whether its state lives on the **client** or **server** session (see Sessions below).
- **How the user triggers it in game** (key, terminal tab, sitting in a cockpit...).
- **Default keys** - pick keys the game doesn't bind (Numpad0-9 and NumpadDivide/Multiply/
  Subtract/Add are usually free; the game binds most F-keys, Delete, [ ] and backtick; Home and
  End are popular with other plugins, so avoid them as defaults). State them in the 3-5 lines.

Project location: ask once where they keep code (default `%USERPROFILE%\source\repos`). Name:
`SE2<Feature>` PascalCase.

## 2. Scaffold (builds and loads with zero edits)

```
scripts/new-prototype.ps1 -Name SE2Feature -ParentDir <dir>
```

Creates and builds:
- `FeaturePlugin.cs` - entry point (safe lifecycle: lazy install, 1 Hz engine-thread tick).
- `Game\Hooks.cs` - the plugin's **features** and every game hook each needs (section 4b).
  `Game\` is the only folder allowed to touch the game.
- `SE2Core\` - shared core vendored from this skill's `core\`: `Log`, `Sessions` (client + server
  session lookup), `Feature` (check-then-patch, guarded hooks), `Bind` (tolerant member lookup),
  `GameVersion`, `Compat` (offline check) and `SE2Core.targets` (build rules). **Never edit a
  project's copy, or `core\` here** - core fixes arrive with skill updates, and
  `scripts/update-core.ps1` then refreshes every project.
- `SE2Feature.csproj` (net9.0, Lib.Harmony, game DLLs referenced in place), `PluginKeys.cs`
  (rebindable keys), `run-game.ps1`, `deploy.cmd`, `package.ps1`, `tools\` (launcher + game
  finder for the zip), `README.md`.

**No game path is ever written into the project.** It is the Steam install, remembered per machine
in `Game2Dir.local.props` (git-ignored). If that file is missing or stale, both `dotnet build` and
`run-game.ps1` run `tools\find-game.ps1` and rewrite it, so a project copied from another machine
builds and launches with no edits. Never hard-code a drive or Steam path in scripts or code you
add; read `$(Game2Dir)` / `Game2Dir.local.props` or call `tools\find-game.ps1`.

**Recommended first launch**: run the untouched scaffold once (`run-game.ps1`), have the user load
a world, then `scripts/logs.ps1 -Short Feature` must show "Plugin loaded", a `Feature "...": ON`
line, "Client session attached", "Server session attached". This proves the whole pipeline on their
machine before any feature code exists. Skip only if they've done this before.

## 3. Research the API — decompile, don't guess

**Read `references/api-cookbook.md` first** — it covers most systems prototypes touch, with the
traps — then `%LOCALAPPDATA%\prototype-engineers\local-discoveries.md` if it exists (this user's own verified notes from earlier prototypes). Then confirm every member you rely on against the installed build - for anything beyond a
quick lookup, ask `pe-api-scout` (see "Cheaper helpers") rather than reading decompiled files here:

| Need | Command (`scripts/api-search.ps1 -Game2Dir <dir> ...`) |
|---|---|
| Find a type by name | `-Type 'Thrust\w*Component$'` |
| Which DLL mentions an identifier (fields, methods, strings) | `-Text 'ComputeThrust\w*'` |
| The game's own XML doc comments | `-Doc 'resource node'` |
| Read the code | `-Decompile Full.Type.Name` → prints path to a cached `.cs`; read it |

Method: start from the nearest thing the player can see (a HUD value, a terminal screen, a
block's component) and follow the code to where the value is computed or consumed. That call
site is the hook. Also grep the shipped content JSON under `<SE2 root>\GameData\Vanilla\Content`
for tuning values and GUIDs — content often overrides code defaults.

Prefer, in order (most to least likely to survive a game update): reading/writing public state
from the 1 Hz tick → subscribing to a signal → a postfix on a public method → a prefix on a private
one → a prefix that returns bool (can skip the game's code). Never a transpiler. Keep patches few.

## 4. Implement — rules that prevent the classic failures

- **Constructor does nothing but log and start the timer.** The game starts twice (bootstrapper
  then real game); patching, ports, Avalonia objects and game types all wait until
  `VRageCore.HasValue` (the template does this).
- **No `AllocConsole`** (D3D12 black screen). Log to file via `Log(...)`.
- **Game state only on the engine thread** (`UpdateQueue.Enqueue`) or inside a hook; UI thread
  reads immutable snapshots you publish. Per-tick input injection belongs in a `Scene.Tick` prefix.
- **Budget engine-thread work** (~8 ms per tick, resume from a cursor). A synchronous bulk loop
  freezes the game.
- **Client vs server**: single player runs both in one process. Simulation (movement, thrust,
  power, production, ownership) is server-side; rendering, UI, particles, planets, input are
  client-side. Hooks fire on both scenes — filter.
- **Avalonia**: create brushes/controls on the UI thread, never in static fields. Terminal tabs via
  `TerminalScreenViewModel` ctor postfix; HUD via OverlayLayer (cookbook).
- **No `async void`.** Use `async Task`.
- **Don't modify game files or game content.** The plugin changes the game at runtime only.
- **Feel-heavy prototypes need a tuning surface**: a hot-reloaded JSON/.cfg next to the deployed
  DLL, or for rich tuning a tiny local HTTP page (`HttpListener` on 127.0.0.1, started lazily,
  `ExclusiveAddressUse`, falling back to the next port if the default is taken). Log the URL it
  ended up on. Tuning live beats rebuild/relaunch loops.
- **Keys go through `PluginKeys`** so players can rebind them without rebuilding:
  `static readonly DigitalInput ToggleKey = PluginKeys.Get("toggle", "Keyboard::Numpad5");` then
  `kb.GetDigitalState(ToggleKey)`. `GetVirtualKey(id, default)` for `GetAsyncKeyState` code,
  `Label(id, default)` for any text that names the key ("Press {label} to close"). Resolve once
  into a static field, never per frame. Players rebind with a `keys.json` next to the DLL
  (`{"toggle": "Keyboard::F6"}`). Keys that only matter inside the plugin's own tool/menu can read
  `KeyboardInputs.X` directly.
- **Keep the README's Keys section current** whenever keys change: one line per key (default,
  what it does, its id).
- **Harmony prefixes that return `bool`** can skip the original - and every other plugin's patch
  on it. Prefer a postfix or a `void` prefix; on shared hot spots (`InputContext.Dispatch`,
  `Scene.Tick`, `DrawHUDMarkers`) only return false for your own case.
- Log state transitions and one line per hook on first fire. Remove noisy diagnostics once proven.

## 4b. Structure that survives game updates

Most game updates are renames, moved members and changed signatures. This structure turns those
into "one feature switches off with a clear message" or "fix one folder", never a crash:

1. **Game code lives only in `Game\`.** Everything else (the prototype's own logic, tuning, UI that
   doesn't touch game types) works on plain data and may use only `Keen.VRage.Library.Mathematics`.
   The build enforces it (`SE2Core.targets`; `<SE2Boundary>Warning</SE2Boundary>` relaxes it).
   When logic needs a game service, the `Game\` side plugs it in (a delegate, or a small bridge
   class that converts game types to the plugin's own).
   Use plugin-owned types (`GridKey`-style wrappers, own structs) in logic, not game structs.
2. **Declare features, don't patch directly.** In `Game\Hooks.cs`:
   ```csharp
   [FeatureDeclarations]
   internal static class Hooks
   {
       public static Feature Docking = null!, Tab = null!;
       public static void Declare()
       {
           Docking = Feature.Define("Hold P to dock", Dock.Declare);       // Dock.Declare(Feature f) lists its needs
           Tab = Feature.Define("Docking tab", f =>
           {
               f.Postfix(typeof(TerminalScreenViewModel), ".ctor", typeof(Hooks), nameof(TabCtor), allOverloads: true);
               Header = f.NeedField(typeof(TerminalSubScreen), "<LocalizedHeader>k__BackingField");
           }, Docking);                                                    // optional dependencies
       }
   }
   ```
   `Feature.InstallAll` (the template's pump calls it) resolves EVERYTHING first and patches only
   complete features, logging `Feature "X": ON (n hooks)` or `OFF - <what's missing>`. One
   feature per thing the player would notice, so a break disables as little as possible.
3. **Targets as literals**: `typeof(T)` or `"Full.Type.Name"`, a `"Method"` string (`"New|Old"` for
   a rename - first that exists wins), the patch via `nameof`. Overloaded names need `args:` or
   `pick:`; `allOverloads: true` patches all.
4. **Private members through the feature**: `f.NeedField / NeedProperty / NeedMethod` (required,
   checked up front) or `Bind.Field / Method` (optional - degrade instead). Never resolve private
   members in a `static readonly` initializer: a rename throws there and takes the class down.
   Stable public API stays plain C#; a break there is a compile error in compat-check, which is fine.
5. **Patch methods take only the parameters they use** (Harmony matches by name; the core checks
   names against the target, so a renamed game parameter turns the feature off instead of crashing).
6. **Feature code outside hooks** (ticks, UI handlers, timers): `feature.Run(() => ...)`. It does
   nothing while the feature is off and counts exceptions instead of letting them escape.
7. **Compiled references are checked too.** Plain C# like `info.InteractedEntity` compiles against
   one build and fails on another when the member's type changed (field became property, `Entity?`
   became `NullOnReplicationFail<Entity>`) - with `MissingFieldException` the first time that code
   runs. The core resolves every game type/member the DLL references at startup and in
   compat-check; a changed one switches off the feature whose code uses it (or warns, if shared).
   For members known to change, read them with `Bind.ReadAs<T>(obj, "Name")` - it accepts field or
   property and unwraps wrappers - so one DLL works on both builds.
8. **Guarded automatically**: every patched method gets a finalizer that swallows exceptions from
   *this plugin's* patch code (never the game's). A throwing prefix switches its feature off at
   once; other hooks after 5 errors. Subscribe to `Feature.Notice` to show these in your HUD.
9. **Keep state out of saves**, look content up by GUID with a fallback, and keep numbers in JSON.

## 5. Build → deploy → launch → read logs (the loop)

1. `dotnet build <project>` until clean (fix errors by checking the decompiled signature, not by
   guessing). More than a couple of errors → hand them to `pe-build-fixer`.
   **Before the first launch of new feature code, and before sharing**, run `pe-reviewer` on the
   project (name the files that changed). It reports findings with file:line and confidence and
   never edits; fix the high ones, judge the rest - it can be wrong. Skip it for tuning tweaks and
   one-line fixes. A trap it missed that cost a game loop → add it to `%LOCALAPPDATA%\prototype-engineers\local-discoveries.md`
   (see "Record what you learned"); the reviewer reads that file too.
2. If `SpaceEngineers2` is running, **ask the user to close it** (unsaved progress is theirs), or
   ask permission to stop the process.
3. `run-game.ps1` — builds, copies to `bin\Deploy`, launches the Steam install with
   `"-plugins:<deploy dll>"`. (`-NoLaunch` if they prefer starting from Steam; it prints the Launch
   Options line.) Launching the exe directly is fine — Steam relaunches it and the argument survives.
4. Tell the user exactly what to do in game to see the feature (load which world, sit where, press
   what), then wait for them.
5. `scripts/logs.ps1 -Short <Short>` — plugin log tail + loader lines/exceptions from the game log.
   Usually via `pe-log-reader`, which returns the load status, features and exceptions in a few
   lines. Diagnose from that evidence yourself; `references/troubleshooting.md` maps common
   symptoms to causes.
6. Iterate. Each loop should change one thing and say what to look for.

## 6. Share it

`package.ps1 [-Version 0.2.0]` → `dist\SE2Feature_v0.2.0.zip` with the DLL, 0Harmony.dll,
`launch.cmd` (double-click; finds the player's Steam install, checks it is at least the build the
plugin was compiled against - recorded in `built-for.txt` - and starts it with the plugin),
Harmony's licence (`0Harmony-LICENSE.txt`, required when its DLL is redistributed) and the
README. Fill the README's TODOs (what it does, how to trigger it, keys, known limits) before
packaging, and **keep its "Before you run it" note**: a plugin is code with full access to the
player's PC, so the reader must be told to run it only if they trust the sender. It needs the game build it was compiled against or newer; on a newer build the feature
checks decide what still works (run compat-check to know in advance). Never upload or publish the
zip anywhere yourself; hand the path to the user.

Test the zip the way a player will: extract it to a new folder and run `launch.cmd` from a
**fresh** `cmd` window (not your working shell — inherited state like `$LASTEXITCODE` hides
launcher bugs). In PowerShell scripts, judge a called `.ps1` by its output, never by
`$LASTEXITCODE` (it stays null when a script succeeds without `exit`).

## 6b. When the game updates

After a Steam update, check every prototype without launching the game:
`scripts/compat-check.ps1 -Root <folder of SE2* projects>` (or `-Project <dir>`). For each plugin it
reports which features still pass on the new build as shipped, whether it rebuilds, and which
features pass after a rebuild. Fix what it reports in `Game\` (`api-search.ps1` for the new names;
`"New|Old"` method strings and `Bind.ReadAs` keep one DLL working on both builds), then rebuild.

**Core fix → every prototype**: core fixes arrive with a skill update. After updating,
`scripts/update-core.ps1 -Root <folder> -Build` refreshes each project's `SE2Core\` and rebuilds.
Don't edit `core\` yourself (local edits block the skill's updates). If the core itself has a bug,
tell the user, work around it in the project's `Game\` code, and note it in
`%LOCALAPPDATA%\prototype-engineers\local-discoveries.md`.

## 7. Optional follow-ups

- **Pitch/design doc** — only if asked: `references/design-doc.md`.
- **Record what you learned.** When you verify an API not in the cookbook (or find an entry
  wrong for this build), append it to `%LOCALAPPDATA%\prototype-engineers\local-discoveries.md` as
  `- [game version] area: fact (how verified)`; create the file if it doesn't exist. This is how the
  skill gets faster for the next prototype. **Never edit the skill's own files**: the skill is a git
  clone, and local edits make its next update fail with a conflict.
