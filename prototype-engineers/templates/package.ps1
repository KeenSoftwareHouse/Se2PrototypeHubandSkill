# Builds Release and zips a package a player can run without building anything:
#   __NAME__.dll, 0Harmony.dll, launch.cmd (double-click), launch.ps1, find-game.ps1, README.md, 0Harmony-LICENSE.txt,
#   built-for.txt (the game build it was compiled against - the launcher won't pick an older one)
# Output: dist\__NAME___v<version>.zip
param([string]$Version = "0.1.0")
$ErrorActionPreference = "Stop"
$repo = $PSScriptRoot
$name = "__NAME__"

dotnet build $repo -c Release --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "Release build failed" }

$dist = Join-Path $repo "dist"
$stage = Join-Path $dist "stage"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force $stage | Out-Null

Copy-Item (Join-Path $repo "bin\Release\$name.dll") $stage
Copy-Item (Join-Path $repo "bin\Release\0Harmony.dll") $stage
Copy-Item (Join-Path $repo "tools\launch.cmd") $stage
Copy-Item (Join-Path $repo "tools\launch.ps1") $stage
Copy-Item (Join-Path $repo "tools\find-game.ps1") $stage
Copy-Item (Join-Path $repo "README.md") $stage
# Harmony is MIT: its licence text must travel with 0Harmony.dll.
$harmonyLicense = Join-Path $repo "tools\0Harmony-LICENSE.txt"
if (-not (Test-Path $harmonyLicense)) { throw "tools\0Harmony-LICENSE.txt is missing; it must ship with 0Harmony.dll. Copy it from the skill's templates folder." }
Copy-Item $harmonyLicense $stage

# The game build the DLL was compiled against (this machine's Game2Dir), for the launcher.
$props = Join-Path $repo "Game2Dir.local.props"
if (Test-Path $props) {
    $game2 = ([xml](Get-Content $props -Raw)).Project.PropertyGroup.Game2Dir
    $sim = Join-Path $game2 "Game2.Simulation.dll"
    if (Test-Path $sim) {
        Set-Content (Join-Path $stage "built-for.txt") (Get-Item $sim).VersionInfo.FileVersion -Encoding ascii
    }
}

$zip = Join-Path $dist "${name}_v$Version.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
# Zip with forward-slash entry names: Compress-Archive in Windows PowerShell 5.1 writes
# backslashes, which non-Windows unzip tools treat as part of the file name.
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$stageFull = (Get-Item $stage).FullName
$archive = [IO.Compression.ZipFile]::Open($zip, 'Create')
try {
    foreach ($f in Get-ChildItem $stageFull -Recurse -File) {
        $entry = $f.FullName.Substring($stageFull.Length + 1).Replace('\', '/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $f.FullName, $entry) | Out-Null
    }
} finally { $archive.Dispose() }
Remove-Item $stage -Recurse -Force
Write-Host "Package created: $zip"
