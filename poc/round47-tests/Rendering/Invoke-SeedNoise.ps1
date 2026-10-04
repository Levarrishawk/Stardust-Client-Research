param([Parameter(Mandatory=$true)][string]$Python,[Parameter(Mandatory=$true)][string]$Exe,[Parameter(Mandatory=$true)][string]$Packages,[Parameter(Mandatory=$true)][string]$AssetRoot,[Parameter(Mandatory=$true)][string]$Config,[Parameter(Mandatory=$true)][string]$CachedPackages,[string]$Dotnet='dotnet',[string]$RunRoot)
$ErrorActionPreference='Stop';$repository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
if(!$RunRoot){$RunRoot=Join-Path $repository ('local-private/round47/replay-'+[Guid]::NewGuid().ToString('N'))}
$RunRoot=[IO.Path]::GetFullPath($RunRoot);$prefix=[IO.Path]::GetFullPath((Join-Path $repository 'local-private'))+[IO.Path]::DirectorySeparatorChar
if(!$RunRoot.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Private output root required.'}
if(Test-Path -LiteralPath $RunRoot){throw 'Fresh run root required.'}
New-Item -ItemType Directory -Force (Join-Path $RunRoot 'appdata/NuGet'),(Join-Path $RunRoot 'packages') | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'NuGet.Config') (Join-Path $RunRoot 'appdata/NuGet/NuGet.Config')
$env:APPDATA=Join-Path $RunRoot 'appdata';$env:DOTNET_CLI_HOME=Join-Path $RunRoot 'dotnet-home';$env:NUGET_PACKAGES=Join-Path $RunRoot 'packages';$env:PYTHONUTF8='1';$env:PYTHONDONTWRITEBYTECODE='1';$env:DOTNET_CLI_TELEMETRY_OPTOUT='1';$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false';$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1';$env:DOTNET_NOLOGO='1'
foreach($name in @('bcnencoder.net','communitytoolkit.highperformance')){Copy-Item -LiteralPath (Join-Path $CachedPackages $name) -Destination $env:NUGET_PACKAGES -Recurse}
$tool=Join-Path $repository 'tools/python/verify_terrain_fractal_seed_noise.py'
$frozen=Join-Path $repository 'tools/python/verify_terrain_fractal_sampler.py'
$ffra=Join-Path $repository 'tools/python/verify_terrain_filter_fractal.py'
$ahfr=Join-Path $repository 'tools/python/verify_terrain_height_fractal.py'
& $Python $tool --exe $Exe --packages $Packages --asset-root $AssetRoot --config $Config --run-root $RunRoot
if($LASTEXITCODE -ne 0){throw 'Static/oracle failed.'}
& $Python $frozen --exe $Exe --packages $Packages --run-root $RunRoot
if($LASTEXITCODE -ne 0){throw 'Frozen sampler fixtures failed.'}
& $Python $ffra --exe $Exe --packages $Packages --asset-root $AssetRoot --config $Config --run-root $RunRoot
if($LASTEXITCODE -ne 0){throw 'Frozen FFRA fixtures failed.'}
& $Python $ahfr generate (Join-Path $RunRoot 'height-fractal.json')
if($LASTEXITCODE -ne 0){throw 'Frozen AHFR fixtures failed.'}
& $Dotnet build (Join-Path $PSScriptRoot 'Rendering.csproj') --artifacts-path (Join-Path $RunRoot 'cpu') --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -p:RestoreLockedMode=true -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false --nologo *> (Join-Path $RunRoot 'cpu-build.log')
if($LASTEXITCODE -ne 0){throw 'Actual-Core reference build failed.'}
& $Dotnet (Join-Path $RunRoot 'cpu/bin/Rendering/debug/Rendering.dll') (Join-Path $RunRoot 'seed-fixtures.json') (Join-Path $RunRoot 'sampler-fixtures.json') (Join-Path $RunRoot 'fixtures.json') (Join-Path $RunRoot 'carrier.json') (Join-Path $RunRoot 'filtered.json') (Join-Path $RunRoot 'height-fractal.json') $AssetRoot $Config (Join-Path $RunRoot 'seed-native.json') (Join-Path $RunRoot 'sampler-native.json') (Join-Path $RunRoot 'ffra-native.json') (Join-Path $RunRoot 'ahfr-native.json') *> (Join-Path $RunRoot 'native.log')
if($LASTEXITCODE -ne 0){throw 'Native scenarios failed.'}
& $Python $tool --exe $Exe --packages $Packages --asset-root $AssetRoot --config $Config --run-root $RunRoot --native (Join-Path $RunRoot 'seed-native.json') *> (Join-Path $RunRoot 'seed-verify.log')
if($LASTEXITCODE -ne 0){throw 'Seed/noise comparison failed.'}
& $Python $frozen --exe $Exe --packages $Packages --run-root $RunRoot --native (Join-Path $RunRoot 'sampler-native.json') *> (Join-Path $RunRoot 'sampler-verify.log')
if($LASTEXITCODE -ne 0){throw 'Frozen sampler comparison failed.'}
& $Python $ffra --exe $Exe --packages $Packages --asset-root $AssetRoot --config $Config --run-root $RunRoot --native (Join-Path $RunRoot 'ffra-native.json') *> (Join-Path $RunRoot 'ffra-verify.log')
if($LASTEXITCODE -ne 0){throw 'Frozen FFRA comparison failed.'}
& $Python $ahfr verify (Join-Path $RunRoot 'ahfr-native.json') $Exe $Packages $AssetRoot $Config *> (Join-Path $RunRoot 'ahfr-verify.log')
if($LASTEXITCODE -ne 0){throw 'Frozen AHFR comparison failed.'}
& $Python (Join-Path $PSScriptRoot 'test_identity.py') --exe $Exe --config $Config --work-root (Join-Path $RunRoot 'identity')
if($LASTEXITCODE -ne 0){throw 'Identity negatives failed.'}
Write-Output "PASS RunRoot=$RunRoot"
