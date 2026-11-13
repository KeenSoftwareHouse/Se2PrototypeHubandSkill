# Fast API discovery over the Space Engineers 2 game assemblies.
#
#   api-search.ps1 -Game2Dir <dir> -Type  <regex>     type full names matching regex (uses a cached index)
#   api-search.ps1 -Game2Dir <dir> -Text  <regex>     which DLLs contain an identifier/string (raw metadata grep)
#   api-search.ps1 -Game2Dir <dir> -Doc   <regex>     search shipped XML doc comments (members + summaries)
#   api-search.ps1 -Game2Dir <dir> -Decompile <Full.Type.Name> [-Dll <name.dll>]
#                                                    decompile one type to C#, prints the .cs path
#
# The type index is built on first use per game version (one ilspycmd call per Keen DLL, ~1-2 min)
# and cached under %LOCALAPPDATA%\prototype-engineers\api-cache\<version>\. Decompiled types are cached
# there too, so re-reading a type is free. Needs ilspycmd: dotnet tool install -g ilspycmd
param(
    [Parameter(Mandatory)][string]$Game2Dir,
    [string]$Type,
    [string]$Text,
    [string]$Doc,
    [string]$Decompile,
    [string]$Dll,
    [int]$Max = 80
)
$ErrorActionPreference = "Stop"

if (-not (Test-Path (Join-Path $Game2Dir "Game2.Simulation.dll"))) { throw "Not a Game2 folder: $Game2Dir" }
$version = (Get-Item (Join-Path $Game2Dir "Game2.Simulation.dll")).VersionInfo.FileVersion
$cache = Join-Path $env:LOCALAPPDATA "prototype-engineers\api-cache\$version"
$typesDir = Join-Path $cache "types"
$decompDir = Join-Path $cache "decompiled"
New-Item -ItemType Directory -Force $typesDir, $decompDir | Out-Null

# Keen-authored assemblies only; third-party DLLs (Avalonia, SharpDX, ...) are noise for this.
$keenDlls = Get-ChildItem $Game2Dir -Filter *.dll -File |
    Where-Object { $_.Name -match '^(VRage|Game2|SpaceEngineers2|Keen)' }

function Get-Ilspy {
    $cmd = Get-Command ilspycmd -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $p = Join-Path $env:USERPROFILE ".dotnet\tools\ilspycmd.exe"
    if (Test-Path $p) { return $p }
    throw "ilspycmd not found. Install it: dotnet tool install -g ilspycmd"
}

function Ensure-Index {
    $missing = $keenDlls | Where-Object { -not (Test-Path (Join-Path $typesDir "$($_.Name).txt")) }
    if (-not $missing) { return }
    $ilspy = Get-Ilspy
    Write-Host "Building type index for $version ($($missing.Count) assemblies, one-off)..." -ForegroundColor DarkGray
    # Run several ilspycmd processes at once; each lists classes, interfaces, structs, enums, delegates.
    $jobs = @()
    foreach ($d in $missing) {
        while (@($jobs | Where-Object { -not $_.HasExited }).Count -ge 6) { Start-Sleep -Milliseconds 200 }
        $out = Join-Path $typesDir "$($d.Name).txt"
        $jobs += Start-Process $ilspy -ArgumentList "`"$($d.FullName)`" -l cisde" -NoNewWindow -PassThru `
            -RedirectStandardOutput $out -RedirectStandardError "$out.err"
    }
    $jobs | ForEach-Object { $_.WaitForExit() }
    Get-ChildItem $typesDir -Filter *.err | Remove-Item -Force
}

if ($Type) {
    Ensure-Index
    $hits = foreach ($f in Get-ChildItem $typesDir -Filter *.txt) {
        $dllName = $f.BaseName
        foreach ($line in [IO.File]::ReadLines($f.FullName)) {
            if ($line -notmatch '^(\w+)\s+(\S+)$') { continue }
            $kind = $Matches[1]; $full = $Matches[2]
            if ($full -match $Type) { "{0,-10} {1}   [{2}]" -f $kind, $full, $dllName }
        }
    }
    $hits | Sort-Object | Select-Object -First $Max
    if (@($hits).Count -gt $Max) { Write-Host "... $(@($hits).Count - $Max) more; tighten the regex." -ForegroundColor DarkGray }
    exit 0
}

if ($Text) {
    # Identifiers live in the metadata #Strings heap as UTF-8, so an ASCII grep over the raw bytes
    # finds member names that are not in any type list (fields, methods, properties, string literals).
    $rx = [regex]::new($Text)
    foreach ($d in $keenDlls) {
        $s = [Text.Encoding]::ASCII.GetString([IO.File]::ReadAllBytes($d.FullName))
        $ms = $rx.Matches($s) | ForEach-Object { $_.Value } | Select-Object -Unique -First 8
        if ($ms) { "{0,-40} {1}" -f $d.Name, ($ms -join ", ") }
    }
    exit 0
}

if ($Doc) {
    # Shipped *.xml doc files: <member name="M:Ns.Type.Method(args)"><summary>...</summary>
    $rx = [regex]::new($Doc, 'IgnoreCase')
    $n = 0
    foreach ($x in Get-ChildItem $Game2Dir -Filter *.xml -File | Where-Object { $_.Name -match '^(VRage|Game2|SpaceEngineers2)' }) {
        try { [xml]$xml = Get-Content $x.FullName -Raw } catch { continue }
        foreach ($m in $xml.doc.members.member) {
            $node = $m.SelectSingleNode('summary')
            $summary = if ($node) { $node.InnerXml } else { "" }
            $summary = $summary -replace '<see\s+(?:cref|langword)="(?:\w:)?([^"]+)"\s*/>', '$1' -replace '<[^>]+>', '' -replace '\s+', ' '
            $summary = $summary.Trim()
            if ($rx.IsMatch($m.name) -or $rx.IsMatch($summary)) {
                if ($summary.Length -gt 160) { $summary = $summary.Substring(0, 160) + "..." }
                "$($m.name)`n    $summary   [$($x.BaseName)]"
                if (++$n -ge $Max) { Write-Host "... more; tighten the regex." -ForegroundColor DarkGray; exit 0 }
            }
        }
    }
    exit 0
}

if ($Decompile) {
    $outFile = Join-Path $decompDir "$Decompile.cs"
    if (Test-Path $outFile) { Write-Output $outFile; exit 0 }
    if (-not $Dll) {
        Ensure-Index
        # Nested types are listed as Outer.Inner; decompile the outermost declaring type.
        $hit = Get-ChildItem $typesDir -Filter *.txt | Where-Object {
            Select-String -Path $_.FullName -Pattern ("\s" + [regex]::Escape($Decompile) + "$") -Quiet
        } | Select-Object -First 1
        if (-not $hit) { throw "Type not found in index: $Decompile (try -Type to find its exact name)" }
        $Dll = $hit.BaseName
    }
    $ilspy = Get-Ilspy
    $tmp = Join-Path $decompDir ("_tmp_" + [guid]::NewGuid().ToString("N"))  # unique: several agents may decompile at once
    if (Test-Path $tmp) { Remove-Item $tmp -Recurse -Force }
    New-Item -ItemType Directory -Force $tmp | Out-Null
    # Start-Process, not &: ilspycmd writes an "update available" note to stderr, which Windows
    # PowerShell turns into a terminating error under ErrorActionPreference=Stop.
    Start-Process $ilspy -ArgumentList "`"$(Join-Path $Game2Dir $Dll)`" -t $Decompile -o `"$tmp`"" -NoNewWindow -Wait `
        -RedirectStandardOutput (Join-Path $tmp "out.txt") -RedirectStandardError (Join-Path $tmp "err.txt")
    $cs = Get-ChildItem $tmp -Filter *.cs -Recurse | Select-Object -First 1
    if (-not $cs) { throw "ilspycmd produced no output for $Decompile in $Dll" }
    try { Move-Item $cs.FullName $outFile -Force } catch { if (-not (Test-Path $outFile)) { throw } }  # another run may have written it first
    Remove-Item $tmp -Recurse -Force
    Write-Output $outFile
    exit 0
}

Write-Host "Specify one of -Type, -Text, -Doc, -Decompile. Cache: $cache"
