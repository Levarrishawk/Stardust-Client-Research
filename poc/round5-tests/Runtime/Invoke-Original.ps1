[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$GodotPath,[Parameter(Mandatory=$true)][string]$SceneSpecification,[string]$ArchiveRoot='C:/Stardust-DEV',[string]$Configuration='C:/Stardust-DEV/swgemu_live.cfg',[switch]$Gpu,[string]$CapturePath,[switch]$DefaultRoute)
$ErrorActionPreference='Stop'
$preview=@{
 GodotPath=$GodotPath;ArchiveRoot=$ArchiveRoot;Configuration=$Configuration
 Member='appearance/thm_must_mining_outpost.pob';ExpectedSha256='79085C909D585A9530017150EB07DA49EDAABA1E010C48D845FFD19F97B4D818'
 SceneSpecification=$SceneSpecification;SceneSpecificationSha256='12BF5B21EC0D7BDA15CB322BB14FF5FC4C7C640B81E7C1DB38CA11F66D9349CA'
 DoorTableSha256='C4C572DEBD51DC9D77E57E7D5A8D6741A01A03CAF00BF31601180A05551536B7'
 ActorTemplate='object/creature/player/shared_human_male.iff';ActorTemplateSha256='E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243'
 SceneGeometry=$true;SceneTextures=$true;SceneTriangleGuard=$true;SceneCharacter=$true
 SceneCharacterAppearanceSha256='951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C'
 SceneCharacterMaterialDefaults=$true;SceneCharacterPaletteDefaults=$true;SceneCharacterAlbedoFactors=$true;SceneCharacterNormalDiffuse=$true;SceneCharacterSpecular=$true
 SceneCellLighting=$true;SceneRoomLighting=$true;SceneDoorLighting=$true;PortalVisibility=$true;HoldClosingDoors=$true
 SceneCameraProfileSha256='01A19A5B18E81C9366B86B5F313C31D8F0C1961F8230DB4535AA5C50E95C543D'
 SceneCharacterLocomotionProfile='poc/preview-profiles/human-locomotion.json'
 SceneCharacterAnimationTable='appearance/lat/all_m.lat';SceneCharacterAnimationName='emt_standing_idle_ambient_1_ag';SceneCharacterAnimationSha256='798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B'
 SceneCharacterEvents=(-not $DefaultRoute);Headless=(-not $Gpu)
}
if($CapturePath){if(-not $Gpu){throw 'Capture requires explicit GPU mode.'};$preview.CapturePath=$CapturePath}
& tools/powershell/Invoke-StardustAssetPreview.ps1 @preview
