# Automatic retained original-human detail preview

## Scope and compatibility

The existing retained inspector now precreates original details 0..3. Manual buttons select a detail and disable Auto. Auto uses the accepted engine-neutral [SkeletalDetailSelector](SKELETAL_DETAIL_SELECTION_POLICY.md). Defaults are explicitly modern presentation choices: projected diameters `(240,120,60)` pixels and hysteresis `0.10`. They are shown in the UI; no recovered legacy thresholds, scheduler, hardware selection or automatic skeleton coupling is claimed. This is an isolated inspector, not interior/gameplay LOD integration.

[Retained preparation](../../SWG-Godot-Client/poc/Stardust.Core/Assets/RetainedSkeletalDetailBundle.cs) adds `LoadAll` returning `RetainedSkeletalDetailSet.Details[0..3]`. R4 `Load`, `RetainedSkeletalDetailBundle.Detail2/Detail3`, limits/signature and pair closure remain compatible. Both APIs use one shared bounded cache through existing readers. Every exposed MLOD member is selected from the original object/SAT chain; all eight MGN identities match [the original lower-detail table](SKELETAL_LOWER_DETAIL_CONTRACT.md). Selected idle and explicit skeleton0 remain unchanged. No Main, selector, parser, mesh/material equation or old asset launcher changes are required.

Details0/1 prepare accepted texture-choice/compressed-normal/specular resources and use the existing normal/tangent adapter path. Details2/3 use the accepted vertex-lit-only material path. Both families retain authored material/palette/albedo, selected idle and source-backed pixel semantics. Generated face, customization and shape editing remain outside this inspector. Diagnostic lighting and compiled vertex LIT precision remain as bounded in earlier contracts.

## Owned preparation and counts

| Detail | Paths | Source bytes | Expanded vertices | Triangles | Images | Mips | Decoded image bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 0 | 48 | 5751879 | 1790 | 2810 | 13 | 110 | 18524828 |
| 1 | 48 | 5648263 | 1256 | 1904 | 13 | 110 | 18524828 |
| 2 | 25 | 580246 | 542 | 756 | 1 | 9 | 349524 |
| 3 | 25 | 539928 | 163 | 190 | 1 | 9 | 349524 |

The union has58 unique present paths/6111276 raw bytes, externally resolved once. The independently owned decoded image closures total37748704 bytes. Fine-detail images include the existing bounded texture-choice alternatives, even though this inspector adds no texture-choice controls. Expanded vertices count adapter surface records, not unique MGN positions. Pair `Load` still has27 paths/590916 bytes and the original25-path details2/3 closures.

`LoadAll` defaults to128 attempted paths/32MiB union source bytes. Each unchanged per-detail reader is bounded64 paths/16MiB. Decoded images are limited48MiB per fine detail,2MiB per coarse detail and100MiB across all four. These bound source/image payloads, not all managed-object overhead. Temporary shared raw copies coexist with owned per-reader data and clear in `finally`. The caller owns its resolver; the worker closes the original overlay before main-thread construction. Missing/masked/truncated pinned inputs reject, including authored palette; pre/mid cancellation publishes no partial set.

## Camera conversion and selection

The [Node adapter](../../SWG-Godot-Client/poc/Stardust.Godot/SkeletalRetainedDetailPreview.cs) merges all four initial idle-frame0 AABBs, fixes the sphere center at that union's center and uses half its diagonal as local radius. This is a fixed presentation bound, not an exact animated silhouette. All retained roots must have the same world transform. The world center is that transform applied to the local center; world radius multiplies local radius by the largest absolute basis scale. Nonuniform orthogonal scaling is supported; shear rejects. Default roots use identity scale. An invalid projection leaves selector state unchanged and causes the interactive boundary guard to stop the preview.

Center depth is negative Z after transforming the world center by `camera.GetCameraTransform().AffineInverse()`; this uses the actual camera transform including offsets. Camera projection is perspective with `KeepAspect=Height`, so FOV is vertical in degrees. Height is the viewport visible rectangle height in pixels, converted to a checked integer, not OS screen height. Tested GPU viewport is1280x720. The accepted selector computes `P=(r/z)*H/tan(FOV*pi/360)`. It uses center depth and a small-angle sphere approximation, not off-axis silhouette, exact clipping/occlusion or terrain visibility.

Depth at/below radius chooses detail0 with null projected pixels before division; zero depth is finite. Negative/behind-camera depth and invalid bounded/nonfinite inputs reject atomically. Near depth is a conservative proxy, not proof that an off-axis eye is inside the sphere. Headless Godot exposes a64-pixel dummy viewport; the headless fixture explicitly supplies720 projection pixels while retaining actual camera transforms. It does not claim GPU readback.

Auto evaluates before visible-pose ticking and after its toggle/reset. A changed selection samples the destination at the shared clock's current frame before visibility changes. Same selection does not resample unnecessarily. Manual buttons disable Auto and reset selector history while preserving clock phase. Reset idle seeks frame0 and clears selector history; when Auto is enabled it immediately reinitializes from the current camera. Focus/pause/scrub and orbit controls remain retained. No selection reopens an archive, constructs an adapter, replaces a clock, or recreates mesh/material/shader/texture objects. Existing pose upload may update surface buffers on the retained mesh object.

## Validation and evidence

The [native runner](../../SWG-Godot-Client/poc/round5-tests/Rendering/Program.cs) confirms all original geometry/closure counts,58 external reads, R4 pair compatibility, and ownership after clearing every resolver payload (geometry, sampled idle matrices and mip fingerprints).29 negatives include source/byte/invalid budgets, missing/masked/truncated pinned inputs across all details, authored palette, pre/second-detail cancellation, and atomic invalid projection samples. Three-byte malformed inputs are modern synthetic fixtures.

Four fresh GPU captures at idle frame45 show complete bodies below controls. Each run passes480 ticks/two idle loops,134 manual selections/current hidden phase, actual mip/MATL/normal/tangent surface readback and retained resource IDs/static geometry/mip fingerprints. Actual camera sweeps exercise0/1/2/3 in both directions;24 alternating235/245-pixel samples retain detail1,275 refines and210 coarsens. Reset at245 reinitializes detail0. Nonuniform root scale, near/inside/zero/behind-camera guard, manual override and teardown also pass. [Independent verifier](../../SWG-Godot-Client/poc/round5-tests/Rendering/verify_preview.py) matches all58 Native/Godot path/priority/byte/hash identities and34 projected diameter/hysteresis cases per run.

Headless with Auto and first-lookup cancellation pass. Eleven [launcher negatives](../../SWG-Godot-Client/poc/round5-tests/Rendering/verify_options.ps1) reject before engine launch. Fine-detail captures also run the accepted twelve-case normal/specular GPU oracle (maximum0.002764); coarse captures run eight single-color cases (maximum0.003052). Both use2/255 display tolerance. Fresh unchanged full detail0 interior regression passes its prior retained room/cell/material/pose gates. Builds pass zero warnings/errors. Tested .NET8.0.300/Godot4.7.2 .NET/OpenGL compatibility/RTX4070 SUPER. Startup certificate-store errors persist despite passed offline assertions. Vulkan, high-DPI/VR projection, animated bound recomputation and original executable/live server were not tested.

## Reproduction

Use accepted original root/configuration pins from [the retained contract](SKELETAL_RETAINED_DETAIL_PREVIEW.md) and accepted locked package assets. Console cache is an explicit input; main uses `local-private/nuget-packages`. Godot.NET.Sdk4.7.2 must already exist in the user's default SDK resolver cache, with accepted restored Godot assembly assets. Do not replace lock hashes or promise an uncached Godot restore.

```powershell
$env:APPDATA = Join-Path (Get-Location) 'local-private/round5/build-appdata'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
$env:PYTHONUTF8 = '1'
New-Item -ItemType Directory -Force $env:APPDATA, local-private/round5 | Out-Null
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content -Encoding utf8 local-private/round5/NuGet.Config
dotnet restore poc/round5-tests/Rendering/Rendering.csproj --configfile local-private/round5/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round5-tests/Rendering/Rendering.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false -p:NuGetAudit=false
dotnet poc/round5-tests/Rendering/bin/Debug/net8.0/Rendering.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg > local-private/round5/native.txt
Remove-Item Env:NUGET_PACKAGES -ErrorAction SilentlyContinue
dotnet build poc/Stardust.Godot/Stardust.Godot.csproj --no-restore -p:UseSharedCompilation=false -p:NuGetAudit=false -p:RestoreConfigFile=local-private/round5/NuGet.Config
$taskGodot = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/godot-4.7.2-dotnet/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe'
$taskArguments = @{GodotPath=$taskGodot; ArchiveRoot='C:/Stardust-DEV'; Configuration='C:/Stardust-DEV/swgemu_live.cfg'}
& ./tools/powershell/Invoke-StardustRetainedDetailPreview.ps1 @taskArguments -Headless -Auto
& ./tools/powershell/Invoke-StardustRetainedDetailPreview.ps1 @taskArguments -CancelLoad
foreach ($taskDetail in @('0','1','2','3')) {
    $taskCapture = Join-Path (Get-Location) ('local-private/round5/detail'+$taskDetail+'-'+[guid]::NewGuid().ToString('N')+'.png')
    & ./tools/powershell/Invoke-StardustRetainedDetailPreview.ps1 @taskArguments -SelectedDetail $taskDetail -CapturePath $taskCapture
}
& ./poc/round5-tests/Rendering/verify_options.ps1 @taskArguments
# Use private log paths printed by the launcher.
python poc/round5-tests/Rendering/verify_preview.py local-private/round5/native.txt <gpu0-log> <gpu1-log> <gpu2-log> <gpu3-log> <headless-log>
python tools/python/validate_organized_workspace.py .
git diff --check
```

Use `-Interactive -Auto` for an initially automatic live inspector; manual detail defaults to2 otherwise. Capture mode runs the automatic fixture then selects the requested manual detail for comparable whole-body captures. Bounded modes run hidden with fresh private PNG paths and45-second deadlines. Original-derived images/logs remain ignored. Lead owns shared registration/status changes. Next step: lead integration review; no next milestone begins here.
