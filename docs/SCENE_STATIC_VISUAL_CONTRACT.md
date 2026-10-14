# Owned static visual boundary

R14-G, 2026-10-02. [SceneStaticVisual](../../SWG-Godot-Client/poc/Stardust.Godot/SceneStaticVisual.cs)
wraps accepted [static resources](SCENE_STATIC_RESOURCE_CONTRACT.md) and
[explicit SWTS selection](SWITCH_TEXTURE_STATIC_RESOURCE_CONTRACT.md) with owned
Godot GPU/node lifetime. It uses the unchanged GeometryAppearancePreview. Core,
existing inspectors, shared Main and world APIs are unchanged.

## Publication and lifetime

Two overloads are available:

```csharp
SceneStaticVisual? TryPublish(SceneObjectRegistry registry,
    SceneObjectHandle handle, SceneStaticResources source, Node parent,
    bool terminalFrame0Inspection=false, SceneStaticVisualLimits? limits=null,
    Action? beforePublication=null);
SceneStaticVisual? TryPublish(SceneStaticResources source, Transform3D transform,
    Node parent, Func<bool> currentLifetime, bool terminalFrame0Inspection=false,
    SceneStaticVisualLimits? limits=null, Action? beforePublication=null);
```

The registry overload requires an exact current finalized handle and byte-identical
47-byte raw create. The exact handle protects registry identity, generation, object
ID and revision, including destruction/recreation. It derives placement using
SceneCreatePlacementBuilder with zero-quaternion Reject, then the accepted Godot
SRS reflection. Containment/end revisions that change the exact handle require
explicit retirement/republication; there is no inferred parent transform.

The second overload accepts caller-derived placement and a borrowed current-lifetime
predicate. This lets a future accepted world publication supply its own freshness
check without a duplicate registry. The caller owns provenance and serialized
access to that predicate. It is checked before allocation, after staging and again
after attachment, before visibility. Predicate false returns null before allocation
or disposes the staged candidate. A transfer exception rolls back owned GPU/node
resources. No publication callback is retained. Arbitrary forged descriptor graphs
are not authenticated by these checks.

Every mutating/verification/disposal call requires the Godot main thread and the
instance's serialized owner thread. Stale calls retire resources before mutation;
disposed calls return false. SetVisible, SelectDetail, ApplyPlacement and
SetEngineTransform preserve existing GPU IDs. ApplyPlacement accepts only the
source's byte-identical create; SetEngineTransform explicitly permits a validated
modern transform. Finite, bounded, nonsingular orthogonal transforms are required.
Sheared/parent/gameplay transforms are outside this proof.

Default descriptor budgets are64 unique meshes,64 distinct texture paths and256
surfaces across mesh templates. Explicit maxima are4096/4096/65536; invalid limits
and exceeded budgets reject before GPU allocation. Accepted graph/reader limits
remain in force. These bounds do not promise peak memory usage.

The adapter owns its root, GeometryAppearancePreview and extra inspection material.
Dispose frees the root and its child tree, releases geometry/texture resources and
clears source/predicate references. It borrows immutable CPU resources and never
closes the caller's registry. Publication checks are serialized; they do not add a
world observer or asynchronous publication API.

## Exact terminal inspection policy

CPU resources must be RequireComplete, with every required geometry/material mesh
present and no omission diagnostics. The default opaque presentation rejects the
terminal's special screens rather than enabling an override implicitly.
`terminalFrame0Inspection:true` explicitly permits only CRC2907007746, all four
serialized terminal LODs, pinned `shader/anim_mission_terminal.sht` frame0,
`effect/a_emis_full.eft`, source flag0 and MAIN/UV0 binding. The shader pin is
`B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62`.

The unchanged geometry adapter's two exact opaque-policy diagnostics are checked.
Only those two SWTS screen surfaces receive the accepted built-in unshaded MAIN
inspection material. The root and screen label identify this modern inspection
policy. Original effect translation, automatic SWTS timing and original initial
frame semantics remain unknown. Other effect behavior is not broadened.

All four LOD meshes and seven GPU textures are retained. Explicit ordinal selection
uses the accepted detail-band midpoint and initially chooses finest ordinal3.
Sixteen repeated selections, visibility toggles and raw-placement refresh retain
exact GPU fingerprints. Original CPU closure:27 sources/1,401,794 bytes per actor,
four inherited templates, vertex counts208/276/427/496, triangle counts102/142/232/296,
five shader keys/four effects and all62 decoded mips. GPU verification checks
uploaded geometry and all mip bytes of bound MAIN textures; the independent CPU
oracle checks all62 source mips, including auxiliary textures.

## Standalone proof and reproduction

[SceneStaticVisualPreview](../../SWG-Godot-Client/poc/Stardust.Godot/SceneStaticVisualPreview.cs) loads
independent A/B closures off the owner thread in one read-only archive scope. B is
hidden and retained while A is destroyed/recreated. Recreating A reuses its immutable
prepared CPU closure under a new exact handle and creates new GPU resources; it
performs no archive reload. B keeps exact GPU IDs. Source evidence is identical
before/after the full visual lifecycle. Registry close and injected false predicates
retire visuals. Seven allocated positive/negative-control roots all dispose.

Synthetic [fixture producer](../../SWG-Godot-Client/poc/round14-tests/Rendering/Generate-Fixture.py)
authors distinct high-bit IDs, a nonunit quaternion (.2,-0,.3,1.7), wire positions
(-2,7,4)/(8,7,4), and raw flag255. These are real-layout authored bytes, not captures.
The fixed camera faces the authored screen using transformed normals. Fresh whole
terminal captures and recreation match exactly below UI row100:30,326 nonbackground
pixels, crop bbox(536,105,743,420),4,374 warm pixels. Both images were visually
inspected. Confidence: **Confirmed** for this replacement and original-byte fixture;
placement normalization, camera, lifetime/limits and unshaded inspection are modern
policy. Original runtime placement, lighting/render fidelity and parent/gameplay
semantics are not established.

```powershell
python poc/round14-tests/Rendering/Generate-Fixture.py local-private/round14/fixture.json
dotnet build poc/Stardust.Godot/Stardust.Godot.csproj --no-restore -p:UseSharedCompilation=false -p:NuGetAudit=false
dotnet restore poc/round14-tests/Rendering/Rendering.csproj --configfile poc/round14-tests/Rendering/NuGet.Config -p:NuGetAudit=false
dotnet run --project poc/round14-tests/Rendering/Rendering.csproj --no-restore -p:UseSharedCompilation=false -p:NuGetAudit=false -- C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round14/fixture.json
tools/powershell/Invoke-StardustSceneStaticVisualPreview.ps1 -GodotPath $GodotPath -ArchiveRoot C:/Stardust-DEV -Configuration C:/Stardust-DEV/swgemu_live.cfg -FixturePath local-private/round14/fixture.json -CapturePath local-private/round14/fresh.png
```

Use the existing offline NuGet cache/private APPDATA. Launcher default/`-Headless`
uses the headless lifecycle proof; `-CancelLoad` waits for the first lookup, resets
registry and cancels, publishing zero. Capture mode is separate and requires two
fresh private PNGs: first and `-recreated.png`. Hidden automated windows have a
45-second deadline. The launcher writes a small ignored private scene, so no shared
Main edit is necessary to verify. Close waits on the same shutdown task; worker
fault cleanup uses finally. External tree removal cancels and observes cleanup.

Native arguments: root, configuration, fixture. Native68 negatives include all47
create truncations, exact lifecycle/foreign identity, descriptor budgets/policy,
zero/nonfinite placement, reset/close and registry capacity. Clearing54 borrowed
resolver buffers preserves complete original evidence. Godot22 negatives cover
wrong thread4, stale/foreign/disposed checks, placement errors, budgets, detached
parent, injected lifetime/transfer rollback and explicit default effect rejection.
Five prelaunch negatives leave logs/captures absent. GPU/headless/cancel/build and
independent oracles pass. The existing certificate-store startup diagnostic is
nonfatal; no positive-run implementation exception or leak warning was observed.

## Export schemas and lead integration

[Verify-Original.py](../../SWG-Godot-Client/poc/round14-tests/Rendering/Verify-Original.py) takes root,
configuration, native/GPU/headless logs. It independently reconstructs original
source union/priority/SHA, inheritance, all LOD geometry and all62 mip hashes.
[Verify-Visual.py](../../SWG-Godot-Client/poc/round14-tests/Rendering/Verify-Visual.py) takes fixture,
native/GPU/headless/cancel logs, first/recreated PNG. It independently hand-decodes
raw create, derives rotation with quaternion cross products and SRS reflection,
checks handle/incarnation metadata and actual whole-terminal pixels.
[Verify-Launch.ps1](../../SWG-Godot-Client/poc/round14-tests/Rendering/Verify-Launch.ps1) takes common
launcher arguments plus ExistingCapture.

- `switch_frame` reuses the accepted independently audited source/material/geometry/
  texture evidence schema, twice at explicit frame0 for distinct A/B create IDs.
- `static_native_pose` contains core rows, origin, normalization flag/norm.
- `static_native_validation` contains negatives68, truncations47, ownedBuffers54,
  frame0, strict CPU/ownership/worker fields and gpu=false.
- `static_state` contains initial/recreated stage, id, handle2/6, incarnation1/5,
  engine origin/basis columns, exact GPU fingerprint, detail3/meshes4/textures7.
- `static_validation` contains negatives22, selections16, frame0, resource counts,
  screenOverrides2, retained-other/injected predicate and worker-thread/close fields.
- Teardown marker requires allocations7/disposals7; cancel requires zero publications.

Lead-owned Main registration proposal before generic asset-root handling:

```csharp
if(args.Contains("--scene-static-visual-preview")){AddChild(new SceneStaticVisualPreview());return;}
```

Lead registered that Main branch and freshly ran it headlessly. The accepted
[mixed world preview](MIXED_WORLD_PREVIEW_CONTRACT.md) uses the explicit
transform/predicate overload with generic publication currentness and no duplicate
registry. The worker milestone was independently built against the prior baseline;
its [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R14_STATIC_VISUAL.md)
records that scope. All workers are idle; no Round15 work.
