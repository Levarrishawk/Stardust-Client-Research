param([Parameter(Mandatory=$true)][string]$GodotPath,[Parameter(Mandatory=$true)][string]$ArchiveRoot,[Parameter(Mandatory=$true)][string]$Configuration)
$ErrorActionPreference='Stop'
$launcher=Join-Path $PSScriptRoot '../../../tools/powershell/Invoke-StardustAssetPreview.ps1'
$base=@{GodotPath=$GodotPath;ArchiveRoot=$ArchiveRoot;Configuration=$Configuration;Member='object/creature/player/shared_human_male.iff';ExpectedSha256='E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243';Creature=$true;CreatureAppearanceSha256='951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C'}
$cases=@(@{CreatureMeshDetail='-1'},@{CreatureMeshDetail='4'},@{CreatureMeshDetail='1.5'},@{CreatureMeshDetail='abc'},@{CreatureMeshDetail='1';CreatureShapeProfile='missing.json'},@{CreatureMeshDetail='1';CreatureFaceProfile='missing.json'},@{CreatureMeshDetail='1';CreatureCustomizationProfile='missing.json'},@{CreatureMeshDetail='2';CreatureNormalDiffuse=$true},@{CreatureMeshDetail='3';CreatureSpecular=$true},@{CreatureMeshDetail='2';CreatureTextureProfile='missing.json'},@{CreatureVertexLitOnly=$true},@{CreatureMeshDetail='2';CreatureVertexLitOnly=$true},@{CreatureMeshDetail='1';Creature=$false})
$logs=Join-Path (Get-Location) 'local-private/generated-evidence/asset-preview'
$before=@(Get-ChildItem $logs -Filter '*.log').Count
foreach($case in $cases){$a=$base.Clone();foreach($k in $case.Keys){$a[$k]=$case[$k]};$rejected=$false;try{& $launcher @a}catch{$rejected=$true};if(-not $rejected){throw 'Invalid option combination accepted.'}}
if(@(Get-ChildItem $logs -Filter '*.log').Count -ne $before){throw 'Invalid options started engine.'}
Write-Output "PASS: $($cases.Count) invalid option combinations rejected before engine start."
