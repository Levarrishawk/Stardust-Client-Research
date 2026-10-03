[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$GodotPath,[Parameter(Mandatory=$true)][string]$ArchiveRoot,[Parameter(Mandatory=$true)][string]$Configuration,[switch]$Gpu,[string]$CapturePrefix)
$ErrorActionPreference='Stop'
$taskWorkspace=(Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$taskPrivate=Join-Path $taskWorkspace 'local-private\round29';New-Item -ItemType Directory -Force $taskPrivate | Out-Null
$taskGodot=(Resolve-Path -LiteralPath $GodotPath).Path;$taskRoot=(Resolve-Path -LiteralPath $ArchiveRoot).Path;$taskConfig=(Resolve-Path -LiteralPath $Configuration).Path
if((Get-FileHash -LiteralPath $taskConfig -Algorithm SHA256).Hash -ne '22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'){throw 'Configuration identity mismatch'}
if($CapturePrefix -and -not $Gpu){throw 'Capture requires GPU'}
$taskScene=Join-Path $PSScriptRoot 'SceneStaticSwitchTexturePreview.tscn';$taskLog=Join-Path $taskPrivate ('switch-'+[Guid]::NewGuid().ToString('N')+'.log')
$taskArgs=@('--path',(Join-Path $taskWorkspace 'poc\Stardust.Godot'),'--log-file',$taskLog,$taskScene)
if(-not $Gpu){$taskArgs+='--headless'}
$taskArgs+=@('--','--asset-root',$taskRoot,'--asset-config',$taskConfig)
if($CapturePrefix){$taskArgs+=@('--capture',[IO.Path]::GetFullPath($CapturePrefix))}
if($taskArgs.Where({$_ -match '["\r\n]'}).Count){throw 'Quotes/newlines unsupported'}
$taskOld=$env:APPDATA
try{$env:APPDATA=Join-Path $taskPrivate 'appdata';$taskProcess=Start-Process -FilePath $taskGodot -ArgumentList ($taskArgs.ForEach({'"'+$_+'"'})) -WindowStyle Hidden -WorkingDirectory $taskWorkspace -PassThru}
finally{if($null -eq $taskOld){Remove-Item Env:APPDATA -ErrorAction SilentlyContinue}else{$env:APPDATA=$taskOld}}
if(-not $taskProcess.WaitForExit(45000)){Stop-Process -Id $taskProcess.Id;throw '45-second preview deadline'}
if($taskProcess.ExitCode -ne 0){Get-Content -LiteralPath $taskLog -Tail 25;throw "Preview failed. Log: $taskLog"}
if(-not (Select-String -LiteralPath $taskLog -SimpleMatch 'PASS: switch visual teardown')){Get-Content -LiteralPath $taskLog -Tail 25;throw 'Acceptance marker absent'}
Write-Output "Log: $taskLog"
