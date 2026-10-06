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
 [string]$TerrainMaterialProfile,
 [string]$NominalSpeedFixture,
 [string]$TerrainPointTargetProfile,
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
if($TerrainMaterialProfile){if(-not $TerrainPeer){throw 'Explicit material requires terrain'};$taskArguments+=@('--terrain-material-profile',(Resolve-Path -LiteralPath $TerrainMaterialProfile).Path)}
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
if($TerrainPointTargetProfile){
 if(-not $LocalInput -or -not $TerrainPeer -or -not $CreatureView4PartialPeer -or $NominalSpeedFixture -or $SourcedInputSource -or $InputLoss -or $CancelLoad){throw 'Point target requires separate complete terrain partial-view host'}
 if((Get-Item -LiteralPath $TerrainPointTargetProfile).Length -gt 16384 -or (Get-Item -LiteralPath $TerrainPeer).Length -gt 524288){throw 'Bounded point/terrain inputs required'}
 $taskPoint=Get-Content -LiteralPath $TerrainPointTargetProfile -Raw | ConvertFrom-Json
 $taskTerrain=Get-Content -LiteralPath $TerrainPeer -Raw | ConvertFrom-Json
 $taskPointV1=$taskPoint.Version -eq 1 -and $taskPoint.Policy -eq 'SuppliedTerrainCameraPointTargetWithFullRootCorrection'
 $taskPointV2=$taskPoint.Version -eq 2 -and $taskPoint.Policy -eq 'SuppliedTerrainInputPausedTargetWithQueuedFullRootCorrection'
 $taskPointV3=$taskPoint.Version -eq 3 -and $taskPoint.Policy -eq 'SuppliedViewportPointTargetClearAndOfflineRootMotion'
 $taskPointV4=$taskPoint.Version -eq 4 -and $taskPoint.Policy -eq 'SuppliedViewportManualPointTargetAndOwnedOfflineQueue'
 $taskPointV11=$taskPoint.Version -eq 11 -and $taskPoint.Policy -eq 'SuppliedOfflineSessionControls'
 $taskPointV13=$taskPoint.Version -eq 13 -and $taskPoint.Policy -eq 'SuppliedConfiguredOfflinePlayground'
 $taskPointV12=$taskPointV13 -or ($taskPoint.Version -eq 12 -and $taskPoint.Policy -eq 'SuppliedIdleOfflinePlayground')
 $taskPointV10=$taskPoint.Version -eq 10 -and $taskPoint.Policy -eq 'SuppliedElapsedOfflinePlayground'
 $taskPointV9=$taskPoint.Version -eq 9 -and $taskPoint.Policy -eq 'SuppliedLongOfflinePlayground'
 if(-not $taskPointV9 -and -not $taskPointV10 -and -not $taskPointV11 -and -not $taskPointV12 -and (Get-Item -LiteralPath $TerrainPointTargetProfile).Length -gt 4096){throw 'Prior bounded point policy required'}
 $taskPointV8=$taskPoint.Version -eq 8 -and $taskPoint.Policy -eq 'SuppliedFollowingCameraAndOuterPeerDisconnect'
 $taskPointV7=$taskPoint.Version -eq 7 -and $taskPoint.Policy -eq 'SuppliedAcceptedLocomotionAndOfflinePeerDisconnect'
 $taskPointV6=$taskPoint.Version -eq 6 -and $taskPoint.Policy -eq 'SuppliedCameraRelativeOrbitAndOfflineSoeIngress'
 $taskPointV5=$taskPoint.Version -eq 5 -and $taskPoint.Policy -eq 'SuppliedViewportWalkRunAndOfflineSoeRootTransfer'
 if((-not $taskPointV1 -and -not $taskPointV2 -and -not $taskPointV3 -and -not $taskPointV4 -and -not $taskPointV5 -and -not $taskPointV6 -and -not $taskPointV7 -and -not $taskPointV8 -and -not $taskPointV9 -and -not $taskPointV10 -and -not $taskPointV11 -and -not $taskPointV12) -or $taskTerrain.Version -ne 9){throw 'Explicit point-target and terrain policy required'}
 $taskArguments+=@('--terrain-point-target-profile',(Resolve-Path -LiteralPath $TerrainPointTargetProfile).Path)
}
if($NominalSpeedFixture){
 if(-not $LocalInput -or -not $CreatureView4PartialPeer -or $SourcedInputSource -or $InputLoss -or $CancelLoad){throw 'Nominal speed requires the complete partial-view host'}
 if($TerrainPeer){
  if((Get-Item -LiteralPath $NominalSpeedFixture).Length -gt 262144 -or (Get-Item -LiteralPath $TerrainPeer).Length -gt 524288){throw 'Bounded joined terrain/playback inputs required'}
  $taskNominal=Get-Content -LiteralPath $NominalSpeedFixture -Raw | ConvertFrom-Json
  $taskTerrain=Get-Content -LiteralPath $TerrainPeer -Raw | ConvertFrom-Json
  $taskDirect=$taskNominal.Version -eq 4 -and $taskNominal.Policy -eq 'supplied_local_current_reader_and_nominal_plan_modern_terrain_trace' -and $taskNominal.TerrainBindingPolicy -eq 'OwnedTerrainDriverAndCurrentPlanPlayback'
  $taskSlot=$taskNominal.Version -eq 5 -and $taskNominal.Policy -eq 'supplied_local_current_reader_and_replaced_nominal_plan_modern_terrain_trace' -and $taskNominal.TerrainBindingPolicy -eq 'OwnedTerrainPlaybackSlot' -and $taskNominal.ReplacementAtStep -eq 30
  if((-not $taskDirect -and -not $taskSlot) -or $taskTerrain.Version -ne 9){throw 'Explicit current-plan terrain binding required'}
 }
 $taskArguments+=@('--nominal-speed-fixture',(Resolve-Path -LiteralPath $NominalSpeedFixture).Path)
}
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
$taskDeadlineMilliseconds=if($TerrainPointTargetProfile -and $taskPointV12 -and -not $taskPoint.InteractiveReplay){90000}else{45000}
if(-not $taskProcess.WaitForExit($taskDeadlineMilliseconds)){Stop-Process -Id $taskProcess.Id;throw ('Pump preview exceeded bounded deadline: '+$taskDeadlineMilliseconds+'ms')}
$taskMarker=if($InputLoss){'PASS: pumped input_loss'}elseif($CancelLoad){'PASS: pumped canceled'}else{'PASS: pumped world'}
if($taskProcess.ExitCode -ne 0 -or -not(Select-String -LiteralPath $taskLog -SimpleMatch $taskMarker)){
 Get-Content -LiteralPath $taskLog -Tail 35
 throw ('Pump preview failed. Log: '+$taskLog)
}
Write-Output ('PASS: original continuous pump fixture. Log: '+$taskLog)
