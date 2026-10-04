[CmdletBinding()]
param(
 [Parameter(Mandatory=$true)][string]$GodotPath,
 [Parameter(Mandatory=$true)][string]$ArchiveRoot,
 [Parameter(Mandatory=$true)][string]$Configuration,
 [Parameter(Mandatory=$true)][string]$FixturePath,
 [Parameter(Mandatory=$true)][string]$CatalogProfile,
 [switch]$CancelLoad,
 [switch]$Gpu,
 [string]$MotionPeer,
 [switch]$LocalInput,
 [ValidateSet("AuthoredSpeed0","AuthoredSpeed1","ReceivedField7","ReceivedField11","CurrentField7","CurrentField11")][string]$SourcedInputSource,
 [switch]$InputLoss,
 [switch]$CustomizationRevisions,
 [switch]$CustomizationClosePending,
 [string]$FaceProfile,
 [string]$CustomizationWirePeer,
 [switch]$CustomizationScalars,
 [string]$CustomizationAssetContext,
 [switch]$CustomizationExtended,
 [string]$CustomizationNameContext,
 [string]$TangiblePeer,
 [string]$TangibleView6Peer,
 [string]$CreatureView6Peer,
 [string]$CreatureView6CompositePeer,
 [string]$CreatureView6FullPeer,
 [string]$CreatureView4FullPeer,
 [string]$CreatureView4PartialPeer,
 [string]$TerrainPeer,
 [switch]$TerminalAnimation,
 [switch]$TerminalClock,
 [switch]$TerminalClockFault,
 [string]$CustomizationDictionary,
 [string]$CapturePath
)
$ErrorActionPreference='Stop'
$taskRoot=(Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$taskGodot=(Resolve-Path -LiteralPath $GodotPath).Path
$taskArchive=(Resolve-Path -LiteralPath $ArchiveRoot).Path
$taskConfig=(Resolve-Path -LiteralPath $Configuration).Path
$taskFixture=(Resolve-Path -LiteralPath $FixturePath).Path
$taskCatalog=(Resolve-Path -LiteralPath $CatalogProfile).Path
if((Get-FileHash -LiteralPath $taskConfig).Hash -ne '22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'){throw 'Configuration identity'}
$taskPrivate=Join-Path $taskRoot 'local-private/parallel-development/round16/pump-host'
New-Item -ItemType Directory -Force $taskPrivate | Out-Null
$taskScene=Join-Path $taskPrivate 'PumpedWorld.tscn'
@"
[gd_scene load_steps=2 format=3]
[ext_resource type="Script" path="res://ScenePumpedWorldPreview.cs" id="1"]
[node name="ScenePumpedWorldPreview" type="Node"]
script = ExtResource("1")
"@ | Set-Content -LiteralPath $taskScene
$taskLog=Join-Path $taskPrivate ('preview-'+[Guid]::NewGuid().ToString('N')+'.log')
$taskArguments=@('--path',(Join-Path $taskRoot 'poc/Stardust.Godot'),'--log-file',$taskLog,$taskScene)
if(-not $Gpu){$taskArguments+='--headless'}
$taskArguments+=@('--','--asset-root',$taskArchive,'--asset-config',$taskConfig,'--mixed-peer',$taskFixture,'--catalog-profile',$taskCatalog)
if($CancelLoad){$taskArguments+='--pump-cancel'}
if($MotionPeer){$taskArguments+=@('--motion-peer',(Resolve-Path -LiteralPath $MotionPeer).Path)}
if($LocalInput){if(-not $MotionPeer){throw 'Local input requires motion peer'};$taskArguments+='--local-input'}
if($TerrainPeer){if(-not $LocalInput -or $CancelLoad -or $InputLoss){throw 'Supplied terrain requires bounded local input mode'};$taskArguments+=@('--terrain-peer',(Resolve-Path -LiteralPath $TerrainPeer).Path)}
if($FaceProfile){$taskArguments+=@('--face-profile',(Resolve-Path -LiteralPath $FaceProfile).Path)}
if($CustomizationWirePeer){if(-not $LocalInput -or -not $FaceProfile -or -not $CustomizationDictionary -or $CustomizationRevisions -or $InputLoss -or $CancelLoad){throw 'Wire customization requires separate local-input face and dictionary mode'};$taskArguments+=@('--customization-wire','--customization-peer',(Resolve-Path -LiteralPath $CustomizationWirePeer).Path,'--customization-dictionary',(Resolve-Path -LiteralPath $CustomizationDictionary).Path)}
if($CustomizationAssetContext){if(-not $CustomizationScalars -or -not $CustomizationWirePeer){throw 'Full creature context requires mixed wire mode'};$taskArguments+=@('--customization-full','--customization-asset-context',(Resolve-Path -LiteralPath $CustomizationAssetContext).Path)}
if($CustomizationExtended){if(-not $CustomizationAssetContext){throw 'Extended creature mode requires original context'};$taskArguments+='--customization-extended'}
if($CustomizationNameContext){if(-not $CustomizationExtended){throw 'Named mode requires extended mode'};$taskArguments+=@('--customization-named','--customization-name-context',(Resolve-Path -LiteralPath $CustomizationNameContext).Path)}
if($TangiblePeer){if(-not $CustomizationNameContext){throw 'Typed tangible proof requires the named mixed host'};$taskArguments+=@('--tangible-peer',(Resolve-Path -LiteralPath $TangiblePeer).Path)}
if($TangibleView6Peer){if(-not $TangiblePeer -or -not $TerminalClock -or $TerminalClockFault){throw 'TANO6 peer requires normal typed terminal clock mode'};$taskArguments+=@('--tangible6-peer',(Resolve-Path -LiteralPath $TangibleView6Peer).Path)}
if(@($CreatureView6Peer,$CreatureView6CompositePeer,$CreatureView6FullPeer).Where({$_}).Count -gt 1){throw 'Choose one CREO6 world partition'}
if($CreatureView6Peer){if(-not $TangibleView6Peer){throw 'CREO6 peer requires joined TANO6 mode'};$taskArguments+=@('--creature6-peer',(Resolve-Path -LiteralPath $CreatureView6Peer).Path)}
if($CreatureView6CompositePeer){if(-not $TangibleView6Peer){throw 'Composite CREO6 peer requires joined TANO6 mode'};$taskArguments+=@('--creature6-composite-peer',(Resolve-Path -LiteralPath $CreatureView6CompositePeer).Path)}
if($CreatureView6FullPeer){if(-not $TangibleView6Peer){throw 'Full CREO6 peer requires joined TANO6 mode'};$taskArguments+=@('--creature6-full-peer',(Resolve-Path -LiteralPath $CreatureView6FullPeer).Path)}
if($CreatureView4FullPeer -and $CreatureView4PartialPeer){throw 'Choose one CREO4 world policy'}
if($CreatureView4PartialPeer){if(-not $CreatureView6FullPeer){throw 'Partial CREO4 preview requires joined full CREO6 proof'};$taskArguments+=@('--creature4-partial-peer',(Resolve-Path -LiteralPath $CreatureView4PartialPeer).Path)}
if($CreatureView4FullPeer){if(-not $CreatureView6FullPeer){throw 'Full CREO4 preview requires joined full CREO6 proof'};$taskArguments+=@('--creature4-full-peer',(Resolve-Path -LiteralPath $CreatureView4FullPeer).Path)}
if($SourcedInputSource){if(-not $LocalInput -or -not $CreatureView4PartialPeer -or $TerrainPeer -or $InputLoss -or $CancelLoad){throw 'Sourced input requires the complete planar partial-view host'};$taskArguments+=@('--sourced-input',$SourcedInputSource)}
if($TerminalAnimation){if(-not $TangiblePeer){throw 'Terminal animation requires typed tangible proof'};$taskArguments+='--terminal-animation'}
if($TerminalClock){if(-not $TerminalAnimation){throw 'Terminal clock requires animation'};$taskArguments+='--terminal-clock'}
if($TerminalClockFault){if(-not $TerminalClock){throw 'Clock fault proof requires clock'};$taskArguments+='--terminal-clock-fault'}
if($CustomizationScalars){if(-not $CustomizationWirePeer){throw 'Mixed scalars require wire peer'};$taskArguments+='--customization-scalars'}
if($CustomizationClosePending){if(-not $CustomizationRevisions){throw 'Pending close requires customization mode'};$taskArguments+='--customization-close-pending'}
if($CustomizationRevisions){if(-not $LocalInput -or -not $FaceProfile -or $InputLoss -or $CancelLoad){throw 'Customization revisions require separate local input and face profile'};$taskArguments+='--customization-revisions'}
if($InputLoss){if(-not $LocalInput -or $CancelLoad -or $CapturePath){throw 'Input loss requires separate local input mode'};$taskArguments+='--input-loss'}
if($CapturePath){
 if(-not $Gpu -or -not $MotionPeer -or $CancelLoad){throw 'Motion capture requires separate GPU motion mode'}
 $taskCapture=[IO.Path]::GetFullPath($CapturePath)
 $taskPrefix=[IO.Path]::GetFullPath((Join-Path $taskRoot 'local-private'))+[IO.Path]::DirectorySeparatorChar
 if(-not $taskCapture.StartsWith($taskPrefix,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetExtension($taskCapture) -ne '.png' -or (Test-Path -LiteralPath $taskCapture) -or -not(Test-Path -LiteralPath (Split-Path $taskCapture -Parent))){throw 'Fresh private PNG required'}
 $taskArguments+=@('--motion-capture',$taskCapture)
}
if($taskArguments.Where({$_ -match '["\r\n]'}).Count){throw 'Unsupported quoted argument'}
$taskQuoted=$taskArguments.ForEach({'"'+$_+'"'})
$taskSavedAppData=$env:APPDATA
$taskAppData=Join-Path $taskPrivate 'appdata';New-Item -ItemType Directory -Force $taskAppData | Out-Null
try {
 $env:APPDATA=$taskAppData
 $taskProcess=Start-Process -FilePath $taskGodot -ArgumentList $taskQuoted -WindowStyle Hidden -WorkingDirectory $taskRoot -PassThru
} finally {
 if($null -eq $taskSavedAppData){Remove-Item Env:APPDATA -ErrorAction SilentlyContinue}else{$env:APPDATA=$taskSavedAppData}
}
if(-not $taskProcess.WaitForExit(45000)){Stop-Process -Id $taskProcess.Id;throw 'Pump preview exceeded45-second deadline'}
$taskMarker=if($InputLoss){'PASS: pumped input_loss'}elseif($CancelLoad){'PASS: pumped canceled'}else{'PASS: pumped world'}
if($taskProcess.ExitCode -ne 0 -or -not(Select-String -LiteralPath $taskLog -SimpleMatch $taskMarker)){
 Get-Content -LiteralPath $taskLog -Tail 35
 throw ('Pump preview failed. Log: '+$taskLog)
}
Write-Output ('PASS: original continuous pump fixture. Log: '+$taskLog)
