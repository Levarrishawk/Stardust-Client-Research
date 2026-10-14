# Owned scene creature visual contract

R6-RENDER-01, 2026-10-02. This is a bounded replacement-client adapter contract, not recovered original runtime behavior. It composes the accepted [scene resource contract](SCENE_CREATURE_RESOURCE_CONTRACT.md) and [registry contract](SCENE_OBJECT_REGISTRY_CONTRACT.md).

## Publication and identity

`SceneCreatureVisual.TryPublish` receives an owner registry, exact handle, parsed `SceneCreatureResources`, explicit engine `Transform3D`, and live parent. `Matches` checks registry identity, scene generation, object ID, revision, and the exact encoded create bytes. Float bit patterns and the raw flag matter; negative zero cannot match positive zero. A stale preflight returns null without visual allocation. The candidate stays hidden during allocation and attachment; checks after the staging callback and after attachment reject intervening invalidation and synchronously free the candidate.

All visual creation, updates, and disposal require the Godot main thread. The registry must be serialized with those operations. Finalize, contain, replace, destroy, reset, and close invalidate the old handle. Retirement occurs on the next guarded visual operation, which disposes the old root/resources. There is no registry event subscription: the caller must tick/check after registry changes before rendering. A fresh current snapshot requires explicit publication. This strict modern policy does not assert original seamless reconciliation.

## Placement, animation, and ownership

The caller supplies engine placement. No source-create quaternion or position conversion occurs. The accepted basis is finite, orthogonal, nonsingular, with axis lengths from 1e-4 through 1e4, normalized pairwise dot magnitude at most 1e-4, and each origin component at most 1e6 in magnitude. Orthogonal nonuniform scale and reflection are accepted. This validation is a modern bounded policy, not an original coordinate specification.

The visual owns its idle clock, root, mesh, materials, shader resources, and textures. Tick accepts finite seconds from zero through 60 and clamps advancement to 0.25 seconds. Pause/focus gate idle advancement; visibility changes retain resources. Explicit optional walk/run frames use the existing mesh helper and caller-owned movement clocks. Resume idle reuses its clock. Dispose is idempotent on the main thread and frees the native root immediately, releases mesh resources, and drops parsed-source and clock references.

Pose, placement, focus, pause, and visibility updates retain geometry topology and root/material/shader/MAIN texture identities. GPU checks verify authored mip bytes. The accepted loader retains its existing limits and ownership: 64 source entries and 16 MiB aggregate source budget, with the existing decoded-texture budget; no shared reader or clock changed.

## Evidence and limits

The original closure contains 37 distinct sources, 4,683,848 bytes, ten decoded textures, four MAIN shader bindings, and 53 bound joints. Each resolver path is read once. Mutating borrowed resolver buffers after loading leaves owned geometry, mip bytes, and sampled idle/walk/run poses unchanged. Configuration SHA-256 is `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. Human template CRC is `AF1DC1A1`; accepted template/SAT and clip pins remain in the accepted reader/profile.

Confirmed by replacement-code execution: native production identity/transform methods reject 19 negative cases; headless and OpenGL GPU fixtures exercise 128 idle and 96 movement updates, six invalidating transitions, explicit new revision publication, stale preflight, staged cleanup, reentrant post-attachment cleanup, wrong-thread rejection, and teardown. GPU capture verifies mip readback and explicit engine origin `(1.4, 0.2, -0.75)`. The synthetic create uses source fields `(positionX=-2, positionZ=7, positionY=4)` and a nonunit quaternion; these remain unapplied. Its 47-byte oracle is synthetic, not a captured packet. Cancellation occurs during the first lookup and closes the worker-owned overlay.

Native tests reference the actual built adapter and managed Godot math library; they exercise pure methods without allocating Godot objects or claiming GPU execution. `verify_preview.py` independently packs the synthetic create and compares every path, priority, length, and hash between native/headless/GPU runs. Seven invalid launcher combinations fail before engine launch. The prior accepted [source reader audit](../../SWG-Godot-Client/tools/python/verify_scene_creature_resources.py) remains the original-format evidence; renderer tests do not replace it.

Authored MAIN textures are retained; this slice does not apply customization albedo factors or generated face textures, nor implement normal/specular shading. The pale captured actor is not a claim of complete customized appearance. Vulkan, general species, source-to-engine placement, attachment reconciliation, and automatic registry notifications remain unverified or deferred. The Godot startup certificate-store diagnostic remains in logs and did not fail fixture assertions.

## Reproduction

Use the original `C:/Stardust-DEV` overlay and pinned `swgemu_live.cfg`. Godot 4.7.2 .NET and its already restored SDK assets are prerequisites. Build the adapter first with the default SDK resolver; do not redirect it to the console-only package cache:

```powershell
Remove-Item Env:NUGET_PACKAGES -ErrorAction SilentlyContinue
dotnet build poc/Stardust.Godot/Stardust.Godot.csproj --no-restore -p:UseSharedCompilation=false -p:NuGetAudit=false
```

Then build the console using the accepted offline Core cache. Supply a sources-cleared NuGet configuration and existing accepted dependencies; this does not promise an uncached network restore. `GodotManagedReference` can override the accepted user-cache GodotSharp DLL path. The adapter reference is `.godot/mono/temp/bin/Debug/Stardust.Godot.dll` (no target-framework subdirectory).

```powershell
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
dotnet build poc/round6-tests/Rendering/Rendering.csproj --configfile local-private/round2/NuGet.Config -p:UseAppHost=false -p:UseSharedCompilation=false -p:NuGetAudit=false
dotnet poc/round6-tests/Rendering/bin/Debug/net8.0/Rendering.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/human-locomotion.json
```

Use `tools/powershell/Invoke-StardustSceneCreatureVisualPreview.ps1` with explicit `-GodotPath`, `-ArchiveRoot`, and `-Configuration`. Default or `-Headless` runs the headless fixture; `-CancelLoad` runs cancellation; `-CapturePath` takes a separate bounded GPU capture to a fresh PNG in an existing checkout private directory. `-Interactive` explicitly opens controls. Automated runs have a 45-second deadline. `verify_preview.py` takes native, GPU, headless, and cancellation log paths in that order. `verify_options.ps1` takes those three launcher inputs and `-ExistingCapture`.

See the [R6 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R6_CREATURE_VISUAL.md) for the handoff and precise next probe.


## World-owned publication extension — Round8

The [offline world preview](OFFLINE_SCENE_WORLD_PREVIEW_CONTRACT.md) adds TryPublishWorld and MatchesWorld. A private identity delegate preserves the older registry/create/attempt guards and checks authentic world publications before allocation, after preparation/attachment and during updates. No second registry or mutable registry exposure is added.
