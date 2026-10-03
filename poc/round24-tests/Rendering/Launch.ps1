<# Own standalone headless host proof. Existing replacement toolchain only. #>
[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$GodotPath,[Parameter(Mandatory=$true)][string]$ArchiveRoot,[Parameter(Mandatory=$true)][string]$Configuration,[Parameter(Mandatory=$true)][string]$FixturePath,[string]$CapturePath,[ValidateSet('generation','destroy')][string]$Retirement='generation')
$ErrorActionPreference='Stop'
$taskWorkspace=(Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$taskGodot=(Resolve-Path -LiteralPath $GodotPath).Path
$taskPrivate=Join-Path $taskWorkspace 'local-private\round24\customization'
New-Item -ItemType Directory -Force $taskPrivate | Out-Null
$taskAppData=Join-Path $taskPrivate 'appdata';New-Item -ItemType Directory -Force $taskAppData | Out-Null
$taskLog=Join-Path $taskPrivate ('preview-'+[Guid]::NewGuid().ToString('N')+'.log')
$taskScene=Join-Path $taskPrivate 'SceneWorldCreatureCustomization.tscn'
@'
[gd_scene load_steps=2 format=3]
[ext_resource type="Script" path="res://SceneWorldCreatureCustomizationPreview.cs" id="1"]
[node name="SceneWorldCreatureCustomizationPreview" type="Node"]
script = ExtResource("1")
'@ | Set-Content -LiteralPath $taskScene
$taskArguments=@('--path',(Join-Path $taskWorkspace 'poc\Stardust.Godot'),'--log-file',$taskLog,$taskScene)
if(-not $CapturePath){$taskArguments+='--headless'}
$taskExport=Join-Path $taskPrivate ('evidence-'+[Guid]::NewGuid().ToString('N')+'.json')
$taskArguments+=@('--','--asset-root',(Resolve-Path -LiteralPath $ArchiveRoot).Path,'--asset-config',(Resolve-Path -LiteralPath $Configuration).Path,'--customization-export',$taskExport)
$taskArguments+=@('--mixed-peer',(Resolve-Path -LiteralPath $FixturePath).Path,'--customization-visibility-race','--customization-race-retire',$Retirement)
if($CapturePath){
 $taskCapture=[IO.Path]::GetFullPath($CapturePath)
 $taskGenerated=Join-Path (Split-Path $taskCapture -Parent) ([IO.Path]::GetFileNameWithoutExtension($taskCapture)+'-varied.png')
 $taskPrefix=[IO.Path]::GetFullPath((Join-Path $taskWorkspace 'local-private'))+[IO.Path]::DirectorySeparatorChar
 if(-not $taskCapture.StartsWith($taskPrefix,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetExtension($taskCapture) -ne '.png' -or (Test-Path -LiteralPath $taskCapture) -or (Test-Path -LiteralPath $taskGenerated) -or -not(Test-Path -LiteralPath (Split-Path $taskCapture -Parent))){throw 'Fresh private PNG pair required.'}
 $taskArguments+=@('--customization-capture',$taskCapture)
}
if($taskArguments.Where({$_ -match '["\r\n]'}).Count){throw 'Arguments containing quotes/newlines are unsupported.'}
$taskQuoted=$taskArguments.ForEach({'"'+$_+'"'})
$taskOldAppData=$env:APPDATA
try{$env:APPDATA=$taskAppData;$taskProcess=Start-Process -FilePath $taskGodot -ArgumentList $taskQuoted -WindowStyle Hidden -WorkingDirectory $taskWorkspace -PassThru}
finally{if($null -eq $taskOldAppData){Remove-Item Env:APPDATA -ErrorAction SilentlyContinue}else{$env:APPDATA=$taskOldAppData}}
if(-not $taskProcess.WaitForExit(45000)){Stop-Process -Id $taskProcess.Id;throw 'Host proof exceeded its 45-second deadline.'}
if($taskProcess.ExitCode -ne 0){Get-Content -LiteralPath $taskLog -Tail 25;throw ('Host proof failed: '+$taskProcess.ExitCode+'. Log: '+$taskLog)}
if(-not (Select-String -LiteralPath $taskLog -SimpleMatch 'PASS: world customization complete')){throw 'Host exited without acceptance marker.'}
Write-Output ('Log: '+$taskLog)
Write-Output ('Export: '+$taskExport)
