param([Parameter(Mandatory=$true)][string]$GodotPath,[Parameter(Mandatory=$true)][string]$ArchiveRoot,[Parameter(Mandatory=$true)][string]$Configuration)
$ErrorActionPreference='Stop'
$launcher=Join-Path $PSScriptRoot '../../../tools/powershell/Invoke-StardustRetainedDetailPreview.ps1'
$base=@{GodotPath=$GodotPath;ArchiveRoot=$ArchiveRoot;Configuration=$Configuration}
$cases=@(@{SelectedDetail='-1'},@{SelectedDetail='5'},@{SelectedDetail='4'},@{SelectedDetail='2.5'},@{SelectedDetail='02'},@{Interactive=$true;Headless=$true},@{Interactive=$true;CancelLoad=$true},@{CapturePath='missing.png';Headless=$true},@{CapturePath='missing.png';CancelLoad=$true},@{CapturePath='missing.png';Interactive=$true},@{CapturePath='outside.png'})
$logs=Join-Path (Get-Location) 'local-private/round5/retained-details'
$before=@(Get-ChildItem $logs -Filter '*.log' -ErrorAction SilentlyContinue).Count
foreach($case in $cases){$a=$base.Clone();foreach($k in $case.Keys){$a[$k]=$case[$k]};$rejected=$false;try{& $launcher @a}catch{$rejected=$true};if(-not $rejected){throw 'Invalid retained option combination accepted.'}}
if(@(Get-ChildItem $logs -Filter '*.log' -ErrorAction SilentlyContinue).Count -ne $before){throw 'Invalid options started engine.'}
Write-Output "PASS: $($cases.Count) invalid retained option combinations rejected before engine start."
