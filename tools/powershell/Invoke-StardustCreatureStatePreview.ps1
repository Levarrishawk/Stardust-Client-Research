<# Replacement Godot creature states only. No original executable, server, or effect/audio dispatch. #>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$GodotPath,
    [Parameter(Mandatory=$true)][string]$ArchiveRoot,
    [Parameter(Mandatory=$true)][string]$Configuration,
    [switch]$Headless,
    [switch]$CancelLoad,
    [switch]$Interactive,
    [Parameter(Mandatory=$true)][string]$FixturePath,
    [string]$CapturePath
)
$ErrorActionPreference='Stop'
if($Interactive -and ($Headless -or $CancelLoad)){throw 'Interactive inspection cannot use headless/cancellation fixtures.'}
$taskWorkspace=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$taskGodot=(Resolve-Path -LiteralPath $GodotPath).Path
$taskArchiveRoot=(Resolve-Path -LiteralPath $ArchiveRoot).Path
$taskConfiguration=(Resolve-Path -LiteralPath $Configuration).Path
$taskFixture=(Resolve-Path -LiteralPath $FixturePath).Path
if((Get-FileHash -LiteralPath $taskConfiguration -Algorithm SHA256).Hash -ne '22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'){throw 'Configuration identity mismatch.'}
if($CapturePath -and ($Interactive -or $Headless -or $CancelLoad)){throw 'Capture is a separate bounded GPU mode.'}
$taskCapture=$null
if($CapturePath){
    $taskCapture=[IO.Path]::GetFullPath($CapturePath)
    $taskSecond=Join-Path (Split-Path $taskCapture -Parent) ([IO.Path]::GetFileNameWithoutExtension($taskCapture)+'-frame1.png')
    if(Test-Path -LiteralPath $taskSecond){throw 'Second capture already exists.'}
    $taskPrivatePrefix=[IO.Path]::GetFullPath((Join-Path $taskWorkspace 'local-private'))+[IO.Path]::DirectorySeparatorChar
    if(-not $taskCapture.StartsWith($taskPrivatePrefix,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetExtension($taskCapture) -ne '.png' -or (Test-Path -LiteralPath $taskCapture) -or -not (Test-Path -LiteralPath (Split-Path $taskCapture -Parent))){throw 'Choose a fresh PNG in an existing checkout local-private directory.'}
}
$taskPrivate=Join-Path $taskWorkspace 'local-private\round10\state-presentation'
New-Item -ItemType Directory -Force $taskPrivate | Out-Null
$taskAppData=Join-Path $taskPrivate 'appdata'; New-Item -ItemType Directory -Force $taskAppData | Out-Null
$taskLog=Join-Path $taskPrivate ('preview-'+[Guid]::NewGuid().ToString('N')+'.log')
$taskArguments=@('--path',(Join-Path $taskWorkspace 'poc\Stardust.Godot'),'--log-file',$taskLog)
if(-not $Interactive -and -not $CapturePath){$taskArguments+='--headless'}
$taskArguments+=@('--','--creature-state-preview','--asset-root',$taskArchiveRoot,'--asset-config',$taskConfiguration,'--state-fixture',$taskFixture)
if($CapturePath){$taskArguments+=@('--state-capture',$taskCapture)}elseif($CancelLoad){$taskArguments+='--state-cancel'}elseif(-not $Interactive){$taskArguments+='--state-self-test'}
if($taskArguments.Where({$_ -match '["\r\n]'}).Count){throw 'Arguments containing quotes/newlines are unsupported.'}
$taskQuoted=$taskArguments.ForEach({'"'+$_+'"'})
$taskOldAppData=$env:APPDATA
try
{
    $env:APPDATA=$taskAppData
    $taskWindow=if($Interactive){'Normal'}else{'Hidden'}
    $taskProcess=Start-Process -FilePath $taskGodot -ArgumentList $taskQuoted -WindowStyle $taskWindow -WorkingDirectory $taskWorkspace -PassThru
}
finally
{
    if($null -eq $taskOldAppData){Remove-Item Env:APPDATA -ErrorAction SilentlyContinue}else{$env:APPDATA=$taskOldAppData}
}
if($Interactive){Write-Output ('Original creature state running. Height delta, baseline reset, hide; drag to orbit and scroll to zoom. ProcessId='+$taskProcess.Id); Write-Output ('Log: '+$taskLog); return}
if(-not $taskProcess.WaitForExit(45000)){Stop-Process -Id $taskProcess.Id;throw 'Creature state fixture exceeded its 45-second deadline.'}
if($taskProcess.ExitCode -ne 0){Get-Content -LiteralPath $taskLog -Tail 24;throw ('Creature state fixture failed: '+$taskProcess.ExitCode+'. Log: '+$taskLog)}
$taskExpected=if($CapturePath){'PASS: state GPU pair'}elseif($CancelLoad){'PASS: state canceled'}else{'PASS: state teardown'}
if(-not (Select-String -LiteralPath $taskLog -SimpleMatch $taskExpected)){throw ('Fixture exited without acceptance marker. Log: '+$taskLog)}
if(-not $CancelLoad -and -not (Select-String -LiteralPath $taskLog -SimpleMatch 'PASS: state teardown')){throw 'State fixture exited without cleanup marker.'}
if($CapturePath -and (-not (Test-Path -LiteralPath $taskCapture) -or -not (Test-Path -LiteralPath $taskSecond))){throw 'GPU preview exited without its capture pair.'}
Write-Output 'PASS: bounded original creature state diagnostic fixture.'
Write-Output ('Log: '+$taskLog)
