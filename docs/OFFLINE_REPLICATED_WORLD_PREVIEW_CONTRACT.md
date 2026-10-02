# Offline replicated world creature presentation

R11-RENDER-01, October 2, 2026. This joins the accepted
[typed world state](SCENE_WORLD_REPLICATION_CONTRACT.md),
[positive bounded scalar presentation](CREATURE_STATE_PRESENTATION_CONTRACT.md),
and [optional original albedo resources](SCENE_CREATURE_ALBEDO_RESOURCE_CONTRACT.md).
It adds no original Height, posture, lighting or readiness claim.

`SceneWorldCreatureStatePresentation` owns one existing `SceneCreatureVisual` and
accepts a world, its current `ScenePreparedCreaturePublication`, its current
`SceneWorldReplicationSnapshot`, an explicit base engine transform, and a live
Godot parent. It creates no registry or replication store and exposes neither.
The existing visual's `TryPublishWorld` gains only a final optional
`Func<bool>? currentAttempt` parameter; null preserves previous callers' behavior.
The additional guard runs before allocation, after staging and after attachment.

Publication checks world identity and external generation through both accepted
world freshness APIs, exact resource handle/attempt through the publication guard,
object ID and registry identity/epoch through the incarnation token, and current
state revision through the replication guard. World `long Generation` is compared
to publication/snapshot generation only. Registry `ulong SceneGeneration` is
compared to the incarnation's registry epoch only. A failure/reselection fixture
makes these counters unequal and still publishes successfully.

During staging, the supplied full state snapshot must remain current. The hidden
candidate is disposed if a callback changes it before publication or during
attachment. After successful publication the closure clears the staged snapshot,
retaining the incarnation token and current-entry query rather than an obsolete
baseline. The resource publication stays guarded independently. New state
revisions do not retire a valid actor; resource restarts, handle expiry, selection,
destroy/recreate, failure or closure retire it on the next guarded operation.
Retirement/disposal releases the wrapper's borrowed world and resource-publication
references; it does not close the caller-owned world.

`Apply` rejects old/foreign snapshots without retiring valid resources. It checks
the current snapshot, derives a valid presentation transform, runs the optional
bounded `beforeApplication` callback, then rechecks before changing transform and
visibility. Both state and resource operations remain on the serialized Godot
main thread. Height is the accepted modern uniform base-basis multiplier in the
inclusive range 0.25–2.0; origin is unchanged. Invalid values throw before visual
mutation and remain raw in world state. Baseline Visibility 0 hides; nonzero shows.
Other scalars remain state/diagnostic data with no posture animation mapping.

The opt-in `--offline-replicated-world-preview` branch uses `ReplicatedWorldPeer`,
a separate bounded offline driver. Its independently packed SOE fixture negotiates
496-byte datagrams, validates CRC isolation, reliable reordering/duplicate delivery,
select/ACK and retained ready retry, and delivers complete baseline/scalar/lifecycle
messages through `world.Accept`. No socket, DNS, live server, capture or original
executable is involved. Synthetic fixture bytes do not establish original arrival
ordering. Typed mode is the accepted Round 10 scalar mode; wounds and unsupported
fields still fail rather than depending on unfinished Round 11 codecs.

The caller barrier is explicit and false/stale/duplicate barriers are checked.
Finalized, Prepared and Ready are separate observations: ordered typed replay
finalizes before the first resource poll; original preparation occurs on a worker;
server Ready is a distinct framed application after preparation. Resources use
the portable accepted catalog profile and `Load(..., loadAlbedoFactors: true)`.
Archives close before owner transfer. The worker reads each effective source once,
and completed worker tasks are removed after transfer.

Pinned original human inputs retain 39 sources / 4,686,371 bytes, five inherited
object templates, two selected detail0 geometries, ten textures / 80 mips, four
factor descriptions and two PS1.1 program descriptions. Configuration SHA-256 is
`22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`.
The existing albedo shader receives authored factor RGB and alpha-mask policy on
every source surface, including repeated material bindings. This is sampled
authored albedo; face generation, new palette policy, normal/specular/full lighting
and original effect behavior remain separate. No Round 11 asset API is required.

The fixed camera frames the whole actor in a three-quarter view. Both automated
captures hold idle frame zero and use the same camera. A framed Height 1.5 update
changes state through world.Accept and applies the new snapshot without a load or
GPU resource replacement. Framed hidden/shown baselines subsequently verify
Visibility 0/255 with retained IDs. Old snapshots return false while the actor
remains usable. Unsupported finite heights stay current in the world but reject
at presentation. Shutdown closes transport/world, releases roots, and awaits
resource drain, including cancellation-ignoring work and late faults.

## Reproduction

With the existing offline toolchain, first build `poc/Stardust.Godot`. Restore the
new native project with its source-cleared config and an existing package cache.
Use a private APPDATA directory if the host's ordinary NuGet config is inaccessible.

```powershell
python poc/round11-tests/Rendering/Generate-Peer.py local-private/round11/peer.json
dotnet restore poc/round11-tests/Rendering/Rendering.csproj --configfile poc/round11-tests/Rendering/NuGet.Config -p:NuGetAudit=false
dotnet run --project poc/round11-tests/Rendering/Rendering.csproj --no-restore -- C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json local-private/round11/peer.json
tools/powershell/Invoke-StardustReplicatedWorldPreview.ps1 -GodotPath $GodotPath -ArchiveRoot C:/Stardust-DEV -Configuration C:/Stardust-DEV/swgemu_live.cfg -FixturePath local-private/round11/peer.json -CatalogProfile poc/preview-profiles/scene-object-catalog.json -Headless
```

Use `-CancelLoad` for bounded cancellation, `-Interactive` for visible inspection,
or `-CapturePath` for a fresh private PNG pair. Automated windows are hidden and
bounded to 45 seconds. The sibling second image is named `-frame1.png`. Invalid
mode combinations or capture destinations reject before launch. The inspector
can apply Height 1.5, reset the baseline or hide the actor; those controls use the
accepted world API rather than a duplicate state store.

[Verify-Original.py](../poc/round11-tests/Rendering/Verify-Original.py) takes original
root, configuration, catalog and native log; it independently reads effective
archive bytes/priorities, inheritance and programs/factors, ten textures/80 mip
hashes, and four original sampled pixels with hand-evaluated hue algebra.
[Verify-World.py](../poc/round11-tests/Rendering/Verify-World.py) takes peer JSON,
native/GPU/headless/cancel logs and the capture pair in that order. It independently
checks framing/order/retry, raw signed-zero/scalar bits, quaternion cross-product
placement and SRS reflection, source/factor/mip equality, bound shader parameters,
whole-actor extents and actual pixels below UI row 180.

Observation: the fresh pair changes 28,113 actor pixels; widths 132→204, heights
237→367; warm-color pixels 10,569/25,209. Camera and GPU allocation IDs remain fixed.
Confidence: **Confirmed for the bounded replacement fixture**, not original visual
fidelity. Native twelve negatives, GPU/headless lifecycle/freshness/staging/drain
checks, cancellation, eight prelaunch negatives and the old opaque-world headless
regression pass. Next action: lead integration and a separately dispatched Round 12;
this boundary does not depend on unfinished palette, observer or mixed-delta work.

## Round 12 observer and palette extension (2026-10-02)

The inspector now consumes the accepted [coalesced change batches](SCENE_WORLD_CHANGE_DELIVERY_CONTRACT.md) through `SceneWorldPresentationConsumer`. It drains the latest entries, validates world identity/generation, resource publication, object incarnation and replication revision separately, applies current presentation, then acknowledges. A stale acknowledgement leaves pending IDs intact; bounded retry redrains the latest batch. Resource polling and publication acceptance remain separate. Entries without a prepared actor can be acknowledged because later resource acceptance reads current state. Selecting, failed and closed boundaries explicitly retire all actors even when drain returns null.

The consumer keeps compact generation/revision acknowledgement metadata and actor wrappers, with no retained batches or callbacks. All public operations require the owning thread before changing metadata. Invalid presentation Height preserves the previous transform and leaves the batch pending. Application across multiple actors is not atomic: an earlier valid entry may already have applied before a later entry rejects. Consumer disposal releases borrowed references; the preview owns world close and resource drain.

Optional `-PaletteProfile poc/preview-profiles/human-scene-colors.json` passes bounded owned bytes through Main. The resource worker uses strict `SceneCreaturePaletteCustomization.LoadProfile` and `Apply` off the render thread, following the accepted [palette contract](SCENE_CREATURE_PALETTE_CUSTOMIZATION_CONTRACT.md). The authored and selected views share the original 39-source closure, geometry, programs, textures and all 80 mips. Presentation changes only four factor records on existing materials after closure/current-state checks. It retains copied factors for verification, with no additional archive reads, GPU resource allocations or full-face/wire customization. Existing callers without this option preserve authored colors.

Profile-enabled captures produce the original PNG, `-frame1.png` at Height 1.5, and `-authored.png` at the same Height, pose and camera with authored factors. Interactive controls expose both color choices. Fresh private destinations and the profile's 1..65536-byte budget reject before launch.

The independently authored [Round 12 tests](../poc/round12-tests/Rendering/Program.cs) use accepted R11 scalar/palette APIs, without Runtime 12 mixed-delta or Assets 12 DDS dependencies. Generate the peer with `Generate-Peer.py`; restore/run `Rendering.csproj` using its source-cleared `NuGet.Config`. Native arguments are original root, configuration, catalog, animation bank, peer JSON and palette profile. `Verify-Original.py` arguments are original root, configuration, catalog, profile and native log. `Verify-World.py` arguments are peer, native/GPU/headless/cancel logs and the three captures. `Verify-Launch.ps1` exercises eight prelaunch negatives.

Observation: the final fixed-camera Height pair changes 28,119 actor pixels, widths 132→204 and heights 237→367. The authored/profile choice changes 25,184 pixels with identical actor extent. Native nineteen negatives, original-source/palette oracle, coalesced state oracle, GPU/headless lifecycle/thread/ack/resource checks, cancellation, eight prelaunch negatives and default-profile headless regression pass. Confidence: **Confirmed for this bounded replacement fixture**. Original Height semantics, full-face customization, lighting and original-render fidelity remain unknown or outside scope. The next action is lead integration of this handoff; no Round 13 work is authorized here.

## Lead Round12 mixed-mode integration

After worker handoffs are integrated, -MixedReplication maps to --replicated-mixed and selects CreatureView3Mixed with LegacyBounded, payload1024,16 fields/scalars,64 command/value/element caps and retained-byte4096. Absent flag preserves scalar-only mode and its existing strict policy metadata. Duplicate direct mixed flags reject before world creation. Current wounds count/counter are diagnostic; no damage/posture callback behavior is inferred. Height and palette effects still follow the documented modern presentation policy.

The [combined lead proof](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_12_2026-10-02.md) verifies native original palette resources plus framed mixed state/acknowledgment/failure. A fresh GPU Height+wounds transaction reaches current revision3,counter13 and values[-2,2147483647,-2]. All three actor crops exactly match ordinary scalar-mode Height/palette images, with retained GPU IDs and cleanup. Mode does not add components or other field/view support. The [mixed fixture producer](../poc/round12-tests/Integration/Generate-Mixed-Presentation.py) and [independent oracle](../poc/round12-tests/Integration/Verify-Mixed-Presentation.py) give exact reproduction; raw images/bytes remain private.
