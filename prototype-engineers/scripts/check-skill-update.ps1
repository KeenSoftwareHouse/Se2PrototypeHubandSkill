# Checks whether this skill's git clone is behind the repository it was cloned from.
# Never changes anything.
#   check-skill-update.ps1 [-SkillDir <folder>]    default: the skill this script ships in
# Prints one status line, then details:
#   UP TO DATE | BEHIND <n> (+ the new commits) | LOCAL CHANGES | NOT A CLONE | CHECK FAILED
param([string]$SkillDir = (Join-Path $PSScriptRoot '..'))

$item = Get-Item (Resolve-Path $SkillDir).Path
# install.ps1 links ~/.claude/skills/<name> to the clone; follow the link to reach the repo.
$dir = if ($item.LinkType) { @($item.Target)[0] } else { $item.FullName }
$name = $item.Name

if (-not (Get-Command git -ErrorAction SilentlyContinue)) { "CHECK FAILED: git is not installed"; exit 0 }
$root = git -C $dir rev-parse --show-toplevel 2>$null
if ($LASTEXITCODE -ne 0 -or -not $root) {
    "NOT A CLONE: $name was copied in by hand, so it can't be checked for updates."
    "Install it by cloning its repository and running install.ps1 (see the repository's README) to get updates."
    exit 0
}
$root = $root -replace '/', '\'

$remote = git -C $root remote get-url origin 2>$null
if (-not $remote) { "NOT A CLONE: $root has no 'origin' remote to check against."; exit 0 }
git -C $root fetch --quiet origin 2>$null
if ($LASTEXITCODE -ne 0) {
    "CHECK FAILED: couldn't reach $remote (offline, or no access to the repo). Carry on with the installed version."
    exit 0
}

$branch = git -C $root rev-parse --abbrev-ref HEAD
$behind = [int](git -C $root rev-list --count "HEAD..origin/$branch")
$dirty  = git -C $root status --porcelain

if ($behind -eq 0) {
    if ($dirty) { "LOCAL CHANGES: $root is current with $remote but has uncommitted edits." }
    else { "UP TO DATE: $root matches $remote" }
    exit 0
}

"BEHIND $behind`: $root is $behind commit(s) behind $remote"
git -C $root log --format='  %h %ad %s' --date=short "HEAD..origin/$branch"
if ($dirty) {
    "LOCAL CHANGES: uncommitted edits in the clone; a pull may conflict with them:"
    $dirty | ForEach-Object { "  $_" }
}
"Update with: git -C `"$root`" pull"
"Then run:    `"$root\install.ps1`""
