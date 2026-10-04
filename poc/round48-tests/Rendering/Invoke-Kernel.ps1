param([Parameter(Mandatory=$true)][string]$Python,[Parameter(Mandatory=$true)][string]$Exe,[Parameter(Mandatory=$true)][string]$Packages,[Parameter(Mandatory=$true)][string]$AssetRoot,[Parameter(Mandatory=$true)][string]$Config,[Parameter(Mandatory=$true)][string]$CachedPackages,[string]$Dotnet='dotnet',[string]$RunRoot)
$ErrorActionPreference='Stop';$repository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
if(!$RunRoot){$RunRoot=Join-Path $repository ('local-private/round48/replay-'+[Guid]::NewGuid().ToString('N'))}
$RunRoot=[IO.Path]::GetFullPath($RunRoot);$prefix=[IO.Path]::GetFullPath((Join-Path $repository 'local-private'))+[IO.Path]::DirectorySeparatorChar
if(!$RunRoot.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Private output root required.'}
if(Test-Path -LiteralPath $RunRoot){throw 'Fresh run root required.'}
# Check existing lexical ancestors before creating the private output directory.
$taskAncestor=[IO.DirectoryInfo]::new($RunRoot).Parent
while($null -ne $taskAncestor){if($taskAncestor.Exists -and ($taskAncestor.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Linked output ancestor rejected.'};$taskAncestor=$taskAncestor.Parent}
New-Item -ItemType Directory -Force (Join-Path $RunRoot 'appdata/NuGet'),(Join-Path $RunRoot 'packages') | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'NuGet.Config') (Join-Path $RunRoot 'appdata/NuGet/NuGet.Config')
$env:APPDATA=Join-Path $RunRoot 'appdata';$env:DOTNET_CLI_HOME=Join-Path $RunRoot 'dotnet-home';$env:NUGET_PACKAGES=Join-Path $RunRoot 'packages';$env:PYTHONUTF8='1';$env:PYTHONDONTWRITEBYTECODE='1';$env:DOTNET_CLI_TELEMETRY_OPTOUT='1';$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false';$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1';$env:DOTNET_NOLOGO='1'
foreach($name in @('bcnencoder.net','communitytoolkit.highperformance')){Copy-Item -LiteralPath (Join-Path $CachedPackages $name) -Destination $env:NUGET_PACKAGES -Recurse}
$seed=Join-Path $repository 'tools/python/verify_terrain_fractal_seed_noise.py';$oracle=Join-Path $PSScriptRoot 'verify_kernel.py';$frozen=Join-Path $RunRoot 'frozen47'
& $Python $seed --exe $Exe --packages $Packages --asset-root $AssetRoot --config $Config --run-root $frozen *> (Join-Path $RunRoot 'frozen47-producer.log')
if($LASTEXITCODE -ne 0){throw 'SHA-gated frozen producer failed.'}
& $Python $oracle --frozen-fixtures (Join-Path $frozen 'seed-fixtures.json') --out $RunRoot
if($LASTEXITCODE -ne 0){throw 'Independent fixtures failed.'}
& $Dotnet build (Join-Path $PSScriptRoot 'Rendering.csproj') --artifacts-path (Join-Path $RunRoot 'cpu') --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -p:RestoreLockedMode=true -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false --nologo *> (Join-Path $RunRoot 'cpu-build.log')
if($LASTEXITCODE -ne 0){throw 'Core kernel build failed.'}
& $Dotnet (Join-Path $RunRoot 'cpu/bin/Rendering/debug/Rendering.dll') (Join-Path $RunRoot 'kernel-fixtures.json') (Join-Path $RunRoot 'kernel-native.json')
if($LASTEXITCODE -ne 0){throw 'Native kernel scenarios failed.'}
& $Python $oracle --frozen-fixtures (Join-Path $frozen 'seed-fixtures.json') --out $RunRoot --native (Join-Path $RunRoot 'kernel-native.json')
if($LASTEXITCODE -ne 0){throw 'Independent comparison failed.'}
& $Dotnet build (Join-Path $repository 'poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj') --artifacts-path (Join-Path $RunRoot 'smoke-cpu') --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -p:RestoreLockedMode=true -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false --nologo *> (Join-Path $RunRoot 'smoke-build.log')
if($LASTEXITCODE -ne 0){throw 'Default smoke build failed.'}
& $Dotnet (Join-Path $RunRoot 'smoke-cpu/bin/Stardust.Core.SmokeTests/debug/Stardust.Core.SmokeTests.dll') *> (Join-Path $RunRoot 'smoke.log')
if($LASTEXITCODE -ne 0){throw 'Default smoke failed.'}
& (Join-Path $repository 'poc/round47-tests/Rendering/Invoke-SeedNoise.ps1') -Python $Python -Exe $Exe -Packages $Packages -AssetRoot $AssetRoot -Config $Config -CachedPackages $CachedPackages -Dotnet $Dotnet -RunRoot (Join-Path $RunRoot 'regressions') *> (Join-Path $RunRoot 'regressions.log')
if($LASTEXITCODE -ne 0){throw 'Frozen regressions failed.'}
Write-Output "PASS RunRoot=$RunRoot"
