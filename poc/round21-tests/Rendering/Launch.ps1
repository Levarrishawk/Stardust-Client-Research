<# Offline synthetic world/journal source proof; cached native dependencies only. #>
[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$PythonPath,[Parameter(Mandatory=$true)][string]$PackageCache)
$ErrorActionPreference='Stop'
$taskWorkspace=(Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$taskPython=(Resolve-Path -LiteralPath $PythonPath).Path
$taskCache=(Resolve-Path -LiteralPath $PackageCache).Path
$taskPrivate=Join-Path $taskWorkspace 'local-private\round21\source'
New-Item -ItemType Directory -Force $taskPrivate | Out-Null
$taskAppData=Join-Path $taskPrivate 'appdata';New-Item -ItemType Directory -Force $taskAppData | Out-Null
$taskPeer=Join-Path $taskPrivate ('peer-'+[Guid]::NewGuid().ToString('N')+'.json')
$taskExport=Join-Path $taskPrivate ('export-'+[Guid]::NewGuid().ToString('N')+'.json')
$taskOldAppData=$env:APPDATA;$taskOldPackages=$env:NUGET_PACKAGES;$taskOldUtf8=$env:PYTHONUTF8
try {
    $env:APPDATA=$taskAppData;$env:NUGET_PACKAGES=$taskCache;$env:PYTHONUTF8='1'
    & $taskPython (Join-Path $taskWorkspace 'poc\round18-tests\Rendering\Generate-Peer.py') $taskPeer
    if($LASTEXITCODE){throw 'Synthetic peer generation failed.'}
    $taskProject=Join-Path $PSScriptRoot 'Rendering.csproj'
    dotnet restore $taskProject --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -p:NuGetAudit=false -v quiet
    if($LASTEXITCODE){throw 'Offline restore failed.'}
    dotnet run --project $taskProject --no-restore -p:NuGetAudit=false -p:UseSharedCompilation=false -- $taskPeer (Join-Path $taskWorkspace 'poc\preview-profiles\scene-object-catalog.json') $taskExport
    if($LASTEXITCODE){throw 'Customization source proof failed.'}
    Write-Output ('Export: '+$taskExport)
} finally {
    if($null -eq $taskOldAppData){Remove-Item Env:APPDATA -ErrorAction SilentlyContinue}else{$env:APPDATA=$taskOldAppData}
    if($null -eq $taskOldPackages){Remove-Item Env:NUGET_PACKAGES -ErrorAction SilentlyContinue}else{$env:NUGET_PACKAGES=$taskOldPackages}
    if($null -eq $taskOldUtf8){Remove-Item Env:PYTHONUTF8 -ErrorAction SilentlyContinue}else{$env:PYTHONUTF8=$taskOldUtf8}
}
