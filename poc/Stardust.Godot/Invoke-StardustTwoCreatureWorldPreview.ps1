<# Replacement Godot two-creature world only. No original executable, server, or effect/audio dispatch. #>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$GodotPath,
    [Parameter(Mandatory=$true)][string]$ArchiveRoot,
    [Parameter(Mandatory=$true)][string]$Configuration,
    [switch]$Headless,
    [switch]$CancelLoad,

    [Parameter(Mandatory=$true)][string]$FixturePath,
    [Parameter(Mandatory=$true)][string]$CatalogProfile,
    [string]$CapturePath
)
$ErrorActionPreference='Stop'
$taskWorkspace=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$taskGodot=(Resolve-Path -LiteralPath $GodotPath).Path
$taskArchiveRoot=(Resolve-Path -LiteralPath $ArchiveRoot).Path
$taskConfiguration=(Resolve-Path -LiteralPath $Configuration).Path
$taskFixture=(Resolve-Path -LiteralPath $FixturePath).Path
$taskCatalog=(Resolve-Path -LiteralPath $CatalogProfile).Path
if((Get-FileHash -LiteralPath $taskConfiguration -Algorithm SHA256).Hash -ne '22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524'){throw 'Configuration identity mismatch.'}
if($CapturePath -and ($Headless -or $CancelLoad)){throw 'Capture is a separate bounded GPU mode.'}
$taskCapture=$null
if($CapturePath){
    $taskCapture=[IO.Path]::GetFullPath($CapturePath)
    $taskSecond=Join-Path (Split-Path $taskCapture -Parent) ([IO.Path]::GetFileNameWithoutExtension($taskCapture)+'-updated.png')
    $taskThird=Join-Path (Split-Path $taskCapture -Parent) ([IO.Path]::GetFileNameWithoutExtension($taskCapture)+'-recreated.png')
    if((Test-Path -LiteralPath $taskSecond) -or (Test-Path -LiteralPath $taskThird)){throw 'Sibling capture already exists.'}
    $taskPrivatePrefix=[IO.Path]::GetFullPath((Join-Path $taskWorkspace 'local-private'))+[IO.Path]::DirectorySeparatorChar
    if(-not $taskCapture.StartsWith($taskPrivatePrefix,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetExtension($taskCapture) -ne '.png' -or (Test-Path -LiteralPath $taskCapture) -or -not (Test-Path -LiteralPath (Split-Path $taskCapture -Parent))){throw 'Choose a fresh PNG in an existing checkout local-private directory.'}
}
$taskPrivate=Join-Path $taskWorkspace 'local-private\round13\two-world'
New-Item -ItemType Directory -Force $taskPrivate | Out-Null
$taskAppData=Join-Path $taskPrivate 'appdata'; New-Item -ItemType Directory -Force $taskAppData | Out-Null
$taskLog=Join-Path $taskPrivate ('preview-'+[Guid]::NewGuid().ToString('N')+'.log')
$taskScene=Join-Path $taskPrivate 'TwoCreatureWorld.tscn'
@'
[gd_scene load_steps=2 format=3]
[ext_resource type="Script" path="res://TwoCreatureWorldPreview.cs" id="1"]
[node name="TwoCreatureWorldPreview" type="Node"]
script = ExtResource("1")
'@ | Set-Content -LiteralPath $taskScene
$taskArguments=@('--path',(Join-Path $taskWorkspace 'poc\Stardust.Godot'),'--log-file',$taskLog,$taskScene)
if(-not $CapturePath){$taskArguments+='--headless'}
$taskArguments+=@('--','--two-creature-world-preview','--asset-root',$taskArchiveRoot,'--asset-config',$taskConfiguration,'--two-fixture',$taskFixture,'--catalog-profile',$taskCatalog)
if($CapturePath){$taskArguments+=@('--two-capture',$taskCapture)}elseif($CancelLoad){$taskArguments+='--two-cancel'}
if($taskArguments.Where({$_ -match '["\r\n]'}).Count){throw 'Arguments containing quotes/newlines are unsupported.'}
$taskQuoted=$taskArguments.ForEach({'"'+$_+'"'})
$taskOldAppData=$env:APPDATA
try
{
    $env:APPDATA=$taskAppData
    $taskWindow='Hidden'
    $taskProcess=Start-Process -FilePath $taskGodot -ArgumentList $taskQuoted -WindowStyle $taskWindow -WorkingDirectory $taskWorkspace -PassThru
}
finally
{
    if($null -eq $taskOldAppData){Remove-Item Env:APPDATA -ErrorAction SilentlyContinue}else{$env:APPDATA=$taskOldAppData}
}
if(-not $taskProcess.WaitForExit(45000)){Stop-Process -Id $taskProcess.Id;throw 'Two creature fixture exceeded its 45-second deadline.'}
if($taskProcess.ExitCode -ne 0){Get-Content -LiteralPath $taskLog -Tail 24;throw ('Two creature fixture failed: '+$taskProcess.ExitCode+'. Log: '+$taskLog)}
$taskExpected=if($CancelLoad){'PASS: two canceled'}else{'PASS: two teardown'}
if(-not (Select-String -LiteralPath $taskLog -SimpleMatch $taskExpected)){throw ('Fixture exited without acceptance marker. Log: '+$taskLog)}
if(-not $CancelLoad -and -not (Select-String -LiteralPath $taskLog -SimpleMatch 'PASS: two teardown')){throw 'Two creature fixture exited without cleanup marker.'}
if($CapturePath -and (-not (Test-Path -LiteralPath $taskCapture) -or -not (Test-Path -LiteralPath $taskSecond) -or -not (Test-Path -LiteralPath $taskThird))){throw 'GPU preview exited without its capture triple.'}
Write-Output 'PASS: bounded original two creature world diagnostic fixture.'
Write-Output ('Log: '+$taskLog)
