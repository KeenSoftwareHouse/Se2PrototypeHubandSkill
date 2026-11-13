# Shows what happened in the last game run, for the plugin with the given short name.
#   logs.ps1 -Short AutoDock [-Lines 80] [-Since 30]
# 1. Tail of the newest plugin log(s) in %APPDATA%\SpaceEngineers2\<Short>\ (the game starts two
#    processes, so the last two files written in the window are shown).
# 2. Plugin-loader lines, exceptions and crashes from the newest game log(s)
#    in %APPDATA%\SpaceEngineers2\Temp\Logs\.
param(
    [Parameter(Mandatory)][string]$Short,
    [int]$Lines = 80,
    [int]$Since = 30   # minutes
)
$ErrorActionPreference = "SilentlyContinue"
$cut = (Get-Date).AddMinutes(-$Since)

$pluginDir = Join-Path $env:APPDATA "SpaceEngineers2\$Short"
$pl = Get-ChildItem $pluginDir -Filter *.log | Where-Object LastWriteTime -gt $cut | Sort-Object LastWriteTime -Descending | Select-Object -First 2
if (-not $pl) { Write-Output "== No plugin log in the last $Since min under $pluginDir (plugin never constructed? check the game log below)" }
foreach ($f in $pl) {
    Write-Output "== PLUGIN $($f.FullName)  ($($f.LastWriteTime))"
    Get-Content $f.FullName -Tail $Lines
}

$gameLogs = Get-ChildItem (Join-Path $env:APPDATA "SpaceEngineers2\Temp\Logs") -Filter *.log |
    Where-Object { $_.LastWriteTime -gt $cut -and $_.Name -notmatch '_Stats' } | Sort-Object LastWriteTime -Descending | Select-Object -First 2
foreach ($g in $gameLogs) {
    Write-Output "== GAME $($g.FullName)  ($($g.LastWriteTime))"
    Select-String -Path $g.FullName -Pattern ('Plugin|Harmony|Exception|Unhandled|DEVICE_REMOVED|Device removed|Crash|FATAL|' + [regex]::Escape($Short)) |
        Select-Object -Last $Lines | ForEach-Object { "{0,6}: {1}" -f $_.LineNumber, $_.Line.Trim() }
}
