param([Parameter(Mandatory=$true)][string]$Python,[Parameter(Mandatory=$true)][string]$Exe,[Parameter(Mandatory=$true)][string]$StaticPackages,[Parameter(Mandatory=$true)][string]$CachedPackages,[string]$Dotnet='dotnet',[string]$RunRoot)
$ErrorActionPreference='Stop'
$repository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
if(!$RunRoot){$RunRoot=Join-Path $repository ('local-private/round40/replay-'+[Guid]::NewGuid().ToString('N'))}
$RunRoot=[IO.Path]::GetFullPath($RunRoot);$prefix=[IO.Path]::GetFullPath((Join-Path $repository 'local-private'))+[IO.Path]::DirectorySeparatorChar
if(!$RunRoot.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Private output root required.'}
New-Item -ItemType Directory -Force (Join-Path $RunRoot 'appdata/NuGet'),(Join-Path $RunRoot 'packages') | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'NuGet.Config') (Join-Path $RunRoot 'appdata/NuGet/NuGet.Config')
$env:APPDATA=Join-Path $RunRoot 'appdata';$env:DOTNET_CLI_HOME=Join-Path $RunRoot 'dotnet-home';$env:NUGET_PACKAGES=Join-Path $RunRoot 'packages';$env:PYTHONUTF8='1';$env:DOTNET_CLI_TELEMETRY_OPTOUT='1';$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false';$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1';$env:DOTNET_NOLOGO='1'
$env:PYTHONDONTWRITEBYTECODE='1'
foreach($name in @('bcnencoder.net','communitytoolkit.highperformance')){Copy-Item -LiteralPath (Join-Path $CachedPackages $name) -Destination $env:NUGET_PACKAGES -Recurse}
$fixtures=Join-Path $RunRoot 'fixtures.json';$native=Join-Path $RunRoot 'native.json';$receipt=Join-Path $RunRoot 'receipt.json'
& $Python (Join-Path $PSScriptRoot 'verify_layer.py') --exe $Exe --packages $StaticPackages --fixtures $fixtures --receipt $receipt
if($LASTEXITCODE -ne 0){throw 'Static/oracle generation failed.'}
& $Dotnet build (Join-Path $PSScriptRoot 'Rendering.csproj') --artifacts-path (Join-Path $RunRoot 'cpu') --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -p:RestoreLockedMode=true -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false --nologo *> (Join-Path $RunRoot 'cpu-build.log')
if($LASTEXITCODE -ne 0){throw 'Actual Core-reference runner build failed.'}
& $Dotnet (Join-Path $RunRoot 'cpu/bin/Rendering/debug/Rendering.dll') $fixtures $native
if($LASTEXITCODE -ne 0){throw 'Native public API scenarios failed.'}
& $Python (Join-Path $PSScriptRoot 'verify_layer.py') --exe $Exe --packages $StaticPackages --fixtures $fixtures --native $native --receipt $receipt
if($LASTEXITCODE -ne 0){throw 'Independent exact receipt verification failed.'}
& $Dotnet build (Join-Path $repository 'poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj') --artifacts-path (Join-Path $RunRoot 'combined') --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -p:RestoreLockedMode=true -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false --nologo *> (Join-Path $RunRoot 'combined-build.log')
if($LASTEXITCODE -ne 0){throw 'Actual separate Core/SmokeTests build failed.'}
& $Python (Join-Path $PSScriptRoot 'test_identity.py') --exe $Exe --work-root (Join-Path $RunRoot 'identity')
if($LASTEXITCODE -ne 0){throw 'Identity negatives failed.'}
Write-Output "PASS RunRoot=$RunRoot"

