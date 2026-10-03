param(
    [Parameter(Mandatory=$true)][string]$Exe,
    [Parameter(Mandatory=$true)][string]$Packages,
    [Parameter(Mandatory=$true)][string]$Python,
    [string]$Dotnet = 'dotnet',
    [string]$RunRoot
)
$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
if (!$RunRoot) { $RunRoot = Join-Path $repository 'local-private/round31/rendering-run' }
$RunRoot = [IO.Path]::GetFullPath($RunRoot)
$privatePrefix = [IO.Path]::GetFullPath((Join-Path $repository 'local-private')) + [IO.Path]::DirectorySeparatorChar
if (!$RunRoot.StartsWith($privatePrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'RunRoot must be within repository local-private.' }
New-Item -ItemType Directory -Path (Join-Path $RunRoot 'appdata/NuGet') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'NuGet.Config') -Destination (Join-Path $RunRoot 'appdata/NuGet/NuGet.Config')
$env:APPDATA = Join-Path $RunRoot 'appdata'
$env:DOTNET_CLI_HOME = Join-Path $RunRoot 'dotnet-home'
$env:NUGET_PACKAGES = Join-Path $RunRoot 'packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_NOLOGO = '1'
$env:PYTHONUTF8 = '1'
$fixtures = Join-Path $RunRoot 'fixtures.json'
$receipt = Join-Path $RunRoot 'receipt.json'
$native = Join-Path $RunRoot 'native.json'
$verifier = Join-Path $PSScriptRoot 'verify_topology.py'
& $Python $verifier --exe $Exe --packages $Packages --fixtures $fixtures --receipt $receipt 2>&1 | Tee-Object -FilePath (Join-Path $RunRoot 'static.log')
if ($LASTEXITCODE -ne 0) { throw 'Static table audit failed.' }
& $Dotnet build (Join-Path $PSScriptRoot 'Rendering.csproj') --configfile (Join-Path $PSScriptRoot 'NuGet.Config') "-p:BaseIntermediateOutputPath=$(Join-Path $RunRoot 'obj')/" "-p:OutputPath=$(Join-Path $RunRoot 'bin')/" -p:NuGetAudit=false --nologo 2>&1 | Tee-Object -FilePath (Join-Path $RunRoot 'build.log')
if ($LASTEXITCODE -ne 0) { throw 'Native build failed.' }
& $Dotnet (Join-Path $RunRoot 'bin/Rendering.dll') $fixtures $native 2>&1 | Tee-Object -FilePath (Join-Path $RunRoot 'native.log')
if ($LASTEXITCODE -ne 0) { throw 'Native scenarios failed.' }
& $Python $verifier --exe $Exe --packages $Packages --fixtures $fixtures --receipt $receipt --native $native 2>&1 | Tee-Object -FilePath (Join-Path $RunRoot 'oracle.log')
if ($LASTEXITCODE -ne 0) { throw 'Independent native oracle failed.' }
