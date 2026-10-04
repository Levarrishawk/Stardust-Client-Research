param([Parameter(Mandatory=$true)][string]$Exe,[Parameter(Mandatory=$true)][string]$Packages,[Parameter(Mandatory=$true)][string]$Python,[string]$RunRoot)
$ErrorActionPreference='Stop'
$repository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
if(!$RunRoot){$RunRoot=Join-Path $repository 'local-private/round35/rendering-run'}
$RunRoot=[IO.Path]::GetFullPath($RunRoot)
$prefix=[IO.Path]::GetFullPath((Join-Path $repository 'local-private'))+[IO.Path]::DirectorySeparatorChar
if(!$RunRoot.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'RunRoot must be private.'}
New-Item -ItemType Directory -Path $RunRoot -Force | Out-Null
$env:PYTHONUTF8='1'
& $Python (Join-Path $PSScriptRoot 'verify_runtime_policy.py') --exe $Exe --packages $Packages --receipt (Join-Path $RunRoot 'receipt.json')
if($LASTEXITCODE -ne 0){throw 'Static/oracle verification failed.'}
# A fresh directory makes receipt absence an independent check on each invocation.
$negativeRoot=Join-Path $RunRoot ('identity-'+[Guid]::NewGuid().ToString('N'))
& $Python (Join-Path $PSScriptRoot 'test_identity.py') --exe $Exe --work-root $negativeRoot
if($LASTEXITCODE -ne 0){throw 'Identity/truncation negative cases failed.'}
Write-Output "NEGATIVE_ROOT=$negativeRoot"
