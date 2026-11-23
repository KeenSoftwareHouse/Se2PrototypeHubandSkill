# Registers this repo's skills with Google Antigravity (agy CLI, desktop app or IDE): adds this
# folder to the user-level skill registry, %USERPROFILE%\.gemini\config\skills.json. Antigravity
# scans each registered folder one level deep for <name>\SKILL.md, so the skill is read straight
# from here and a later `git pull` updates it in place.
# Safe to re-run. Keeps everything else in skills.json as it is.
$ErrorActionPreference = 'Stop'
$root = (Split-Path $PSScriptRoot -Parent)
$config = Join-Path $env:USERPROFILE '.gemini\config'
$file = Join-Path $config 'skills.json'
New-Item -ItemType Directory -Force $config | Out-Null

$doc = $null
if (Test-Path $file) {
    $text = Get-Content $file -Raw -Encoding utf8
    if ($text -and $text.Trim()) {
        try { $doc = $text | ConvertFrom-Json }
        catch { "SKIPPED - $file is not valid JSON; fix it or move it aside, then re-run."; exit 1 }
    }
}
if (-not $doc) { $doc = [pscustomobject]@{} }

$entries = @()
if ($doc.PSObject.Properties['entries'] -and $doc.entries) { $entries = @($doc.entries) }
$same = { param($p) $p -and ($p.TrimEnd('\', '/') -replace '/', '\') -ieq $root.TrimEnd('\') }
if ($entries | Where-Object { & $same $_.path }) {
    "already registered: $root"
} else {
    $entries += [pscustomobject]@{ path = $root }
    if ($doc.PSObject.Properties['entries']) { $doc.entries = $entries }
    else { $doc | Add-Member -NotePropertyName entries -NotePropertyValue $entries }
    if (Test-Path $file) { Copy-Item $file "$file.bak" -Force }
    $json = ConvertTo-Json -InputObject $doc -Depth 20
    [IO.File]::WriteAllText($file, $json + "`n", (New-Object Text.UTF8Encoding($false)))
    "registered: $root in $file"
}
Get-ChildItem $root -Directory | Where-Object { Test-Path (Join-Path $_.FullName 'SKILL.md') } |
    ForEach-Object { "skill: $($_.Name)" }
"Antigravity lists the skill from your next session."
