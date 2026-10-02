<# Replacement Godot scene static switch textures only. No original executable, server, or effect/audio dispatch. #>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$GodotPath,
    [Parameter(Mandatory=$true)][string]$ArchiveRoot,
    [Parameter(Mandatory=$true)][string]$Configuration,
    [switch]$Headless,
    [switch]$CancelLoad,
    [switch]$Interactive,
    [string]$CapturePath
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
$taskSecondCapture=$null
if($CapturePath){
    $taskCapture=[IO.Path]::GetFullPath($CapturePath)
    $taskSecondCapture=Join-Path ([IO.Path]::GetDirectoryName($taskCapture)) ([IO.Path]::GetFileNameWithoutExtension($taskCapture)+'-frame1.png')
    $taskPrivatePrefix=[IO.Path]::GetFullPath((Join-Path $taskWorkspace 'local-private'))+[IO.Path]::DirectorySeparatorChar
    if(-not $taskCapture.StartsWith($taskPrivatePrefix,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetExtension($taskCapture) -ne '.png' -or (Test-Path -LiteralPath $taskCapture) -or (Test-Path -LiteralPath $taskSecondCapture) -or -not (Test-Path -LiteralPath (Split-Path $taskCapture -Parent))){throw 'Choose a fresh PNG in an existing checkout local-private directory.'}
}
$taskPrivate=Join-Path $taskWorkspace 'local-private\round9\switch-textures'
New-Item -ItemType Directory -Force $taskPrivate | Out-Null
$taskAppData=Join-Path $taskPrivate 'appdata'; New-Item -ItemType Directory -Force $taskAppData | Out-Null
$taskLog=Join-Path $taskPrivate ('preview-'+[Guid]::NewGuid().ToString('N')+'.log')
$taskArguments=@('--path',(Join-Path $taskWorkspace 'poc\Stardust.Godot'),'--log-file',$taskLog)
if(-not $Interactive -and -not $CapturePath){$taskArguments+='--headless'}
$taskArguments+=@('--','--static-switch-texture-preview','--asset-root',$taskArchiveRoot,'--asset-config',$taskConfiguration)
if($CapturePath){$taskArguments+=@('--switch-capture',$taskCapture)}elseif($CancelLoad){$taskArguments+='--switch-cancel'}elseif(-not $Interactive){$taskArguments+='--switch-self-test'}
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
if($Interactive){Write-Output ('Original terminal inspection running. Manual frame0/1 and four serialized LOD choices. ProcessId='+$taskProcess.Id); Write-Output ('Log: '+$taskLog); return}
if(-not $taskProcess.WaitForExit(45000)){Stop-Process -Id $taskProcess.Id;throw 'Static switch texture fixture exceeded its 45-second deadline.'}
if($taskProcess.ExitCode -ne 0){Get-Content -LiteralPath $taskLog -Tail 24;throw ('Static switch texture fixture failed: '+$taskProcess.ExitCode+'. Log: '+$taskLog)}
$taskExpected=if($CapturePath){'PASS: switch GPU capture'}elseif($CancelLoad){'PASS: switch canceled'}else{'PASS: switch teardown'}
if(-not (Select-String -LiteralPath $taskLog -SimpleMatch $taskExpected)){throw ('Fixture exited without acceptance marker. Log: '+$taskLog)}
if($CapturePath -and (-not (Test-Path -LiteralPath $taskCapture) -or -not (Test-Path -LiteralPath $taskSecondCapture))){throw 'GPU preview exited without its capture.'}
Write-Output 'PASS: bounded original static switch texture diagnostic fixture.'
Write-Output ('Log: '+$taskLog)
