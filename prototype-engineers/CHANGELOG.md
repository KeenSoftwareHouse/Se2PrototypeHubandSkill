# PrototypeEngineers skill - Changelog

## 1.0.3

### Added
- **Setup works for more than one assistant.** One neutral prompt (in the README) gets the skill
  and then follows `SETUP.md`, which picks a flow for the tool it is running in:
  - **OpenAI Codex** (`setup\codex.md`, `setup\install-codex.ps1`): links the skill into Codex's own
    skills folder.
  - **Google Antigravity** (`setup\antigravity.md`, `setup\install-antigravity.ps1`): registers the
    skill in Antigravity's skill registry.
  - **Claude Code** (`setup\claude-code.md`): as before, `install.ps1`.
  - **Anything else** (`setup\other-assistants.md`): follows the instructions directly.
  Each flow says how to read `SKILL.md` in that tool's terms (sub-agents, asking questions). The
  Codex and Antigravity flows were run on a PC with both installed.
- The setup carries on in the same conversation: it checks the PC, then asks what to prototype.

- Codex and Antigravity each built a small test prototype with the skill (a cockpit speed
  readout): both scaffolded, looked up the game code, built cleanly and passed the offline
  check. Neither was run in the game.

### Fixed
- `SKILL.md` said the first API index takes about 10 seconds; it can take a minute or two.
- The API cookbook said `Vector3` has no `Length()`; on retail 4.0.0.3169 it does.

## 1.0.2

### Added
- The README has a prompt to paste into Claude Code that clones and installs the skill for you.

### Fixed
- The runtime-mesh note in the API cookbook named an engine helper that the retail game doesn't
  have. The cookbook's newer notes were re-checked against retail 4.0.0.3169: every other game
  type and member they name exists there.

## 1.0.1

### Added
- Every packaged zip now carries Harmony's MIT licence (`0Harmony-LICENSE.txt`), which must travel
  with `0Harmony.dll`. New projects get it in `tools\`; `package.ps1` stops if it is missing.
- The project README starts with a "Before you run it" note: a plugin is unsigned code with full
  access to the player's PC, so only run one from someone you trust, and back up saves first.

### Fixed
- The skill no longer tells you to edit its own `core\` folder, which blocked later updates. Core
  fixes arrive with skill updates; `update-core.ps1` then refreshes each project.
- The API cookbook header no longer lists exact build numbers.

## 1.0.0

First public release.

- Builds Space Engineers 2 gameplay prototypes as in-process C# plugins, end to end: scaffold,
  API research by decompiling the game's DLLs, Harmony hooks, UI/HUD, build, launch, log reading,
  packaging.
- Retail only: every project builds against and launches the Steam install of Space Engineers 2.
  The Steam folder is discovered on first run and remembered.
- SE2Core: features that check their game hooks before patching, so a game update switches off
  one feature with a clear message instead of crashing; offline compatibility check after updates.
- Rebindable keys through `PluginKeys` and a `keys.json` next to the plugin DLL.
- Standalone zip packaging with a double-click launcher.
- Plugins you share carry nothing about your PC: builds replace your folder paths (which include
  your Windows username) with `/_/<project>/...`, and Release builds make no `.pdb` file.
- What Claude learns about the game API while building is saved to
  `%LOCALAPPDATA%\prototype-engineers\local-discoveries.md`, outside the skill folder, so skill updates
  never conflict with it. The skill, the API scout and the reviewer read it alongside the cookbook.
- Cheaper helper agents (`pe-api-scout`, `pe-log-reader`, `pe-build-fixer`, `pe-reviewer`) for
  research, logs, compile fixes and review.
