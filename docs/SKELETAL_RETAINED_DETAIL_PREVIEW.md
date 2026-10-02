# Retained original-human detail inspector

## Bounded behavior

The separate [launcher](../tools/powershell/Invoke-StardustRetainedDetailPreview.ps1) opens a manual retained detail-2/3 inspector. Both original bundles prepare once in one background-owned load. The [Core reader](../poc/Stardust.Core/Assets/RetainedSkeletalDetailBundle.cs) calls existing accepted creature/material/animation readers through a shared source-byte cache. No existing parser, material equation, mesh adapter or asset launcher is changed. The only existing entry-point edit is one `--retained-detail-preview` branch before `--asset-creature`.

Both Godot meshes construct on the main thread after the archives close. Selection samples the destination at the current shared idle-clock frame and toggles visibility. It keeps mesh/material/shader/texture resources and the clock instance; it performs no archive lookup or bundle preparation. Only the visible detail samples during ordinary ticks; activation catches up a hidden detail before making it visible. The two skin bindings explicitly select skeleton level 0 through the existing adapter. Manual selection is a modern diagnostic policy, not recovered automatic LOD scheduling.

Controls: Detail 2/3, play/pause idle, window-focus plus explicit playback gate, scrub, reset idle cursor, orbit drag/scroll and camera reset with R. Resetting the idle cursor does not replace its clock. The Node owns process/focus callbacks, pending-load cancellation, accepted bundles and GPU resources. Its loading button cancels the pending request. Tree exit disables updates, disposes the request/material/mesh resources, frees both geometry roots and drops bundle references. Late transfer is not published after exit.

## Original identities and budgets

The input object/SAT/configuration/idle and four MGN pins are enforced by the new reader/adapter and remain those in [the explicit-detail contract](SKELETAL_DETAIL_PREVIEW_CONTRACT.md). Original root is `C:/Stardust-DEV`; configuration SHA-256 is `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. Material shader/effect/texture/palette/PS20 source hashes are enforced from [the independently audited shading contract](SKELETAL_LOWER_DETAIL_SHADING_CONTRACT.md). Missing/masked authored palette rejects in this pinned inspector, even though the underlying general reader supports diagnostic palette fallback.

| Closure | Present paths | Source bytes | Surfaces | Expanded vertices | Triangles |
| --- | ---: | ---: | ---: | ---: | ---: |
| Detail 2 with selected idle | 25 | 580246 | 2 | 542 | 756 |
| Detail 3 with selected idle | 25 | 539928 | 2 | 163 | 190 |
| Shared union | 27 | 590916 | 4 retained | 705 retained | 946 retained |

Each external path resolves once across the union. Raw present payloads are copied into a local shared cache; existing readers take their own owned copies. Missing/masked results are cached too. No shared raw-cache dictionary escapes: it clears in `finally` on success, rejection and cancellation. The returned source registry contains identities, not extracted bytes. Resolver/overlay disposal remains the caller's responsibility; the Godot worker owns and closes its overlay with `using` before publication.

Default union limits are 64 attempted paths and 16 MiB raw source bytes; each existing per-detail reader also has 64/16 MiB limits. Each decoded image closure is limited to 2 MiB (4 MiB across the pair). Actual images are one 256x256 BC3-decoded RGBA image with nine mips per detail (349524 bytes each), shared by its MAIN/SPEC bindings. Both GPU upload sets remain separate and retained. Limits bound source bytes rather than all managed object overhead. Source cache copies exist temporarily alongside the two readers' owned data.

Both details use the accepted single-color nodot3 family, palette/albedo factors, power 20 and `ScalesBloomRgb=true`, with compressed normals disabled. Authored pixel semantics are confirmed by prior independent original-bytecode research. This inspector retains existing diagnostic lighting; it does not establish literal compiled vertex LIT precision, hardware candidate negotiation or full original scene lighting.

## Validation

The [native runner](../poc/round4-tests/Rendering/Program.cs) directly calls the new Core reader. Original geometry/source/mip counts and external read-once assertions pass. Clearing every resolver payload preserves geometry, sampled idle matrices and decoded mip fingerprints. Twenty negative cases cover source/byte/invalid budgets, pre-cancel without lookup, missing/masked/truncated selected inputs including authored palette, and cancellation during the second detail. Three-byte malformed inputs are synthetic negative fixtures.

The deterministic Godot fixture advances 480 ticks/two shared idle loops, performs 132 selections, and checks current-frame activation, visibility, direct same-frame pose agreement, pause/focus/resume/scrub/reset signals and teardown. Mesh/material/shader/texture instance IDs, static geometry and (GPU only) mip bytes remain identical before/after each switch. Existing `VerifyBoundary(true)` reads actual surface arrays, material uniforms and full uploaded mip bytes for both details. Headless checks do not claim GPU readback.

Fresh hidden GPU captures for both details at idle frame 45 show the entire body below controls. The accepted single-color eight-case synthetic bloom/fade/alpha GPU oracle also passes (maximum display error 0.003052 at 2/255 tolerance, bloom discriminator 0.056471). Native/Godot comparison matches all 27 path/priority/byte/hash identities. Eleven launcher negatives reject before engine start. Headless and first-lookup cancellation pass; cancellation publishes no geometry and closes the archive. Fresh unchanged detail-0 full interior GPU regression passes retained room/cell ownership and prior GPU assertions.

Tested with .NET 8.0.300, Godot 4.7.2 .NET, OpenGL compatibility and RTX 4070 SUPER. The existing startup certificate-store error remains in logs despite passed acceptance markers. Vulkan and original executable execution were not run. No server or network contact was used.

## Reproduction

Use an explicit accepted Core package cache for the console. Main's accepted cache is `local-private/nuget-packages`; the worker used its existing `local-private/round2/nuget-packages`. Do not replace lock hashes. Godot.NET.Sdk 4.7.2 resides in the user's default NuGet SDK resolver cache; remove `NUGET_PACKAGES` for main's existing restored Godot build. Accepted Godot assembly package assets and valid restored project assets are prerequisites. These commands do not promise an uncached Godot restore. Adjust the explicit Core cache/tool paths to an accepted installation if different. The sources-cleared config below allows the bounded console restore without package feeds.

```powershell
$env:APPDATA = Join-Path (Get-Location) 'local-private/round4/build-appdata'
$taskCoreCache = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
$env:NUGET_PACKAGES = $taskCoreCache
$env:PYTHONUTF8 = '1'
New-Item -ItemType Directory -Force $env:APPDATA, local-private/round4 | Out-Null
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content -Encoding utf8 local-private/round4/NuGet.Config
dotnet restore poc/round4-tests/Rendering/Rendering.csproj --configfile local-private/round4/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round4-tests/Rendering/Rendering.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false -p:NuGetAudit=false
dotnet poc/round4-tests/Rendering/bin/Debug/net8.0/Rendering.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg > local-private/round4/native.txt
# Godot SDK resolution uses the user's default SDK cache. Existing assembly assets must already be restored.
Remove-Item Env:NUGET_PACKAGES -ErrorAction SilentlyContinue
dotnet build poc/Stardust.Godot/Stardust.Godot.csproj --no-restore -p:UseSharedCompilation=false -p:NuGetAudit=false -p:RestoreConfigFile=local-private/round4/NuGet.Config
$taskGodot = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/godot-4.7.2-dotnet/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe'
$taskArguments = @{GodotPath=$taskGodot; ArchiveRoot='C:/Stardust-DEV'; Configuration='C:/Stardust-DEV/swgemu_live.cfg'}
& ./tools/powershell/Invoke-StardustRetainedDetailPreview.ps1 @taskArguments -Headless
& ./tools/powershell/Invoke-StardustRetainedDetailPreview.ps1 @taskArguments -CancelLoad
foreach ($taskDetail in @('2','3')) {
    $taskCapture = Join-Path (Get-Location) ('local-private/round4/detail'+$taskDetail+'-'+[guid]::NewGuid().ToString('N')+'.png')
    & ./tools/powershell/Invoke-StardustRetainedDetailPreview.ps1 @taskArguments -SelectedDetail $taskDetail -CapturePath $taskCapture
}
& ./poc/round4-tests/Rendering/verify_options.ps1 @taskArguments
# Pass the private log paths printed by the preceding launcher runs.
python poc/round4-tests/Rendering/verify_sources.py local-private/round4/native.txt <detail2-log> <detail3-log> <headless-log>
python tools/python/validate_organized_workspace.py .
git diff --check
```

For manual controls, use the same launcher with `-Interactive`. Bounded modes run hidden, require fresh captures under the checkout's ignored private directory and have a 45-second deadline. No captured original image/bytes are curated. Lead owns combined runner/status/registration changes. Next action is integration review; any automatic detail policy is a separate assignment.
