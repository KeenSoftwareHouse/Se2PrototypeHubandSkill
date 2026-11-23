# Setting up the PrototypeEngineers skill (instructions for the AI assistant)

You are reading this because the user asked you to set up the PrototypeEngineers skill. This
folder (the one containing this file) is the skill's home. Work through the steps in order, tell
the user what you are doing, and guide them through anything you cannot do yourself.

## 1. Register the skill, if your tool has skills

- **Claude Code**: run `install.ps1` from this folder with
  `powershell -NoProfile -ExecutionPolicy Bypass -File install.ps1` and show the user what it
  printed. It links the skill into the user's skills folder and installs the helper agents. They
  load in the user's next session; for this session keep going with step 2.
- **Any other assistant**: nothing to register. The instructions are in
  `prototype-engineers\SKILL.md`; where it names a helper agent (`pe-api-scout`, `pe-log-reader`,
  `pe-build-fixer`, `pe-reviewer`), do that job yourself, following `prototype-engineers\agents\<name>.md`.
  If your tool has a place for standing project or user instructions, offer to add a line there
  saying: "For Space Engineers 2 prototypes, read and follow
  `<this folder>\prototype-engineers\SKILL.md`."

## 2. Check the PC

Read `prototype-engineers\SKILL.md` now and follow its "Environment" section: find the user's Steam
install of Space Engineers 2 and check the .NET SDK and the other tools it lists.

For anything missing, tell the user exactly what to install and offer to do it for them. Always
ask before installing anything. If the game isn't installed through Steam, say so and stop there:
the skill only works with the retail Steam version.

## 3. Carry on

When everything is ready, do not stop. Tell the user setup is done in one or two lines, ask what
they would like to prototype, and keep following `prototype-engineers\SKILL.md` from this folder
in this same conversation.

## Updating later

If this folder is a git clone, `git pull` here updates the skill; then follow this file again
from step 1. If it was extracted from a zip, download the zip again and replace the folder.
