[CmdletBinding()]
param(
 [Parameter(Mandatory=$true)][string]$Python,
 [Parameter(Mandatory=$true)][string]$GodotPath,
 [Parameter(Mandatory=$true)][string]$CachedPackages,
 [Parameter(Mandatory=$true)][string]$Scenario,
 [Parameter(Mandatory=$true)][string]$Trajectory,
 [Parameter(Mandatory=$true)][string]$RunRoot,
 [switch]$Gpu,
 [switch]$CloseSignal
)
$ErrorActionPreference='Stop'
$taskRoot=(Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$taskOutput=[IO.Path]::GetFullPath($RunRoot)
$taskPrivate=[IO.Path]::GetFullPath((Join-Path $taskRoot 'local-private'))+[IO.Path]::DirectorySeparatorChar
if(-not $taskOutput.StartsWith($taskPrivate,[StringComparison]::OrdinalIgnoreCase) -or (Test-Path -LiteralPath $taskOutput)){throw 'Fresh ignored run root required'}
New-Item -ItemType Directory -Path $taskOutput | Out-Null
$taskPython=(Resolve-Path -LiteralPath $Python).Path
$taskGodot=(Resolve-Path -LiteralPath $GodotPath).Path
$taskScenario=(Resolve-Path -LiteralPath $Scenario).Path
$env:PYTHONUTF8='1';$env:PYTHONDONTWRITEBYTECODE='1'
$taskPrepare=@("$PSScriptRoot/verify_scenario_playground.py",'prepare','--scenario',$taskScenario,'--trajectory',$Trajectory,'--out',"$taskOutput/expected-before-native.json")
if($CloseSignal){$taskPrepare+='--close'}
& $taskPython @taskPrepare
if($LASTEXITCODE){throw 'Pre-native scenario expectations rejected'}
New-Item -ItemType Directory -Force "$taskOutput/appdata/NuGet" | Out-Null
$taskConfig=Join-Path $taskRoot 'poc/round49-tests/Runtime/Godot/NuGet.Config'
Copy-Item -LiteralPath $taskConfig -Destination "$taskOutput/appdata/NuGet/NuGet.Config"
$env:APPDATA="$taskOutput/appdata";$env:DOTNET_CLI_HOME="$taskOutput/dotnet-home"
$env:NUGET_PACKAGES=(Resolve-Path -LiteralPath $CachedPackages).Path
$env:STARDUST_R49_GODOT_NUPKGS=Join-Path (Split-Path $taskGodot) 'GodotSharp/Tools/nupkgs'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false';$env:DOTNET_CLI_TELEMETRY_OPTOUT='1';$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
& dotnet build "$taskRoot/poc/Stardust.Godot/Stardust.Godot.csproj" --configfile $taskConfig -p:NuGetAudit=false -p:UseSharedCompilation=false -p:TreatWarningsAsErrors=true --nologo *> "$taskOutput/build.log"
if($LASTEXITCODE){Get-Content "$taskOutput/build.log" -Tail 25;throw 'Full offline host build failed'}
$taskNative=@('--path',"$taskRoot/poc/Stardust.Godot",'--log-file',"$taskOutput/native.log","$PSScriptRoot/ScenarioPlayground.tscn")
if(-not $Gpu){$taskNative+='--headless'}
$taskNative+=@('--','--offline-scenario',$taskScenario)
$env:STARDUST_ROUND82_CLOSE_SIGNAL=if($CloseSignal){'1'}else{'0'}
$env:STARDUST_ROUND82_CAPTURE=if($Gpu){"$taskOutput/inspection.png"}else{''}
try { & $taskGodot @taskNative *> "$taskOutput/console.log"; $taskExit=$LASTEXITCODE }
finally { Remove-Item Env:STARDUST_ROUND82_CLOSE_SIGNAL -ErrorAction SilentlyContinue; Remove-Item Env:STARDUST_ROUND82_CAPTURE -ErrorAction SilentlyContinue }
if($taskExit){Get-Content "$taskOutput/console.log" -Tail 30;throw 'Native manifest entry failed'}
& $taskPython "$PSScriptRoot/verify_scenario_playground.py" verify --scenario $taskScenario --trajectory $Trajectory --expected "$taskOutput/expected-before-native.json" --log "$taskOutput/native.log" --out "$taskOutput/receipt.json"
if($LASTEXITCODE){throw 'Independent manifest-entry verification failed'}
