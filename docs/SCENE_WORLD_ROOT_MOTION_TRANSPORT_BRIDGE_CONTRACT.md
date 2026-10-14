# Offline root-to-reliable transport bridge

Round69 borrows the actual [owned root queue](SCENE_WORLD_CLIENT_ROOT_MOTION_QUEUE_CONTRACT.md) and socket-independent SoeClientTransport. It owns at most one transferred62-byte array. It adds no socket/send, datagram pumping, receive loop, cadence or world/store ownership.

```csharp
using var bridge = new SceneWorldRootMotionTransportBridge(queue, transport);
bool accepted = bridge.TryQueueNext(world.Generation, cancellationToken);
int pendingBytes = bridge.OwnedPendingBytes;
ulong acceptedCount = bridge.AcceptedCount;
bool terminalFault = bridge.IsFaulted;
```

Constructor proves borrowed queue owner affinity by reading Count. Count remains readable after queue disposal, so this is not a proof that the queue is open: eligible TryQueueNext on a disposed queue propagates ObjectDisposedException before transfer. Transport has no owner-affinity API; callers must serialize its use. Bridge methods/property reads and disposal enforce construction thread and defensive nonreentrancy. There are no callbacks or test injection hooks.

## Admission and successful transfer

TryQueueNext checks bridge disposed, precanceled token and terminal fault before effects. Unless transport.State is Established and transport.PendingMessageCount is exactly zero, return false before queue.TryTake. This is conservative whole-transport pending-message backpressure, including unrelated channels, not a claim about per-channel availability or an empty reliable send window. Already retained reliable packets can exist even when PendingMessageCount is zero; the transport remains authoritative for its send window.

AcceptedCount checked increment is preflighted before take. The actual queue owns generation/active-world/store/incarnation checks and bounded stale-prefix discard. A false take leaves bridge/transport untouched; any queue stale prefix already discarded remains an honest queue effect. A queue exception before successful transfer does not latch bridge fault.

After successful take, bridge adopts the exact transferred array as pending. It checks cancellation again and calls actual QueueReliableMessage(array,0), whose existing implementation copies to its own PendingMessage bytes. On success bridge clears its transferred array, drops the reference and increments AcceptedCount. OwnedPendingBytes is then zero, while the accepted transport copy may still be pending or reliable-retained. The bridge never calls PumpQueuedDatagrams, AcceptDatagram or any network API. Those are caller-owned operations exercised only offline by the verifier.

## Failure prefixes and disposal

A cancellation or QueueReliableMessage error after adoption latches terminal IsFaulted and retains bridge pending bytes for owner cleanup. The frame has left the root queue; it is not requeued, retried, rolled back or reported as accepted. Subsequent attempts throw terminal InvalidOperationException. If a transport call ever failed after an internal prefix, the bridge cannot undo that borrower effect. No generic callback, reflection or mutable test hook was added to manufacture this rare branch; it is source-inspected, not dynamically tested. AcceptedCount overflow is also source-inspected and preflighted before transfer.

Dispose is owner-thread/idempotent, clears only an owned pending array and releases queue/transport references. It never closes/disposes/clears queue, world, store, transport, or accepted SOE copies. AcceptedCount and IsFaulted remain historical after disposal; OwnedPendingBytes is zero. TryQueueNext after disposal throws; property reads remain owner-thread. Previously accepted transport bytes survive bridge disposal and can be pumped by their actual owner.

Private bridge/queue/transport retained arrays are not exposed normally, so zeroing at their explicit Array.Clear/ZeroMemory calls is source-inspected. Native counts and emitted bytes prove accepted copies survive source clearing/disposal. A normal caller-owned unrelated-channel input is explicitly cleared after QueueReliableMessage, and its exact decoded payload survives, testing the actual copy boundary without a private backdoor.

## Reproduction and independent evidence

[Own replay](../../SWG-Godot-Client/poc/round69-tests/Networking/replay.py) handpacks three fixed full-root frames with arbitrary flags/U32 counter/timestamp words and representative rigid poses. Before native execution it uses the frozen independent round14 peer's selector4/seeded CRC/uncompressed framing to produce five exact datagrams and five exact ACKs, including an unrelated channel3 packet and a replacement-incarnation packet. This reuses accepted primitive evidence rather than repeating a CRC/channel/reliability census.

The frozen SoeSessionCodec has EncodeRequest/DecodeResponse but no EncodeResponse. The verifier independently handpacks a synthetic17-byte response and passes it to actual BeginSession/AcceptSessionResponse. It then executes QueueReliableMessage/PumpQueuedDatagrams, a second in-memory transport's AcceptDatagram and actual sender ACK release. No response-encoder or wire expected value is inferred from native output.

```powershell
python poc/round69-tests/Networking/replay.py --output-dir local-private/round69-review-replay
```

Fresh guarded ignored output and cached offline .NET8 packages are required. Final replay passes80 native checks, three root cases, five exact datagrams and five exact ACKs; build has zero warnings/errors. Synthetic typed lifecycle fixtures create actual current local rows. Gates cover Offline/SessionRequested/Closed admission, pending channel0 and channel3 before take, precancellation, foreign generation/thread/disposal/constructor, empty/disposed queue, destroyed/replaced stale prefixes, current-world reset and closed store/world. World change-batch currentness/entries remain intact. Actual root FIFO and SOE copies survive bridge-owned source clearing; disposing before the final accepted copy is pumped leaves that copy and all borrowers intact.

Rare post-take fault/cancellation, concurrent borrowed transport mutation, reentry and overflow are unrun/source-inspected; no live operation or original sender/interpolation parity is claimed. The queue's same-incarnation historical policy and the builder's metadata/coordinate policy remain unchanged. Zero resource jobs/original assets load; only catalog/bank profile JSON and synthetic data are used. Confidence: Confirmed tested offline bridge/ownership/framing behavior. Next action: lead host joins this borrowed bridge with the actual grounded FIFO and independently pumps/ACKs synthetic in-memory datagrams while preserving offline-only operation and cleanup ownership.
