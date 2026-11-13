# Build -> copy to bin\Deploy -> launch Space Engineers 2 with the plugin.
# The game locks the DLL it loaded, so it always runs a deploy COPY and the build output stays writable.
#   run-game.ps1            build, deploy, launch
#   run-game.ps1 -NoLaunch  build + deploy only (then start the game yourself, e.g. from Steam)
#   run-game.ps1 -NoBuild   deploy + launch the last build
# Game folder: the retail Steam install - this machine's Game2Dir.local.props, else found through
# Steam by tools\find-game.ps1 (then remembered in Game2Dir.local.props).
param([switch]$NoBuild, [switch]$NoLaunch)
$ErrorActionPreference = "Stop"

$repo = $PSScriptRoot
$name = "__NAME__"
$deployDir = Join-Path $repo "bin\Deploy"
$deployed = Join-Path $deployDir "$name.dll"

$localProps = Join-Path $repo "Game2Dir.local.props"

function Resolve-GameDir {
    # 1. this machine's remembered path, 2. find it through Steam.
    if (Test-Path $localProps) {
        $m = [regex]::Match((Get-Content $localProps -Raw), '<Game2Dir>([^<]+)</Game2Dir>')
        if ($m.Success -and (Test-Path (Join-Path $m.Groups[1].Value "Game2.Simulation.dll"))) { return $m.Groups[1].Value }
    }
    $line = & (Join-Path $repo "tools\find-game.ps1") | Select-Object -First 1
    if ($line -notmatch '^Steam\|') { throw "$line" }
    $dir = ($line -split '\|')[1]
    $xml = "<Project><PropertyGroup><Game2Dir>$dir</Game2Dir></PropertyGroup></Project>"
    [IO.File]::WriteAllText($localProps, $xml, [Text.UTF8Encoding]::new($false))
    Write-Host "Found Space Engineers 2 at $dir (saved to Game2Dir.local.props)"
    return $dir
}

$gameDir = Resolve-GameDir
$gameExe = Join-Path $gameDir "SpaceEngineers2.exe"

if (Get-Process SpaceEngineers2 -ErrorAction SilentlyContinue) {
    throw "Space Engineers 2 is running and has the deployed DLL locked. Close the game first."
}

if (-not $NoBuild) {
    dotnet build $repo -c Debug "-p:Game2Dir=$gameDir" --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "Build failed." }
}

$built = Join-Path $repo "bin\Debug\$name.dll"
if (-not (Test-Path $built)) { throw "No build output at $built" }
New-Item -ItemType Directory -Force $deployDir | Out-Null
Copy-Item (Join-Path $repo "bin\Debug\*.dll") $deployDir -Force   # plugin + 0Harmony.dll
Copy-Item (Join-Path $repo "bin\Debug\*.pdb") $deployDir -Force -ErrorAction SilentlyContinue
Write-Host "Deployed: $deployed"
Write-Host "Plugin log: $env:APPDATA\SpaceEngineers2\__SHORT__\"
Write-Host "Game log:   $env:APPDATA\SpaceEngineers2\Temp\Logs\"

if ($NoLaunch) {
    Write-Host "Not launching. Steam Launch Options alternative:  -plugins:`"$deployed`""
    exit 0
}

Write-Host "Launching: $gameExe"
# Quote the argument: a path with spaces is otherwise split by the OS argument parser.
# Launching the exe directly is fine: the game asks Steam to relaunch it and -plugins: survives.
Start-Process $gameExe -WorkingDirectory $gameDir -ArgumentList "`"-plugins:$deployed`""
