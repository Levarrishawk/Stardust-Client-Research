# Modern bounded terrain region residency

TerrainRegionResidency (external historical reference)
prepares and atomically publishes bounded terrain windows. This is an explicit
modern scheduling policy, not recovered original terrain LOD, cache eviction,
thread affinity or worker parity. The geometry and height evaluation retain
their existing compatibility evidence and numeric limitations.

## Owner and worker boundary

The creating thread owns `AdoptInitial`, `Request`, `TryPublish`, `TryGetPrepared`,
`CommitPrepared`, `ReleasePrepared`, `CancelPending` and the initial
`DisposeAsync` call. Both chunk generation and complete region
composition execute on one owned worker task. Requests never evaluate
original terrain on the owner thread. No lock encloses a resource factory.

`Request(request, retry:false)` validates the existing
`OriginalTerrainRegionRequest` bounds and returns a monotonically increasing
generation. A repeated identical desired request returns the same generation
and does not restart/cancel work, including after failure or cancellation.
The caller may explicitly retry with `retry:true`. Rapid changed requests
replace a single latest pending target and cancel older work. The next worker
starts only when owner polling observes the prior task's completion and any
exposed prepared lease has been released; factories
that temporarily ignore cancellation cannot create concurrent successors.
Polling is therefore necessary to progress a superseded request.

`TryPublish(out completion)` returns true only after publishing a complete
successful candidate matching both the latest generation and desired request.
A false return can still carry an observed `Failed`, `Cancelled` or
`Superseded` completion; `LastCompletion` retains that diagnostic. No partial,
failed, cancelled or stale candidate changes `Current`. Errors are observed
and exposed in the completion, and no automatic retry occurs. A caller that
sets `CancelPending` also needs an explicit retry to reattempt that desired
request.

The committed snapshot exposes its request, generation, region, immutable
row-major chunk list and read-only key map. The coordinator retains at most
sixteen committed chunks and one candidate of at most sixteen by default. During a shift
the upper bound is32 unique coordinator-owned chunks, typically fewer because
overlap is shared. There is no history cache. Callers retaining older snapshots
own those additional references; this bound is not a process heap guarantee.

`DisposeAsync` idempotently retires publication, cancels pending/active work and
returns the owned worker drain. Source/host resources must outlive that await.
After disposal owner operations except prepared-lease release reject, so
completed stale work cannot publish.
Cancellation callback exceptions cannot prevent observation/drain of the task.
`IsDrained` is true only after its task completes. No detached worker or hidden
periodic background work remains. Injected factories must honor cancellation
for prompt shutdown; a factory that blocks indefinitely necessarily delays
its owned drain.

## Original source composition

OriginalTerrainRegionSource (external historical reference)
adds immutable `OriginalTerrainRegionChunk` receipts and complete sampling
keys: signed X/Z indices plus exact chunk-width bits. `BuildChunk` produces
the existing four-cell, three-margin, scale-one coordinate/height/normal/mesh
receipt. Each chunk records only its own applied height layers; `Build` unions
these records through `Compose`.

`Compose(request, chunks, token)` requires a complete row-major rectangle,
matching terrain/generator identity and exact requested keys. Actual coordinate
metadata must agree in width, X/Z, tiles, margin and scale. It retains the exact
supplied patch instances and invokes existing strict patch-set and surface-query
admission: shared heights, positions and normals must match bit for bit. It
does not rebuild reused chunk normals or geometry. Successful regions preserve
the original source hashes and existing request-center start position. Existing
`Build` and `BuildMosTaike` interfaces remain available.

The coordinator's generic factories exist to inject deterministic fixtures;
the original adapter is simply:

```csharp
var residency = new TerrainRegionResidency<OriginalTerrainRegionChunk, OriginalTerrainRegion>(
    source.Terrain.WorldSize, source.BuildChunk, source.Compose);
```

Only chunks overlapping the committed window under the complete key are reused.
A one-column shift of a4×4 window reuses twelve exact references and builds
four entering chunks. Failed candidate chunks are discarded and are regenerated
on a later retry. Different widths do not share a key.

## Validation and limits

The focused tests (external historical reference) use deterministic
injected generators, gates and authored exceptions. They cover initial commit,
overlap reuse, partial-candidate invisibility, a cancellation-ignoring stale
generation, latest-request collapse, repeated requests, explicit retry,
cancellation, generator/composer failure, reentry after eviction, owner-thread
rejection, idempotent disposal/drain and throwing cancellation callbacks.
Maximum simultaneous chunk builders is exactly one. The synthetic test reports
55 assertions and64 chunk factory calls; its fixtures do not represent original
terrain.

```powershell
dotnet run --no-restore --project poc/terrain-residency-tests/TerrainResidencyTests.csproj
dotnet run --no-restore --project poc/terrain-residency-tests/TerrainResidencyTests.csproj -- $OriginalTerrain
```

Fresh restore uses the project's explicit `NuGet.Config` and an available local
package cache. No download is required. Save original-input receipts only in
ignored private output paths.

October13,2026 original verification uses the pinned Tatooine input documented
in the [second-curve contract](TERRAIN_SECOND_CURVE_FRACTAL_BATCH_CONTRACT.md).
The adjacent windows `(93,57,4,4)` and `(94,57,4,4)` build16 then4 chunks,
reuse twelve exact references and each pass234 strict shared height/normal
comparisons. All sixteen successor chunks match a separately generated fresh
region's stored height and normal words. An independent barycentric triangle
query agrees with the region query within1e−5m. Wrong rectangle, ordering,
missing-chunk and cancellation composition negatives also pass. The complete
original/synthetic run reports109 assertions and original numeric parity false.

This does not establish unlimited geographic rule support, original client
arithmetic, original runtime streaming, terrain material parity or GPU resource
ownership. A host adapter must install all successor visuals consistently,
preserve the prior window on any host upload failure, retire old visuals only
after successful publication, and await the owned drain before teardown.

## Deferred CPU publication for staged GPU upload

The owner may now borrow a complete prepared window while retaining the prior
`Current`:

```csharp
bool TryGetPrepared(out TerrainResidencySnapshot<TChunk,TRegion>? snapshot,
                    out TerrainResidencyCompletion? completion);
bool CommitPrepared(long generation, out TerrainResidencyCompletion? completion);
bool ReleasePrepared(long generation, out TerrainResidencyCompletion? completion,
                     Exception? error = null);
```

A one-output `TryGetPrepared(out snapshot)` overload is also available.
Successful preparation returns the same immutable snapshot on repeated peeks
and leaves `Current` and `LastCompletion` unchanged. Failed/cancelled/stale
worker results returnfalse with a one-shot completion and preserve `Current`.
Legacy `TryPublish` still prepares and commits in one owner-thread call when
there is no exposed stage. Once a stage is exposed, it cannot bypass deferred
host publication: `TryPublish` returnsfalse until explicit commit/release.

`CommitPrepared` requires the matching prepared generation, current desired
generation and request, and no cancellation. It atomically transfers that exact
snapshot to `Current`, preserving chunk/region object identity and reuse counts.
Wrong-generation commits returnfalse without retiring any other lease. Stale
or cancelled commits returnfalse with their invalidation diagnostic once and
retain the prepared lease, so they cannot start another worker while the host
still owns staged references.

Changed requests and `CancelPending` invalidate an exposed stage but do not
remove it. The creating thread must dispose staged GPU resources and clear all
borrowed CPU snapshot references **before** acknowledging `ReleasePrepared`.
Only a matching release retires the coordinator's prepared references and
permits the latest pending worker to start. Valid abandonment reportsCancelled;
a supplied upload error reportsFailed; stale generations reportSuperseded.
Failed/cancelled same-desired requests still require explicit retry. Release
does not dispose generic CPU chunk/region objects: these are immutable managed
receipts, may overlap `Current`, and their GPU ownership belongs to the host.

The default coordinator retains at most16 current chunks and16 prepared chunks;
overlap keeps the unique count lower. A prepared lease blocks both another
candidate and another worker. `PreparedChunkCount` exposes this ownership,
and `InFlightGenerations` includes an exposed prepared lease. Caller-retained
historical snapshots remain outside this coordinator reference bound.

Dispose retires `Current`, invalidates the exposed stage and cancels/drains
worker work, but `IsDrained` staysfalse and the returned task stays incomplete
until every exposed prepared reference is acknowledged. `ReleasePrepared`
is therefore allowed after disposal on the creating thread; other owner
operations remain rejected. Wrong stamps cannot complete this lease drain.
The host should abort/release its staged upload before awaiting disposal;
otherwise that await intentionally waits for the outstanding lease.

Focused synthetic tests additionally cover repeated peek identity, initial
invisibility, exact commit identity, legacy bypass refusal, wrong stamps,
16+16 retention, stale-stage worker blocking, explicit stale release, upload
failure, cancellation, once-only retirement and post-dispose release/drain.
Maximum simultaneous builders remains one. The updated synthetic run reports
1115 assertions and1271 with the optional original input; the original run
verifies the same existing
seam/fresh-build/roaming joins. This adds no original streaming-parity claim.
Workspace validation passes1941 Markdown files with zero broken/private links,
and git diff --check passes. The deferred core tests exercise CPU ownership;
host GPU upload/disposal verification belongs to the adapter gate.

## Explicit full-resolution capacity and startup adoption

TerrainRegionCapacity (external historical reference)
defines `Default16` and the opt-in `FullResolution64` profile. Existing request,
source build/compose, coordinator and patch-set/surface-query calls retain their
sixteen-chunk defaults and original hard ceilings. Passing explicit enlarged
limits without the capacity discriminator still rejects. The new profile admits
at most8 rows and8 columns of40m chunks:320m per axis,64 chunks, and no change
to tile4/margin3/scale1 generation or5m height spacing. Unknown profiles, larger
rectangles, changed widths and coordinates outside the signed world extent reject.
The source constructor's surface-preview policy is independent of capacity.

The coordinator constructor accepts an optional fourth `capacity` argument;
`MaximumChunks` and `CapacityProfile` expose the selected bound. Source overloads
are `Build(request, capacity, token)` and `Compose(request, chunks, capacity, token)`.
Their older signatures continue selecting `Default16`. Composition supplies
the explicit patch-set and surface-query limits from the selected profile.
The separately bounded ray-query API is unchanged.

A full window contains12,544 padded cells,1,024 fans,9,216 selected vertices and
8,192 triangles. Strict shared height/normal checks cover112 edge pairs and98
diagonal corner pairs,1,106 shared sample comparisons. The quadratic seam scan
has2,016 possible pairs and6,048 total pair-loop checks across its two preflights
and final pass. Cancellation and work reservations remain in force. Patch-set
own reservations are50,688 copy bytes,58,624 array bytes and2,395,264 work units;
coherent original chunk aggregates reserve1,808,384 copy bytes,3,257,344 array
bytes and31,333,376 work units. Surface construction reserves270,336 copy bytes,
729,600 array bytes and1,183,872 work units. The profile caps query batches at
1,024, retaining the existing8,388,608 triangle-test ceiling. These are explicit
logical reservations, not process RSS or GPU memory measurements.

An enlarged coordinator retains at most64 current and64 candidate chunk
references. A prepared lease blocks another worker exactly as under the default
profile. Overlap reduces unique references; caller-retained historical snapshots
are outside this bound. A one-axis step reuses56 exact receipts and builds8.

```csharp
void AdoptInitial(OriginalTerrainRegionRequest request,
                  IReadOnlyList<TChunk> chunks, TRegion region);
```

The owner can adopt an already generated, rendered initial region before any
successful `Request` or adoption. This initializes `Current`, `DesiredRequest`
and generation zero without invoking either factory. An identical request
returns zero and starts no candidate; the first changed request is generation
one. Adoption validates selected capacity, exact row-major count and keys,
non-null receipts/region and owner affinity. Receipts must implement
`ITerrainResidencyChunkReceipt.Key`; regular factory-based clients do not need
that interface. Failed adoption leaves the coordinator virgin. A second adoption
or adoption after a request rejects. Adoption publishes a completion with zero
built chunks and all initial chunks reused.

The coordinator detaches the input list and key map while retaining exact object
identity. **The caller guarantees that the region is immutable and already
composed from these exact receipts.** The generic coordinator cannot inspect or
repair region geometry or detach arbitrary region internals. Source composition
fulfills this contract and exposes its detached read-only receipt list through
`OriginalTerrainRegion.SourceChunks`; the historical direct region constructor
and direct legacy `Build` leave that list empty. Adoption does not create GPU
objects or transfer their destruction to the core.

Focused tests cover both profiles' adoption, detached input mutation, exact
receipt identities, wrong count/order/keys/nulls, unkeyed receipts, foreign
threads, invalid capacity/bounds, repeated adoption, post-request/dispose refusal,
same-request generation-zero deduplication and generation-one entering-column
reuse. Capacity tests include exact geometry/reservations, one-below limits,
hard-ceiling rejection, cancellation,64+64 stage ownership and stale-stage blocking.
The pinned original terrain additionally checks64 receipts/8,192 triangles,
56 retained neighboring identities, all shared seams, every padded height/normal
against a fresh build,5m spacing and independent triangle grounding. These are
modern capacity and ownership claims; original scheduler/x87 parity is not claimed.
The final capacity/adoption gate passes1,456 synthetic assertions and1,879
with the pinned original terrain input. Maximum simultaneous builders is one.

## Roaming surface replacement

OfflineTerrainRoaming (external historical reference)
adds `ReplaceSurface(next)` for an owner-controlled window handoff. It queries
the current published float32 X/Z on the candidate surface, then requires the
queried height cast to float32 to match `Position.Y` bit for bit. A missing
point, changed stored height or query failure leaves the old surface intact.
Only after successful validation does the controller assign its retained surface.

Replacement preserves `Position`, the private double X/Z accumulators, heading,
speed, update count, edge/obstacle holds and last horizontal distance. It does
not initialize a new controller or reconstruct accumulation from published
float32 coordinates. This is a modern continuity condition at the displayed
triangle boundary, not an original client terrain-streaming claim.

`CanReset` queries whether the original spawn remains in the current window.
`TryReset()` returns false without any state mutation when that spawn is absent.
A successful reset uses the existing reset semantics, including leaving the
update counter unchanged. `Reset()` retains its previous throwing behavior for
a missing spawn. The host may use the safe API after window shifts.

The focused tests additionally build actual authored flat meshes for overlapping,
missing-point and changed-height windows. A one-F32-step height change rejects;
1,000 subfloat movement steps after replacement match an unswapped controller's
published position, heading, speed, counters and distance words exactly. Prior
obstacle/edge holds survive replacement, movement enters newly available terrain,
and missing-spawn reset and the historical throwing reset both preserve state.

The October13 follow-up passes1,069 synthetic assertions and1,224 combined with
the optional original input. On the two adjacent original windows,100 further
subfloat movement updates match the unchanged-surface control bit for bit.
The residency tests and original seam/fresh-build checks described above also
remain in that run. No engine/GPU upload is exercised by these core tests.
