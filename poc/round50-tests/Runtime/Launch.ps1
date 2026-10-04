<# Bounded offline native Godot sourced-input proof; no original executable/assets/server/GPU. #>
[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$GodotPath,[Parameter(Mandatory=$true)][string]$PackageCache,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$taskRoot=(Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$taskExe=(Resolve-Path -LiteralPath $GodotPath).Path
$taskCache=(Resolve-Path -LiteralPath $PackageCache).Path
$taskOutput=[IO.Path]::GetFullPath($OutputDirectory)
$taskPrivate=[IO.Path]::GetFullPath((Join-Path $taskRoot 'local-private'))+[IO.Path]::DirectorySeparatorChar
if(-not $taskOutput.StartsWith($taskPrivate,[StringComparison]::OrdinalIgnoreCase)){throw 'Choose an own local-private output directory.'}
# Inspect lexical ancestors before creating any output, including the target itself.
$taskAncestor=[IO.DirectoryInfo]::new($taskOutput)
while($null -ne $taskAncestor){
    $taskItem=Get-Item -LiteralPath $taskAncestor.FullName -Force -ErrorAction SilentlyContinue
    if($null -ne $taskItem -and ($taskItem.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Linked output ancestor rejected.'}
    $taskAncestor=$taskAncestor.Parent
}
if(Test-Path -LiteralPath $taskOutput){throw 'Fresh output directory required.'}
New-Item -ItemType Directory -Path $taskOutput | Out-Null
$taskProject=Join-Path $PSScriptRoot 'Godot'
$taskFixture=Join-Path $PSScriptRoot 'ordered-fixtures.json'
$taskReceipt=Join-Path $taskOutput ('receipt-'+[Guid]::NewGuid().ToString('N')+'.json')
$taskLog=Join-Path $taskOutput ('godot-'+[Guid]::NewGuid().ToString('N')+'.log')
$env:APPDATA=Join-Path $taskOutput 'appdata'
$env:NUGET_PACKAGES=$taskCache
$env:STARDUST_R50_GODOT_NUPKGS=Join-Path (Split-Path $taskExe -Parent) 'GodotSharp/Tools/nupkgs'
if(-not(Test-Path -LiteralPath $env:STARDUST_R50_GODOT_NUPKGS)){throw 'Installed Godot offline package source required.'}
dotnet restore (Join-Path $taskProject 'Godot.csproj') --configfile (Join-Path $taskProject 'NuGet.Config') -p:NuGetAudit=false
if($LASTEXITCODE -ne 0){throw 'Offline Godot restore failed.'}
dotnet build (Join-Path $taskProject 'Godot.csproj') --no-restore -p:NuGetAudit=false -p:UseSharedCompilation=false -p:UseAppHost=false
if($LASTEXITCODE -ne 0){throw 'Godot build failed.'}
$taskArguments=@('--headless','--quit-after','1800','--path',$taskProject,'--log-file',$taskLog,'--','--fixture',$taskFixture,'--output',$taskReceipt)
if($taskArguments.Where({$_ -match '["\r\n]'}).Count){throw 'Unsupported quoted/newline arguments.'}
$taskQuoted=$taskArguments.ForEach({'"'+$_+'"'})
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList $taskQuoted -WindowStyle Hidden -WorkingDirectory $taskRoot -PassThru
if(-not $taskProcess.WaitForExit(45000)){Stop-Process -Id $taskProcess.Id;throw 'Own Godot proof exceeded 45 seconds.'}
if($taskProcess.ExitCode -ne 0 -or -not(Test-Path -LiteralPath $taskReceipt) -or -not(Select-String -LiteralPath $taskLog -SimpleMatch 'R50_GODOT_PASS')){throw "Godot proof failed: $taskLog"}
Write-Output "R50_GODOT_RECEIPT=$taskReceipt"
Write-Output "R50_GODOT_LOG=$taskLog"
