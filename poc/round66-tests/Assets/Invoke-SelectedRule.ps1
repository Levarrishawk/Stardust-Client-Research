param(
    [Parameter(Mandatory=$true)][string]$Python,
    [Parameter(Mandatory=$true)][string]$Exe,
    [Parameter(Mandatory=$true)][string]$Packages,
    [Parameter(Mandatory=$true)][string]$AssetRoot,
    [Parameter(Mandatory=$true)][string]$Config,
    [string]$RunRoot = ('local-private/round66/replay-' + [Guid]::NewGuid().ToString('N'))
)
$ErrorActionPreference = 'Stop'
$env:PYTHONUTF8 = '1'
$env:PYTHONDONTWRITEBYTECODE = '1'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
Push-Location $taskRoot
try {
    & $Python (Join-Path $PSScriptRoot 'peer.py') --exe $Exe --packages $Packages --asset-root $AssetRoot --config $Config --run-root $RunRoot
    if ($LASTEXITCODE -ne 0) { throw 'Static family-entry accessor replay failed' }
} finally {
    Pop-Location
}
