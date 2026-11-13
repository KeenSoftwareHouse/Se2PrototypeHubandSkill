# Starts Space Engineers 2 (retail, Steam) with the __NAME__.dll sitting next to this script.
# A plugin needs the game build it was made for or newer - an older game refuses it. package.ps1
# records that build in built-for.txt; if Steam's install is older, update the game in Steam.
$ErrorActionPreference = "Stop"
$plugin = Join-Path $PSScriptRoot "__NAME__.dll"
if (-not (Test-Path $plugin)) { throw "__NAME__.dll not found next to this script." }
$minVersion = [version]"0.0"
$builtFor = Join-Path $PSScriptRoot "built-for.txt"
if (Test-Path $builtFor) { [void][version]::TryParse((Get-Content $builtFor -Raw).Trim(), [ref]$minVersion) }

# Judge by output, not $LASTEXITCODE: a script that succeeds without calling exit leaves it unset,
# and in a fresh PowerShell an unset (null) exit code is -ne 0.
$line = & (Join-Path $PSScriptRoot "find-game.ps1") | Select-Object -First 1
if ($line -notmatch '^Steam\|') { throw "$line" }
$p = $line -split '\|'
$gameDir = $p[1]
$version = $null
[void][version]::TryParse($p[2], [ref]$version)
if ($version -and $version -lt $minVersion) {
    throw "This plugin needs Space Engineers 2 $minVersion or newer; Steam has $version installed. Update the game in Steam and run again."
}

$gameExe = Join-Path $gameDir "SpaceEngineers2.exe"
Write-Host "Game:   $gameExe ($version)"
Write-Host "Plugin: $plugin"
Write-Host "Log:    $env:APPDATA\SpaceEngineers2\__SHORT__\"
# Launching the exe directly is fine: the game asks Steam to relaunch it and -plugins: survives.
Start-Process $gameExe -WorkingDirectory $gameDir -ArgumentList "`"-plugins:$plugin`""
