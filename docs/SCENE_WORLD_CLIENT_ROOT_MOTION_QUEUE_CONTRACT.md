# Owned offline outbound root FIFO

Round68 owns the frozen [client root builder](SCENE_WORLD_CLIENT_ROOT_MOTION_CONTRACT.md) and its generated application arrays, borrowing only the exact current world/store. It provides no send/socket/SOE integration, cadence or automatic timestamp/counter policy.

```csharp
using var queue = new SceneWorldClientRootMotionQueue(world, motions, policy,
    maxPackets: 8, maxBytes: 496);
queue.Enqueue(actualSnapshot, currentLocalMotion, flags, movementCounter, timestampBits, token);
if (queue.TryTake(world.Generation, out byte[]? frame, token)) {
    // frame belongs exclusively to the caller; this example performs no send.
}
int count = queue.Count;
int bytes = queue.QueuedBytes;
```

Constructor requires explicit builder policy, open store bound to the exact world and owner-thread affinity via the builder. Maximum packets is1..4096; maximum bytes is62..253952 (4096*62). Caps are independent and need not divide evenly: maxBytes125 admits two62-byte packets. Invalid caps reject before constructing the owned builder. All methods/property reads enforce construction thread and defensive nonreentrancy. There are no callbacks or asynchronous work.

## Enqueue and retained identity

Enqueue checks disposed/precanceled state, active AwaitingServerReady/Ready world and open bound store, then both capacity limits before Build or any packet allocation. A full queue rejects even malformed snapshot/motion arguments before builder validation. It does not silently prune stale entries to make room. All ordinary validation/cancellation/capacity failures leave retained rows and bytes intact; builder publication and world acknowledgement never occur.

The frozen builder then requires exact current typed finalized uncontained LOCAL motion, explicit metadata and caller bounds. On success the queue directly adopts packet.Application with no second copy. Packet.Raw and source snapshot/motion are not retained. Each private row owns only the fresh62-byte array, captured world generation and essential SceneObjectIncarnation metadata. Count and QueuedBytes are exact retained totals; property reads do not prune.

Later motion revisions, local/authoritative interleave, typed-state refresh and containment changes within the same finalized incarnation do not stale already-built frames. They are historical offline bytes, not current-send authorization. In particular, a root queued before later containment remains historical and transferable; Enqueue of a newly contained source still rejects through the builder. The queue does not infer protocol legality/cadence from current movement state.

## Take and disposal

TryTake initializes output to null. Disposed/precanceled calls throw. A supplied generation unequal to current world generation, inactive/closed/failed world or closed/unbound store returns false without pruning or altering retained prefix. Such retained bytes remain owned until a valid take or disposal. There is no close/clear of borrowers.

On an eligible current-generation take, one current finalized-incarnation inventory is captured. At most the original retained Count entries are inspected (never more than4096). Old-generation, destroyed or replaced/non-finalized incarnation prefixes are dequeued, charged out of QueuedBytes and cleared. The first same-generation live-incarnation row is dequeued and its exact owned array is transferred; it is never cleared by the queue afterward. If all rows were stale, return false with count/bytes zero. Replacement rows retain normal FIFO order after the stale prefix is dropped. Cancellation is checked before each dequeue; a concurrent cancellation after some stale drops can leave an honest already-discarded prefix, with no rollback promise. No mid-scan cancellation injection is provided by this callback-free API.

Dispose is owner-thread/idempotent. It clears retained arrays, drains metadata/counts, disposes its owned builder and releases world/store references. Previously transferred caller arrays remain intact. Count/QueuedBytes after disposal are zero; enqueue/take after disposal throw. Foreign-thread disposal rejects before draining. No source snapshot, motion row, world changes, actor, resource job or transport is owned by the queue.

## Verification and evidence limits

[Own replay](../poc/round68-tests/Networking/replay.py) computes three fixed identity/half-turn/cyclic native bases through independent Python cross products, then packs expected full62-byte frames with struct before native execution. These representative cases reuse the accepted conversion boundary rather than repeating the prior full quaternion census. They include arbitrary U32 flags/counter/timestamp words, NaN-looking raw timestamp bits and signed-zero native position/speed. Exact complete FIFO arrays are compared independently.

```powershell
python poc/round68-tests/Networking/replay.py --output-dir local-private/round68-review-replay
```

Fresh guarded ignored private output and cached offline .NET8 packages are required. Final replay passes three exact FIFO frames,70 native checks and six lifecycle gates; build has zero warnings/errors. Native scenarios verify actual issued local rows, world change-batch currency/entry preservation, historical same-incarnation transfer after authoritative/typed/containment changes, zero/negative/bounded-value admission, independent byte/count caps, precancellation, wrong generation, foreign/copy/stale/authoritative sources, thread/disposal, destroyed/recreated/reset prefixes, closed world/store preservation and transferred-array independence. Altering one taken array cannot affect the next; disposing with a retained suffix cannot clear the transferred array or borrowed store.

Negative speed/invalid rigid matrices cannot be issued by the frozen store: native tests distinguish actual store-admission rejection from queue/builder copied-row identity rejection and actual lower caller-bound rejection. Inherited numerical guards retain their prior evidence. No packet buffer is exposed while retained, so actual retained-byte zeroing is source-inspected at the explicit Array.Clear calls, while draining/counts and transferred-byte survival are dynamically tested. No reflection, private hooks or debug exposure were added solely to manufacture byte-clear coverage. Exact-array adoption and absence of retained snapshot/motion references are likewise source-inspected structural facts.

Confidence: Confirmed offline FIFO/admission/identity/ownership behavior on tested paths; callback-free mid-scan reentry/cancellation and OOM are unrun. Catalog/bank profile JSON and synthetic lifecycle records are used; zero original assets/resources loaded, no live sends. Original sender cadence/units/gameplay admission remain Unknown. Next action: bounded host adoption and draining of this owned queue, verifying FIFO bytes against actual local trajectory and preserving offline-only operation.
