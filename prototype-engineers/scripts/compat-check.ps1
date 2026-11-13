# Checks prototypes against the installed game build WITHOUT launching it. Run it whenever Steam
# updates the game.
#   compat-check.ps1 [-Game2Dir <Game2 folder>] (-Root <folder of SE2* projects> | -Project <dir>)
#   -Game2Dir defaults to the retail Steam install (find-game.ps1).
#
# For each project:
#   1. SHIPPED  - the plugin as built today (bin\Release, else bin\Debug), loaded against the new
#                 build: which features still pass their checks. This is what players who don't
#                 rebuild will get.
#   2. REBUILD  - compiles the project against the new build's DLLs (into a temp folder; the
#                 project's own bin\ is untouched). Compile errors = public API that moved.
#   3. REBUILT  - feature checks on the rebuilt DLL.
# Projects not yet on SE2Core only get step 2.
# Writes a markdown report to %LOCALAPPDATA%\prototype-engineers\compat\<version>.md
param(
    [string]$Game2Dir,
    [string]$Root,
    [string]$Project
)
$ErrorActionPreference = "Stop"
$skill = Split-Path $PSScriptRoot -Parent

if (-not $Game2Dir) {
    $line = & (Join-Path $PSScriptRoot "find-game.ps1") | Select-Object -First 1
    if ($line -notmatch '^Steam\|') { throw "$line" }
    $Game2Dir = ($line -split '\|')[1]
}

if (-not (Test-Path (Join-Path $Game2Dir "Game2.Simulation.dll")) -and (Test-Path (Join-Path $Game2Dir "Game2\Game2.Simulation.dll"))) { $Game2Dir = Join-Path $Game2Dir "Game2" }
if (-not (Test-Path (Join-Path $Game2Dir "Game2.Simulation.dll"))) { throw "Not a Game2 folder: $Game2Dir" }
$Game2Dir = (Resolve-Path $Game2Dir).Path
$version = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $Game2Dir "VRage.Core.dll")).Version.ToString()

$projects = if ($Project) { @(Get-Item $Project) }
            elseif ($Root) { Get-ChildItem $Root -Directory | Where-Object { (Test-Path (Join-Path $_.FullName "package.ps1")) -and (Get-ChildItem $_.FullName -Filter *.csproj -File) } }
            else { throw "Pass -Root or -Project" }

# Build the offline host once (rebuilt when its source changes).
$hostSrc = Join-Path $skill "tools\compat-host"
$hostDir = Join-Path $env:LOCALAPPDATA "prototype-engineers\compat-host"
$hostExe = Join-Path $hostDir "compat-host.exe"
$srcTime = (Get-ChildItem $hostSrc -File | Measure-Object LastWriteTime -Maximum).Maximum
if (-not (Test-Path $hostExe) -or (Get-Item $hostExe).LastWriteTime -lt $srcTime) {
    dotnet build $hostSrc -c Release -o $hostDir --nologo -v q | Out-Null
    if (-not (Test-Path $hostExe)) { throw "Could not build the compat host ($hostSrc)" }
}

function Invoke-Host([string]$dll) {
    if (-not (Test-Path $dll)) { return [pscustomobject]@{ Status = "no build"; Off = @() } }
    $lines = & $hostExe $dll $Game2Dir 2>&1 | ForEach-Object { "$_" }
    $code = $LASTEXITCODE
    if ($code -eq 3) { return [pscustomobject]@{ Status = "not on SE2Core"; Off = @() } }
    if ($code -ne 0) { return [pscustomobject]@{ Status = "check crashed"; Off = @(($lines | Select-Object -Last 1)) } }
    $ok = @($lines | Where-Object { $_ -like "OK|*" })
    $off = @($lines | Where-Object { $_ -like "OFF|*" } | ForEach-Object { $p = $_ -split '\|', 3; "$($p[1]): $($p[2])" })
    $warn = @($lines | Where-Object { $_ -like "WARN|*" } | ForEach-Object { $p = $_ -split '\|', 3; "changed game member $($p[1]) - $($p[2])" })
    $total = $ok.Count + $off.Count
    $status = if ($total -eq 0) { "no features declared" } elseif ($off.Count -eq 0) { "all $total features OK" } else { "$($off.Count) of $total features OFF" }
    if ($warn.Count -gt 0) { $status += ", $($warn.Count) warning(s)"; $off += $warn }
    return [pscustomobject]@{ Status = $status; Off = $off }
}

$tmp = Join-Path $env:TEMP "se2-compat-$version"
$rows = foreach ($p in $projects) {
    $name = $p.Name
    Write-Host "Checking $name ..." -ForegroundColor DarkGray
    $shippedDll = @("bin\Release\$name.dll", "bin\Debug\$name.dll") | ForEach-Object { Join-Path $p.FullName $_ } | Where-Object { Test-Path $_ } | Select-Object -First 1
    $shipped = if ($shippedDll) { Invoke-Host $shippedDll } else { [pscustomobject]@{ Status = "no build"; Off = @() } }

    $out = Join-Path $tmp $name
    $log = & dotnet build $p.FullName -c Release "-p:Game2Dir=$Game2Dir" "-p:OutDir=$out\" "-p:IntermediateOutputPath=obj\compat\" --nologo -v q 2>&1 | ForEach-Object { "$_" }
    $errors = @($log | Where-Object { $_ -match ': error ' } | ForEach-Object { ($_ -replace '^.*?: error ', '') -replace '\s*\[[^\]]+\]$', '' } | Select-Object -Unique)
    if ($errors.Count -gt 0) {
        $rebuild = "FAILS ($($errors.Count) errors)"
        $rebuilt = [pscustomobject]@{ Status = "-"; Off = @() }
    } else {
        $rebuild = "OK"
        $rebuilt = Invoke-Host (Join-Path $out "$name.dll")
    }
    [pscustomobject]@{ Name = $name; Shipped = $shipped.Status; Rebuild = $rebuild; Rebuilt = $rebuilt.Status
                       ShippedOff = $shipped.Off; RebuiltOff = $rebuilt.Off; Errors = $errors }
}

$rows | Format-Table Name, Shipped, Rebuild, Rebuilt -AutoSize | Out-String -Width 200 | Write-Host

# Markdown report with the details.
$reportDir = Join-Path $env:LOCALAPPDATA "prototype-engineers\compat"
New-Item -ItemType Directory -Force $reportDir | Out-Null
$report = Join-Path $reportDir "$version.md"
$md = @("# Compat check: game $version", "", "Game: ``$Game2Dir``  ", "Checked: $(Get-Date -Format 'yyyy-MM-dd HH:mm')", "",
        "| Plugin | Shipped DLL on this build | Rebuild | Rebuilt DLL |", "|---|---|---|---|")
$md += $rows | ForEach-Object { "| $($_.Name) | $($_.Shipped) | $($_.Rebuild) | $($_.Rebuilt) |" }
foreach ($r in $rows | Where-Object { $_.ShippedOff.Count -or $_.RebuiltOff.Count -or $_.Errors.Count }) {
    $md += "", "## $($r.Name)"
    if ($r.ShippedOff.Count) { $md += "", "Shipped DLL, features off:"; $md += $r.ShippedOff | ForEach-Object { "- $_" } }
    if ($r.Errors.Count) { $md += "", "Rebuild errors (first 15):"; $md += $r.Errors | Select-Object -First 15 | ForEach-Object { "- ``$_``" } }
    if ($r.RebuiltOff.Count) { $md += "", "Rebuilt DLL, features off:"; $md += $r.RebuiltOff | ForEach-Object { "- $_" } }
}
[IO.File]::WriteAllLines($report, $md, [Text.UTF8Encoding]::new($false))
Write-Host "Report: $report"
# Report written; the result is in the table, not the exit code (a failed project build must not fail the run).
exit 0
