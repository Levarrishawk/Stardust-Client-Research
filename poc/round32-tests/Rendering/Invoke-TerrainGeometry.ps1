param(
 [Parameter(Mandatory=$true)][string]$GodotPath,
 [Parameter(Mandatory=$true)][string]$Python,
 [Parameter(Mandatory=$true)][string]$Exe,
 [Parameter(Mandatory=$true)][string]$Packages,
 [string]$Dotnet='dotnet',
 [string]$CachedGodotPackages,
 [string]$RunRoot,
 [switch]$Build,
 [switch]$Gpu,
 [string]$Capture
)
$ErrorActionPreference='Stop'
$repository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
if(!$RunRoot){$RunRoot=Join-Path $repository 'local-private/round32/rendering-run'}
$RunRoot=[IO.Path]::GetFullPath($RunRoot)
$privatePrefix=[IO.Path]::GetFullPath((Join-Path $repository 'local-private'))+[IO.Path]::DirectorySeparatorChar
if(!$RunRoot.StartsWith($privatePrefix,[StringComparison]::OrdinalIgnoreCase)){throw 'RunRoot must be private.'}
New-Item -ItemType Directory -Path (Join-Path $RunRoot 'appdata/NuGet') -Force | Out-Null
$env:APPDATA=Join-Path $RunRoot 'appdata';$env:DOTNET_CLI_HOME=Join-Path $RunRoot 'dotnet-home';$env:NUGET_PACKAGES=Join-Path $RunRoot 'nuget-packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1';$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false';$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1';$env:DOTNET_NOLOGO='1';$env:PYTHONUTF8='1'
$feed=Join-Path $repository 'local-private/toolchain/godot-4.7.2-dotnet/Godot_v4.7.2-stable_mono_win64/GodotSharp/Tools/nupkgs'
if(!(Test-Path -LiteralPath $feed)){throw 'Existing bundled Godot package feed required.'}
[IO.File]::WriteAllText((Join-Path $RunRoot 'appdata/NuGet/NuGet.Config'),"<configuration><packageSources><clear/><add key='offline' value='$feed'/></packageSources></configuration>")
$fixture=Join-Path $RunRoot 'fixtures.json';$receipt=Join-Path $RunRoot 'receipt.json';$native=Join-Path $RunRoot 'native.json'
if($Build){
 if(!$CachedGodotPackages){throw 'Build requires an existing cached Godot package directory.'}
 New-Item -ItemType Directory -Path $env:NUGET_PACKAGES -Force | Out-Null
 foreach($package in @('godot.net.sdk','godot.sourcegenerators','godotsharp','godotsharpeditor')){
  if(!(Test-Path -LiteralPath (Join-Path $env:NUGET_PACKAGES $package))){Copy-Item -LiteralPath (Join-Path $CachedGodotPackages $package) -Destination $env:NUGET_PACKAGES -Recurse}
 }
 & $Python (Join-Path $PSScriptRoot 'verify_geometry.py') --exe $Exe --packages $Packages --fixtures $fixture --receipt $receipt 2>&1 | Tee-Object -FilePath (Join-Path $RunRoot 'static.log')
 if($LASTEXITCODE -ne 0){throw 'Static semantic audit failed.'}
 & $Dotnet build (Join-Path $PSScriptRoot 'Native.csproj') --configfile (Join-Path $PSScriptRoot 'NuGet.Config') "-p:BaseIntermediateOutputPath=$(Join-Path $RunRoot 'native-obj')/" "-p:OutputPath=$(Join-Path $RunRoot 'native-bin')/" -p:NuGetAudit=false --nologo 2>&1 | Tee-Object -FilePath (Join-Path $RunRoot 'native-build.log')
 if($LASTEXITCODE -ne 0){throw 'Native build failed.'}
 & $Dotnet (Join-Path $RunRoot 'native-bin/Native.dll') $fixture $native 2>&1 | Tee-Object -FilePath (Join-Path $RunRoot 'native-run.log')
 if($LASTEXITCODE -ne 0){throw 'Native scenarios failed.'}
 & $Python (Join-Path $PSScriptRoot 'verify_geometry.py') --exe $Exe --packages $Packages --fixtures $fixture --receipt $receipt --native $native 2>&1 | Tee-Object -FilePath (Join-Path $RunRoot 'native-oracle.log')
 if($LASTEXITCODE -ne 0){throw 'Native independent oracle failed.'}
 & $Dotnet build (Join-Path $PSScriptRoot 'TerrainProof.csproj') --configfile (Join-Path $PSScriptRoot 'Godot.NuGet.Config') "-p:BaseIntermediateOutputPath=$(Join-Path $RunRoot 'godot-obj')/" -p:NuGetAudit=false --nologo 2>&1 | Tee-Object -FilePath (Join-Path $RunRoot 'godot-build.log')
 if($LASTEXITCODE -ne 0){throw 'Godot build failed.'}
}
$log=Join-Path $RunRoot ('godot-'+[Guid]::NewGuid().ToString('N')+'.log')
$arguments=@('--path',$PSScriptRoot,'--log-file',$log,'--quit-after','300')
if(!$Gpu){$arguments+='--headless'}else{$arguments+=@('--position','-32000,-32000')}
if($Capture){
 $Capture=[IO.Path]::GetFullPath($Capture)
 if(!$Gpu||!$Capture.StartsWith($privatePrefix,[StringComparison]::OrdinalIgnoreCase)||(Test-Path -LiteralPath $Capture)){throw 'Choose a fresh private GPU capture.'}
 $arguments+=@('--','--capture',$Capture)
}
if($arguments.Where({$_ -match '["\r\n]'}).Count){throw 'Quotes/newlines unsupported.'}
$process=Start-Process -FilePath $GodotPath -ArgumentList ($arguments.ForEach({'"'+$_+'"'})) -WindowStyle Hidden -WorkingDirectory $repository -PassThru
if(!$process.WaitForExit(45000)){Stop-Process -Id $process.Id;throw '45-second proof deadline.'}
if($process.ExitCode -ne 0){Get-Content -LiteralPath $log -Tail 25;throw "Godot proof exit $($process.ExitCode)"}
if(!(Select-String -LiteralPath $log -SimpleMatch 'PASS: terrain geometry teardown')){Get-Content -LiteralPath $log -Tail 25;throw 'Acceptance marker absent.'}
$seenState=$false;$certificateErrors=0
foreach($line in Get-Content -LiteralPath $log){
 if($line.StartsWith('terrain_geometry=')){$seenState=$true}
 if($line -match '^\s*(ERROR:|WARNING:|SCRIPT ERROR:)'){
  if($seenState -or $line -ne 'ERROR: Failed to read the root certificate store.' -or $certificateErrors -ne 0){throw "Unexpected engine diagnostic: $line"}
  $certificateErrors++
 }
}
Write-Output "PASS log=$log"
