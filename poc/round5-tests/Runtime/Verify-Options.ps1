$ErrorActionPreference='Stop'
$pin='A'*64
$valid=@{GodotPath='missing-engine';ArchiveRoot='missing-assets';Configuration='missing-config';Member='unused';ExpectedSha256=$pin;SceneCharacterEvents=$true;SceneSpecification='missing-spec';SceneSpecificationSha256=$pin;SceneCharacter=$true;SceneCharacterAppearanceSha256=$pin;SceneGeometry=$true;ActorTemplate='unused';ActorTemplateSha256=$pin;DoorTableSha256=$pin;SceneCharacterLocomotionProfile='missing-bank';SceneCharacterAnimationTable='unused';SceneCharacterAnimationName='unused';SceneCharacterAnimationSha256=$pin}
$count=0
foreach($key in @('SceneSpecification','SceneSpecificationSha256','SceneCharacter','SceneCharacterAppearanceSha256','SceneGeometry','ActorTemplate','ActorTemplateSha256','DoorTableSha256','SceneCharacterLocomotionProfile','SceneCharacterAnimationTable','SceneCharacterAnimationName','SceneCharacterAnimationSha256')){
 $candidate=$valid.Clone();$candidate.Remove($key)
 try{& tools/powershell/Invoke-StardustAssetPreview.ps1 @candidate;throw 'Invalid options accepted'}catch{if($_.Exception.Message -notlike 'SceneCharacterEvents requires*'){throw};$count++}
}
foreach($key in @('CancelSceneLoad','Creature','Interior','Detail','Component','PaletteInspector','DoorStyle')){
 $candidate=$valid.Clone();$candidate[$key]=if($key -eq 'DoorStyle'){'unused'}else{$true}
 try{& tools/powershell/Invoke-StardustAssetPreview.ps1 @candidate;throw 'Conflicting options accepted'}catch{if($_.Exception.Message -notlike 'SceneCharacterEvents requires*'){throw};$count++}
}
Write-Output "PASS: $count launcher options reject before path resolution/asset reads/engine launch."
