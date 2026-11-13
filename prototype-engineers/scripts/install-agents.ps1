# Installs the skill's cheaper helper agents (pe-api-scout, pe-log-reader, pe-build-fixer,
# pe-reviewer) into %USERPROFILE%\.claude\agents so Claude Code can hand research, log reading,
# compile fixes and code review to Haiku/Sonnet. Re-run after updating the skill. Start a new Claude Code session afterwards.
#   install-agents.ps1 [-Check]    -Check only reports what's installed / out of date
param([switch]$Check)

$src = Join-Path $PSScriptRoot '..\agents'
$dst = Join-Path $env:USERPROFILE '.claude\agents'
if (-not $Check) { New-Item -ItemType Directory -Force $dst | Out-Null }

foreach ($f in Get-ChildItem $src -Filter 'pe-*.md') {
    $target = Join-Path $dst $f.Name
    $state = if (-not (Test-Path $target)) { 'missing' }
             elseif ((Get-FileHash $target).Hash -ne (Get-FileHash $f.FullName).Hash) { 'out of date' }
             else { 'up to date' }
    if ($Check -or $state -eq 'up to date') { "$($f.BaseName): $state"; continue }
    Copy-Item $f.FullName $target -Force
    "$($f.BaseName): installed ($state before)"
}
if (-not $Check) { "Done. Start a new Claude Code session to pick up the agents." }
