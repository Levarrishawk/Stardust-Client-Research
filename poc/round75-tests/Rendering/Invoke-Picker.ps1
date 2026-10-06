param([Parameter(Mandatory=$true)][string]$Python,[Parameter(Mandatory=$true)][string]$Exe,[Parameter(Mandatory=$true)][string]$CachedPackages,[string]$Dotnet='dotnet',[string]$RunRoot)
$ErrorActionPreference='Stop';$repository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
if(!$RunRoot){$RunRoot=Join-Path $repository ('local-private/round75/replay-'+[Guid]::NewGuid().ToString('N'))}
$RunRoot=[IO.Path]::GetFullPath($RunRoot);$prefix=[IO.Path]::GetFullPath((Join-Path $repository 'local-private'))+[IO.Path]::DirectorySeparatorChar
if(!$RunRoot.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Private output root required.'}
$taskOutput=Get-Item -LiteralPath $RunRoot -Force -ErrorAction SilentlyContinue
if($null -ne $taskOutput){throw 'Fresh run root required.'}
$taskLexical=$RunRoot
while(![string]::IsNullOrEmpty($taskLexical)){
    $taskEntry=Get-Item -LiteralPath $taskLexical -Force -ErrorAction SilentlyContinue
    if($null -ne $taskEntry){
        if($taskEntry.Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Linked output ancestor rejected.'}
        if(!$taskEntry.PSIsContainer){throw 'Non-directory output ancestor rejected.'}
    }
    $taskLexical=[IO.Path]::GetDirectoryName($taskLexical)
}
if((Get-FileHash -Algorithm SHA256 -LiteralPath $Exe).Hash -ne 'E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150'){throw 'Executable identity rejected before output/model/build.'}
New-Item -ItemType Directory -Force (Join-Path $RunRoot 'appdata/NuGet'),(Join-Path $RunRoot 'packages') | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'NuGet.Config') (Join-Path $RunRoot 'appdata/NuGet/NuGet.Config')
$env:APPDATA=Join-Path $RunRoot 'appdata';$env:DOTNET_CLI_HOME=Join-Path $RunRoot 'dotnet-home';$env:NUGET_PACKAGES=Join-Path $RunRoot 'packages';$env:PYTHONUTF8='1';$env:PYTHONDONTWRITEBYTECODE='1';$env:DOTNET_CLI_TELEMETRY_OPTOUT='1';$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false';$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1';$env:DOTNET_NOLOGO='1'
foreach($name in @('bcnencoder.net','communitytoolkit.highperformance','godot.net.sdk','godot.sourcegenerators','godotsharp','godotsharpeditor')){Copy-Item -LiteralPath (Join-Path $CachedPackages $name) -Destination $env:NUGET_PACKAGES -Recurse}
$oracle=Join-Path $PSScriptRoot 'verify_picker.py'
& $Python $oracle --exe $Exe --out $RunRoot *> (Join-Path $RunRoot 'producer.log')
if($LASTEXITCODE -ne 0){throw 'Independent picker fixture generation failed.'}
$project=Join-Path $RunRoot 'project';New-Item -ItemType Directory $project | Out-Null
foreach($name in @('Picker.csproj','project.godot','Picker.tscn','NuGet.Config')){Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $project}
foreach($name in @('TerrainOfflineSessionInputHost.cs','TerrainOfflineSessionInputHostVerification.cs','TerrainOfflinePlaygroundHud.cs')){Copy-Item -LiteralPath (Join-Path $repository ('poc/Stardust.Godot/'+$name)) -Destination $project}
foreach($name in @('TerrainCameraOrbitHost.cs','TerrainOfflinePlaygroundHudVerification.cs')){Copy-Item -LiteralPath (Join-Path $RunRoot $name) -Destination $project}
& $Dotnet build (Join-Path $project 'Picker.csproj') --artifacts-path (Join-Path $RunRoot 'artifacts') --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -p:RepositoryRoot=$repository -p:NuGetAudit=false -p:UseSharedCompilation=false --nologo *> (Join-Path $RunRoot 'build.log')
if($LASTEXITCODE -ne 0){throw 'Actual Godot input host build failed.'}
$binary=Join-Path $project '.godot/mono/temp/bin/Debug';New-Item -ItemType Directory -Force $binary | Out-Null
if(!(Test-Path -LiteralPath (Join-Path $binary 'Picker.dll'))){throw 'Godot private assembly missing.'}
$godot='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/godot-4.7.2-dotnet/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe'
& $godot --headless --editor --path $project --import *> (Join-Path $RunRoot 'import.log')
if($LASTEXITCODE -ne 0){throw 'Godot picker import failed.'}
& $godot --headless --path $project -- --fixtures (Join-Path $RunRoot 'input-fixtures.json') --repository $repository --out (Join-Path $RunRoot 'headless.json') *> (Join-Path $RunRoot 'headless.log')
if($LASTEXITCODE -ne 0){throw 'Headless input host verification failed.'}
& $Python $oracle --exe $Exe --out $RunRoot --native (Join-Path $RunRoot 'headless.json') *> (Join-Path $RunRoot 'headless-comparison.log')
if($LASTEXITCODE -ne 0){throw 'Independent headless picker comparison failed.'}
& $godot --path $project --rendering-method gl_compatibility -- --fixtures (Join-Path $RunRoot 'input-fixtures.json') --repository $repository --out (Join-Path $RunRoot 'gpu.json') *> (Join-Path $RunRoot 'gpu.log')
if($LASTEXITCODE -ne 0){throw 'GPU input host verification failed.'}
& $Python $oracle --exe $Exe --out $RunRoot --native (Join-Path $RunRoot 'gpu.json') *> (Join-Path $RunRoot 'gpu-comparison.log')
if($LASTEXITCODE -ne 0){throw 'Independent GPU picker comparison failed.'}
$regressionScene=Join-Path $project 'Regression.tscn'
[IO.File]::WriteAllText($regressionScene,([IO.File]::ReadAllText((Join-Path $project 'Picker.tscn')).Replace('TerrainOfflineSessionInputHostVerification','TerrainOfflinePlaygroundHudVerification')),[Text.UTF8Encoding]::new($false))
& $godot --headless --path $project Regression.tscn -- --fixtures (Join-Path $RunRoot 'regression-fixtures.json') --repository $repository --out (Join-Path $RunRoot 'regression.json') *> (Join-Path $RunRoot 'regression.log')
if($LASTEXITCODE -ne 0){throw 'Default HUD regression failed.'}
& $Python $oracle --exe $Exe --out $RunRoot --native (Join-Path $RunRoot 'regression.json') *> (Join-Path $RunRoot 'regression-comparison.log')
if($LASTEXITCODE -ne 0){throw 'Independent default HUD regression failed.'}
Write-Output "PASS RunRoot=$RunRoot"
