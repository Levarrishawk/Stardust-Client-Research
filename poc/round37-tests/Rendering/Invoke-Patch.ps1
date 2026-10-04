param([Parameter(Mandatory=$true)][string]$Python,[Parameter(Mandatory=$true)][string]$Exe,[Parameter(Mandatory=$true)][string]$StaticPackages,[Parameter(Mandatory=$true)][string]$CachedPackages,[Parameter(Mandatory=$true)][string]$CachedGodotSdkPackages,[Parameter(Mandatory=$true)][string]$GodotPath,[string]$Dotnet='dotnet',[string]$RunRoot)
$ErrorActionPreference='Stop'
$repository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
if(!$RunRoot){$RunRoot=Join-Path $repository ('local-private/round37/replay-'+[Guid]::NewGuid().ToString('N'))}
$RunRoot=[IO.Path]::GetFullPath($RunRoot);$prefix=[IO.Path]::GetFullPath((Join-Path $repository 'local-private'))+[IO.Path]::DirectorySeparatorChar
if(!$RunRoot.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Private RunRoot required.'}
New-Item -ItemType Directory -Force (Join-Path $RunRoot 'appdata/NuGet'),(Join-Path $RunRoot 'packages') | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'NuGet.Config') (Join-Path $RunRoot 'appdata/NuGet/NuGet.Config')
$env:APPDATA=Join-Path $RunRoot 'appdata';$env:DOTNET_CLI_HOME=Join-Path $RunRoot 'dotnet-home';$env:NUGET_PACKAGES=Join-Path $RunRoot 'packages';$env:PYTHONUTF8='1';$env:DOTNET_CLI_TELEMETRY_OPTOUT='1';$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false';$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1';$env:DOTNET_NOLOGO='1'
foreach($name in @('bcnencoder.net','communitytoolkit.highperformance','godot.sourcegenerators','godotsharp','godotsharpeditor')){Copy-Item -LiteralPath (Join-Path $CachedPackages $name) -Destination $env:NUGET_PACKAGES -Recurse}
Copy-Item -LiteralPath (Join-Path $CachedGodotSdkPackages 'godot.net.sdk') -Destination $env:NUGET_PACKAGES -Recurse
$fixtures=Join-Path $RunRoot 'fixtures.json';$native=Join-Path $RunRoot 'native.json';$receipt=Join-Path $RunRoot 'receipt.json'
& $Python (Join-Path $PSScriptRoot 'verify_patch.py') --exe $Exe --packages $StaticPackages --fixtures $fixtures --receipt $receipt
if($LASTEXITCODE -ne 0){throw 'Static/synthetic oracle failed.'}
& $Dotnet build (Join-Path $PSScriptRoot 'Rendering.csproj') --artifacts-path (Join-Path $RunRoot 'cpu') --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -p:RestoreLockedMode=true -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false --nologo *> (Join-Path $RunRoot 'cpu-build.log')
if($LASTEXITCODE -ne 0){throw 'CPU build failed.'}
& $Dotnet (Join-Path $RunRoot 'cpu/bin/Rendering/debug/Rendering.dll') $fixtures $native
if($LASTEXITCODE -ne 0){throw 'CPU scenarios failed.'}
& $Python (Join-Path $PSScriptRoot 'verify_patch.py') --exe $Exe --packages $StaticPackages --fixtures $fixtures --native $native --receipt $receipt
if($LASTEXITCODE -ne 0){throw 'Exact independent oracle failed.'}
& $Dotnet build (Join-Path $repository 'poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj') --artifacts-path (Join-Path $RunRoot 'combined') --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -p:RestoreLockedMode=true -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false --nologo *> (Join-Path $RunRoot 'combined-build.log')
if($LASTEXITCODE -ne 0){throw 'Actual Core/SmokeTests build failed.'}
& $Dotnet build (Join-Path $PSScriptRoot 'CircleHeightProof.csproj') --artifacts-path (Join-Path $RunRoot 'godot') --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false --nologo *> (Join-Path $RunRoot 'godot-build.log')
if($LASTEXITCODE -ne 0){throw 'Standalone Godot build failed.'}
foreach($gpu in @($false,$true)){
 $name=if($gpu){'gpu'}else{'headless'};$log=Join-Path $RunRoot ($name+'.log')
 $arguments=@('--path',$PSScriptRoot,'--log-file',$log,'--quit-after','300')
 if($gpu){$arguments+=@('--position','-32000,-32000')}else{$arguments+='--headless'}
 $arguments+=@('--','--fixtures',$fixtures)
 if($gpu){$arguments+=@('--capture',(Join-Path $RunRoot 'capture.png'))}
 if($arguments.Where({$_ -match '["\r\n]'}).Count){throw 'Quotes/newlines unsupported.'}
 $process=Start-Process -FilePath $GodotPath -ArgumentList ($arguments.ForEach({'"'+$_+'"'})) -WindowStyle Hidden -PassThru
 if(!$process.WaitForExit(45000)){Stop-Process -Id $process.Id;throw '45-second preview deadline.'}
 if($process.ExitCode -ne 0){throw 'Replacement preview failed.'}
 if(!(Select-String -LiteralPath $log -SimpleMatch 'PASS: circle height patch teardown')){throw 'Missing acceptance marker.'}
 $state=$false;$certificates=0
 foreach($line in Get-Content $log){
  if($line.StartsWith('circle_height_patch=')){$state=$true}
  if($line -match '^\s*(ERROR:|WARNING:|SCRIPT ERROR:)'){
   if($state -or $line -ne 'ERROR: Failed to read the root certificate store.' -or $certificates -ne 0){throw "Unexpected engine diagnostic: $line"}
   $certificates++
  }
 }
}
Write-Output "PASS RunRoot=$RunRoot"
