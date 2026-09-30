<# Generates standards-only synthetic fixtures and runs the replacement Godot host; no original assets or server. #>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$GodotPath,
    [Parameter(Mandatory=$true)][string]$PythonPath,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [switch]$Headless,
    [switch]$Materials
)
$ErrorActionPreference='Stop'
$taskWorkspace=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$taskGodot=(Resolve-Path -LiteralPath $GodotPath).Path
$taskPython=(Resolve-Path -LiteralPath $PythonPath).Path
$taskPrivate=[IO.Path]::GetFullPath((Join-Path $taskWorkspace 'local-private'))+[IO.Path]::DirectorySeparatorChar
$taskOutput=[IO.Path]::GetFullPath($OutputDirectory)
if(-not $taskOutput.StartsWith($taskPrivate,[StringComparison]::OrdinalIgnoreCase)){throw 'Generated verification output must remain under ignored local-private.'}
if(Test-Path -LiteralPath $taskOutput){throw 'Choose a fresh output directory to preserve previous proof.'}
New-Item -ItemType Directory -Path $taskOutput | Out-Null
$taskFixtures=Join-Path $taskOutput 'fixtures'
& $taskPython (Join-Path $taskWorkspace 'tools/python/generate_bc6h_fixtures.py') $taskFixtures | Out-File (Join-Path $taskOutput 'fixture-identities.txt') -Encoding utf8
if($LASTEXITCODE -ne 0){throw 'Synthetic HDR fixture generation failed.'}
if($Materials)
{
    & $taskPython (Join-Path $taskWorkspace 'tools/python/generate_hdr_material_fixtures.py') $taskFixtures | Out-File (Join-Path $taskOutput 'material-fixture-identities.txt') -Encoding utf8
    if($LASTEXITCODE -ne 0){throw 'Synthetic HDR material fixture generation failed.'}
}
$taskLog=Join-Path $taskOutput 'verification.log'
$taskArguments=@('--log-file',$taskLog,'--path',(Join-Path $taskWorkspace 'poc/Stardust.Godot'))
if($Headless){$taskArguments+='--headless'}
$taskArguments+=@('--','--hdr-fixtures',$taskFixtures)
if($Materials){$taskArguments+='--hdr-materials'}
if($taskArguments.Where({$_ -match '["\r\n]'}).Count){throw 'Arguments containing quotes/newlines are unsupported.'}
$taskQuoted=$taskArguments.ForEach({'"'+$_+'"'})
$taskOldAppData=$env:APPDATA
try
{
    $env:APPDATA=Join-Path $taskWorkspace 'local-private/godot-appdata'
    $taskProcess=Start-Process -FilePath $taskGodot -ArgumentList $taskQuoted -WindowStyle Hidden -WorkingDirectory $taskWorkspace -PassThru
}
finally
{
    if($null -eq $taskOldAppData){Remove-Item Env:APPDATA -ErrorAction SilentlyContinue}else{$env:APPDATA=$taskOldAppData}
}
if(-not $taskProcess.WaitForExit(45000)){Stop-Process -Id $taskProcess.Id;throw 'Owned HDR verifier exceeded the 45-second deadline.'}
Get-Content -LiteralPath $taskLog -Tail 3
if($taskProcess.ExitCode -ne 0){throw ('HDR verification failed. Log: '+$taskLog)}
if(-not (Select-String -LiteralPath $taskLog -SimpleMatch 'PASS: independent BC6H half agreement fixtures=40 mips=264')){throw 'Expected complete BC6H gate was not reported.'}
if($Materials -and -not (Select-String -LiteralPath $taskLog -SimpleMatch 'PASS: shared BC6H material bundles fixtures=40 mips=264 templates=80')){throw 'Expected HDR material gate was not reported.'}
Write-Output ('PASS: bounded HDR verification. Log: '+$taskLog)
