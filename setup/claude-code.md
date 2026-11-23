# Setup flow: Claude Code

1. From the skill's home folder (one level up from this file) run
   `powershell -NoProfile -ExecutionPolicy Bypass -File install.ps1` and show the user what it
   printed. It links `prototype-engineers` into `%USERPROFILE%\.claude\skills\` and installs the
   helper agents into `%USERPROFILE%\.claude\agents\`.
   - "SKIPPED - ... is a real folder": the user has a hand-copied `prototype-engineers` folder in
     their skills folder. Ask them whether to rename it, then run `install.ps1` again.
2. The skill and its helper agents load in the user's **next** session. For this session, keep
   going: read `prototype-engineers\SKILL.md` from the skill's home folder and follow it directly.
   Until the helper agents are loaded, use the fallback `SKILL.md` describes under "Cheaper helpers".
3. Go back to `SETUP.md` step 2.

Nothing in `SKILL.md` needs adjusting for you: it is written in Claude Code's terms.
