# Asynchronous placed scene creature preview

R7-RENDER-01, October 2, 2026. This offline presentation fixture composes the accepted [initializer](SCENE_CREATURE_RESOURCE_INITIALIZER_CONTRACT.md), [placement](SCENE_CREATE_PLACEMENT_CONTRACT.md), [resource reader](SCENE_CREATURE_RESOURCE_CONTRACT.md), and [visual](SCENE_CREATURE_VISUAL_CONTRACT.md). It does not implement a world minimum barrier, live state, or an original runtime recovery policy.

## Transfer and publication

`AsyncSceneCreaturePreview.DecodeFixture` decodes an independently authored 47-byte application create. Clearing its input bytes leaves the scalar DTO owned. The preview creates the registry object, accepts end-baselines, snapshots the finalized object into an owned resource request, and starts the accepted initializer. A `Task.Run` worker opens and closes its own configured overlay and reads through the unchanged original resource reader. Its cancellation token is associated with the task as well as passed to the reader. There are no Godot calls or visual allocations on that worker.

After completion, main-thread `Poll` transfers the prepared closure exactly once. Publication requires `initializer.IsCurrent(ticket)` in addition to the visual's exact registry/create-byte identity. The preview explicitly chooses `SceneCreateZeroQuaternionPolicy.Reject`; its nonunit quaternion is normalized by the accepted Core builder. The unchanged `GodotAppearanceTransform.ToGodot` applies the Z reflection once. The derived transform, not the unprocessed wire fields, is passed to the visual. No parent/container or root-motion inference is made: this fixture selects its identity scene parent.

The existing visual adds one optional `Func<bool>` current-attempt guard. Omission preserves the R6 API behavior. The guard is checked before allocation, after staged allocation, after attachment while hidden, and on every guarded update. Callbacks execute on the main thread and must be serialized, nonthrowing freshness checks. Disposal drops the callback reference. A same-handle restart retires the previous attempt on the next guarded operation even though its registry handle remains current. Registry changes and attempt changes must be processed before rendering; there is no automatic registry subscription.

## Ownership, capacity, and controls

The preview owns one registry, one initializer, its worker-task tracking, immutable request/configuration snapshots, current visual, animation clocks, and environment resource. Limits are two outstanding jobs (including cancellation-ignoring work), one tracked initializer object, and registry capacity eight. Request/source/image budgets remain those of the accepted reader. Capacity rejection leaves the previous current ticket intact; completed/stale jobs must be polled before capacity is reused. Cancellation is cooperative and cannot terminate a worker forcibly.

Interactive controls provide spawn, load, cancel, restart, replace, remove, reset scene, idle/walk/run, pause, and orbit/zoom/reset camera. Spawn finalizes the diagnostic object; replace changes X by one, finalizes its fresh revision, and requests a load. Remove/reset retire stale visuals and polling cancels invalidated pending jobs. Load without restart follows initializer deduplication; explicit restart is required to retry a completed or canceled attempt. Walk/run are presentation clips with caller clocks and do not move an authoritative simulation object.

Explicit shutdown disposes the visual and initializer, closes the registry, and awaits `DrainCompletion` before releasing retained environment/configuration state. Normal interactive window close disables automatic quit and takes this awaited path. An externally forced tree exit cannot await a Godot notification; its fallback starts shutdown and observes completion errors. Forced process termination is not a drain guarantee. Fixtures execute the awaited path and prove outstanding jobs reach zero.

## Observations and confidence

Confirmed modern implementation observations: native and Godot workers transfer the same 37 original source identities, 4,683,848 source bytes, ten textures, and prepared idle/walk/run; archives close before transfer and every path is read once. Mutating borrowed native resolver buffers leaves owned geometry, mip bytes, and sampled poses unchanged. Configuration SHA-256 is `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`; the accepted reader/profile retain root, SAT, and clip pins. No original executable addresses or original dynamic execution support new claims here.

The synthetic quaternion is binary32 `(0.2, -0, 0.3, 1.7)`, with wire position X/Z/Y `(-2, 7, 4)`, CRC `AF1DC1A1`, object ID `DEADBEEF00000001`, and raw flag 255. Derived source translation is `(-2, 4, 7)`; actual engine origin is `(-2, 4, -7)`. Raw negative zero and all encoded bits remain unchanged. Input quaternion norm is approximately 1.7378147687453016; normalization and zero rejection are modern presentation choices, not original repair semantics.

The native console calls actual production identity/transform methods and invokes the existing managed adapter through reflection; it allocates no native Godot objects and makes no GPU claim. Nineteen native identity/transform negatives pass. An independent Python oracle packs the raw packet, uses `math.hypot` and quaternion cross products to derive rotation, and checks source rows, reflected engine columns, translation, and five basis/asymmetric point probes. Basis tolerance is 2e-7; point tolerance is 3e-6 times maximum of one and expected magnitude. All 37 path/priority/size/hash tuples match native, headless, and GPU runs.

Godot fixtures retain topology and root/material/shader/MAIN texture identities through 96 idle ticks, 64 explicit movement updates, visibility, focus, and pause. GPU checks verify authored mip readback and capture idle frame 45. Controlled task barriers separately prove same-handle restart retirement, stale preflight with no allocation, cleanup after staged allocation and after reentrant attachment, capacity rollback at two outstanding jobs, five registry changes, canceled late success, stale late fault, and disposal with success/fault workers that deliberately ignore cancellation. Finalize starts from a pending object; contain/replace/destroy/reset start from finalized objects. These controlled completions reuse an accepted parsed closure and are synthetic scheduling evidence, distinct from the actual original archive worker.

First-lookup cancellation closes the real overlay with zero visual publications. Seven invalid launcher combinations reject before engine launch. The unchanged R6 headless fixture passes with the optional guard absent. Known certificate-store startup diagnostics remain in passing Godot logs. The inspected capture shows the complete authored MAIN actor in the derived rotation; customization factors/generated face and normal/specular shading remain outside this slice. Vulkan, arbitrary species, original quaternion normalization, real container placement, and live replication remain unverified.

## Reproduction

Use .NET 8, Godot 4.7.2 .NET, the pinned configured `C:/Stardust-DEV` corpus, existing accepted SDK/package caches, and an existing sources-cleared private NuGet configuration. No downloads or lock edits are required. From this checkout:

```powershell
$env:APPDATA = Join-Path (Get-Location) 'local-private/round7/appdata'
New-Item -ItemType Directory -Force $env:APPDATA | Out-Null
Remove-Item Env:NUGET_PACKAGES -ErrorAction SilentlyContinue
dotnet build poc/Stardust.Godot/Stardust.Godot.csproj --no-restore -p:UseSharedCompilation=false -p:NuGetAudit=false
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
dotnet build poc/round7-tests/Rendering/Rendering.csproj --configfile local-private/round2/NuGet.Config -p:UseSharedCompilation=false -p:UseAppHost=false -p:NuGetAudit=false
dotnet poc/round7-tests/Rendering/bin/Debug/net8.0/Rendering.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/human-locomotion.json
```

Build Godot first: the console references its actual Debug adapter DLL, without a `net8.0` subdirectory, and the accepted managed GodotSharp DLL. `GodotManagedReference` can override that managed DLL path. Clearing `NUGET_PACKAGES` for Godot preserves its default already-restored SDK resolver; the console cache is separate. An uncached restore is not promised.

Run [the launcher](../../SWG-Godot-Client/tools/powershell/Invoke-StardustAsyncSceneCreaturePreview.ps1) with `-GodotPath`, `-ArchiveRoot C:/Stardust-DEV`, and `-Configuration C:/Stardust-DEV/swgemu_live.cfg`. Default/`-Headless` executes the bounded fixture; `-CancelLoad` executes first-lookup cancellation; `-CapturePath` selects a separate GPU run to a fresh PNG under an existing checkout private directory; `-Interactive` opens controls. Automated runs are hidden and bounded to 45 seconds.

The [oracle](../../SWG-Godot-Client/poc/round7-tests/Rendering/verify_preview.py) takes native, GPU, headless, and cancellation logs in that order. The [options verifier](../../SWG-Godot-Client/poc/round7-tests/Rendering/verify_options.ps1) takes the three launcher inputs plus `-ExistingCapture`. Original static source audit remains the accepted [reader verifier](../../SWG-Godot-Client/tools/python/verify_scene_creature_resources.py). See the [R7 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R7_ASYNC_CREATURE_PREVIEW.md) for scope and next action.


### Lead retention review in Round8

Completed loader tasks are pruned during scheduling and polling, so interactive reloads do not retain historical Task result bundles until shutdown. The initializer still observes completion/faults and owns drain; only unfinished jobs and the current presentation closure remain tracked. This lead fix is verified with the final integrated legacy headless/cancellation runs recorded in [Round8](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_8_2026-10-02.md).
