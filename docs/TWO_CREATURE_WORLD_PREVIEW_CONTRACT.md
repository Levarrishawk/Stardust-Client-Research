# Separate two-creature offline world preview

R13-R, 2026-10-02. This bounded replacement preview uses the accepted
[world mixed-state mode](SCENE_WORLD_REPLICATION_CONTRACT.md),
[acknowledged observer](SCENE_WORLD_CHANGE_DELIVERY_CONTRACT.md),
[resource initializer](SCENE_CREATURE_RESOURCE_INITIALIZER_CONTRACT.md) and
[Godot presentation consumer](OFFLINE_REPLICATED_WORLD_PREVIEW_CONTRACT.md).
It adds no engine-neutral API or change to the existing one-actor inspector.

## Input and behavior

[Generate-Fixture.py](../../SWG-Godot-Client/poc/round13-tests/Rendering/Generate-Fixture.py) authors
synthetic real-layout application bytes for IDs `F122334455667788` and
`E122334455667799`, human template CRC 2937962913, identity quaternions and wire
positions (-1.5,0,0)/(1.5,0,0). There are six staged create/full18-field
CREO3 baseline/end messages behind an explicit scene barrier. Direct application
acceptance is offline: this milestone does not add a transport or socket.

The pinned catalog routes each finalized ID through its own resource initializer
job and the original inherited human detail0 closure. Each load reads 39 distinct
sources/4,686,371 bytes, including five inherited templates, two programs, four
albedo factors, ten textures/80 mips. Each job opens and closes its own read-only
overlay off the owner thread; the closures are independent even though their
source content is identical. Fixed authored palette factors are used. No optional
palette profile, face generation, additional asset route or same-round dependency
is introduced.

The current two-slot presentation consumer applies current batches before ack.
A held A update followed by B's update makes the old ack stale and preserves both
pending IDs. A current drain applies their latest state and acknowledges once.
Mixed updates contain Height plus an empty wounds operation list; wounds have no
presentation meaning here. A changes to Height 1.4; B changes to 1.2, hides with a
full baseline, and returns to Height 1/Visibility 255 with a full baseline. Both
state updates retain exact GPU fingerprints and cause no reload or allocation.
An unsupported raw negative-zero Height preserves A's accepted pose and pending
ID until a valid update retries.

Destroying A retires only its presentation and publication; B stays current with
exact GPU IDs. Recreating A requires a new create/baseline/end, new incarnation and
one new 39-source job. The new actor receives Height 1.4 before publication. Its
GPU IDs differ; B's remain identical. Stale state/resources, foreign world
state/resources/batch and three wrong-thread consumer operations reject. A
one-slot consumer test rejects its second actor before allocation. A late
truncated mixed application fails the world and retires both actors globally.
Consumer application remains sequential rather than atomic across GPU actors.

The fixed frontal camera captures both complete actors at idle frame zero.
The initial, updated and recreated captures use identical camera and pose timing.
The left actor region changes 13,435 pixels; the right changes zero. Updated and
recreated actor crops are byte-identical. Observation/confidence: **Confirmed** for
this replacement fixture and original-byte closure. Height multipliers, camera,
placement and observer policy are modern choices; original Height units, lighting,
parent transforms, components/damage/gameplay effects and visual fidelity remain
unestablished.

## Invocation and lead integration

The new [Main partial](../../SWG-Godot-Client/poc/Stardust.Godot/Main.TwoCreatureWorld.cs) supplies a
method; Main.cs remains lead-owned. Proposed exact branch before the generic
asset-root handler:

```csharp
if(args.Contains("--two-creature-world-preview")){await BuildTwoCreatureWorldPreview(args);return;}
```

The [launcher](../../SWG-Godot-Client/poc/Stardust.Godot/Invoke-StardustTwoCreatureWorldPreview.ps1)
can verify independently of that registration: it writes a tiny ignored private
scene referring to `res://TwoCreatureWorldPreview.cs` and launches that scene
explicitly. Its arguments are `-GodotPath`, `-ArchiveRoot`, `-Configuration`,
`-FixturePath`, `-CatalogProfile`, and one optional `-CapturePath` or `-CancelLoad`.
Default/`-Headless` runs the full bounded scenario without GPU readback; capture
runs use GPU and three fresh private PNGs: first, `-updated.png`, `-recreated.png`.
Automated windows are hidden, deadline45 seconds. A cancellation barrier waits
until both loaders have opened their overlays, then selection cancellation
publishes neither actor, closes both archives and drains all jobs.

```powershell
python poc/round13-tests/Rendering/Generate-Fixture.py local-private/round13/fixture.json
dotnet build poc/Stardust.Godot/Stardust.Godot.csproj --no-restore -p:UseSharedCompilation=false -p:NuGetAudit=false
dotnet restore poc/round13-tests/Rendering/Rendering.csproj --configfile poc/round13-tests/Rendering/NuGet.Config -p:NuGetAudit=false
dotnet run --project poc/round13-tests/Rendering/Rendering.csproj --no-restore -p:UseSharedCompilation=false -p:NuGetAudit=false -- C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json local-private/round13/fixture.json
poc/Stardust.Godot/Invoke-StardustTwoCreatureWorldPreview.ps1 -GodotPath $GodotPath -ArchiveRoot C:/Stardust-DEV -Configuration C:/Stardust-DEV/swgemu_live.cfg -FixturePath local-private/round13/fixture.json -CatalogProfile poc/preview-profiles/scene-object-catalog.json -CapturePath local-private/round13/fresh.png
```

Use the existing offline package cache and private APPDATA as required by the
host. The native runner's five arguments are root, configuration, catalog,
animation-bank profile, fixture. Its fifty negatives include all41 truncations of
the mixed update, foreign owners, thread rejection, stale destruction state,
pending capacity rollback, late failure and close. It verifies factors and all
mips remain owned after borrowed original resolver buffers are cleared.

## Export and oracle schemas

All detailed logs/images remain ignored/private. The independently authored
[world oracle](../../SWG-Godot-Client/poc/round13-tests/Rendering/Verify-World.py) takes eight paths:
fixture, native log, GPU log, headless log, cancel log, initial PNG, updated PNG,
recreated PNG. It hand-decodes layouts, independently models global registry/store
revisions and pending-ID coalescing, checks actual disjoint actor regions below
the fixed UI, and compares native/GPU/headless state. The
[original oracle](../../SWG-Godot-Client/poc/round13-tests/Rendering/Verify-Original.py) takes root,
configuration, catalog, native/GPU/headless logs. It independently checks effective
source priority/SHA, inheritance, programs/factors, every mip and sampled hue
algebra, including both actors' identical-content but separate closures.
[Verify-Launch.ps1](../../SWG-Godot-Client/poc/round13-tests/Rendering/Verify-Launch.ps1) takes the common
launcher arguments plus `-ExistingCapture` and rejects five mode/destination cases
without new process logs/captures.

- `two_source_begin=U64` starts an actor's `source`, `factor`, `program`, `texture`,
  `sample` JSON records with accepted evidence schemas.
- `two_batch={stage,Generation,MutationRevision,entries}` exports current IDs,
  flags and nullable `{incarnation,handle,revision,heightBits,visibility,wounds}`.
  Stages: initial, both_height, b_hidden, b_shown, height_retry, destroy, recreate.
- `two_native_state={stage,calls,closed,actors}` exports ID/incarnation/revision,
  raw Height bits, visibility and current wounds for initial/updated/recreated.
- `two_state` adds engine transform origin/basis columns, exact GPU fingerprint,
  object phase, allocation/disposal
  counts. IDs are compared within a run; their values are not stable across runs.
- `two_loads` records bounded serial1..3, actor ID, worker thread,39 reads, byte
  count and closure source SHA. `two_capture` records the PNG basename.

The scene and launcher require lifecycle/cleanup acceptance markers. Positive
native, GPU, headless, cancellation, original/world/pixel oracles and five prelaunch
negatives pass; all roots are disposed and outstanding jobs reach zero. The known
Godot certificate-store startup diagnostic is nonfatal. See the
[checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R13_TWO_CREATURE_WORLD.md)
for input identity, scope and exact next action.


## Lead Round13 integration acceptance

The proposed Main branch is now registered and a fresh primary-Main headless run passes its two-actor teardown marker. Lead fresh native/GPU/headless/cancel/source/state/pixel/prelaunch checks all pass from the main checkout; all three whole-actor captures were inspected. Pixel regions again change13,435/0 and updated/recreated crops are byte-identical. The existing consumer and single-actor preview remain unchanged. [Round13 report](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_13_2026-10-02.md) records scope, corrections, reproduction and next action.
