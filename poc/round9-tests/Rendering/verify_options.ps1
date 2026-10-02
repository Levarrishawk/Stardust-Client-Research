[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$GodotPath,[Parameter(Mandatory=$true)][string]$ArchiveRoot,[Parameter(Mandatory=$true)][string]$Configuration,[Parameter(Mandatory=$true)][string]$ExistingCapture)
$ErrorActionPreference='Stop'
$workspace=(Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$launcher=Join-Path $workspace 'tools/powershell/Invoke-StardustStaticSwitchTexturePreview.ps1'
$logs=Join-Path $workspace 'local-private/round9/switch-textures'
$before=@(Get-ChildItem $logs -Filter 'preview-*.log').Count
$cases=@(@{Interactive=$true;Headless=$true},@{Interactive=$true;CancelLoad=$true},@{CapturePath=$ExistingCapture},@{CapturePath=(Join-Path $workspace 'outside.png')},@{Headless=$true;CapturePath=$ExistingCapture},@{CancelLoad=$true;CapturePath=$ExistingCapture},@{Interactive=$true;CapturePath=$ExistingCapture})
foreach($case in $cases){$rejected=$false;try{& $launcher -GodotPath $GodotPath -ArchiveRoot $ArchiveRoot -Configuration $Configuration @case}catch{$rejected=$true};if(-not $rejected){throw 'Invalid options accepted.'}}
if(@(Get-ChildItem $logs -Filter 'preview-*.log').Count -ne $before){throw 'Invalid options launched engine.'}
$fresh=Join-Path $workspace ('local-private/round9/options-'+[Guid]::NewGuid().ToString('N')+'.png')
$second=Join-Path ([IO.Path]::GetDirectoryName($fresh)) ([IO.Path]::GetFileNameWithoutExtension($fresh)+'-frame1.png')
try {
    [IO.File]::WriteAllBytes($second,[byte[]]@())
    $rejected=$false
    try{& $launcher -GodotPath $GodotPath -ArchiveRoot $ArchiveRoot -Configuration $Configuration -CapturePath $fresh}catch{$rejected=$true}
    if(-not $rejected -or (Test-Path -LiteralPath $fresh) -or @(Get-ChildItem $logs -Filter 'preview-*.log').Count -ne $before){throw 'Existing second capture was not rejected before engine launch.'}
} finally {Remove-Item -LiteralPath $second -ErrorAction SilentlyContinue}
'PASS: eight invalid option/capture-pair cases rejected before engine launch.' 
