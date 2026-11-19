param([switch]$CheckOnly)
$ErrorActionPreference = "Stop"
Set-Location -LiteralPath $PSScriptRoot
if (-not (Test-Path -LiteralPath '.\apply_polish.py')) { throw 'apply_polish.py bulunamadi. ZIP tamamini proje kokune cikarin.' }
$command = Get-Command py -ErrorAction SilentlyContinue
if ($null -eq $command) { throw 'Python launcher bulunamadi. py -3 --version ile kontrol edin.' }
$argsList = @('-3', '.\apply_polish.py')
if ($CheckOnly) { $argsList += '--check-only' }
& py @argsList
exit $LASTEXITCODE
