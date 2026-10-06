param([Parameter(Mandatory=$true)][string]$Python,[Parameter(Mandatory=$true)][string]$Exe,[Parameter(Mandatory=$true)][string]$Packages,[string]$RunRoot=('local-private/round74/replay-'+[Guid]::NewGuid().ToString('N')))
$ErrorActionPreference='Stop'
$env:PYTHONUTF8='1';$env:PYTHONDONTWRITEBYTECODE='1'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
Push-Location $taskRoot
try {
 & $Python (Join-Path $PSScriptRoot 'verify.py') --exe $Exe --packages $Packages --output $RunRoot
 if($LASTEXITCODE -ne 0){throw 'Bounded static physical-track consumer witness failed.'}
} finally {Pop-Location}
