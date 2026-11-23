# Setting up the PrototypeEngineers skill (instructions for the AI assistant)

You are reading this because the user asked you to set up the PrototypeEngineers skill. This
folder (the one containing this file) is the skill's home. Work through the steps in order, tell
the user what you are doing, and guide them through anything you cannot do yourself.

## 1. Work out which assistant you are, and follow that flow

You know what tool you are running as. Pick the one that matches and read that file now:

| You are | Read and follow |
|---|---|
| Claude Code (the CLI, the desktop app's Code tab, or an IDE extension) | `setup\claude-code.md` |
| OpenAI Codex / ChatGPT's coding agent (CLI, desktop app or IDE extension) | `setup\codex.md` |
| Google Antigravity, whichever model you run on (agy CLI, desktop app or IDE) | `setup\antigravity.md` |
| Anything else (Cursor, Gemini CLI, Copilot, Windsurf, ...) | `setup\other-assistants.md` |

Not sure? Use `setup\other-assistants.md`: it assumes nothing about your tool. Tell the user in
one line which flow you picked.

## 2. Check the PC

Read `prototype-engineers\SKILL.md` and follow its "Environment" section, as adjusted by the flow
file you read in step 1: find the user's Steam install of Space Engineers 2 and check the .NET
SDK and the other tools it lists.

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
