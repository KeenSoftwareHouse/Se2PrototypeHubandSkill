# PrototypeEngineers skill (Claude Code)

A Claude Code skill for building playable **Space Engineers 2** gameplay prototypes as in-process
C# game plugins: describe a mechanic, and Claude researches the game code, scaffolds the plugin,
builds it, launches the game with it and iterates with you.

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
- [Claude Code](https://claude.com/claude-code)
- .NET SDK 9 or newer
- git (for installing and updates)

## Install

The easy way: paste this into your AI coding assistant (it is built for Claude Code) and let it
do the work. It gets the skill, checks what your PC needs and then carries on in the same
conversation.

```text
Set up the PrototypeEngineers skill for Space Engineers 2 on this PC and guide me through
anything you can't do yourself. Do it step by step and tell me what you're doing.

1. Get the skill from https://github.com/KeenSoftwareHouse/Se2PrototypeHubandSkill into
   %USERPROFILE%\.claude\prototype-engineers-skill. If the folder is already a git clone, run
   git pull in it. If git isn't installed, download
   https://github.com/KeenSoftwareHouse/Se2PrototypeHubandSkill/archive/refs/heads/main.zip
   and extract it there instead.
2. If you are Claude Code, run install.ps1 from that folder with PowerShell and show me what it
   printed.
3. Read prototype-engineers\SKILL.md in that folder now and follow its "Environment" section:
   find my Steam install of Space Engineers 2 and check the .NET SDK and the other tools it
   lists. For anything missing, tell me exactly what to install and offer to do it for me; ask
   before installing anything.
4. When everything is ready, don't stop: ask me what I'd like to prototype, and keep following
   SKILL.md from that folder in this same conversation.
```

Or by hand:

```powershell
git clone https://github.com/KeenSoftwareHouse/Se2PrototypeHubandSkill "$env:USERPROFILE\.claude\prototype-engineers-skill"
& "$env:USERPROFILE\.claude\prototype-engineers-skill\install.ps1"
```

`install.ps1` links the skill into `%USERPROFILE%\.claude\skills\` and installs the helper agents.
If you already have a hand-copied `prototype-engineers` folder there, it skips it; rename or
remove that folder and run it again.

Start a new Claude Code session, then ask for something like "prototype X in SE2". On the first
run the skill finds your Steam install; if Steam is somewhere unusual it will ask you where.

## Update

The skill checks for updates at the start of each session and offers to pull. To update by hand:

```powershell
git -C "$env:USERPROFILE\.claude\prototype-engineers-skill" pull
& "$env:USERPROFILE\.claude\prototype-engineers-skill\install.ps1"
```
