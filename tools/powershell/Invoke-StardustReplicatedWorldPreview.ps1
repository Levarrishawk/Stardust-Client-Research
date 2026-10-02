<# Replacement Godot replicated worlds only. No original executable, server, or effect/audio dispatch. #>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$GodotPath,
    [Parameter(Mandatory=$true)][string]$ArchiveRoot,
    [Parameter(Mandatory=$true)][string]$Configuration,
    [switch]$Headless,
    [switch]$CancelLoad,
    [switch]$Interactive,
    [Parameter(Mandatory=$true)][string]$FixturePath,
    [Parameter(Mandatory=$true)][string]$CatalogProfile,
    [string]$PaletteProfile,
    [switch]$MixedReplication,
    [string]$CapturePath
)
$ErrorActionPreference='Stop'
if($Interactive -and ($Headless -or $CancelLoad)){throw 'Interactive inspection cannot use headless/cancellation fixtures.'}
$taskWorkspace=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$taskGodot=(Resolve-Path -LiteralPath $GodotPath).Path
$taskArchiveRoot=(Resolve-Path -LiteralPath $ArchiveRoot).Path
$taskConfiguration=(Resolve-Path -LiteralPath $Configuration).Path
$taskFixture=(Resolve-Path -LiteralPath $FixturePath).Path
$taskCatalog=(Resolve-Path -LiteralPath $CatalogProfile).Path
$taskPalette=$null
if($PSBoundParameters.ContainsKey('PaletteProfile')){if([string]::IsNullOrWhiteSpace($PaletteProfile)){throw 'Explicit palette profile path required.'};$taskPalette=(Resolve-Path -LiteralPath $PaletteProfile).Path;$taskPaletteBytes=(Get-Item -LiteralPath $taskPalette).Length;if($taskPaletteBytes -lt 1 -or $taskPaletteBytes -gt 65536){throw 'Palette profile must contain 1..65536 bytes.'}}
if((Get-FileHash -LiteralPath $taskConfiguration -Algorithm SHA256).Hash -ne '22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'){throw 'Configuration identity mismatch.'}
if($CapturePath -and ($Interactive -or $Headless -or $CancelLoad)){throw 'Capture is a separate bounded GPU mode.'}
$taskCapture=$null
if($CapturePath){
    $taskCapture=[IO.Path]::GetFullPath($CapturePath)
    $taskSecond=Join-Path (Split-Path $taskCapture -Parent) ([IO.Path]::GetFileNameWithoutExtension($taskCapture)+'-frame1.png')
    if(Test-Path -LiteralPath $taskSecond){throw 'Second capture already exists.'}
    if($taskPalette){$taskColor=Join-Path (Split-Path $taskCapture -Parent) ([IO.Path]::GetFileNameWithoutExtension($taskCapture)+'-authored.png');if(Test-Path -LiteralPath $taskColor){throw 'Authored color capture already exists.'}}
    $taskPrivatePrefix=[IO.Path]::GetFullPath((Join-Path $taskWorkspace 'local-private'))+[IO.Path]::DirectorySeparatorChar
    if(-not $taskCapture.StartsWith($taskPrivatePrefix,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetExtension($taskCapture) -ne '.png' -or (Test-Path -LiteralPath $taskCapture) -or -not (Test-Path -LiteralPath (Split-Path $taskCapture -Parent))){throw 'Choose a fresh PNG in an existing checkout local-private directory.'}
}
$taskPrivate=Join-Path $taskWorkspace 'local-private\round12\replicated-world'
New-Item -ItemType Directory -Force $taskPrivate | Out-Null
$taskAppData=Join-Path $taskPrivate 'appdata'; New-Item -ItemType Directory -Force $taskAppData | Out-Null
$taskLog=Join-Path $taskPrivate ('preview-'+[Guid]::NewGuid().ToString('N')+'.log')
$taskArguments=@('--path',(Join-Path $taskWorkspace 'poc\Stardust.Godot'),'--log-file',$taskLog)
if(-not $Interactive -and -not $CapturePath){$taskArguments+='--headless'}
$taskArguments+=@('--','--offline-replicated-world-preview','--asset-root',$taskArchiveRoot,'--asset-config',$taskConfiguration,'--peer-fixture',$taskFixture,'--catalog-profile',$taskCatalog)
if($taskPalette){$taskArguments+=@('--palette-profile',$taskPalette)}
if($MixedReplication){$taskArguments+='--replicated-mixed'}
if($CapturePath){$taskArguments+=@('--replicated-capture',$taskCapture)}elseif($CancelLoad){$taskArguments+='--replicated-cancel'}elseif(-not $Interactive){$taskArguments+='--replicated-self-test'}
if($taskArguments.Where({$_ -match '["\r\n]'}).Count){throw 'Arguments containing quotes/newlines are unsupported.'}
$taskQuoted=$taskArguments.ForEach({'"'+$_+'"'})
$taskOldAppData=$env:APPDATA
try
{
    $env:APPDATA=$taskAppData
    $taskWindow=if($Interactive){'Normal'}else{'Hidden'}
    $taskProcess=Start-Process -FilePath $taskGodot -ArgumentList $taskQuoted -WindowStyle $taskWindow -WorkingDirectory $taskWorkspace -PassThru
}
finally
{
    if($null -eq $taskOldAppData){Remove-Item Env:APPDATA -ErrorAction SilentlyContinue}else{$env:APPDATA=$taskOldAppData}
}
if($Interactive){Write-Output ('Original replicated world running. Height delta, baseline reset, hide; drag to orbit and scroll to zoom. ProcessId='+$taskProcess.Id); Write-Output ('Log: '+$taskLog); return}
if(-not $taskProcess.WaitForExit(45000)){Stop-Process -Id $taskProcess.Id;throw 'Replicated world fixture exceeded its 45-second deadline.'}
if($taskProcess.ExitCode -ne 0){Get-Content -LiteralPath $taskLog -Tail 24;throw ('Replicated world fixture failed: '+$taskProcess.ExitCode+'. Log: '+$taskLog)}
$taskExpected=if($CapturePath){'PASS: replicated GPU pair'}elseif($CancelLoad){'PASS: replicated canceled'}else{'PASS: replicated teardown'}
if(-not (Select-String -LiteralPath $taskLog -SimpleMatch $taskExpected)){throw ('Fixture exited without acceptance marker. Log: '+$taskLog)}
if(-not $CancelLoad -and -not (Select-String -LiteralPath $taskLog -SimpleMatch 'PASS: replicated teardown')){throw 'Replicated fixture exited without cleanup marker.'}
if($CapturePath -and (-not (Test-Path -LiteralPath $taskCapture) -or -not (Test-Path -LiteralPath $taskSecond))){throw 'GPU preview exited without its capture pair.'}
if($CapturePath -and $taskPalette -and -not (Test-Path -LiteralPath $taskColor)){throw 'GPU preview exited without its authored color comparison.'}
Write-Output 'PASS: bounded original replicated world diagnostic fixture.'
Write-Output ('Log: '+$taskLog)
