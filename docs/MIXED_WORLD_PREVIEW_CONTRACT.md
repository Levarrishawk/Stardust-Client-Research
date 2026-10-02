# Original human and terminal mixed-world preview

Round14 lead integration, October2,2026. [SceneMixedWorldPreview](../poc/Stardust.Godot/SceneMixedWorldPreview.cs) is a bounded diagnostic over the accepted [generic coordinator](SCENE_MIXED_WORLD_CONTRACT.md), [pure completion gate](SCENE_OBJECT_RESOURCE_VALIDATION_CONTRACT.md), [creature visual](SCENE_CREATURE_VISUAL_CONTRACT.md) and [static visual](SCENE_STATIC_VISUAL_CONTRACT.md).

## Delivery and ownership

One coordinator receives independently authored APPLICATION bytes for two distinct IDs. The human uses CreatureDetail0 with explicit idle/walk/run and albedo options; the mission terminal uses StaticAppearance, RequireComplete and explicit pinned SWTS frame0. Each load runs on its own worker with a read-only configured overlay and copied configuration. Archives close before generic polling transfers resources. The completion gate validates exact create/catalog/options and prepared closure without source I/O. Publication predicates then check world owner, generation, finalized exact handle and attempt.

Original object IFF inheritance and appearance dependencies remain in their original installed state: human object/SAT/detail0/skeleton/animation/material closure39 sources/4,686,371bytes/80mips; terminal object/inheritance/APT/LOD/all four meshes/material closure27 sources/1,401,794bytes/62mips. No assets are rewritten. Original source precedence/pins/pixels are independently audited in the accepted worker proof and lead reruns.

The host branches explicitly on the sealed resource discriminator, publishes a guarded creature or static actor and retains no duplicate registry. Raw create placement is converted once through the accepted Godot reflection. Creature Height is a modern presentation policy; terminal TANO3 remains opaque. State/resource currentness stay separate. No static field semantics or parent transform hierarchy is inferred.

## Bounded scenario

Synthetic IDs are human0xF122334455667788 and terminal0xE122334455667799. An explicit minimum-scene barrier replays two creates/baselines/end messages before resource loading. The human complete18-field CREO3 baseline seeds typed state; terminal baseline/delta bodies are explicitly synthetic opaque bytes.

Height1->1.4 and a terminal delta preserve both exact resource attempts/GPU IDs. A stale acknowledgment preserves both pending IDs until a current batch is applied/acknowledged. Destroy/recreate reloads only the terminal and replaces its creation incarnation/GPU IDs, preserving the human's incarnation, state, resources and IDs. A terminal CREO-family message deliberately fails the whole world before typed seed/history publication; both visuals retire and all jobs drain.

The preview applies these two actors explicitly in serialized order. It is not a reusable heterogeneous presentation consumer or a promise of atomic GPU updates. That consumer is the next bounded milestone. The legacy creature constructor/consumer remains available unchanged.

## Rendering evidence and limits

The fixed camera faces both fronts at(0,1.3,-9), FOV40. All three front captures were visually inspected; the terminal's selected screen is visible. Below UI row180, Height changes14,333 human pixels while the entire terminal region changes0. Updated/recreated actor regions are byte-identical. GPU/headless state, pose, closure counts, opaque history and incarnation outputs agree. Real cancellation waits for both open archive jobs, resets selection, transfers0 resources and drains both scopes. Standalone terminal proof also reproduces30,326 whole-object pixels and identical recreation.

The terminal's two exact SWTS screen surfaces use the accepted, explicitly labeled built-in unshaded MAIN/UV0 inspection override. Other materials retain the existing adapter policy. Original effect equations, automatic frame timing, full character face/customization/lighting, movement/terrain and live interoperability remain open. The human is the accepted base albedo closure without a full face/customization extension. Static HDR and other unrequested transformed completions remain outside the current pure gate; existing readers are unchanged.

Normal close awaits shutdown before quitting. Forced tree/process termination cannot guarantee an awaited drain. The existing Godot certificate-store startup diagnostic remains nonfatal; passing runs require explicit lifecycle and teardown markers.

## Reproduction

Use installed cached .NET8/Python/Godot4.7.2; no downloads. Generate the independent fixture with [Generate-Peer.py](../poc/round14-tests/Integration/Generate-Peer.py) into ignored storage. Run the [native Integration project](../poc/round14-tests/Integration/Integration.csproj) with root, configuration, catalog, locomotion bank, peer JSON and export JSON; run [verify.py](../poc/round14-tests/Integration/verify.py) with peer/export.

Build the Godot host using its existing SDK cache. [Invoke-StardustMixedWorldPreview.ps1](../poc/Stardust.Godot/Invoke-StardustMixedWorldPreview.ps1) accepts GodotPath, ArchiveRoot, Configuration, FixturePath and CatalogProfile. Default/-Headless runs the bounded lifecycle; -CancelLoad runs real cancellation; -CapturePath requires a fresh private PNG in an existing directory and produces first, -updated and -recreated siblings. Automated windows are hidden with45-second deadlines.

Run [verify_preview.py](../poc/round14-tests/Integration/verify_preview.py) with GPU log, headless log, cancellation log, first/updated/recreated PNGs. This independently checks state/incarnation/pose/currentness receipts and actor pixels, excluding labels. Primary Main flags are --mixed-world-preview and --scene-static-visual-preview; both were freshly executed headlessly.

Shared native flags: --round14-peer PEER EXPORT; --round14-validation ROOT CFG CATALOG BANK; --round14-world ROOT CFG CATALOG BANK PEER EXPORT. Generated peers/logs/captures/source-derived detail stay ignored and are never linked into curated documents. [Integration checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R14_MIXED_WORLD_INTEGRATION.md) records findings/confidence and the resume point.
