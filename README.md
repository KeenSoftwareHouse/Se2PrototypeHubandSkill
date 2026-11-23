# PrototypeEngineers skill

A skill for AI coding assistants that builds playable **Space Engineers 2** gameplay prototypes
as in-process C# game plugins: describe a mechanic, and your assistant researches the game code,
scaffolds the plugin, builds it, launches the game with it and iterates with you.

Works with the **retail Steam version** of Space Engineers 2 only.

## SE2 Prototype Hub

The **SE2 Prototype Hub** is a small Windows app for browsing the public prototype plugins and
launching the game with one of them. Download `SE2PrototypeHub.exe` from this repo's
[**tools** release](../../releases/tag/tools), put it somewhere you own (Desktop or Documents, not
Program Files) and run it; it updates itself after that. The first time, Windows may show
"Windows protected your PC" because the app isn't code-signed: click **More info**, then **Run anyway**.

You don't need the Hub to use the skill, or the skill to use the Hub.

## Requirements

- Windows, Space Engineers 2 installed through Steam
- An AI coding assistant that can read files and run PowerShell on your PC
- .NET SDK 9 or newer
- git (for updates)

The setup prompt below checks these for you and tells you what is missing.

## Install

Paste this into your AI coding assistant. It gets the skill, checks what your PC needs and then
carries on in the same conversation.

```text
Set up the PrototypeEngineers skill for Space Engineers 2 on this PC and guide me through
anything you can't do yourself. Do it step by step and tell me what you're doing.

1. Get https://github.com/KeenSoftwareHouse/Se2PrototypeHubandSkill into
   %USERPROFILE%\prototype-engineers-skill. If that folder is already a git clone, run git pull
   in it. If git isn't installed, download
   https://github.com/KeenSoftwareHouse/Se2PrototypeHubandSkill/archive/refs/heads/main.zip
   and extract it there instead.
2. Read SETUP.md in that folder and follow it to the end.
```

Then just say what you want to try, for example "prototype a jetpack boost in SE2".

## Update

The skill checks for updates when you start working with it and offers to pull them. To update
by hand:

```powershell
git -C "$env:USERPROFILE\prototype-engineers-skill" pull
```

Then ask your assistant to follow `SETUP.md` again.

## Which assistants work

The skill is plain files: instructions (`prototype-engineers\SKILL.md`), PowerShell scripts, a
project template and notes on the game's code. Any assistant that can read files and run
PowerShell can follow it.

`SETUP.md` has your assistant work out which tool it is and pick the matching flow from the
`setup\` folder: one for Claude Code, which registers the skill and its helper agents, and one for
every other assistant, which follows the same instructions directly. The skill has been built and
tested with Claude Code; the other flow is new, so tell us how it goes with your tool.
