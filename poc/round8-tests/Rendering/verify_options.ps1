[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$GodotPath,[Parameter(Mandatory=$true)][string]$ArchiveRoot,[Parameter(Mandatory=$true)][string]$Configuration,[Parameter(Mandatory=$true)][string]$PeerFixture,[Parameter(Mandatory=$true)][string]$ExistingCapture)
$ErrorActionPreference='Stop'
$workspace=(Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$launcher=Join-Path $workspace 'tools/powershell/Invoke-StardustOfflineSceneWorldPreview.ps1'
$logs=Join-Path $workspace 'local-private/round8/world-creatures'
$before=@(Get-ChildItem $logs -Filter 'preview-*.log').Count
$cases=@(@{Interactive=$true;Headless=$true},@{Interactive=$true;CancelLoad=$true},@{CapturePath=$ExistingCapture},@{CapturePath=(Join-Path $workspace 'outside.png')},@{Headless=$true;CapturePath=$ExistingCapture},@{CancelLoad=$true;CapturePath=$ExistingCapture},@{Interactive=$true;CapturePath=$ExistingCapture})
foreach($case in $cases){$rejected=$false;try{& $launcher -GodotPath $GodotPath -ArchiveRoot $ArchiveRoot -Configuration $Configuration -PeerFixture $PeerFixture @case}catch{$rejected=$true};if(-not $rejected){throw 'Invalid options accepted.'}}
if(@(Get-ChildItem $logs -Filter 'preview-*.log').Count -ne $before){throw 'Invalid options launched engine.'}
'PASS: seven invalid option combinations rejected before engine launch.'
