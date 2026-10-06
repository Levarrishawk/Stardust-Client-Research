param(
    [Parameter(Mandatory=$true)][string]$Python,
    [Parameter(Mandatory=$true)][string]$Exe,
    [Parameter(Mandatory=$true)][string]$Packages,
    [string]$RunRoot = ('local-private/round70/replay-' + [Guid]::NewGuid().ToString('N'))
)
$ErrorActionPreference = 'Stop'
$env:PYTHONUTF8 = '1'
$env:PYTHONDONTWRITEBYTECODE = '1'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
Push-Location $taskRoot
try {
    & $Python (Join-Path $PSScriptRoot 'peer.py') --exe $Exe --packages $Packages --run-root $RunRoot
    if ($LASTEXITCODE -ne 0) { throw 'Bounded export metadata replay failed' }
} finally {
    Pop-Location
}
