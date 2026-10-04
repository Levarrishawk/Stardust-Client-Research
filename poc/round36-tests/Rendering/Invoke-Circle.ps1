param([Parameter(Mandatory=$true)][string]$Exe,[Parameter(Mandatory=$true)][string]$Packages,[Parameter(Mandatory=$true)][string]$Python,[string]$Dotnet='dotnet',[string]$RunRoot)
$ErrorActionPreference='Stop'
$repository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
if(!$RunRoot){$RunRoot=Join-Path $repository 'local-private/round36/rendering-run'}
$RunRoot=[IO.Path]::GetFullPath($RunRoot)
$prefix=[IO.Path]::GetFullPath((Join-Path $repository 'local-private'))+[IO.Path]::DirectorySeparatorChar
if(!$RunRoot.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'RunRoot must be private.'}
New-Item -ItemType Directory -Path (Join-Path $RunRoot 'appdata/NuGet') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'NuGet.Config') -Destination (Join-Path $RunRoot 'appdata/NuGet/NuGet.Config')
$env:APPDATA=Join-Path $RunRoot 'appdata';$env:DOTNET_CLI_HOME=Join-Path $RunRoot 'dotnet-home';$env:NUGET_PACKAGES=Join-Path $RunRoot 'packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1';$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false';$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1';$env:DOTNET_NOLOGO='1';$env:PYTHONUTF8='1'
$fixtures=Join-Path $RunRoot 'fixtures.json';$receipt=Join-Path $RunRoot 'receipt.json';$native=Join-Path $RunRoot 'native.json'
& $Python (Join-Path $PSScriptRoot 'verify_circle.py') --exe $Exe --packages $Packages --fixtures $fixtures --receipt $receipt
if($LASTEXITCODE -ne 0){throw 'Static oracle failed.'}
& $Dotnet build (Join-Path $PSScriptRoot 'Rendering.csproj') --configfile (Join-Path $PSScriptRoot 'NuGet.Config') "-p:BaseIntermediateOutputPath=$(Join-Path $RunRoot 'obj')/" "-p:OutputPath=$(Join-Path $RunRoot 'bin')/" -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false --nologo
if($LASTEXITCODE -ne 0){throw 'Native build failed.'}
& $Dotnet (Join-Path $RunRoot 'bin/Rendering.dll') $fixtures $native
if($LASTEXITCODE -ne 0){throw 'Native scenarios failed.'}
& $Python (Join-Path $PSScriptRoot 'verify_circle.py') --exe $Exe --packages $Packages --fixtures $fixtures --receipt $receipt --native $native
if($LASTEXITCODE -ne 0){throw 'Independent output oracle failed.'}

$negativeRoot=Join-Path $RunRoot ('identity-'+[Guid]::NewGuid().ToString('N'))
& $Python (Join-Path $PSScriptRoot 'test_identity.py') --exe $Exe --work-root $negativeRoot
if($LASTEXITCODE -ne 0){throw 'Identity/truncation negative cases failed.'}
Write-Output "NEGATIVE_ROOT=$negativeRoot"
