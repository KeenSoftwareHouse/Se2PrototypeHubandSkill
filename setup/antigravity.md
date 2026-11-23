# Setup flow: Google Antigravity (Gemini and other models - agy CLI, desktop app or IDE)

1. From the skill's home folder (one level up from this file) run
   `powershell -NoProfile -ExecutionPolicy Bypass -File setup\install-antigravity.ps1` and show the
   user what it printed. It adds the skill's home folder to your user-level skill registry,
   `%USERPROFILE%\.gemini\config\skills.json` (keeping whatever is already there, with a `.bak`
   copy), so the skill is read straight from the home folder and a later `git pull` there updates
   it in place.
   - "SKIPPED - ... is not valid JSON": show the user the file and ask how to proceed.
2. The skill is listed for you from the user's next session. For this session, read
   `prototype-engineers\SKILL.md` from the skill's home folder and follow it directly.
3. Do **not** run `install.ps1` or `scripts\install-agents.ps1`: they register the skill with a
   different tool.
4. Read "How to read SKILL.md" below, then go back to `SETUP.md` step 2.

## How to read SKILL.md

`SKILL.md` was written in another tool's terms. Everything in it applies to you, with these swaps:

| SKILL.md says | You do |
|---|---|
| "this skill's folder" / `<skill>`, and every `scripts\...` or `references\...` path | Relative to `<skill home>\prototype-engineers`. From the skill home, run scripts as `prototype-engineers\scripts\<name>.ps1`. |
| "Before anything: is this skill current?" | Run `prototype-engineers\scripts\check-skill-update.ps1`. On "BEHIND", after the user agrees, run `git pull` in the skill home and re-read `SKILL.md`; skip the `install.ps1` part. |
| Environment step 5, "Helper agents" / `install-agents.ps1` | Skip it. |
| Hand a job to `pe-api-scout`, `pe-log-reader`, `pe-build-fixer` or `pe-reviewer` | Use `invoke_subagent` with `TypeName: "self"` (the read-only `research` type cannot run the scripts or edit files) and `Model: "flash"`, and give it the body of `agents\<name>.md` followed by the question. No sub-agents available in this session → do the job yourself, following that file's rules. |
| Model names and token costs in "Cheaper helpers" | Ignore the numbers; keep the idea: bulky, checkable reading (decompiled code, long logs) is worth handing off, because it stays in your context. |
| "AskUserQuestion" | Your `ask_question` tool, or a plain question in chat for a simple yes/no. |
| "next session" / "new session" | A new conversation. |
| Paths inside `agents\<name>.md` that start `%USERPROFILE%\.claude\skills\prototype-engineers` | Read them as `<skill home>\prototype-engineers`, and say so when you pass that file to a sub-agent. |

Everything else - the scripts, the project template, the rules in sections 3 to 5, the API
cookbook, where discoveries are saved - is tool-independent. Follow it exactly.
