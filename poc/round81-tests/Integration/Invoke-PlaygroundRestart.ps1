[CmdletBinding()]
param(
 [Parameter(Mandatory=$true)][string]$Python,
 [Parameter(Mandatory=$true)][string]$GodotPath,
 [Parameter(Mandatory=$true)][string]$CachedPackages,
 [Parameter(Mandatory=$true)][string]$BaseArguments,
 [Parameter(Mandatory=$true)][string]$NormalProfile,
 [Parameter(Mandatory=$true)][string]$FaultProfile,
 [Parameter(Mandatory=$true)][string]$Terrain,
 [Parameter(Mandatory=$true)][string]$Clips,
 [Parameter(Mandatory=$true)][string]$RunRoot,
 [switch]$Gpu
)
$ErrorActionPreference='Stop'
$taskRoot=(Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$taskOutput=[IO.Path]::GetFullPath($RunRoot)
$taskPrivate=[IO.Path]::GetFullPath((Join-Path $taskRoot 'local-private'))+[IO.Path]::DirectorySeparatorChar
if(-not $taskOutput.StartsWith($taskPrivate,[StringComparison]::OrdinalIgnoreCase) -or (Test-Path -LiteralPath $taskOutput)){throw 'Fresh ignored run root required'}
New-Item -ItemType Directory -Path $taskOutput | Out-Null
$taskPython=(Resolve-Path -LiteralPath $Python).Path
$taskGodot=(Resolve-Path -LiteralPath $GodotPath).Path
$env:PYTHONUTF8='1';$env:PYTHONDONTWRITEBYTECODE='1'
& $taskPython "$PSScriptRoot/playground_restart_peer.py" generate --normal $NormalProfile --fault $FaultProfile --terrain $Terrain --clips $Clips --out "$taskOutput/inputs"
if($LASTEXITCODE){throw 'Independent input/oracle generation failed'}
$taskPlan=Get-Content -LiteralPath "$taskOutput/inputs/plan.json" -Raw | ConvertFrom-Json
$taskArgs=@(Get-Content -LiteralPath $BaseArguments -Raw | ConvertFrom-Json)
$taskIndex=[Array]::IndexOf($taskArgs,'--terrain-point-target-profile')
if($taskIndex -lt 0 -or $taskIndex+1 -ge $taskArgs.Length -or @($taskArgs | Where-Object {$_ -eq '--terrain-point-target-profile'}).Count -ne 1){throw 'One explicit configured profile argument required'}
$taskRuns=@()
foreach($taskProfile in $taskPlan.Profiles){$taskOne=$taskArgs.Clone();$taskOne[$taskIndex+1]=$taskProfile;$taskRuns+=,@($taskOne)}
ConvertTo-Json -InputObject $taskRuns -Depth 6 | Set-Content -LiteralPath "$taskOutput/arguments.json" -Encoding utf8
New-Item -ItemType Directory -Force "$taskOutput/appdata/NuGet" | Out-Null
$taskConfig=Join-Path $taskRoot 'poc/round49-tests/Runtime/Godot/NuGet.Config'
Copy-Item -LiteralPath $taskConfig -Destination "$taskOutput/appdata/NuGet/NuGet.Config"
$env:APPDATA="$taskOutput/appdata";$env:DOTNET_CLI_HOME="$taskOutput/dotnet-home"
$env:NUGET_PACKAGES=(Resolve-Path -LiteralPath $CachedPackages).Path
$env:STARDUST_R49_GODOT_NUPKGS=Join-Path (Split-Path $taskGodot) 'GodotSharp/Tools/nupkgs'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false';$env:DOTNET_CLI_TELEMETRY_OPTOUT='1';$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
& dotnet build "$taskRoot/poc/Stardust.Godot/Stardust.Godot.csproj" --configfile $taskConfig -p:NuGetAudit=false -p:UseSharedCompilation=false -p:TreatWarningsAsErrors=true --nologo *> "$taskOutput/build.log"
if($LASTEXITCODE){Get-Content "$taskOutput/build.log" -Tail 25;throw 'Full offline host build failed'}
$taskNative=@('--path',"$taskRoot/poc/Stardust.Godot",'--log-file',"$taskOutput/native.log","$PSScriptRoot/Playground.tscn")
if(-not $Gpu){$taskNative+='--headless'}
$taskNative+=@('--','--round81-arguments',"$taskOutput/arguments.json")
if($Gpu){$taskNative+=@('--round81-capture-prefix',"$taskOutput/preview")}
& $taskGodot @taskNative *> "$taskOutput/console.log"
if($LASTEXITCODE){Get-Content "$taskOutput/console.log" -Tail 35;throw 'Native full preview restart failed'}
& $taskPython "$PSScriptRoot/playground_restart_peer.py" verify --inputs "$taskOutput/inputs" --terrain $Terrain --log "$taskOutput/native.log" --out "$taskOutput/receipt.json"
if($LASTEXITCODE){throw 'Independent full preview restart verification failed'}
Get-Content -LiteralPath "$taskOutput/receipt.json" -TotalCount 13
