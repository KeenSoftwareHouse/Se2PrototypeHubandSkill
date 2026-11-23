# PrototypeEngineers skill (Claude Code)

A Claude Code skill for building playable **Space Engineers 2** gameplay prototypes as in-process
C# game plugins: describe a mechanic, and Claude researches the game code, scaffolds the plugin,
builds it, launches the game with it and iterates with you.

Works with the **retail Steam version** of Space Engineers 2 only.

## Requirements

- Windows, Space Engineers 2 installed through Steam
- [Claude Code](https://claude.com/claude-code)
- .NET SDK 9 or newer
- git (for installing and updates)

## Install

The easy way: paste this into Claude Code and let it do the work.

```text
Install the PrototypeEngineers skill for Space Engineers 2 for me. Clone
https://github.com/KeenSoftwareHouse/Se2PrototypeHubandSkill into
%USERPROFILE%\.claude\prototype-engineers-skill (if that folder is already a clone, run git pull
in it instead), then run install.ps1 from that folder with PowerShell and show me what it printed.
When it is done, remind me to start a new Claude Code session and ask for "prototype X in SE2".
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
