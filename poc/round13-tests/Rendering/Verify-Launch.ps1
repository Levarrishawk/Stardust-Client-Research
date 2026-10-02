<# Invalid destinations/modes reject before spawning a replacement preview. #>
[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$GodotPath,[Parameter(Mandatory=$true)][string]$ArchiveRoot,[Parameter(Mandatory=$true)][string]$Configuration,[Parameter(Mandatory=$true)][string]$FixturePath,[Parameter(Mandatory=$true)][string]$CatalogProfile,[Parameter(Mandatory=$true)][string]$ExistingCapture)
$ErrorActionPreference='Stop'
$taskWorkspace=(Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$taskLogs=Join-Path $taskWorkspace 'local-private\round13\two-world'
$taskBefore=@(Get-ChildItem -LiteralPath $taskLogs -Filter 'preview-*.log').Count
$taskFresh=Join-Path $taskWorkspace 'local-private\round13\never-launched.png'
$taskCases=@(@{CapturePath=$taskFresh;Headless=$true},@{CapturePath=$taskFresh;CancelLoad=$true},@{CapturePath=(Join-Path $taskWorkspace 'outside-private.png')},@{CapturePath=$ExistingCapture},@{CapturePath=(Join-Path $taskWorkspace 'local-private\round13\invalid.jpg')})
$taskCommon=@{GodotPath=$GodotPath;ArchiveRoot=$ArchiveRoot;Configuration=$Configuration;FixturePath=$FixturePath;CatalogProfile=$CatalogProfile}
foreach($taskCase in $taskCases){$taskRejected=$false;try{& (Join-Path $taskWorkspace 'poc\Stardust.Godot\Invoke-StardustTwoCreatureWorldPreview.ps1') @taskCommon @taskCase}catch{$taskRejected=$true};if(-not $taskRejected){throw 'Invalid mode/destination accepted.'}}
if(@(Get-ChildItem -LiteralPath $taskLogs -Filter 'preview-*.log').Count -ne $taskBefore -or (Test-Path -LiteralPath $taskFresh)){throw 'Invalid preflight launched a process/capture.'}
Write-Output 'PASS: two prelaunch_negatives=5 logs_unchanged=true captures_absent=true'
