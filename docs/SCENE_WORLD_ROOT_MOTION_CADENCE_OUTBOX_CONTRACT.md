# Modern root-motion cadence outbox

[SceneWorldRootMotionCadenceOutbox](../poc/Stardust.Core/Session/SceneWorldRootMotionCadenceOutbox.cs) schedules the existing [root FIFO](SCENE_WORLD_CLIENT_ROOT_MOTION_QUEUE_CONTRACT.md). It borrows world/store/queue and owns only cadence, modular counter and last successful pose/speed/flags values. No codec, byte history, socket or sender parity is introduced.

```csharp
using var outbox = new SceneWorldRootMotionCadenceOutbox(world, store, queue,
    maxSamples: 1000, maxPendingPackets: 8, maxPendingBytes: 496,
    counterBase: 0, timestampBase: 0);
var receipt = outbox.Observe(snapshot, issuedLocalMotion, elapsedMicroseconds, flags);
```

Constructor fixes step10000 microseconds; defaults movement interval100000, stationary heartbeat1000000. Supplied intervals must be positive step multiples, heartbeat at least movement interval; sample cap1..6000, packet cap1..4096, byte cap62..253952. Elapsed is relative to caller's session origin. First observation must be10000; each subsequent observation exactly previous+10000. No skipped samples or burst replay. This is explicit modern policy; no original cadence/time-unit claim.

Exact world/store binding is checked. Queue/world/store matching and supplied queue capacities are caller construction contracts because frozen queue exposes no binding/capacity introspection. Constructor proves world/queue owner access. Observe requires active world, finalized uncontained typed current snapshot and exact store-issued current LOCAL row with null authoritative counter, matching canonical object/placement/envelopes and typed revision. Once bound, generation/incarnation cannot change. Pose rows/translation and speed must be finite, speed nonnegative; proper rigid pose is inherited from issued-store invariant and queue builder. Auth/stale/foreign rows fail before frontier commit. Owner-thread, disposal, token, terminal fault, exact clock and sample-budget checks also precede commit.

Dirty compares transform/speed/flags to last successful queue sample, not changing row revision. Initial or dirty state queues only after interval from last success (initial origin0). Moving means speed>0. Moving-to-stopped queues immediately once, retaining pending-stop intent if backpressured. Later idle same-pose rows do not generate interval traffic; stationary heartbeat queues after heartbeat from last success. If motion resumes while a stop is pending, next queued sample uses latest current motion, never an intermediate replay.

Count and QueuedBytes are inspected before Enqueue. Full queue returns DeclinedBackpressure but consumes exact observation clock/sample budget, preserving successful time/counter/pose. After drain, next due sample queues latest state once; retained historical frames are untouched. First successful counter is unchecked counterBase+1; timestamp is unchecked timestampBase+(uint)(elapsed/1000). Both are U32 modular fields. Successful queue commits counter, QueuedCount and last sample/time. NotDue/backpressure receipt reports current counter and current sample timestamp without allocating bytes.

An actual Enqueue exception after observation commit latches terminal IsFaulted. Clock/sample frontier and borrowed queue's completed prefix remain; counter/last-success do not advance. No rollback/retry. Tested capacity-contract mismatch leaves one retained FIFO frame, clock200000/sample20, counter1/time100000, then refuses future observation. Rare cancellation during Enqueue is source-inspected, not injected. Existing builder/queue behavior, including its world change-batch operation, is inherited.

Dispose releases only borrowed references. History getters Samples,ElapsedMicroseconds,LastSuccessfulQueueMicroseconds,MovementCounter,QueuedCount,IsFaulted remain owner-readable afterward. Queue, store, driver, world and handed-out arrays survive. Caller closes/drains those actual owners separately.

[Independent replay](../poc/round73-tests/Networking/replay.py) builds all expected statuses and bytes before native.1000 actual terrain-driver ticks move at native Z speed2 for ticks1..300/501..700, otherwise stationary. Binary32 addition predicts position; identity orientation maps native(X,Y,Z) to wire(X,Z,Y). Capacity2/124 retains ticks20..45, yielding six backpressure samples.54 exact62-byte frames and all1000 statuses match, including stop and heartbeat boundaries plus counter/timestamp wrap. First root passes actual queue-to-transport bridge, protected SOE datagram and independently packed ACK; no send occurs.

Final replay2033 native checks, zero build warnings/errors. Additional negatives cover sample cap, invalid constructor cap/step, wrong/duplicate clock, token/owner, stale/foreign/authoritative row, foreign bound store, closed world, actual enqueue failure; immediate stop backpressure/recovery and stationary initial+heartbeat behavior pass. Source invariant supplies finite issued rows; forged/private states and rare mid-call failures are not manufactured.

```powershell
python poc/round73-tests/Networking/replay.py --output-dir local-private/round73-review-replay
```

Requires fresh guarded ignored output and cached offline .NET8 packages. Confidence: Confirmed bounded modern implementation/independent offline fixtures. Original sender policy, prediction, interpolation and live network legality remain Unknown. No original asset loads/EXE execution or live activity. Next: host observes each1000-step trace row, drains each queued frame through actual bridge and compares independently scheduled bytes/ACK release counts.
