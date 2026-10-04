param([Parameter(Mandatory=$true)][string]$Python,[Parameter(Mandatory=$true)][string]$CachedPackages,[Parameter(Mandatory=$true)][string]$Exe,[Parameter(Mandatory=$true)][string]$Packages,[Parameter(Mandatory=$true)][string]$AssetRoot,[Parameter(Mandatory=$true)][string]$Config,[string]$RunRoot,[string]$Dotnet='dotnet')
$ErrorActionPreference='Stop'
$repository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
if(!$RunRoot){$RunRoot=Join-Path $repository ('local-private/round48/replay-'+[Guid]::NewGuid().ToString('N'))}
$RunRoot=[IO.Path]::GetFullPath($RunRoot)
$prefix=[IO.Path]::GetFullPath((Join-Path $repository 'local-private'))+[IO.Path]::DirectorySeparatorChar
if(!$RunRoot.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Private output root required.'}
if(Test-Path -LiteralPath $RunRoot){throw 'Fresh output root required.'}
# Check existing lexical ancestors before creating the private output directory.
$taskAncestor=[IO.DirectoryInfo]::new($RunRoot).Parent
while($null -ne $taskAncestor){if($taskAncestor.Exists -and ($taskAncestor.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Linked output ancestor rejected.'};$taskAncestor=$taskAncestor.Parent}
New-Item -ItemType Directory -Force (Join-Path $RunRoot 'appdata/NuGet'),(Join-Path $RunRoot 'packages') | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'NuGet.Config') -Destination (Join-Path $RunRoot 'appdata/NuGet/NuGet.Config')
$env:APPDATA=Join-Path $RunRoot 'appdata';$env:DOTNET_CLI_HOME=Join-Path $RunRoot 'dotnet-home';$env:NUGET_PACKAGES=Join-Path $RunRoot 'packages'
$env:PYTHONUTF8='1';$env:PYTHONDONTWRITEBYTECODE='1';$env:DOTNET_CLI_TELEMETRY_OPTOUT='1';$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1';$env:DOTNET_NOLOGO='1'
foreach($taskName in @('bcnencoder.net','communitytoolkit.highperformance')){Copy-Item -LiteralPath (Join-Path $CachedPackages $taskName) -Destination $env:NUGET_PACKAGES -Recurse}
$fixture=Join-Path $RunRoot 'fixtures.json';$native=Join-Path $RunRoot 'native.json'
& $Python (Join-Path $PSScriptRoot 'peer.py') generate $Exe $Packages $AssetRoot $Config $fixture
if($LASTEXITCODE -ne 0){throw 'Independent fixture generation failed.'}
& $Dotnet build (Join-Path $PSScriptRoot 'Assets.csproj') --artifacts-path (Join-Path $RunRoot 'cpu') --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -p:RestoreLockedMode=true -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false --nologo *> (Join-Path $RunRoot 'build.log')
if($LASTEXITCODE -ne 0){throw 'Native build failed.'}
& $Dotnet (Join-Path $RunRoot 'cpu/bin/Assets/debug/Assets.dll') $fixture $native *> (Join-Path $RunRoot 'native.log')
if($LASTEXITCODE -ne 0){throw 'Native raw/U64 scenarios failed.'}
& $Python (Join-Path $PSScriptRoot 'peer.py') verify $fixture $native *> (Join-Path $RunRoot 'comparison.log')
if($LASTEXITCODE -ne 0){throw 'Independent comparison failed.'}
& (Join-Path $repository 'poc/round47-tests/Assets/Invoke-RetainedFractal.ps1') -Python $Python -Exe $Exe -Packages $Packages -AssetRoot $AssetRoot -Config $Config -CachedPackages $CachedPackages -RunRoot (Join-Path $RunRoot 'frozen') -Dotnet $Dotnet *> (Join-Path $RunRoot 'frozen.log')
if($LASTEXITCODE -ne 0){throw 'Frozen consumer regression failed.'}
$join=Join-Path $repository 'poc/round47-tests/Integration'
& $Python (Join-Path $join 'retained_fractal_join_peer.py') generate (Join-Path $RunRoot 'join.json')
if($LASTEXITCODE -ne 0){throw 'Frozen join fixture failed.'}
& $Dotnet build (Join-Path $join 'RetainedFractalJoin.csproj') --artifacts-path (Join-Path $RunRoot 'join-cpu') --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -p:RestoreLockedMode=true -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false --nologo *> (Join-Path $RunRoot 'join-build.log')
if($LASTEXITCODE -ne 0){throw 'Frozen join build failed.'}
& $Dotnet (Join-Path $RunRoot 'join-cpu/bin/RetainedFractalJoin/debug/RetainedFractalJoin.dll') (Join-Path $RunRoot 'join.json') (Join-Path $RunRoot 'join-native.json')
if($LASTEXITCODE -ne 0){throw 'Frozen join execution failed.'}
& $Python (Join-Path $join 'retained_fractal_join_peer.py') verify (Join-Path $RunRoot 'join.json') (Join-Path $RunRoot 'join-native.json') (Join-Path $RunRoot 'join-receipt.json') *> (Join-Path $RunRoot 'join-verify.log')
if($LASTEXITCODE -ne 0){throw 'Frozen join comparison failed.'}
Write-Output "PASS RunRoot=$RunRoot"
