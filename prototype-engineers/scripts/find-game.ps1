# Finds the retail Steam install of Space Engineers 2 (Steam appid 1133870) and prints one line:
#   Steam|<Game2 folder>|<Game2.Simulation.dll version>
# Only the retail game installed through Steam is supported.
#
# First run: the Steam folder is discovered (Windows registry, then Steam's default install
# folders) and remembered in %LOCALAPPDATA%\prototype-engineers\steam-path.txt, so later runs skip
# the search. If Steam can't be found, the script prints "STEAM NOT FOUND" and exits 1: ask the
# user where Steam is installed and run again with -SteamDir "<folder containing steam.exe>".
# From the Steam folder, every library in libraryfolders.vdf is checked (any drive) for the SE2
# app manifest, and the game folder is taken from its "installdir". No drive letter is assumed.
#
# Usage:  powershell -NoProfile -ExecutionPolicy Bypass -File find-game.ps1 [-SteamDir <dir>] [-Rediscover]
#   -SteamDir     use (and remember) this Steam folder
#   -Rediscover   forget the remembered Steam folder and search again
#   -First        accepted for compatibility; there is only ever one result
param([string]$SteamDir, [switch]$Rediscover, [switch]$First)
$ErrorActionPreference = "SilentlyContinue"

$SE2_APPID = "1133870"
$configDir = Join-Path $env:LOCALAPPDATA "prototype-engineers"
$configFile = Join-Path $configDir "steam-path.txt"

function Test-SteamRoot([string]$dir) {
    $dir -and (Test-Path (Join-Path $dir "steamapps"))
}

function Save-SteamRoot([string]$dir) {
    New-Item -ItemType Directory -Force $configDir | Out-Null
    [IO.File]::WriteAllText($configFile, $dir, [Text.UTF8Encoding]::new($false))
}

# 1. Which Steam folder: -SteamDir, else the remembered one, else discover it (first run).
$steamRoot = $null
if ($SteamDir) {
    $SteamDir = $SteamDir.Trim('"').TrimEnd('\', '/')
    if (-not (Test-SteamRoot $SteamDir)) {
        Write-Output "STEAM NOT FOUND: $SteamDir has no steamapps folder. Pass the folder that contains steam.exe."
        exit 1
    }
    $steamRoot = (Resolve-Path $SteamDir).Path
    Save-SteamRoot $steamRoot
}
if (-not $steamRoot -and -not $Rediscover -and (Test-Path $configFile)) {
    $remembered = (Get-Content $configFile -Raw).Trim()
    if (Test-SteamRoot $remembered) { $steamRoot = $remembered }
}
if (-not $steamRoot) {
    $candidates = @()
    foreach ($key in "HKCU:\Software\Valve\Steam", "HKLM:\SOFTWARE\WOW6432Node\Valve\Steam", "HKLM:\SOFTWARE\Valve\Steam") {
        $p = Get-ItemProperty $key
        foreach ($v in @($p.SteamPath, $p.InstallPath)) { if ($v) { $candidates += $v.Replace('/', '\') } }
    }
    $candidates += (Join-Path ${env:ProgramFiles(x86)} "Steam"), (Join-Path $env:ProgramFiles "Steam")
    $steamRoot = $candidates | Where-Object { Test-SteamRoot $_ } | Select-Object -First 1
    if (-not $steamRoot) {
        Write-Output "STEAM NOT FOUND: Steam isn't in the registry or its default folders. Ask the user where Steam is installed, then run find-game.ps1 -SteamDir `"<folder containing steam.exe>`"."
        exit 1
    }
    $steamRoot = (Resolve-Path $steamRoot).Path
    Save-SteamRoot $steamRoot
}

# 2. Every Steam library, as Steam records them.
$libraries = @($steamRoot)
foreach ($vdfName in "steamapps\libraryfolders.vdf", "config\libraryfolders.vdf") {
    $vdf = Join-Path $steamRoot $vdfName
    if (-not (Test-Path $vdf)) { continue }
    foreach ($m in [regex]::Matches((Get-Content $vdf -Raw), '"path"\s+"([^"]+)"')) {
        $libraries += $m.Groups[1].Value.Replace('\\', '\')
    }
}

# 3. The library whose app manifest says Space Engineers 2 is installed there.
foreach ($lib in $libraries | Select-Object -Unique) {
    $manifest = Join-Path $lib "steamapps\appmanifest_$SE2_APPID.acf"
    if (-not (Test-Path $manifest)) { continue }
    $installDir = "SpaceEngineers2"
    $m = [regex]::Match((Get-Content $manifest -Raw), '"installdir"\s+"([^"]+)"')
    if ($m.Success) { $installDir = $m.Groups[1].Value }
    $root = Join-Path $lib "steamapps\common\$installDir"
    foreach ($dir in (Join-Path $root "Game2"), $root) {
        if ((Test-Path (Join-Path $dir "SpaceEngineers2.exe")) -and (Test-Path (Join-Path $dir "Game2.Simulation.dll"))) {
            $full = (Resolve-Path $dir).Path
            $ver = (Get-Item (Join-Path $full "Game2.Simulation.dll")).VersionInfo.FileVersion
            Write-Output "Steam|$full|$ver"
            exit 0
        }
    }
}

Write-Output "GAME NOT FOUND: Space Engineers 2 is not installed in any library of the Steam at $steamRoot. Install it from Steam (appid $SE2_APPID), or if Steam lives elsewhere run find-game.ps1 -SteamDir `"<folder containing steam.exe>`"."
exit 1
