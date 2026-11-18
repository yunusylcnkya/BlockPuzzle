# BlockPuzzle v2.0.0-dev.4 installer, Windows PowerShell 5.1 compatible.
# Extract ZIP into Unity project root; close Unity before running this script.
param([switch]$CheckOnly)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$manifestPath = Join-Path $root 'installer\patches.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$changes = @{}
$originals = @{}
$pending = 0
$alreadyApplied = 0
$ordinal = [System.StringComparison]::Ordinal
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

try {
    foreach ($patch in $manifest.patches) {
        $relative = [string]$patch.path
        $full = Join-Path $root $relative
        if (-not [IO.File]::Exists($full)) {
            throw "Unity project file missing: $relative"
        }
        if (-not $changes.ContainsKey($full)) {
            $raw = [IO.File]::ReadAllText($full)
            $originals[$full] = $raw
            $changes[$full] = $raw.Replace("`r`n", "`n")
        }

        $src = [string]$changes[$full]
        $old = ([string]$patch.old).Replace("`r`n", "`n")
        $new = ([string]$patch.new).Replace("`r`n", "`n")
        $i = $src.IndexOf($old, $ordinal)
        if ($src.IndexOf($new, $ordinal) -ge 0) {
            $alreadyApplied++
        }
        elseif ($i -ge 0) {
            if ($src.IndexOf($old, $i + $old.Length, $ordinal) -ge 0) {
                throw "Ambiguous patch: $relative / $($patch.name)"
            }
            $changes[$full] = $src.Remove($i, $old.Length).Insert($i, $new)
            $pending++
        }
        else {
            throw "Preflight failure: $relative / $($patch.name). No files changed."
        }
    }

    Write-Host "Preflight OK: $pending patches pending; $alreadyApplied already applied." -ForegroundColor Green
    if ($CheckOnly -or $pending -eq 0) { exit 0 }

    $backupRoot = Join-Path $root 'Library\BlockPuzzleDev4Backups'
    foreach ($full in $changes.Keys) {
        $raw = [string]$originals[$full]
        $lineFeed = $raw.Replace("`r`n", "`n")
        if ($lineFeed -ceq [string]$changes[$full]) { continue }

        $relative = $full.Substring($root.Length).TrimStart('\','/')
        $backupPath = Join-Path $backupRoot $relative
        $backupDir = Split-Path -Parent $backupPath
        if (-not [IO.Directory]::Exists($backupDir)) {
            [IO.Directory]::CreateDirectory($backupDir) | Out-Null
        }
        if (-not [IO.File]::Exists($backupPath)) {
            [IO.File]::Copy($full, $backupPath)
        }
    }

    foreach ($full in $changes.Keys) {
        $raw = [string]$originals[$full]
        $normalizedOriginal = $raw.Replace("`r`n", "`n")
        $normalizedNew = [string]$changes[$full]
        if ($normalizedOriginal -ceq $normalizedNew) { continue }
        $lineEnding = if ($raw.Contains("`r`n")) { "`r`n" } else { "`n" }
        $output = $normalizedNew.Replace("`n", $lineEnding)
        $tmp = $full + '.dev4.tmp'
        [IO.File]::WriteAllText($tmp, $output, $utf8NoBom)
        try {
            [IO.File]::Replace($tmp, $full, $null)
        }
        catch {
            [IO.File]::Copy($tmp, $full, $true)
            [IO.File]::Delete($tmp)
        }
        Write-Host "Patched: $($full.Substring($root.Length))"
    }

    Write-Host 'Dev.4 integration applied. Existing saves, .meta and catalogs untouched.' -ForegroundColor Green
}
catch {
    Write-Error $_.Exception.Message
    exit 1
}
