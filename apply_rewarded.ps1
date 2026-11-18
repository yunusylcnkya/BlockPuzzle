param([switch]$CheckOnly)
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
if (!(Test-Path '.\Assets\_Project\Scripts\Classic\ClassicGameController.cs')) {
    Write-Host 'ERROR: Extract ZIP CONTENTS into Unity PROJECT ROOT (same level as Assets).'
    exit 1
}
$py = Get-Command py -ErrorAction SilentlyContinue
if ($py) { $cmd = 'py'; $argsList = @('-3', '.\apply_rewarded.py') }
else {
    $py = Get-Command python -ErrorAction SilentlyContinue
    if (!$py) { Write-Host 'ERROR: Python 3 not found; install Python or run manual steps.'; exit 1 }
    $cmd = 'python'; $argsList = @('.\apply_rewarded.py')
}
if ($CheckOnly) { $argsList += '--check-only' }
& $cmd @argsList
exit $LASTEXITCODE
