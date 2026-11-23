# Registers every skill in this repo with OpenAI Codex (CLI, desktop app or IDE extension):
# links <CODEX_HOME>\skills\<name> to the folder here (a directory junction, no admin needed), so a
# later `git pull` updates the skill in place. CODEX_HOME defaults to %USERPROFILE%\.codex.
#   install-codex.ps1          link (default)
#   install-codex.ps1 -Copy    copy the files instead (re-run after every update)
# Safe to re-run. Never deletes or overwrites a folder it did not create.
param([switch]$Copy)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$codexHome = if ($env:CODEX_HOME) { $env:CODEX_HOME } else { Join-Path $env:USERPROFILE '.codex' }
$skills = Join-Path $codexHome 'skills'
$marker = '.installed-by-prototype-engineers'
New-Item -ItemType Directory -Force $skills | Out-Null

foreach ($src in Get-ChildItem $root -Directory | Where-Object { Test-Path (Join-Path $_.FullName 'SKILL.md') }) {
    $dest = Join-Path $skills $src.Name
    $existing = Get-Item $dest -ErrorAction SilentlyContinue
    if ($existing -and $existing.LinkType) {
        if (-not $Copy -and @($existing.Target)[0] -eq $src.FullName) { "$($src.Name): already linked"; continue }
        $existing.Delete()   # a junction: removing it leaves its target alone
    } elseif ($existing -and -not (Test-Path (Join-Path $dest $marker))) {
        "$($src.Name): SKIPPED - $dest is a folder this installer did not create. Rename or remove it, then re-run."
        continue
    } elseif ($existing) {
        Remove-Item $dest -Recurse -Force   # an earlier -Copy install of ours
    }
    if ($Copy) {
        Copy-Item $src.FullName $dest -Recurse
        Set-Content (Join-Path $dest $marker) "Copied from $($src.FullName). Re-run setup\install-codex.ps1 -Copy after updating."
        "$($src.Name): copied -> $dest"
    } else {
        New-Item -ItemType Junction -Path $dest -Target $src.FullName | Out-Null
        "$($src.Name): linked -> $($src.FullName)"
    }
}
"Codex picks up new skills on your next message."
