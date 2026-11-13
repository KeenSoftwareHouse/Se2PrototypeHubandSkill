# Installs every skill in this repo into Claude Code: links %USERPROFILE%\.claude\skills\<name> to
# the folder here (a directory junction, no admin needed), then installs the helper agents.
# Safe to re-run after every `git pull`. Never deletes or overwrites a real folder.
$ErrorActionPreference = 'Stop'
$skills = Join-Path $env:USERPROFILE '.claude\skills'
New-Item -ItemType Directory -Force $skills | Out-Null

foreach ($src in Get-ChildItem $PSScriptRoot -Directory | Where-Object { Test-Path (Join-Path $_.FullName 'SKILL.md') }) {
    $link = Join-Path $skills $src.Name
    $existing = Get-Item $link -ErrorAction SilentlyContinue
    if ($existing -and $existing.LinkType) {
        if (@($existing.Target)[0] -eq $src.FullName) { "$($src.Name): already linked"; continue }
        # A junction pointing elsewhere (an older clone): repoint it. Removing a junction leaves its target alone.
        $existing.Delete()
    } elseif ($existing) {
        "$($src.Name): SKIPPED - $link is a real folder (a hand-copied install). Rename or remove it, then re-run."
        continue
    }
    New-Item -ItemType Junction -Path $link -Target $src.FullName | Out-Null
    "$($src.Name): linked -> $($src.FullName)"
}

& (Join-Path $PSScriptRoot 'prototype-engineers\scripts\install-agents.ps1')
"Start a new Claude Code session to pick up the skill."
