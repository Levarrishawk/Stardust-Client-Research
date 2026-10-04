param([Parameter(Mandatory=$true)][string]$Python,[Parameter(Mandatory=$true)][string]$Exe,[Parameter(Mandatory=$true)][string]$Packages,[Parameter(Mandatory=$true)][string]$AssetRoot,[Parameter(Mandatory=$true)][string]$Config,[string]$RunRoot)
$ErrorActionPreference='Stop'
$repository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
if(!$RunRoot){$RunRoot=Join-Path $repository ('local-private/round44/replay-'+[Guid]::NewGuid().ToString('N'))}
$RunRoot=[IO.Path]::GetFullPath($RunRoot);$prefix=[IO.Path]::GetFullPath((Join-Path $repository 'local-private'))+[IO.Path]::DirectorySeparatorChar
if(!$RunRoot.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Private output root required.'}
if(Test-Path -LiteralPath $RunRoot){throw 'Fresh replay root required.'}
New-Item -ItemType Directory -Path $RunRoot | Out-Null
$env:PYTHONUTF8='1';$env:PYTHONDONTWRITEBYTECODE='1'
& $Python (Join-Path $repository 'tools/python/verify_terrain_height_layer_order.py') --exe $Exe --packages $Packages --asset-root $AssetRoot --config $Config --receipt (Join-Path $RunRoot 'receipt.json')
if($LASTEXITCODE -ne 0){throw 'Pinned topology/static verification failed.'}
& $Python (Join-Path $PSScriptRoot 'test_identity.py') --exe $Exe --config $Config --work-root (Join-Path $RunRoot 'identity')
if($LASTEXITCODE -ne 0){throw 'Identity negatives failed.'}
Write-Output "PASS RunRoot=$RunRoot"
