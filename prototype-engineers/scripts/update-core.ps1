# Refreshes the vendored SE2Core\ folder in every project that has one, from this skill's core\.
# This is how a fix to the shared core reaches all prototypes: edit core\ in the skill once, run
# this, rebuild. Projects without SE2Core\ (not yet on the core) are listed and left alone.
#   update-core.ps1 -Root <folder with SE2* projects> [-Build]
param(
    [Parameter(Mandatory)][string]$Root,
    [switch]$Build
)
$ErrorActionPreference = "Stop"
$core = Join-Path (Split-Path $PSScriptRoot -Parent) "core"
$coreVersion = [regex]::Match([IO.File]::ReadAllText((Join-Path $core "Compat.cs")), 'CoreVersion\s*=\s*"([^"]+)"').Groups[1].Value

foreach ($proj in Get-ChildItem $Root -Directory) {
    if (-not (Get-ChildItem $proj.FullName -Filter *.csproj -File)) { continue }
    $target = Join-Path $proj.FullName "SE2Core"
    if (-not (Test-Path $target)) {
        if (Test-Path (Join-Path $proj.FullName "package.ps1")) { "{0,-22} not on SE2Core (skipped)" -f $proj.Name }
        continue
    }
    $old = [regex]::Match([IO.File]::ReadAllText((Join-Path $target "Compat.cs")), 'CoreVersion\s*=\s*"([^"]+)"').Groups[1].Value
    # Replace the folder wholesale so files removed from the core disappear too.
    Get-ChildItem $target -File | Remove-Item -Force
    Copy-Item (Join-Path $core "*") $target -Recurse -Force
    $status = "core $old -> $coreVersion"
    if ($Build) {
        dotnet build $proj.FullName -c Debug --nologo -v q | Out-Null
        $status += if ($LASTEXITCODE -eq 0) { ", build OK" } else { ", BUILD FAILED" }
    }
    "{0,-22} {1}" -f $proj.Name, $status
}
