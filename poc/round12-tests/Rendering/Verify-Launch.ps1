<# Eight bounded prelaunch errors including optional profile ownership budgets. #>
[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$GodotPath,[Parameter(Mandatory=$true)][string]$ArchiveRoot,[Parameter(Mandatory=$true)][string]$Configuration,[Parameter(Mandatory=$true)][string]$FixturePath,[Parameter(Mandatory=$true)][string]$CatalogProfile,[Parameter(Mandatory=$true)][string]$ExistingCapture)
$ErrorActionPreference='Stop'
$taskWorkspace=(Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$taskPrivate=Join-Path $taskWorkspace 'local-private\round12'
$taskEmpty=Join-Path $taskPrivate 'empty-profile.json';$taskOversize=Join-Path $taskPrivate 'oversize-profile.json'
[IO.File]::WriteAllBytes($taskEmpty,[byte[]]@());[IO.File]::WriteAllBytes($taskOversize,[byte[]]::new(65537))
$taskLogs=Join-Path $taskPrivate 'replicated-world'
$taskBefore=@(Get-ChildItem -LiteralPath $taskLogs -Filter 'preview-*.log').Count
$taskCommon=@{GodotPath=$GodotPath;ArchiveRoot=$ArchiveRoot;Configuration=$Configuration;FixturePath=$FixturePath;CatalogProfile=$CatalogProfile}
$taskFresh=Join-Path $taskPrivate 'never-launched.png'
$taskCases=@(@{Interactive=$true;Headless=$true},@{CapturePath=$taskFresh;Headless=$true},@{CapturePath=$taskFresh;CancelLoad=$true},@{CapturePath=(Join-Path $taskWorkspace 'outside-private.png')},@{CapturePath=$ExistingCapture},@{PaletteProfile=(Join-Path $taskPrivate 'missing-profile.json')},@{PaletteProfile=$taskEmpty},@{PaletteProfile=$taskOversize})
foreach($taskCase in $taskCases){$taskRejected=$false;try{& (Join-Path $taskWorkspace 'tools\powershell\Invoke-StardustReplicatedWorldPreview.ps1') @taskCommon @taskCase}catch{$taskRejected=$true};if(-not $taskRejected){throw 'Invalid launcher/profile case accepted.'}}
if(@(Get-ChildItem -LiteralPath $taskLogs -Filter 'preview-*.log').Count -ne $taskBefore -or (Test-Path -LiteralPath $taskFresh)){throw 'Rejected prelaunch input created a log/capture.'}
Write-Output 'PASS: observer launcher prelaunch_negatives=8 logs_unchanged=true captures_absent=true profile_budgets=true'
