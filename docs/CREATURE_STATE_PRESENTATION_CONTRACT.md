# Creature scalar state presentation

Round 10 rendering boundary, 2026-10-02. This is a modern presentation policy
over the accepted [incarnation replication store](SCENE_CREATURE_REPLICATION_STORE_CONTRACT.md)
and [owned creature visual](SCENE_CREATURE_VISUAL_CONTRACT.md). It does not establish
an original client Height-to-transform or posture-to-animation implementation.

`SceneCreatureStatePresentation` owns one `SceneCreatureVisual`. Its caller supplies
an exact resource handle, original prepared resources, the accepted store's current
snapshot, a base engine transform, and a live Godot parent. The inspector uses the
accepted create placement conversion once, including the engine Z reflection.
The adapter itself consumes an explicit engine transform and never converts raw
create coordinates.

Publication requires the exact registry handle and exact source-create bytes,
including signed zero, to match. The snapshot must belong to the supplied store,
match the resource's incarnation and object ID, and have the current state revision.
These conditions are checked before allocation, after staging, and after attachment
while the root is hidden. Invalidated candidates are disposed deterministically.

After publication the visual guard retains the incarnation token and store, with
no obsolete snapshot/baseline retained in its closure. A newer state revision does
not retire the visual. Destroy/recreate, exact resource-handle expiry, registry
closure, or removal/closure of the store's incarnation entry retires it on the next
guarded operation. Resource rebind after containment/finalization is explicit even
though the Core state store preserves the stable incarnation's state.

`Apply` checks the supplied snapshot's current revision separately. Old snapshots,
foreign-store snapshots, and old incarnations return false; a valid visual survives
state-only rejection. A synchronous `beforeApplication` hook permits bounded tests
of a revision change between validation and application. The adapter rechecks after
that hook. Engine operations and registry/store ownership remain serialized on the
Godot main thread. `Tick(0)` performs resource-lifetime validation without advancing
the animation clock.

Height is a **uniform multiplier of the base transform's three basis columns**.
The base origin remains unchanged. Allowed values are finite binary32 numbers in
the inclusive range **0.25 through 2.0**. The derived transform must also pass the
existing visual's finite origin and orthogonal nonsingular basis budgets. Zero,
negative zero, negative, nonfinite, and out-of-range values throw before transform
or visibility mutation. There is no clamping, coercion, codec change, or write-back
to raw Core state. On rejection the last accepted pose and applied revision remain.
The original raw field's physical units and complete presentation semantics remain
unknown; this multiplier and its bounds are caller policy.

Baseline Visibility byte zero hides the root; any nonzero byte shows it. Visibility
changes use a new complete baseline seed because the accepted scalar delta codec
does not expose a visibility update. Posture, Rank, LinkId, Shock, and States are
diagnostic only. No original posture animation mapping, world arrival ordering,
network connection, or same-round world coordinator is inferred.

The opt-in `--creature-state-preview` branch loads the accepted pinned original
human creature input set: 37 sources, 4,683,848 bytes, ten textures, four authored
MAIN bindings and 53 joints. Configuration SHA-256 is
`22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`.
It preserves the authored MAIN limitation: full customized albedo, face composition,
normal and specular integration are deferred. It uses no Round 10 albedo dependency.
The inspector's application messages are independently packed synthetic fixtures;
they are not captures or evidence of original server arrival behavior.

Generate fixtures and run after building the existing Godot project using the
available offline SDK/cache:

```powershell
python poc/round10-tests/Rendering/Generate-Peer.py local-private/round10/peer.json
dotnet run --project poc/round10-tests/Rendering/Rendering.csproj --no-restore -- local-private/round10/peer.json C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/human-locomotion.json
tools/powershell/Invoke-StardustCreatureStatePreview.ps1 -GodotPath $GodotPath -ArchiveRoot C:/Stardust-DEV -Configuration C:/Stardust-DEV/swgemu_live.cfg -FixturePath local-private/round10/peer.json -Headless
```

The new native test project has a source-cleared `NuGet.Config`. Restore it with
that config and the existing cache; set APPDATA to an existing private directory
when the host's normal NuGet configuration is inaccessible. No download is needed.

Use `-CancelLoad` for bounded cancellation, `-Interactive` for the visible inspector,
or `-CapturePath` for a fresh PNG beneath the checkout's existing private directory.
Automated runs use a hidden window and a 45-second limit. Capture emits the requested
baseline PNG and a sibling `-frame1.png` after Height 1.5, with unchanged camera,
idle frame zero and retained GPU resource IDs. Capture, cancellation, headless and
interactive combinations are validated before launch. Interactive controls apply
the height delta, reset the baseline, or hide the actor.

The independent [state verifier](../../SWG-Godot-Client/poc/round10-tests/Rendering/Verify-State.py)
accepts fixture JSON, native log, GPU log, headless log, cancellation log, baseline
PNG and delta PNG in that order. It verifies the 47-byte create and signed zero,
delta bytes, scalar raw bits, quaternion cross-product rotation and SRS reflection,
source identity equality, and foreground pixel/extent growth below UI row 180.
The [original-source verifier](../../SWG-Godot-Client/poc/round10-tests/Rendering/Verify-Original.py)
independently reads archive bytes and effective priorities for all 37 logged rows.
Neither verifier executes the original client.

Observation: fresh GPU captures show 13,044 changed actor pixels below row 180,
width 62 to 91 pixels, height 200 to 296 pixels, fixed camera and resource IDs.
Confidence: **Confirmed for this bounded replacement fixture**. Compatibility
consequence: supported scalar updates can reuse visual resources, while raw state
and presentation policy remain separate. Original Height and posture behavior
remain **Unknown**. Next probe: integrate this adapter with an explicitly accepted
world publication owner, then assess customized albedo independently.
