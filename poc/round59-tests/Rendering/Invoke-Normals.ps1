param([Parameter(Mandatory=$true)][string]$Python,[Parameter(Mandatory=$true)][string]$Exe,[Parameter(Mandatory=$true)][string]$AssetRoot,[Parameter(Mandatory=$true)][string]$Config,[Parameter(Mandatory=$true)][string]$CachedPackages,[Parameter(Mandatory=$true)][string]$CachedSdk,[string]$Dotnet='dotnet',[string]$RunRoot)
$ErrorActionPreference='Stop';$repository=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
if(!$RunRoot){$RunRoot=Join-Path $repository ('local-private/round59/replay-'+[Guid]::NewGuid().ToString('N'))}
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
foreach($name in @('bcnencoder.net','communitytoolkit.highperformance','godot.sourcegenerators','godotsharp','godotsharpeditor')){Copy-Item -LiteralPath (Join-Path $CachedPackages $name) -Destination $env:NUGET_PACKAGES -Recurse}
Copy-Item -LiteralPath $CachedSdk -Destination $env:NUGET_PACKAGES -Recurse
$oracle=Join-Path $PSScriptRoot 'verify_normals.py'
& $Python $oracle --exe $Exe --out $RunRoot *> (Join-Path $RunRoot 'producer.log')
if($LASTEXITCODE -ne 0){throw 'Independent modern height-normal fixtures failed.'}
& $Dotnet build (Join-Path $PSScriptRoot 'Rendering.csproj') --artifacts-path (Join-Path $RunRoot 'cpu') --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -p:RestoreLockedMode=true -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false --nologo *> (Join-Path $RunRoot 'cpu-build.log')
if($LASTEXITCODE -ne 0){throw 'Actual Core modern height-normal build failed.'}
& $Dotnet (Join-Path $RunRoot 'cpu/bin/Rendering/debug/Rendering.dll') (Join-Path $RunRoot 'normal-fixtures.json') (Join-Path $RunRoot 'normal-native.json') *> (Join-Path $RunRoot 'native.log')
if($LASTEXITCODE -ne 0){throw 'Native modern height-normal/ownership failed.'}
& $Python $oracle --exe $Exe --out $RunRoot --native (Join-Path $RunRoot 'normal-native.json') *> (Join-Path $RunRoot 'comparison.log')
if($LASTEXITCODE -ne 0){throw 'Normal Decimal/Fraction comparison failed.'}
& $Dotnet build (Join-Path $repository 'poc/Stardust.Godot/Stardust.Godot.csproj') --artifacts-path (Join-Path $RunRoot 'godot') --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -p:NuGetAudit=false -p:UseSharedCompilation=false --nologo ('-p:OutputPath='+(Join-Path $RunRoot 'godot-output')) *> (Join-Path $RunRoot 'godot-build.log')
if($LASTEXITCODE -ne 0){throw 'Godot normal/render-policy build failed.'}
Write-Output "PASS RunRoot=$RunRoot"
