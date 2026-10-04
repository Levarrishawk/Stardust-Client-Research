param([Parameter(Mandatory=$true)][string]$Python,[Parameter(Mandatory=$true)][string]$Exe,[Parameter(Mandatory=$true)][string]$Packages,[Parameter(Mandatory=$true)][string]$AssetRoot,[Parameter(Mandatory=$true)][string]$Config,[Parameter(Mandatory=$true)][string]$CachedPackages,[string]$Dotnet='dotnet',[string]$RunRoot,[switch]$ModesOnly)
$ErrorActionPreference='Stop';$repository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
if(!$RunRoot){$RunRoot=Join-Path $repository ('local-private/round50/replay-'+[Guid]::NewGuid().ToString('N'))}
$RunRoot=[IO.Path]::GetFullPath($RunRoot);$prefix=[IO.Path]::GetFullPath((Join-Path $repository 'local-private'))+[IO.Path]::DirectorySeparatorChar
if(!$RunRoot.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Private output root required.'}
if(Test-Path -LiteralPath $RunRoot){throw 'Fresh run root required.'}
$taskAncestor=[IO.DirectoryInfo]::new($RunRoot).Parent
while($null -ne $taskAncestor){if($taskAncestor.Exists -and ($taskAncestor.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Linked output ancestor rejected.'};$taskAncestor=$taskAncestor.Parent}
New-Item -ItemType Directory -Force (Join-Path $RunRoot 'appdata/NuGet'),(Join-Path $RunRoot 'packages') | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'NuGet.Config') (Join-Path $RunRoot 'appdata/NuGet/NuGet.Config')
$env:APPDATA=Join-Path $RunRoot 'appdata';$env:DOTNET_CLI_HOME=Join-Path $RunRoot 'dotnet-home';$env:NUGET_PACKAGES=Join-Path $RunRoot 'packages';$env:PYTHONUTF8='1';$env:PYTHONDONTWRITEBYTECODE='1';$env:DOTNET_CLI_TELEMETRY_OPTOUT='1';$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false';$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1';$env:DOTNET_NOLOGO='1'
foreach($name in @('bcnencoder.net','communitytoolkit.highperformance')){Copy-Item -LiteralPath (Join-Path $CachedPackages $name) -Destination $env:NUGET_PACKAGES -Recurse}
$oracle=Join-Path $PSScriptRoot 'verify_modes.py'
& $Python $oracle --exe $Exe --packages $Packages --asset-root $AssetRoot --config $Config --out $RunRoot *> (Join-Path $RunRoot 'producer.log')
if($LASTEXITCODE -ne 0){throw 'Independent sampler fixtures failed.'}
& $Dotnet build (Join-Path $PSScriptRoot 'Rendering.csproj') --artifacts-path (Join-Path $RunRoot 'cpu') --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -p:RestoreLockedMode=true -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false --nologo *> (Join-Path $RunRoot 'cpu-build.log')
if($LASTEXITCODE -ne 0){throw 'Actual Core sampler build failed.'}
& $Dotnet (Join-Path $RunRoot 'cpu/bin/Rendering/debug/Rendering.dll') (Join-Path $RunRoot 'mode-fixtures.json') (Join-Path $RunRoot 'mode-native.json') *> (Join-Path $RunRoot 'native.log')
if($LASTEXITCODE -ne 0){throw 'Native sampler/lifecycle failed.'}
& $Python $oracle --exe $Exe --packages $Packages --asset-root $AssetRoot --config $Config --out $RunRoot --native (Join-Path $RunRoot 'mode-native.json') *> (Join-Path $RunRoot 'comparison.log')
if($LASTEXITCODE -ne 0){throw 'Sampler oracle comparison failed.'}
if(!$ModesOnly){
 & (Join-Path $repository 'poc/round49-tests/Rendering/Invoke-Sampler.ps1') -Python $Python -Exe $Exe -Packages $Packages -AssetRoot $AssetRoot -Config $Config -CachedPackages $CachedPackages -Dotnet $Dotnet -RunRoot (Join-Path $RunRoot 'frozen49') *> (Join-Path $RunRoot 'frozen49.log')
 if($LASTEXITCODE -ne 0){throw 'Frozen mode0/kernel/raw/terrain/default smoke failed.'}
 $join=Join-Path $repository 'poc/round49-tests/Integration';$fixture=Join-Path $RunRoot 'join-fixtures.json';$native=Join-Path $RunRoot 'join-native.json'
 & $Python (Join-Path $join 'sampler_consumer_join_peer.py') generate $AssetRoot $Config $Exe $RunRoot $fixture *> (Join-Path $RunRoot 'join-producer.log')
 if($LASTEXITCODE -ne 0){throw 'Frozen432 join fixtures failed.'}
 & $Dotnet build (Join-Path $join 'SamplerConsumerJoin.csproj') --artifacts-path (Join-Path $RunRoot 'join-cpu') --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -p:RestoreLockedMode=true -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false --nologo *> (Join-Path $RunRoot 'join-build.log')
 if($LASTEXITCODE -ne 0){throw 'Frozen432 join build failed.'}
 & $Dotnet (Join-Path $RunRoot 'join-cpu/bin/SamplerConsumerJoin/debug/SamplerConsumerJoin.dll') $fixture $native *> (Join-Path $RunRoot 'join-native.log')
 if($LASTEXITCODE -ne 0){throw 'Frozen432 join failed.'}
 & $Python (Join-Path $join 'sampler_consumer_join_peer.py') verify $AssetRoot $Config $Exe $RunRoot $fixture $native (Join-Path $RunRoot 'join-receipt.json') *> (Join-Path $RunRoot 'join-verify.log')
 if($LASTEXITCODE -ne 0){throw 'Frozen432 join comparison failed.'}
}
Write-Output "PASS RunRoot=$RunRoot ModesOnly=$ModesOnly"
