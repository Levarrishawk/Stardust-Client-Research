<# Replacement Godot retained details only. No original executable, server, or effect/audio dispatch. #>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$GodotPath,
    [Parameter(Mandatory=$true)][string]$ArchiveRoot,
    [Parameter(Mandatory=$true)][string]$Configuration,
    [switch]$Headless,
    [switch]$CancelLoad,
    [switch]$Interactive,
    [switch]$Auto,
    [string]$CapturePath,
    [ValidatePattern('^[0-3]$')][string]$SelectedDetail='2'
)
$ErrorActionPreference='Stop'
if($Interactive -and ($Headless -or $CancelLoad)){throw 'Interactive inspection cannot use headless/cancellation fixtures.'}
$taskWorkspace=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$taskGodot=(Resolve-Path -LiteralPath $GodotPath).Path
$taskArchiveRoot=(Resolve-Path -LiteralPath $ArchiveRoot).Path
$taskConfiguration=(Resolve-Path -LiteralPath $Configuration).Path
if((Get-FileHash -LiteralPath $taskConfiguration -Algorithm SHA256).Hash -ne '22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'){throw 'Configuration identity mismatch.'}
if($CapturePath -and ($Interactive -or $Headless -or $CancelLoad)){throw 'Capture is a separate bounded GPU mode.'}
$taskCapture=$null
if($CapturePath){
    $taskCapture=[IO.Path]::GetFullPath($CapturePath)
    $taskPrivatePrefix=[IO.Path]::GetFullPath((Join-Path $taskWorkspace 'local-private'))+[IO.Path]::DirectorySeparatorChar
    if(-not $taskCapture.StartsWith($taskPrivatePrefix,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetExtension($taskCapture) -ne '.png' -or (Test-Path -LiteralPath $taskCapture) -or -not (Test-Path -LiteralPath (Split-Path $taskCapture -Parent))){throw 'Choose a fresh PNG in an existing checkout local-private directory.'}
}
$taskPrivate=Join-Path $taskWorkspace 'local-private\round5\retained-details'
New-Item -ItemType Directory -Force $taskPrivate | Out-Null
$taskAppData=Join-Path $taskPrivate 'appdata'; New-Item -ItemType Directory -Force $taskAppData | Out-Null
$taskLog=Join-Path $taskPrivate ('preview-'+[Guid]::NewGuid().ToString('N')+'.log')
$taskArguments=@('--path',(Join-Path $taskWorkspace 'poc\Stardust.Godot'),'--log-file',$taskLog)
if(-not $Interactive -and -not $CapturePath){$taskArguments+='--headless'}
$taskArguments+=@('--','--retained-detail-preview','--asset-root',$taskArchiveRoot,'--asset-config',$taskConfiguration,'--retained-detail',$SelectedDetail)
if($Auto){$taskArguments+='--retained-auto'}
if($CapturePath){$taskArguments+=@('--retained-capture',$taskCapture)}elseif($CancelLoad){$taskArguments+='--retained-cancel'}elseif(-not $Interactive){$taskArguments+='--retained-self-test'}
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
if($Interactive){Write-Output ('Original retained detail preview running. Detail 0..3 / Auto, pause/resume, scrub/reset, focus gate; manual selection. ProcessId='+$taskProcess.Id); Write-Output ('Log: '+$taskLog); return}
if(-not $taskProcess.WaitForExit(45000)){Stop-Process -Id $taskProcess.Id;throw 'Retained detail fixture exceeded its 45-second deadline.'}
if($taskProcess.ExitCode -ne 0){Get-Content -LiteralPath $taskLog -Tail 24;throw ('Retained detail fixture failed: '+$taskProcess.ExitCode+'. Log: '+$taskLog)}
$taskExpected=if($CapturePath){'PASS: retained GPU capture'}elseif($CancelLoad){'PASS: retained canceled at first lookup'}else{'PASS: retained teardown'}
if(-not (Select-String -LiteralPath $taskLog -SimpleMatch $taskExpected)){throw ('Fixture exited without acceptance marker. Log: '+$taskLog)}
if($CapturePath -and -not (Test-Path -LiteralPath $taskCapture)){throw 'GPU preview exited without its capture.'}
Write-Output 'PASS: bounded original retained detail diagnostic fixture.'
Write-Output ('Log: '+$taskLog)
