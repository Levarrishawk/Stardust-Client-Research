param([Parameter(Mandatory=$true)][string]$Python,[Parameter(Mandatory=$true)][string]$CachedPackages,[Parameter(Mandatory=$true)][string]$Exe,[Parameter(Mandatory=$true)][string]$Packages,[Parameter(Mandatory=$true)][string]$ServerRoot,[Parameter(Mandatory=$true)][string]$AssetRoot,[Parameter(Mandatory=$true)][string]$Config,[string]$Catalog,[string]$Bank,[string]$RunRoot,[string]$Dotnet='dotnet')
$ErrorActionPreference='Stop'
$repository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
if(!$Catalog){$Catalog=Join-Path $repository 'poc/preview-profiles/scene-object-catalog.json'}
if(!$Bank){$Bank=Join-Path $repository 'poc/preview-profiles/human-locomotion.json'}
if(!$RunRoot){$RunRoot=Join-Path $repository ('local-private/round49/replay-'+[Guid]::NewGuid().ToString('N'))}
$RunRoot=[IO.Path]::GetFullPath($RunRoot)
$prefix=[IO.Path]::GetFullPath((Join-Path $repository 'local-private'))+[IO.Path]::DirectorySeparatorChar
if(!$RunRoot.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Private output root required.'}
if(Test-Path -LiteralPath $RunRoot){throw 'Fresh output root required.'}
$taskAncestor=[IO.DirectoryInfo]::new($RunRoot).Parent
while($null -ne $taskAncestor){if($taskAncestor.Exists -and ($taskAncestor.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Linked output ancestor rejected.'};$taskAncestor=$taskAncestor.Parent}
New-Item -ItemType Directory -Force (Join-Path $RunRoot 'appdata/NuGet'),(Join-Path $RunRoot 'packages') | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'NuGet.Config') -Destination (Join-Path $RunRoot 'appdata/NuGet/NuGet.Config')
$env:APPDATA=Join-Path $RunRoot 'appdata';$env:DOTNET_CLI_HOME=Join-Path $RunRoot 'dotnet-home';$env:NUGET_PACKAGES=Join-Path $RunRoot 'packages'
$env:PYTHONUTF8='1';$env:PYTHONDONTWRITEBYTECODE='1';$env:DOTNET_CLI_TELEMETRY_OPTOUT='1';$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1';$env:DOTNET_NOLOGO='1'
foreach($taskName in @('bcnencoder.net','communitytoolkit.highperformance')){Copy-Item -LiteralPath (Join-Path $CachedPackages $taskName) -Destination $env:NUGET_PACKAGES -Recurse}
$oracle=Join-Path $RunRoot 'original-oracle.json';$profile=Join-Path $RunRoot 'profile-fixtures.json';$authored=Join-Path $RunRoot 'authored-fixtures.json';$native=Join-Path $RunRoot 'native.json';$log=Join-Path $RunRoot 'native.log'
& $Python (Join-Path $repository 'tools/python/verify_creature_template_movement.py') --exe $Exe --packages $Packages --server-root $ServerRoot --root $AssetRoot --configuration $Config --receipt $oracle *> (Join-Path $RunRoot 'oracle.log')
if($LASTEXITCODE -ne 0){throw 'Static original oracle failed.'}
& $Python (Join-Path $repository 'poc/round46-tests/Networking/peer.py') generate $profile *> (Join-Path $RunRoot 'profile-generate.log')
if($LASTEXITCODE -ne 0){throw 'Profile fixture generation failed.'}
& $Python (Join-Path $repository 'poc/round47-tests/Networking/peer.py') generate $authored *> (Join-Path $RunRoot 'authored-generate.log')
if($LASTEXITCODE -ne 0){throw 'Authored fixture generation failed.'}
& $Dotnet build (Join-Path $PSScriptRoot 'Assets.csproj') --artifacts-path (Join-Path $RunRoot 'cpu') --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -p:RestoreLockedMode=true -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false --nologo *> (Join-Path $RunRoot 'build.log')
if($LASTEXITCODE -ne 0){throw 'Native build failed.'}
& $Dotnet (Join-Path $RunRoot 'cpu/bin/Assets/debug/Assets.dll') $AssetRoot $Config $Catalog $Bank $native $profile $authored *> $log
if($LASTEXITCODE -ne 0){throw 'Native resource scenarios failed.'}
& $Python (Join-Path $PSScriptRoot 'peer.py') $native $oracle $profile $authored $log $AssetRoot $Config $Bank *> (Join-Path $RunRoot 'comparison.log')
if($LASTEXITCODE -ne 0){throw 'Independent comparison failed.'}
& $Dotnet build (Join-Path $repository 'poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj') --artifacts-path (Join-Path $RunRoot 'default-cpu') --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -p:RestoreLockedMode=true -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false --nologo *> (Join-Path $RunRoot 'default-build.log')
if($LASTEXITCODE -ne 0){throw 'Default smoke build failed.'}
& $Dotnet (Join-Path $RunRoot 'default-cpu/bin/Stardust.Core.SmokeTests/debug/Stardust.Core.SmokeTests.dll') *> (Join-Path $RunRoot 'default-native.log')
if($LASTEXITCODE -ne 0){throw 'Default smoke scenarios failed.'}
Write-Output "PASS RunRoot=$RunRoot"
