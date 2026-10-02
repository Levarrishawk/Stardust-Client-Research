<# This script runs only the replacement Godot proof. It never launches Stardust.exe or contacts a server. #>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$GodotPath,
    [Parameter(Mandatory=$true)][string]$ArchiveRoot,
    [Parameter(Mandatory=$true)][string]$Configuration,
    [Parameter(Mandatory=$true)][string]$Member,
    [Parameter(Mandatory=$true)][ValidatePattern('^[0-9A-Fa-f]{64}$')][string]$ExpectedSha256,
    [string]$CapturePath,
    [switch]$Headless,
    # Additional bounded combined point-color/tangent GPU acceptance probe.
    [switch]$PointColorGpuProbe,
    [switch]$Detail,
    [switch]$Component,
    [switch]$Interior,
    [switch]$PaletteInspector,
    [switch]$Creature,
    [ValidatePattern('^[0-3]$')][string]$CreatureMeshDetail='0',
    [switch]$CreatureVertexLitOnly,
    [ValidatePattern('^[0-9A-Fa-f]{64}$')][string]$CreatureAppearanceSha256,
    [switch]$CancelCreatureLoad,
    [switch]$CreaturePose,
    [string]$CreatureAnimationTable,
    [string]$CreatureAnimationName,
    [ValidatePattern('^[0-9A-Fa-f]{64}$')][string]$CreatureAnimationSha256,
    [double]$CreatureAnimationFrame=-1,
    [switch]$CreatureAnimationPlay,
    [switch]$CreatureMaterialDefaults,
    [switch]$CreaturePaletteDefaults,
    [switch]$CreatureAlbedoFactors,
    [string]$CreatureFaceProfile,
    [string]$CreatureCustomizationProfile,
    [string]$CreatureShapeProfile,
    [switch]$CreatureNormalDiffuse,
    [switch]$CreatureSpecular,
    [string]$CreatureTextureProfile,
    [string]$DoorStyle,
    [string]$SceneSpecification,
    [ValidatePattern('^[0-9A-Fa-f]{64}$')][string]$SceneSpecificationSha256,
    [switch]$HoldClosingDoors,
    [string]$ActorTemplate,
    [ValidatePattern('^[0-9A-Fa-f]{64}$')][string]$ActorTemplateSha256,
    [switch]$SceneGeometry,
    [switch]$SceneTextures,
    [switch]$SceneTriangleGuard,
    [switch]$SceneCharacter,
    [switch]$SceneCharacterEvents,
    [ValidatePattern('^[0-9A-Fa-f]{64}$')][string]$SceneCharacterAppearanceSha256,
    [switch]$SceneCharacterMaterialDefaults,
    [switch]$SceneCharacterPaletteDefaults,
    [switch]$SceneCharacterAlbedoFactors,
    [string]$SceneCharacterFaceProfile,
    [string]$SceneCharacterCustomizationProfile,
    [string]$SceneCharacterShapeProfile,
    [switch]$SceneCharacterNormalDiffuse,
    [switch]$SceneCharacterSpecular,
    [switch]$SceneCellLighting,
    [switch]$SceneRoomLighting,
    [switch]$SceneDoorLighting,
    # Compatibility alias: SceneCellLighting now selects recovered slots by default.
    [switch]$SceneLegacyLightSelection,
    [string]$SceneCharacterTextureProfile,
    [string]$SceneCharacterLocomotionProfile,
    [ValidatePattern('^[0-9A-Fa-f]{64}$')][string]$SceneCameraProfileSha256,
    [string]$SceneCharacterAnimationTable,
    [string]$SceneCharacterAnimationName,
    [ValidatePattern('^[0-9A-Fa-f]{64}$')][string]$SceneCharacterAnimationSha256,
    [switch]$PortalVisibility,
    [switch]$CancelSceneLoad,
    [ValidatePattern('^[0-9A-Fa-f]{64}$')][string]$DoorTableSha256,
    [int]$DoorCell=-1,
    [int]$DoorPortal=-1,
    [ValidatePattern('^[0-9A-Fa-f]{64}$')][string]$DoorLeafSha256,
    [switch]$DoorWalk,
    [switch]$PreciseDoorWalk,
    [switch]$StepDoorWalk,
    [switch]$LocalStepDoorWalk,
    [switch]$ConnectedDoorApproach,
    [int]$Cell=-1,
    [switch]$InteriorFloors,
    [switch]$InteriorPaths,
    [switch]$InteriorTextures,
    [switch]$AuthoredTextureDefaults,
    [switch]$FloorOnly,
    [switch]$ComponentTextures,
    [switch]$CameraLod,
    [switch]$InspectHardpoints,
    [switch]$MainTextureOnly,
    [switch]$Interactive
)
$ErrorActionPreference='Stop'
if ($SceneCharacterEvents -and (-not $SceneSpecification -or -not $SceneCharacter -or -not $SceneCharacterLocomotionProfile -or -not $SceneCharacterAnimationTable -or -not $SceneCharacterAnimationName -or -not $SceneCharacterAnimationSha256 -or -not $SceneGeometry -or -not $ActorTemplate -or -not $ActorTemplateSha256 -or -not $SceneSpecificationSha256 -or -not $SceneCharacterAppearanceSha256 -or -not $DoorTableSha256 -or $CancelSceneLoad -or $Creature -or $Interior -or $Detail -or $Component -or $PaletteInspector -or $DoorStyle)) { throw 'SceneCharacterEvents requires a pinned owned interior character idle/movement bank and excludes cancellation.' }
$taskWorkspace=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$taskGodot=(Resolve-Path -LiteralPath $GodotPath).Path
$taskRoot=(Resolve-Path -LiteralPath $ArchiveRoot).Path
$taskConfiguration=(Resolve-Path -LiteralPath $Configuration).Path
if ($Interactive -and ($Headless -or $CapturePath)) { throw 'Choose interactive inspection or bounded validation/capture.' }
if ($Headless -and $CapturePath) { throw 'Headless validation cannot capture GPU rendering.' }
if (([int][bool]$Component+[int][bool]$Detail+[int][bool]$Interior+[int][bool]$PaletteInspector+[int][bool]$DoorStyle+[int][bool]$SceneSpecification+[int][bool]$Creature) -gt 1) { throw 'Choose one asset inspector.' }
if ([bool]$Creature -ne [bool]$CreatureAppearanceSha256) { throw 'Creature and CreatureAppearanceSha256 are required together.' }
if ($CancelCreatureLoad -and (-not $Creature -or -not $Headless)) { throw 'CancelCreatureLoad requires a headless creature inspector.' }
if (($PSBoundParameters.ContainsKey('CreatureMeshDetail') -or $CreatureVertexLitOnly) -and -not $Creature) { throw 'CreatureMeshDetail/CreatureVertexLitOnly require the standalone Creature inspector.' }
if ([int]$CreatureMeshDetail -gt 0 -and ($CreatureShapeProfile -or $CreatureFaceProfile -or $CreatureCustomizationProfile)) { throw 'Lower mesh details do not support shape/generated-face/customization profiles in this bounded preview.' }
if ([int]$CreatureMeshDetail -ge 2 -and ($CreatureNormalDiffuse -or $CreatureSpecular -or $CreatureTextureProfile)) { throw 'Details 2/3 have no normal/tangent family; select CreatureVertexLitOnly without normal/specular/texture-choice options.' }
if ($CreatureVertexLitOnly -and ([int]$CreatureMeshDetail -lt 2 -or -not $CreatureAlbedoFactors -or $CreatureNormalDiffuse -or $CreatureSpecular -or $CreatureShapeProfile -or $CreatureFaceProfile -or $CreatureCustomizationProfile -or $CreatureTextureProfile)) { throw 'CreatureVertexLitOnly requires detail 2/3 and material/palette/albedo without normal/shape/face/texture choices.' }
if ($CreaturePose -and -not $Creature) { throw 'CreaturePose requires the standalone creature inspector.' }
if ($CreatureAnimationTable -or $CreatureAnimationName -or $CreatureAnimationSha256 -or $CreatureAnimationFrame -ne -1) {
    if (-not $Creature -or -not $CreatureAnimationTable -or -not $CreatureAnimationName -or -not $CreatureAnimationSha256 -or $CreatureAnimationFrame -lt 0 -or [double]::IsNaN($CreatureAnimationFrame) -or [double]::IsInfinity($CreatureAnimationFrame) -or $CreatureAnimationFrame -gt 32767 -or $CreaturePose) { throw 'Selected animation requires Creature, table/name/clip pin and bounded frame, without diagnostic pose.' }
}
if ($CreaturePaletteDefaults -and -not $CreatureMaterialDefaults) { throw 'CreaturePaletteDefaults requires CreatureMaterialDefaults.' }
if ($CreatureMaterialDefaults -and -not $Creature) { throw 'CreatureMaterialDefaults requires Creature.' }
if ($CreatureCustomizationProfile -and (-not $CreatureAlbedoFactors -or $CreatureFaceProfile)) { throw 'CreatureCustomizationProfile requires albedo factors and one profile.' }
if ($CreatureShapeProfile -and -not $Creature) { throw 'CreatureShapeProfile requires Creature.' }
if ($CreatureSpecular -and -not $CreatureNormalDiffuse) { throw 'CreatureSpecular requires CreatureNormalDiffuse.' }
if ($CreatureTextureProfile -and -not $CreatureNormalDiffuse) { throw 'CreatureTextureProfile requires CreatureNormalDiffuse.' }
if ($CreatureNormalDiffuse -and -not $CreatureAlbedoFactors) { throw 'CreatureNormalDiffuse requires CreatureAlbedoFactors.' }
if ($CreatureFaceProfile -and -not $CreatureMaterialDefaults) { throw 'CreatureFaceProfile requires CreatureMaterialDefaults.' }
if ($CreatureAlbedoFactors -and -not $CreaturePaletteDefaults) { throw 'CreatureAlbedoFactors requires CreaturePaletteDefaults.' }
if ($CreatureAnimationPlay -and -not $CreatureAnimationTable) { throw 'CreatureAnimationPlay requires explicit selected animation.' }
if ([bool]($DoorStyle -or $SceneSpecification) -ne [bool]$DoorTableSha256) { throw 'Door style or scene specification requires DoorTableSha256.' }
if ([bool]$SceneSpecification -ne [bool]$SceneSpecificationSha256) { throw 'SceneSpecification and SceneSpecificationSha256 are required together.' }
if ($HoldClosingDoors -and -not $SceneSpecification) { throw 'HoldClosingDoors requires an explicit scene specification.' }
if ([bool]$ActorTemplate -ne [bool]$ActorTemplateSha256) { throw 'ActorTemplate and ActorTemplateSha256 are required together.' }
if ($ActorTemplate -and -not $SceneSpecification) { throw 'ActorTemplate requires an explicit scene specification.' }
if ($SceneGeometry -and -not $SceneSpecification) { throw 'SceneGeometry requires an explicit scene specification.' }
if ($SceneTextures -and -not $SceneGeometry) { throw 'SceneTextures requires SceneGeometry.' }
if ($SceneTriangleGuard -and (-not $ActorTemplate -or -not $SceneTextures)) { throw 'SceneTriangleGuard requires a pinned ActorTemplate and SceneTextures.' }
if ([bool]$SceneCharacter -ne [bool]$SceneCharacterAppearanceSha256) { throw 'SceneCharacter and SceneCharacterAppearanceSha256 are required together.' }
if ($SceneCharacter -and (-not $ActorTemplate -or -not $SceneGeometry)) { throw 'SceneCharacter requires a pinned ActorTemplate and SceneGeometry.' }
if ($SceneCharacterPaletteDefaults -and -not $SceneCharacterMaterialDefaults) { throw 'SceneCharacterPaletteDefaults requires SceneCharacterMaterialDefaults.' }
if ($SceneCharacterAlbedoFactors -and -not $SceneCharacterPaletteDefaults) { throw 'SceneCharacterAlbedoFactors requires SceneCharacterPaletteDefaults.' }
if ($SceneCharacterCustomizationProfile -and (-not $SceneCharacterAlbedoFactors -or $SceneCharacterFaceProfile)) { throw 'SceneCharacterCustomizationProfile requires albedo factors and one profile.' }
if ($SceneLegacyLightSelection -and -not $SceneCellLighting) { throw 'SceneLegacyLightSelection requires SceneCellLighting.' }
if ($SceneDoorLighting -and (-not $SceneCellLighting -or -not $SceneTextures)) { throw 'SceneDoorLighting requires SceneCellLighting and SceneTextures.' }
if ($SceneRoomLighting -and -not $SceneCellLighting) { throw 'SceneRoomLighting requires SceneCellLighting.' }
if ($SceneCellLighting -and -not $SceneCharacterSpecular) { throw 'SceneCellLighting requires SceneCharacterSpecular.' }
if ($SceneCharacterSpecular -and -not $SceneCharacterNormalDiffuse) { throw 'SceneCharacterSpecular requires SceneCharacterNormalDiffuse.' }
if ($SceneCharacterNormalDiffuse -and -not $SceneCharacterAlbedoFactors) { throw 'SceneCharacterNormalDiffuse requires SceneCharacterAlbedoFactors.' }
if ($SceneCharacterTextureProfile -and -not $SceneCharacterNormalDiffuse) { throw 'SceneCharacterTextureProfile requires SceneCharacterNormalDiffuse.' }
if ($SceneCameraProfileSha256 -and (-not $SceneCharacter -or -not $SceneTriangleGuard)) { throw 'SceneCameraProfileSha256 requires owned character and room triangle guard.' }
if ($SceneCharacterLocomotionProfile -and (-not $SceneCharacter -or -not $SceneCharacterAnimationTable)) { throw 'SceneCharacterLocomotionProfile requires owned character and pinned idle selection.' }
if ($SceneCharacterShapeProfile -and -not $SceneCharacter) { throw 'SceneCharacterShapeProfile requires SceneCharacter.' }
if ($SceneCharacterFaceProfile -and -not $SceneCharacterMaterialDefaults) { throw 'SceneCharacterFaceProfile requires SceneCharacterMaterialDefaults.' }
if ($SceneCharacterMaterialDefaults -and -not $SceneCharacter) { throw 'SceneCharacterMaterialDefaults requires SceneCharacter.' }
if ($SceneCharacterAnimationTable -or $SceneCharacterAnimationName -or $SceneCharacterAnimationSha256) {
    if (-not $SceneCharacter -or -not $SceneCharacterAnimationTable -or -not $SceneCharacterAnimationName -or -not $SceneCharacterAnimationSha256) { throw 'Scene character animation requires SceneCharacter and table/name/clip pin.' }
}
if ($PortalVisibility -and -not $SceneGeometry) { throw 'PortalVisibility requires SceneGeometry.' }
if ($CancelSceneLoad -and (-not $SceneSpecification -or -not $Headless)) { throw 'CancelSceneLoad requires a headless pinned scene specification.' }
if ($SceneSpecification -and ($DoorCell -ne -1 -or $DoorPortal -ne -1 -or $DoorLeafSha256 -or $DoorWalk -or $PreciseDoorWalk -or $StepDoorWalk -or $LocalStepDoorWalk -or $ConnectedDoorApproach)) { throw 'Scene specification selects its pinned door instances and precise local-step movement.' }
if (($DoorCell -ne -1 -or $DoorPortal -ne -1 -or $DoorLeafSha256) -and (-not $DoorStyle -or $DoorCell -lt 0 -or $DoorPortal -lt 0 -or -not $DoorLeafSha256)) { throw 'Interior door inspection requires style, cell, portal and leaf identity together.' }
if ($DoorWalk -and $DoorPortal -lt 0) { throw 'DoorWalk requires a pinned interior door instance.' }
if ($PreciseDoorWalk -and -not $DoorWalk) { throw 'PreciseDoorWalk requires DoorWalk.' }
if ($StepDoorWalk -and -not $PreciseDoorWalk) { throw 'StepDoorWalk requires PreciseDoorWalk.' }
if ($LocalStepDoorWalk -and -not $StepDoorWalk) { throw 'LocalStepDoorWalk requires StepDoorWalk.' }
if ($ConnectedDoorApproach -and -not $LocalStepDoorWalk) { throw 'ConnectedDoorApproach requires LocalStepDoorWalk.' }
if ($DoorStyle -and ($CameraLod -or $MainTextureOnly -or $InspectHardpoints -or $InteriorFloors -or $InteriorPaths -or $InteriorTextures -or $ComponentTextures -or $AuthoredTextureDefaults -or $FloorOnly -or $Cell -ne -1)) { throw 'Door inspector accepts style and pinned table/leaf identity only.' }
if ($PaletteInspector -and ($CameraLod -or $MainTextureOnly -or $InteriorFloors -or $InteriorPaths -or $InteriorTextures -or $ComponentTextures -or $AuthoredTextureDefaults -or $FloorOnly -or $Cell -ne -1)) { throw 'PaletteInspector accepts shader identity and capture/interactive options.' }
if ($InteriorPaths -and -not $InteriorFloors) { throw 'InteriorPaths requires InteriorFloors.' }
if ($InteriorFloors -and -not $Interior) { throw 'InteriorFloors requires Interior.' }
if ($AuthoredTextureDefaults -and -not ($InteriorTextures -and $MainTextureOnly)) { throw 'AuthoredTextureDefaults requires InteriorTextures and MainTextureOnly.' }
if ($InteriorTextures -and -not $Interior) { throw 'InteriorTextures requires Interior.' }
if ($Interior -and $MainTextureOnly -and -not $InteriorTextures) { throw 'Interior texture-only inspection requires InteriorTextures.' }
if ($FloorOnly -and -not $InteriorFloors) { throw 'FloorOnly requires InteriorFloors.' }
if ($Cell -ne -1 -and -not $Interior) { throw 'Cell selection requires Interior.' }
if ($ComponentTextures -and -not $Component) { throw 'ComponentTextures requires Component inspection.' }
if ($Component -and $MainTextureOnly -and -not $ComponentTextures) { throw 'Component texture-only inspection requires ComponentTextures.' }
if ($CameraLod -and -not ($Component -or $Detail -or $Interior)) { throw 'CameraLod requires a detail or component inspector.' }
if ($InspectHardpoints -and -not ($Component -or $Interior)) { throw 'InspectHardpoints requires Component or Interior inspection.' }
$taskAppData=Join-Path $taskWorkspace 'local-private\godot-appdata'
if($PointColorGpuProbe -and ($Headless -or $Interactive -or (-not $CreatureSpecular -and -not $SceneCharacterSpecular))){throw 'PointColorGpuProbe requires a noninteractive GPU specular fixture.'}
$taskArguments=@('--path',(Join-Path $taskWorkspace 'poc\Stardust.Godot'))
if($Headless){$taskArguments+= '--headless'}
$taskArguments+=@('--','--asset-root',$taskRoot,'--asset-config',$taskConfiguration,'--asset-member',$Member,'--asset-sha256',$ExpectedSha256)
if($PointColorGpuProbe){$taskArguments+='--point-color-gpu-probe'}
if($Detail){$taskArguments+='--asset-detail'}
if($Component){$taskArguments+='--asset-component'}
if($PaletteInspector){$taskArguments+='--palette-inspector'}
if($Creature){$taskArguments+=@('--asset-creature','--creature-appearance-sha256',$CreatureAppearanceSha256)}
if($PSBoundParameters.ContainsKey('CreatureMeshDetail')){$taskArguments+=@('--creature-mesh-detail',$CreatureMeshDetail)}
if($CreatureVertexLitOnly){$taskArguments+='--creature-vertex-lit-only'}
if($CancelCreatureLoad){$taskArguments+='--cancel-creature-load'}
if($CreaturePose){$taskArguments+='--creature-pose'}
if($CreatureAnimationTable){$taskArguments+=@('--creature-animation-table',$CreatureAnimationTable,'--creature-animation-name',$CreatureAnimationName,'--creature-animation-sha256',$CreatureAnimationSha256,'--creature-animation-frame',$CreatureAnimationFrame.ToString([System.Globalization.CultureInfo]::InvariantCulture))}
if($CreatureAnimationPlay){$taskArguments+='--creature-animation-play'}
if($CreatureMaterialDefaults){$taskArguments+='--creature-material-defaults'}
if($CreaturePaletteDefaults){$taskArguments+='--creature-palette-defaults'}
if($CreatureAlbedoFactors){$taskArguments+='--creature-albedo-factors'}
if($CreatureCustomizationProfile){$taskCustomizationProfile=(Resolve-Path -LiteralPath $CreatureCustomizationProfile).Path;$taskArguments+=@('--creature-customization-profile',$taskCustomizationProfile)}
if($CreatureShapeProfile){$taskShapeProfile=(Resolve-Path -LiteralPath $CreatureShapeProfile).Path;$taskArguments+=@('--creature-shape-profile',$taskShapeProfile)}
if($CreatureNormalDiffuse){$taskArguments+='--creature-normal-diffuse'}
if($CreatureSpecular){$taskArguments+='--creature-specular'}
if($CreatureTextureProfile){$taskTextureProfile=(Resolve-Path -LiteralPath $CreatureTextureProfile).Path;$taskArguments+=@('--creature-texture-profile',$taskTextureProfile)}
if($CreatureFaceProfile){$taskProfile=(Resolve-Path -LiteralPath $CreatureFaceProfile).Path;$taskArguments+=@('--creature-face-profile',$taskProfile)}
if($DoorStyle){$taskArguments+=@('--door-style',$DoorStyle,'--door-table-sha256',$DoorTableSha256)}
if($SceneSpecification){$taskSceneSpecification=(Resolve-Path -LiteralPath $SceneSpecification).Path;$taskArguments+=@('--interior-scene-spec',$taskSceneSpecification,'--interior-scene-sha256',$SceneSpecificationSha256,'--door-table-sha256',$DoorTableSha256)}
if($HoldClosingDoors){$taskArguments+='--hold-closing-doors'}
if($ActorTemplate){$taskArguments+=@('--actor-template',$ActorTemplate,'--actor-template-sha256',$ActorTemplateSha256)}
if($SceneGeometry){$taskArguments+='--scene-geometry'}
if($SceneTextures){$taskArguments+='--scene-textures'}
if($SceneTriangleGuard){$taskArguments+='--scene-triangle-guard'}
if($SceneCharacterEvents){$taskArguments+='--scene-character-events'}
if($SceneCharacter){$taskArguments+=@('--scene-character','--scene-character-appearance-sha256',$SceneCharacterAppearanceSha256)}
if($SceneCharacterMaterialDefaults){$taskArguments+='--scene-character-material-defaults'}
if($SceneCharacterPaletteDefaults){$taskArguments+='--scene-character-palette-defaults'}
if($SceneCharacterAlbedoFactors){$taskArguments+='--scene-character-albedo-factors'}
if($SceneCharacterCustomizationProfile){$taskSceneCustomizationProfile=(Resolve-Path -LiteralPath $SceneCharacterCustomizationProfile).Path;$taskArguments+=@('--scene-character-customization-profile',$taskSceneCustomizationProfile)}
if($SceneCharacterShapeProfile){$taskSceneShapeProfile=(Resolve-Path -LiteralPath $SceneCharacterShapeProfile).Path;$taskArguments+=@('--scene-character-shape-profile',$taskSceneShapeProfile)}
if($SceneCharacterNormalDiffuse){$taskArguments+='--scene-character-normal-diffuse'}
if($SceneCharacterSpecular){$taskArguments+='--scene-character-specular'}
if($SceneCellLighting){$taskArguments+='--scene-cell-lighting'}
if($SceneLegacyLightSelection){$taskArguments+='--scene-legacy-light-selection'}
if($SceneDoorLighting){$taskArguments+='--scene-door-lighting'}
if($SceneRoomLighting){$taskArguments+='--scene-room-lighting'}
if($SceneCameraProfileSha256){$taskArguments+=@('--scene-camera-profile-sha256',$SceneCameraProfileSha256)}
if($SceneCharacterLocomotionProfile){$taskSceneLocomotionProfile=(Resolve-Path -LiteralPath $SceneCharacterLocomotionProfile).Path;$taskArguments+=@('--scene-character-locomotion-profile',$taskSceneLocomotionProfile)}
if($SceneCharacterTextureProfile){$taskSceneTextureProfile=(Resolve-Path -LiteralPath $SceneCharacterTextureProfile).Path;$taskArguments+=@('--scene-character-texture-profile',$taskSceneTextureProfile)}
if($SceneCharacterFaceProfile){$taskSceneProfile=(Resolve-Path -LiteralPath $SceneCharacterFaceProfile).Path;$taskArguments+=@('--scene-character-face-profile',$taskSceneProfile)}
if($SceneCharacterAnimationTable){$taskArguments+=@('--scene-character-animation-table',$SceneCharacterAnimationTable,'--scene-character-animation-name',$SceneCharacterAnimationName,'--scene-character-animation-sha256',$SceneCharacterAnimationSha256)}
if($PortalVisibility){$taskArguments+='--portal-visibility'}
if($CancelSceneLoad){$taskArguments+='--cancel-scene-load'}
if($DoorPortal -ge 0){$taskArguments+=@('--door-cell',[string]$DoorCell,'--door-portal',[string]$DoorPortal,'--door-leaf-sha256',$DoorLeafSha256)}
if($DoorWalk){$taskArguments+='--door-walk'}
if($PreciseDoorWalk){$taskArguments+='--precise-door-walk'}
if($StepDoorWalk){$taskArguments+='--step-door-walk'}
if($LocalStepDoorWalk){$taskArguments+='--local-step-door-walk'}
if($ConnectedDoorApproach){$taskArguments+='--connected-door-approach'}
if($Interior){$taskArguments+='--asset-interior';$taskArguments+=@('--cell',[string]$Cell)}
if($AuthoredTextureDefaults){$taskArguments+='--authored-texture-defaults'}
if($InteriorTextures){$taskArguments+='--interior-textures'}
if($InteriorPaths){$taskArguments+='--interior-paths'}
if($InteriorFloors){$taskArguments+='--interior-floors'}
if($FloorOnly){$taskArguments+='--floor-only'}
if($ComponentTextures){$taskArguments+='--component-textures'}
if($CameraLod){$taskArguments+='--camera-lod'}
if($InspectHardpoints){$taskArguments+='--inspect-hardpoints'}
if($MainTextureOnly){$taskArguments+='--main-texture-only'}
$taskLogDirectory=Join-Path $taskWorkspace 'local-private\generated-evidence\asset-preview'
New-Item -ItemType Directory -Force -Path $taskLogDirectory | Out-Null
$taskLog=Join-Path $taskLogDirectory ('preview-'+[Guid]::NewGuid().ToString('N')+'.log')
$taskArguments=@('--log-file',$taskLog)+$taskArguments
if($CapturePath)
{
    $taskPrivate=[IO.Path]::GetFullPath((Join-Path $taskWorkspace 'local-private'))+[IO.Path]::DirectorySeparatorChar
    $taskCapture=[IO.Path]::GetFullPath($CapturePath)
    if(-not $taskCapture.StartsWith($taskPrivate,[StringComparison]::OrdinalIgnoreCase)){throw 'Original-asset captures must remain under ignored local-private.'}
    if(Test-Path -LiteralPath $taskCapture){throw 'Capture destination already exists; choose a fresh filename.'}
    if(-not (Test-Path -LiteralPath (Split-Path $taskCapture -Parent))){throw 'Create the capture directory explicitly first.'}
    $taskArguments+=@('--capture',$taskCapture)
}
elseif(-not $Interactive){$taskArguments+='--asset-self-test'}
# Start-Process joins ArgumentList; quote each observed value so paths with spaces stay one argument.
if($taskArguments.Where({$_ -match '["\r\n]'}).Count){throw 'Arguments containing quotes/newlines are unsupported.'}
$taskQuoted=$taskArguments.ForEach({'"'+$_+'"'})
$taskWindow=if($Interactive){'Normal'}else{'Hidden'}
$taskOldAppData=$env:APPDATA
try
{
    $env:APPDATA=$taskAppData
    $taskProcess=Start-Process -FilePath $taskGodot -ArgumentList $taskQuoted -WindowStyle $taskWindow -WorkingDirectory $taskWorkspace -PassThru
}
finally
{
    if($null -eq $taskOldAppData){Remove-Item Env:APPDATA -ErrorAction SilentlyContinue}else{$env:APPDATA=$taskOldAppData}
}
if($Interactive){Write-Output ('Preview running. Drag to orbit; scroll to zoom; R to reset. ProcessId='+$taskProcess.Id);return}
if(-not $taskProcess.WaitForExit(45000)) {Stop-Process -Id $taskProcess.Id;throw 'Owned preview exceeded the 45-second validation deadline.'}
if($taskProcess.ExitCode -ne 0){Get-Content -LiteralPath $taskLog -Tail 18;throw ('Preview failed with exit code '+$taskProcess.ExitCode+'. Log: '+$taskLog)}
if($CapturePath -and -not (Test-Path -LiteralPath $taskCapture)){throw 'Preview exited without its requested capture.'}
Write-Output 'PASS: bounded offline Godot asset preview.'
Write-Output ('Log: '+$taskLog)
