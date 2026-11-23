# Setup flow: OpenAI Codex (ChatGPT's coding agent - CLI, desktop app or IDE extension)

1. From the skill's home folder (one level up from this file) run
   `powershell -NoProfile -ExecutionPolicy Bypass -File setup\install-codex.ps1` and show the user
   what it printed. It links `prototype-engineers` into your skills folder
   (`%USERPROFILE%\.codex\skills\`, or `%CODEX_HOME%\skills\`), so a later `git pull` in the skill's
   home folder updates the skill in place.
   - The command writes outside the workspace: if your sandbox blocks it, ask the user to approve it.
   - "SKIPPED - ... a folder this installer did not create": ask the user whether to rename that
     folder, then run the script again.
   - If linked folders turn out not to be listed as a skill on this PC, run it again with `-Copy`
     (and tell the user to re-run it after each update).
2. The skill is listed for you from the user's next message. For the rest of this turn, read
   `prototype-engineers\SKILL.md` from the skill's home folder and follow it directly.
3. Do **not** run `install.ps1` or `scripts\install-agents.ps1`: they register the skill with a
   different tool.
4. Read "How to read SKILL.md" below, then go back to `SETUP.md` step 2.

## How to read SKILL.md

`SKILL.md` was written in another tool's terms. Everything in it applies to you, with these swaps:

| SKILL.md says | You do |
|---|---|
| "this skill's folder" / `<skill>` | `<skill home>\prototype-engineers` |
| "Before anything: is this skill current?" | Run `scripts\check-skill-update.ps1` as written. On "BEHIND", after the user agrees, run `git pull` in the skill home and re-read `SKILL.md`; skip the `install.ps1` part. |
| Environment step 5, "Helper agents" / `install-agents.ps1` | Skip it. |
| Hand a job to `pe-api-scout`, `pe-log-reader`, `pe-build-fixer` or `pe-reviewer` | Spawn a sub-agent (your `spawn_agent` tool, on a cheaper model where you can pick one) and give it the body of `agents\<name>.md` followed by the question. No sub-agents available in this session → do the job yourself, following that file's rules. |
| Model names and token costs in "Cheaper helpers" | Ignore the numbers; keep the idea: bulky, checkable reading (decompiled code, long logs) is worth handing off, because it stays in your context. |
| "AskUserQuestion" | Your `request_user_input` tool where you have it; otherwise ask in your reply and wait for the answer. |
| "next session" / "new session" | The user's next message, or a new thread. |
| `plugin.json` field `builtWith` (the template's default text) | Ask the user what to write; suggest "Codex". Never put your own name in `author`. |
| Game and build commands | They need the user's game folder, the .NET SDK and the NuGet cache, all outside the workspace: ask for the access you need rather than working around it. |

Everything else - the scripts, the project template, the rules in sections 3 to 5, the API
cookbook, where discoveries are saved - is tool-independent. Follow it exactly.
