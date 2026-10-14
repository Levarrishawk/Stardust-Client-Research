# Bounded offline root-motion session

SceneWorldOfflineRootMotionSession (external historical reference) owns the frozen [cadence outbox](SCENE_WORLD_ROOT_MOTION_CADENCE_OUTBOX_CONTRACT.md) and [single-frame drain](SCENE_WORLD_OFFLINE_ROOT_MOTION_DRAIN_CONTRACT.md). It borrows world/store/FIFO/transport and consolidates one sample followed by at most one historical frame drain. No socket, handshake, codec change, burst loop or byte history.

```csharp
using var session = new SceneWorldOfflineRootMotionSession(world, store, queue,
    transport, seed, maxSamples: 1000, counterBase: 0, timestampBase: 0,
    flags: 0x1B, maxPendingPackets: 8, maxPendingBytes: 496, maximumDatagram: 496);
var receipt = session.Observe(snapshot, actualIssuedLocal, relativeElapsedUs,
    nowMs, independentProtectedAck, cancellationToken);
```

Constructor creates outbox then drain, disposing only successfully created own children on partial failure. Existing outbox fixes10000us consecutive sampling, default100000 movement interval/1000000 stationary heartbeat, cap1..6000 and modular U32 fields. Existing drain requires a dedicated fresh channel0/sequence0 established transport with supplied seed, CRC2/selectors1+4/maximum size. Exact queue-world/store association and capacity/configuration matching remain explicit caller contracts where frozen introspection APIs are absent; current issued LOCAL identity/world-store binding is checked by outbox. No sibling round75 dependency or earlier production changes.

Observe validates own disposed/owner/nonreentrant/token/fault and nonnegative monotonic nowMs before child work. Equal now clocks are allowed; relativeElapsedUs must follow actual outbox exact clock. Outbox first consumes one actual current LOCAL sample. Immediately afterward, actual queue Count/QueuedBytes are read into receipt QueueCountAfterCadence/QueueBytesAfterCadence. These are observed prefix values, not reconstructed from cadence status. Drain then tries at most one existing frame, including when cadence NotDue or DeclinedBackpressure. Empty, busy, closed transport/inactive world or stale queue generation declines according to frozen child policy. No queued suffix is replayed in the same call.

Returned immutable receipt contains Cadence, nullable Drain and the two pre-drain queue metrics. Session owns only last receipt/state; caller may retain older string receipts. Samples/Queued report outbox progress; Drained/NextSequence report completed drain progress. State also records IsFaulted, LastNowMilliseconds, RelativeElapsedMicroseconds and LastReceipt. History remains owner-readable after Dispose.

An invalid outbox preflight (stale row, wrong clock, token/owner) that advances no sample leaves session healthy. Any exception after sample frontier advances terminally faults the session, even a bad ACK rejected before queue take. Completed cadence/FIFO prefix remains; session does not retry/rollback or close borrowers. On enqueue failure, outbox may consume sample/relative clock before its own terminal fault; session records the same sample clock and supplied nowMs. LastReceipt remains the previous complete combined result. Bad ACK at sample10 preserves Queued1/FIFO1/Drained0 and prior sample9 LastReceipt. Actual capacity-contract mismatch preserves historical FIFO1/pending external message, Samples10/Queued0/Drained0. The child drain's post-transfer failures retain its own documented reliable prefixes.

Closed transport is an ordinary drain decline: cadence may queue current sample, receipt Drain is null and session stays healthy. Whole-transport pending/retained work likewise declines before take. Once external work is released, a backpressured cadence observation can drain exactly one old FIFO frame; NotDue can do the same. Caller owns live-policy decisions about whether to continue sampling or close these owners.

Dispose captures history, disposes owned drain/outbox and releases borrowed references only. It preserves FIFO contents, world/store/driver and transport; children handle their own private scratch clearing. Historical strings remain after caller later closes actual owners. Partial constructor child disposal/private zeroing and rare mid-call allocation/cancellation failure are source-inspected; no private hooks/reflection manufacture execution claims.

Independent replay (external historical reference) generates120 expected complete receipts and protected root/ACK bytes before native. Actual frozen point-target driver runs manual native Z movement at speed2 on a synthetic flat retained terrain query: ticks1..30 moving,31..40 paused,41..60 resumed,61..120 idle. Binary32 addition predicts exact translation; identity basis/quaternion, native-to-wire(X,Z,Y), flags5 and U32 counter/time wrap are independently packed. All120 cadence/frontier/drain receipts and seven exact root/datagram/ACK triples match.

Replay282 checks, zero native build warnings/errors. Additional executed cases cover bad ACK after enqueue, real enqueue capacity failure, busy/backpressure historical drain, closed transport preservation, NotDue two-frame prefix draining one, token/owner/clock/stale identity, disposed use and invalid second-child constructor size with borrower survival. Historical prefixes1/62 and2/124 are actual measured queue values. Modern pause means driver still issues a zero-speed LOCAL row on each advancing clock tick. Stationary heartbeat is inherited from independently verified outbox; this120-tick trace does not reach its next heartbeat after final stop.

```powershell
python poc/round75-tests/Networking/replay.py --output-dir local-private/round75-review-replay
```

Requires fresh guarded ignored output and cached offline .NET8 dependencies. Confidence: Confirmed bounded modern offline composition/byte fixtures. Original gameplay/cadence/phase/live legality remains Unknown. No original assets/EXE execution, socket/send/capture or unbounded retransmission suite. Next: lead replaces separate outbox/drain calls, logs the actual pre-drain queue metrics and independently checks scheduled receipts through its long profile.
