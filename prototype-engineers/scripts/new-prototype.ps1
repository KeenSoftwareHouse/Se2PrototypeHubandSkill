# Scaffolds a new SE2 prototype plugin project that builds and loads in game with no edits.
#   new-prototype.ps1 -Name SE2AutoDock -ParentDir C:\src [-Game2Dir <...\Game2>] [-NoBuild]
# Name: PascalCase; an "SE2" prefix is recommended. The short name (log folder, class prefix) is
# the name without the SE2 prefix. The game is the retail Steam install (find-game.ps1);
# -Game2Dir must point at that same install if given.
param(
    [Parameter(Mandatory)][ValidatePattern('^[A-Za-z][A-Za-z0-9]+$')][string]$Name,
    [Parameter(Mandatory)][string]$ParentDir,
    [string]$Game2Dir,
    [switch]$NoBuild
)
$ErrorActionPreference = "Stop"
$skill = Split-Path $PSScriptRoot -Parent
$templates = Join-Path $skill "templates"

$short = $Name -replace '^SE2', ''
if (-not $short) { $short = $Name }

if (-not $Game2Dir) {
    $line = & (Join-Path $PSScriptRoot "find-game.ps1") | Select-Object -First 1
    if ($line -notmatch '^Steam\|') { throw "$line" }
    $Game2Dir = ($line -split '\|')[1]
}
if (-not (Test-Path (Join-Path $Game2Dir "Game2.Simulation.dll"))) { throw "Not a Game2 folder: $Game2Dir" }
$gameVersion = (Get-Item (Join-Path $Game2Dir "Game2.Simulation.dll")).VersionInfo.FileVersion

$dir = Join-Path $ParentDir $Name
if (Test-Path $dir) { throw "Already exists: $dir" }
New-Item -ItemType Directory -Force $dir, (Join-Path $dir "tools"), (Join-Path $dir "Game") | Out-Null

$map = [ordered]@{
    "project.csproj" = "$Name.csproj"
    "Plugin.cs"      = "${short}Plugin.cs"
    "Hooks.cs"       = "Game\Hooks.cs"
    "PluginKeys.cs"  = "PluginKeys.cs"
    "run-game.ps1"   = "run-game.ps1"
    "deploy.cmd"     = "deploy.cmd"
    "package.ps1"    = "package.ps1"
    "README.md"      = "README.md"
    "gitignore"      = ".gitignore"
    "launch.ps1"     = "tools\launch.ps1"
    "launch.cmd"     = "tools\launch.cmd"
    "0Harmony-LICENSE.txt" = "tools\0Harmony-LICENSE.txt"
}
$utf8 = [Text.UTF8Encoding]::new($false)
foreach ($k in $map.Keys) {
    $text = [IO.File]::ReadAllText((Join-Path $templates $k))
    $text = $text.Replace("__NAME__", $Name).Replace("__SHORT__", $short).Replace("__GAMEVERSION__", $gameVersion)
    [IO.File]::WriteAllText((Join-Path $dir $map[$k]), $text, $utf8)
}
Copy-Item (Join-Path $PSScriptRoot "find-game.ps1") (Join-Path $dir "tools\find-game.ps1")
# Shared core: vendored copy of the skill's core\ (refresh later with update-core.ps1).
New-Item -ItemType Directory -Force (Join-Path $dir "SE2Core") | Out-Null
Copy-Item (Join-Path $skill "core\*") (Join-Path $dir "SE2Core") -Recurse -Force
# The game path is per machine: remembered in a git-ignored props file, never in the project.
# On another machine the build/run scripts find that machine's Steam install themselves.
[IO.File]::WriteAllText((Join-Path $dir "Game2Dir.local.props"),
    "<Project><PropertyGroup><Game2Dir>$Game2Dir</Game2Dir></PropertyGroup></Project>", $utf8)

Write-Host "Created $dir"
Write-Host "  game: $Game2Dir ($gameVersion)"
Write-Host "  log:  %APPDATA%\SpaceEngineers2\$short\"

if (-not $NoBuild) {
    dotnet build $dir -c Debug --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "Scaffold build failed; see errors above." }
    Write-Host "Build OK."
}
