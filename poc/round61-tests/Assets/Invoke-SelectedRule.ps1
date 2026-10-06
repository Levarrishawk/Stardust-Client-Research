param([Parameter(Mandatory=$true)][string]$Python,[Parameter(Mandatory=$true)][string]$CachedPackages,[Parameter(Mandatory=$true)][string]$Exe,[Parameter(Mandatory=$true)][string]$Packages,[Parameter(Mandatory=$true)][string]$AssetRoot,[Parameter(Mandatory=$true)][string]$Config,[string]$RunRoot,[string]$Dotnet='dotnet')
$ErrorActionPreference='Stop'
$repository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
if(!$RunRoot){$RunRoot=Join-Path $repository ('local-private/round61/replay-'+[Guid]::NewGuid().ToString('N'))}
$RunRoot=[IO.Path]::GetFullPath($RunRoot)
$prefix=[IO.Path]::GetFullPath((Join-Path $repository 'local-private'))+[IO.Path]::DirectorySeparatorChar
if(!$RunRoot.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Private output root required.'}
if($null -ne (Get-Item -LiteralPath $RunRoot -Force -ErrorAction SilentlyContinue)){throw 'Fresh output root required.'}
$taskAncestor=[IO.DirectoryInfo]::new($RunRoot).Parent
while($null -ne $taskAncestor){$taskItem=Get-Item -LiteralPath $taskAncestor.FullName -Force -ErrorAction SilentlyContinue;if($null -ne $taskItem -and ($taskItem.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Linked output ancestor rejected.'};$taskAncestor=$taskAncestor.Parent}
New-Item -ItemType Directory -Force (Join-Path $RunRoot 'appdata/NuGet'),(Join-Path $RunRoot 'packages') | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'NuGet.Config') -Destination (Join-Path $RunRoot 'appdata/NuGet/NuGet.Config')
$env:APPDATA=Join-Path $RunRoot 'appdata';$env:DOTNET_CLI_HOME=Join-Path $RunRoot 'dotnet-home';$env:NUGET_PACKAGES=Join-Path $RunRoot 'packages'
$env:PYTHONUTF8='1';$env:PYTHONDONTWRITEBYTECODE='1';$env:DOTNET_CLI_TELEMETRY_OPTOUT='1';$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1';$env:DOTNET_NOLOGO='1'
foreach($taskName in @('bcnencoder.net','communitytoolkit.highperformance')){Copy-Item -LiteralPath (Join-Path $CachedPackages $taskName) -Destination $env:NUGET_PACKAGES -Recurse}
function Record-Phase([string]$Name){ [pscustomobject]@{Name=$Name;Utc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json -Compress | Add-Content -LiteralPath (Join-Path $RunRoot 'phases.jsonl') -Encoding utf8 }
Record-Phase 'producer-start'
& $Python (Join-Path $PSScriptRoot 'peer.py') generate $Exe $Packages $AssetRoot $Config $RunRoot *> (Join-Path $RunRoot 'producer.log')
if($LASTEXITCODE -ne 0){throw 'Independent shader-family/original fixture generation failed.'}
Record-Phase 'producer-complete-build-start'
& $Dotnet build (Join-Path $PSScriptRoot 'Assets.csproj') --artifacts-path (Join-Path $RunRoot 'cpu') --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -p:RestoreLockedMode=true -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false --nologo *> (Join-Path $RunRoot 'build.log')
if($LASTEXITCODE -ne 0){throw 'Actual Core shader-family build failed.'}
Record-Phase 'build-complete-native-start'
& $Dotnet (Join-Path $RunRoot 'cpu/bin/Assets/debug/Assets.dll') (Join-Path $RunRoot 'fixtures.json') (Join-Path $RunRoot 'native.json') *> (Join-Path $RunRoot 'native.log')
if($LASTEXITCODE -ne 0){throw 'Native shader-family scenarios failed.'}
Record-Phase 'native-complete-comparison-start'
& $Python (Join-Path $PSScriptRoot 'peer.py') verify $RunRoot *> (Join-Path $RunRoot 'comparison.log')
if($LASTEXITCODE -ne 0){throw 'Independent shader-family comparison failed.'}
Record-Phase 'comparison-complete-default-build-start'
& $Dotnet build (Join-Path $repository 'poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj') --artifacts-path (Join-Path $RunRoot 'default-cpu') --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -p:RestoreLockedMode=true -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false --nologo *> (Join-Path $RunRoot 'default-build.log')
if($LASTEXITCODE -ne 0){throw 'Default Core build failed.'}
Record-Phase 'default-build-complete-smoke-start'
& $Dotnet (Join-Path $RunRoot 'default-cpu/bin/Stardust.Core.SmokeTests/debug/Stardust.Core.SmokeTests.dll') *> (Join-Path $RunRoot 'default-native.log')
if($LASTEXITCODE -ne 0){throw 'Default Core smoke failed.'}
Record-Phase 'default-smoke-complete'
Write-Output "PASS RunRoot=$RunRoot"
