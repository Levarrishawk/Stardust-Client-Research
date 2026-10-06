param([Parameter(Mandatory=$true)][string]$Python,[Parameter(Mandatory=$true)][string]$CachedPackages,[Parameter(Mandatory=$true)][string]$Godot,[Parameter(Mandatory=$true)][string]$RuntimeFixture,[Parameter(Mandatory=$true)][string]$TerrainFixture,[Parameter(Mandatory=$true)][string]$RunRoot,[string]$Dotnet='dotnet')
$ErrorActionPreference='Stop';$repository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'));$RunRoot=[IO.Path]::GetFullPath($RunRoot)
$prefix=[IO.Path]::GetFullPath((Join-Path $repository 'local-private'))+[IO.Path]::DirectorySeparatorChar
if(!$RunRoot.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Private output required'}
if(Test-Path -LiteralPath $RunRoot){throw 'Fresh output required'}
$taskLexical=$RunRoot
while(![string]::IsNullOrEmpty($taskLexical)){
 $entry=Get-Item -LiteralPath $taskLexical -Force -ErrorAction SilentlyContinue
 if($null -ne $entry){if(($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) -or !$entry.PSIsContainer){throw 'Linked/non-directory output ancestor rejected'}}
 $taskLexical=[IO.Path]::GetDirectoryName($taskLexical)
}
New-Item -ItemType Directory -Force (Join-Path $RunRoot 'appdata/NuGet'),(Join-Path $RunRoot 'packages')|Out-Null
Copy-Item (Join-Path $PSScriptRoot 'NuGet.Config') (Join-Path $RunRoot 'appdata/NuGet/NuGet.Config')
$env:APPDATA=Join-Path $RunRoot 'appdata';$env:DOTNET_CLI_HOME=Join-Path $RunRoot 'dotnet-home';$env:NUGET_PACKAGES=Join-Path $RunRoot 'packages';$env:PYTHONUTF8='1';$env:PYTHONDONTWRITEBYTECODE='1';$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false';$env:DOTNET_CLI_TELEMETRY_OPTOUT='1';$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
foreach($name in @('bcnencoder.net','communitytoolkit.highperformance','godot.net.sdk','godot.sourcegenerators','godotsharp','godotsharpeditor')){Copy-Item -LiteralPath (Join-Path $CachedPackages $name) -Destination $env:NUGET_PACKAGES -Recurse}
$oracle=Join-Path $PSScriptRoot 'restart_peer.py';$fixture=Join-Path $RunRoot 'fixtures.json'
& $Python $oracle generate --runtime $RuntimeFixture --terrain $TerrainFixture --out $fixture *> (Join-Path $RunRoot 'producer.log')
if($LASTEXITCODE){throw 'Before-native producer failed'}
$project=Join-Path $RunRoot 'project';New-Item -ItemType Directory $project|Out-Null
foreach($name in @('Restart.csproj','Restart.tscn','project.godot','NuGet.Config')){Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $project}
foreach($name in @('TerrainOfflineOutcomeHud.cs','TerrainOfflineRestartHud.cs','TerrainOfflineRestartCompositionVerification.cs')){Copy-Item -LiteralPath (Join-Path $repository ('poc/Stardust.Godot/'+$name)) -Destination $project}
& $Dotnet build (Join-Path $project 'Restart.csproj') --artifacts-path (Join-Path $RunRoot 'artifacts') --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -p:RepositoryRoot=$repository -p:NuGetAudit=false -p:UseSharedCompilation=false --nologo *> (Join-Path $RunRoot 'build.log')
if($LASTEXITCODE){Get-Content (Join-Path $RunRoot 'build.log') -Tail 20;throw 'Joined build failed'}
& $Godot --headless --editor --path $project --import *> (Join-Path $RunRoot 'import.log')
if($LASTEXITCODE){throw 'Joined import failed'}
foreach($mode in @('headless','gpu')){
 $arguments=@('--path',$project)
 if($mode -eq 'headless'){$arguments=@('--headless')+$arguments}else{$arguments+=@('--rendering-method','gl_compatibility')}
 $native=Join-Path $RunRoot ($mode+'.json');$arguments+=@('--','--fixtures',$fixture,'--out',$native)
 & $Godot @arguments *> (Join-Path $RunRoot ($mode+'.log'))
 if($LASTEXITCODE){Get-Content (Join-Path $RunRoot ($mode+'.log')) -Tail 20;throw 'Joined native lifecycle failed'}
 & $Python $oracle verify --runtime $RuntimeFixture --terrain $TerrainFixture --native $native --out (Join-Path $RunRoot ($mode+'-receipt.json')) *> (Join-Path $RunRoot ($mode+'-comparison.log'))
 if($LASTEXITCODE){Get-Content (Join-Path $RunRoot ($mode+'-comparison.log')) -Tail 15;throw 'Joined independent comparison failed'}
}
Write-Output "PASS four-generation joined replay $RunRoot"
