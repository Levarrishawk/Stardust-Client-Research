param([Parameter(Mandatory=$true)][string]$Python,[Parameter(Mandatory=$true)][string]$CachedPackages,[string]$AssetRoot,[string]$Config,[string]$RunRoot,[string]$Dotnet='dotnet')
$ErrorActionPreference='Stop'
$repository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
if(!$RunRoot){$RunRoot=Join-Path $repository ('local-private/round88/replay-'+[Guid]::NewGuid().ToString('N'))}
$RunRoot=[IO.Path]::GetFullPath($RunRoot)
$prefix=[IO.Path]::GetFullPath((Join-Path $repository 'local-private'))+[IO.Path]::DirectorySeparatorChar
if(!$RunRoot.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Private output root required.'}
if($null -ne (Get-Item -LiteralPath $RunRoot -Force -ErrorAction SilentlyContinue)){throw 'Fresh output root required.'}
$taskAncestor=[IO.DirectoryInfo]::new($RunRoot).Parent
while($null -ne $taskAncestor){$taskItem=Get-Item -LiteralPath $taskAncestor.FullName -Force -ErrorAction SilentlyContinue;if($null -ne $taskItem -and ($taskItem.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Linked output ancestor rejected.'};$taskAncestor=$taskAncestor.Parent}
New-Item -ItemType Directory -Force (Join-Path $RunRoot 'appdata/NuGet'),(Join-Path $RunRoot 'packages') | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'NuGet.Config') -Destination (Join-Path $RunRoot 'appdata/NuGet/NuGet.Config')
$env:APPDATA=Join-Path $RunRoot 'appdata';$env:DOTNET_CLI_HOME=Join-Path $RunRoot 'dotnet-home';$env:NUGET_PACKAGES=Join-Path $RunRoot 'packages'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false';$env:PYTHONUTF8='1';$env:PYTHONDONTWRITEBYTECODE='1';$env:DOTNET_CLI_TELEMETRY_OPTOUT='1';$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1';$env:DOTNET_NOLOGO='1'
foreach($taskName in @('bcnencoder.net','communitytoolkit.highperformance')){Copy-Item -LiteralPath (Join-Path $CachedPackages $taskName) -Destination $env:NUGET_PACKAGES -Recurse}
$taskProducer=@('generate',(Join-Path $RunRoot 'fixtures'))
if($AssetRoot -or $Config){if(!$AssetRoot -or !$Config){throw 'Both explicit original inputs required.'};$taskProducer+=@('--asset-root',$AssetRoot,'--config',$Config)}
& $Python (Join-Path $PSScriptRoot 'peer.py') @taskProducer *> (Join-Path $RunRoot 'producer.log')
if($LASTEXITCODE -ne 0){throw 'Independent launch preparation fixture producer failed before native.'}
& $Dotnet build (Join-Path $PSScriptRoot 'Assets.csproj') --artifacts-path (Join-Path $RunRoot 'cpu') --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -p:RestoreLockedMode=true -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false --nologo *> (Join-Path $RunRoot 'build.log')
if($LASTEXITCODE -ne 0){throw 'Core installation launch preparation build failed.'}
& $Dotnet (Join-Path $RunRoot 'cpu/bin/Assets/debug/Assets.dll') (Join-Path $RunRoot 'fixtures/fixtures.json') (Join-Path $RunRoot 'fixtures/receipt.json') *> (Join-Path $RunRoot 'native.log')
if($LASTEXITCODE -ne 0){throw 'Native installation launch preparation scenarios failed.'}
& $Python (Join-Path $PSScriptRoot 'peer.py') verify (Join-Path $RunRoot 'fixtures') *> (Join-Path $RunRoot 'comparison.log')
if($LASTEXITCODE -ne 0){throw 'Independent launch preparation comparison failed.'}
& $Dotnet build (Join-Path $repository 'poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj') --artifacts-path (Join-Path $RunRoot 'default-cpu') --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -p:RestoreLockedMode=true -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false --nologo *> (Join-Path $RunRoot 'default-build.log')
if($LASTEXITCODE -ne 0){throw 'Default Core build failed.'}
& $Dotnet (Join-Path $RunRoot 'default-cpu/bin/Stardust.Core.SmokeTests/debug/Stardust.Core.SmokeTests.dll') *> (Join-Path $RunRoot 'default-native.log')
if($LASTEXITCODE -ne 0){throw 'Default Core smoke failed.'}
Write-Output "PASS RunRoot=$RunRoot"
