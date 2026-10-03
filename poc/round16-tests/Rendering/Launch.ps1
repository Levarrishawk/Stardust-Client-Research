<# Own standalone headless host proof. Existing replacement toolchain only. #>
[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$GodotPath)
$ErrorActionPreference='Stop'
$taskWorkspace=(Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$taskGodot=(Resolve-Path -LiteralPath $GodotPath).Path
$taskPrivate=Join-Path $taskWorkspace 'local-private\round16\host'
New-Item -ItemType Directory -Force $taskPrivate | Out-Null
$taskAppData=Join-Path $taskPrivate 'appdata';New-Item -ItemType Directory -Force $taskAppData | Out-Null
$taskLog=Join-Path $taskPrivate ('preview-'+[Guid]::NewGuid().ToString('N')+'.log')
$taskScene=Join-Path $taskPrivate 'SceneWorldPresentationHost.tscn'
@'
[gd_scene load_steps=2 format=3]
[ext_resource type="Script" path="res://SceneWorldPresentationHostPreview.cs" id="1"]
[node name="SceneWorldPresentationHostPreview" type="Node"]
script = ExtResource("1")
'@ | Set-Content -LiteralPath $taskScene
$taskArguments=@('--path',(Join-Path $taskWorkspace 'poc\Stardust.Godot'),'--headless','--log-file',$taskLog,$taskScene)
if($taskArguments.Where({$_ -match '["\r\n]'}).Count){throw 'Arguments containing quotes/newlines are unsupported.'}
$taskQuoted=$taskArguments.ForEach({'"'+$_+'"'})
$taskOldAppData=$env:APPDATA
try{$env:APPDATA=$taskAppData;$taskProcess=Start-Process -FilePath $taskGodot -ArgumentList $taskQuoted -WindowStyle Hidden -WorkingDirectory $taskWorkspace -PassThru}
finally{if($null -eq $taskOldAppData){Remove-Item Env:APPDATA -ErrorAction SilentlyContinue}else{$env:APPDATA=$taskOldAppData}}
if(-not $taskProcess.WaitForExit(45000)){Stop-Process -Id $taskProcess.Id;throw 'Host proof exceeded its 45-second deadline.'}
if($taskProcess.ExitCode -ne 0){Get-Content -LiteralPath $taskLog -Tail 25;throw ('Host proof failed: '+$taskProcess.ExitCode+'. Log: '+$taskLog)}
if(-not (Select-String -LiteralPath $taskLog -SimpleMatch 'PASS: host proof ')){throw 'Host exited without acceptance marker.'}
Write-Output ('Log: '+$taskLog)
