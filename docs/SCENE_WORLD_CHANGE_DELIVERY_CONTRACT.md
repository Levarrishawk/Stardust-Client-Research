# Coalesced postcommit world changes

R11-RUNTIME-01, 2026-10-02. A modern single-consumer serialized observation API on SceneWorldCoordinator (external historical reference), alongside accepted [typed world state](SCENE_WORLD_REPLICATION_CONTRACT.md). No synchronous callbacks, events, dispatch threads, subscriptions, or resource publication queue are added. This is implementation policy, not recovered original observer behavior.

## Exact API for the next consumer

```csharp
[Flags]
public enum SceneWorldChangeKind { Lifecycle = 1, Replication = 2 }
public sealed record SceneWorldChangePolicy(int MaximumPendingObjects = 4096,
    ulong MutationRevisionSeed = 0);
public sealed record SceneWorldChangeEntry(ulong ObjectId,
    SceneWorldChangeKind Kind, SceneWorldObjectSnapshot? Current);
// Final optional parameter appended after existing replicationPolicy:
SceneWorldChangePolicy? changePolicy = null
public SceneWorldChangeBatch? DrainChanges(long generation);
public bool IsCurrent(SceneWorldChangeBatch batch);
public bool AcknowledgeChanges(SceneWorldChangeBatch batch);
```

The sealed batch exposes Generation, U64 MutationRevision and an owned read-only Entries list; its world discriminator is private. Existing constructors remain source compatible. Null policy enables delivery with capacity4096 and revision seed0. Capacity1..4096 is required; invalid capacity throws ArgumentOutOfRangeException. The optional seed supports deterministic exhaustion tests, not a wire sequence.

DrainChanges returns null for stale generation, selecting/initializing/failed/closed world. An active world returns a batch, possibly empty, without changing world state or inventing baseline/default values. Every entry is resolved from the current private registry/replication state at drain time. Current=null means the ID is absent now; otherwise Current carries the latest object, placement, accepted opaque envelope view and optional typed replication snapshot. No registry/store instance is exposed.

Drain is the prepare phase of an acknowledged drain. It does **not** remove pending slots. After applying a current batch, call AcknowledgeChanges; it clears all slots only while owner, generation, active state and mutation revision still match. Stale/foreign acknowledgments return false and leave pending IDs intact. Repeated current acknowledgment is idempotent. This avoids losing unconsumed changes when another mutation invalidates a held batch. It is one coordinated consumer: multiple independent callers must coordinate acknowledgment rather than expect subscription-specific delivery.

```csharp
var batch = world.DrainChanges(generation);
if (batch is not null && world.IsCurrent(batch))
{
    // Consume current entries on the serialized owner thread.
    // Validate resource/replication publications separately when using them.
    if (!world.AcknowledgeChanges(batch))
    {
        // A later drain retries all still-pending IDs at their current state.
    }
}
```

The consumer may retain immutable batches as observations. IsCurrent checks world owner, external generation, active phase and exact global world mutation revision. Any later accepted object application conservatively invalidates the whole batch, even when another ID changes. Resource polls/restarts/publications and ready control messages do not advance this mutation revision; their existing freshness guards remain separate. Acknowledgment is not a state mutation, so a held acknowledged batch can still be current while its observations are unchanged.

## Recording, order and bounds

Each successfully applied create/end/contain/destroy marks Lifecycle. Full typed baselines/scalar plans/mixed plans and opaque envelope applications mark Replication. Accepted idempotent lifecycle operations or missing-ID destroy attempts also mark their addressed ID as modern activity policy. Transport-suppressed duplicate packets produce no application. Create/baseline/delta/end/contain for one ID coalesce by ORing flags into one slot.

Destroy also marks direct children whose registry handles detach. No historical removed incarnation is retained. Destroy/recreate of the same ID before acknowledgment resolves to its final current replacement at drain. An older removal batch becomes stale and cannot clear the replacement's slot. A removed object's tombstone does not manufacture a default state or resource.

The world retains only ID, ORed flags and first undelivered mutation revision per slot. Ordering is first undelivered revision, then unsigned object ID for ties (for example multiple affected IDs in one parent destroy). Flags describe coalesced accepted activity, not every intervening event. No prior object/replication snapshots, batches, payloads or event histories are retained by this delivery map. Caller-retained results and existing bounded opaque envelope history are outside this metadata map.

All affected IDs are capacity-preflighted, and the next checked U64 mutation revision is computed before application. Overflow or insufficient pending capacity follows existing whole-generation failure: world and typed state/queues/changes clear, jobs cancel, held batches become noncurrent, and no partial replay batch/ready escapes. Unseen changes are never silently dropped to make room. Acknowledge frees slots without changing revision; otherwise distinct removed IDs may occupy slots until acknowledgment. The mutation counter never rewinds through selection/failure. At exhausted sequence, construct a fresh coordinator to resume; Close still succeeds without incrementing the counter.

Initial staging publishes no changes. Barrier replay may accumulate private slots, but they become drainable only after successful ordered replay. Failed replay clears all slots, including earlier valid operations. Selection/reset, failure and close discard delivery rather than emitting partial scene-deletion batches. Consumers must handle scene generation/global failure/close boundaries explicitly. Owner-thread and reentrancy enforcement applies to drain, current check and acknowledgment; no thread dispatch or callback ownership is implied.

## Evidence and reproduction

Confirmed native synthetic behavior: 496-byte offline peer, ordered barrier replay, one latest coalesced object, stale acknowledgment retry, 1000 baseline/delta pairs with one slot, first-touch order, tombstone/recreated ID, parent-destroy child detachment, immutable lists/states, equal generation/revision foreign-owner rejection, foreign thread and actual loader-query reentrancy, capacity and sequence exhaustion during live/replay paths, selection/failure/close cleanup and resource-poll separation. Prior opaque/typed world and exact-state tests pass. These are modern implementation facts; research coverage and original lifecycle semantics are unchanged.

The independent oracle verifies request/response/framing/reliable order and runs a deterministic coalescing/incarnation/scalar model against native exported batches. The producer explicitly negotiates496 and checks every datagram within bound; original128-byte opaque fixture remains unchanged. Baselines, lifecycle bytes and tiny managed resource closure are independently authored synthetic fixtures, not captures/assets.

```powershell
$env:APPDATA=Join-Path $env:TEMP 'stardust-runtime-r11-appdata'
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force $env:APPDATA,local-private/round11 | Out-Null
python poc/round11-tests/Runtime/Generate-Peer.py local-private/round11/peer.json
dotnet restore poc/round11-tests/Runtime/Runtime.csproj --configfile poc/round11-tests/Runtime/NuGet.Config -p:NuGetAudit=false -p:UseSharedCompilation=false
dotnet build poc/round11-tests/Runtime/Runtime.csproj --no-restore -p:NuGetAudit=false -p:UseSharedCompilation=false
dotnet poc/round11-tests/Runtime/bin/Debug/net8.0/Runtime.dll local-private/round11/peer.json local-private/round11/export.json
python poc/round11-tests/Runtime/Verify-Changes.py local-private/round11/peer.json local-private/round11/export.json
```

Shared registration is lead-owned: parse generated peer JSON and call SceneWorldChangeScenarios.Run(peer, optionalExportPath). The [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R11_WORLD_CHANGES.md) records exact scope, input identity, regression gates and resume point. No next-round rendering, wounds API dependency, engine behavior or live connection is included.


## Round12 mixed-state delivery — 2026-10-02

The opt-in [mixed replication mode](SCENE_WORLD_REPLICATION_CONTRACT.md) uses this unchanged acknowledgment API. One complete interleaved scalar/wounds plan produces one Replication mutation and one latest store revision, even when all wounds commands are counter no-ops. Drain resolves `Current.Replication.Scalars` and nullable `Current.Replication.Wounds` together from the committed immutable state. Scalar-only observations retain null current wounds. Flags/capacity/ordering/acknowledgment are unchanged; no per-field event or history is retained.

Mixed decode, policy, element/aggregate budget, local revision or observer preflight failure clears the whole generation and its pending slots. A staged late apply failure makes neither an earlier replay prefix nor ready bytes drainable. Resource loads/publications retain independent freshness and do not mark delivery. Confirmed native mixed tests and independent ordered state/single-mutation comparison are in the [Round12 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R12_WORLD_MIXED_REPLICATION.md); the prior scalar/opaque observer regression still passes.


## Round13 composite state observation

The explicit [components/composite world mode](SCENE_WORLD_REPLICATION_CONTRACT.md) exposes current Components alongside Scalars/Wounds in each latest replication snapshot. One whole accepted delta creates one state revision and one mutation/entry; a successful prefix is never observable. Field5 membership has no inferred callback or render meaning. Resource publication/poll still does not advance the observer revision, and acknowledgment remains current-batch/serialized-consumer policy.
