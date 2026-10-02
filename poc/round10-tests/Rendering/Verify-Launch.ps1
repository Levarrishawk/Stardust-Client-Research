<# Bounded prelaunch rejection checks; no Godot process should start. #>
[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$GodotPath,[Parameter(Mandatory=$true)][string]$ArchiveRoot,[Parameter(Mandatory=$true)][string]$Configuration,[Parameter(Mandatory=$true)][string]$FixturePath,[Parameter(Mandatory=$true)][string]$ExistingCapture)
$ErrorActionPreference='Stop'
$taskWorkspace=(Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$taskRunner=Join-Path $taskWorkspace 'tools\powershell\Invoke-StardustCreatureStatePreview.ps1'
$taskLogs=Join-Path $taskWorkspace 'local-private\round10\state-presentation'
$taskBefore=@(Get-ChildItem -LiteralPath $taskLogs -Filter 'preview-*.log').Count
$taskFresh=Join-Path $taskWorkspace 'local-private\round10\never-launched.png'
$taskCommon=@{GodotPath=$GodotPath;ArchiveRoot=$ArchiveRoot;Configuration=$Configuration;FixturePath=$FixturePath}
$taskCases=@(
    @{Interactive=$true;Headless=$true},
    @{Interactive=$true;CancelLoad=$true},
    @{CapturePath=$taskFresh;Headless=$true},
    @{CapturePath=$taskFresh;CancelLoad=$true},
    @{CapturePath=$taskFresh;Interactive=$true},
    @{CapturePath=(Join-Path $taskWorkspace 'outside-private.png')},
    @{CapturePath=$ExistingCapture},
    @{CapturePath=(Join-Path $taskWorkspace 'local-private\round10\never-launched.jpg')}
)
foreach($taskCase in $taskCases){$taskRejected=$false;try{& $taskRunner @taskCommon @taskCase}catch{$taskRejected=$true};if(-not $taskRejected){throw 'Invalid launcher combination accepted.'}}
if(@(Get-ChildItem -LiteralPath $taskLogs -Filter 'preview-*.log').Count -ne $taskBefore -or (Test-Path -LiteralPath $taskFresh)){throw 'Prelaunch rejection produced a log/capture.'}
Write-Output 'PASS: state launcher prelaunch_negatives=8 logs_unchanged=true captures_absent=true'
