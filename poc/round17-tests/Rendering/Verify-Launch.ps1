<# Five invalid mode/destination cases must reject before launching a process. #>
[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$GodotPath,[Parameter(Mandatory=$true)][string]$ArchiveRoot,[Parameter(Mandatory=$true)][string]$Configuration,[Parameter(Mandatory=$true)][string]$FixturePath,[Parameter(Mandatory=$true)][string]$ExistingCapture)
$ErrorActionPreference='Stop'
$taskWorkspace=(Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$taskLogs=Join-Path $taskWorkspace 'local-private\round17\motion-visual';$taskBefore=@(Get-ChildItem -LiteralPath $taskLogs -Filter 'preview-*.log').Count
$taskFresh=Join-Path $taskWorkspace 'local-private\round17\never-launched.png'
$taskCases=@(@{CapturePath=$taskFresh;Headless=$true},@{CapturePath=$taskFresh;CancelLoad=$true},@{CapturePath=(Join-Path $taskWorkspace 'outside-private.png')},@{CapturePath=$ExistingCapture},@{CapturePath=(Join-Path $taskWorkspace 'local-private\round17\wrong.jpg')})
$taskCommon=@{GodotPath=$GodotPath;ArchiveRoot=$ArchiveRoot;Configuration=$Configuration;FixturePath=$FixturePath}
foreach($taskCase in $taskCases){$taskRejected=$false;try{& (Join-Path $PSScriptRoot 'Launch.ps1') @taskCommon @taskCase}catch{$taskRejected=$true};if(-not $taskRejected){throw 'Invalid launch accepted.'}}
if(@(Get-ChildItem -LiteralPath $taskLogs -Filter 'preview-*.log').Count -ne $taskBefore -or (Test-Path -LiteralPath $taskFresh)){throw 'Invalid input launched process/capture.'}
Write-Output 'PASS: object prelaunch_negatives=5 logs_unchanged=true captures_absent=true'
