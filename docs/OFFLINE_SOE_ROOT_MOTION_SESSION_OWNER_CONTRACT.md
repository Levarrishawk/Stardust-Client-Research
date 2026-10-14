# Owned synthetic offline transport and root session

[OfflineSoeRootMotionSessionOwner](../../SWG-Godot-Client/poc/Stardust.Core/Session/OfflineSoeRootMotionSessionOwner.cs) owns the actual frozen [bootstrap transport](OFFLINE_SOE_SESSION_BOOTSTRAP_CONTRACT.md) and [root session](SCENE_WORLD_OFFLINE_ROOT_MOTION_SESSION_CONTRACT.md). It borrows world/store/FIFO. Fixed pending caps8/496 and wire maximum496 retain existing synthetic CRC2/selectors1+4/sequence0 policy. No new codec, ACK helper, settings or socket.

```csharp
using var owner = new OfflineSoeRootMotionSessionOwner(world, store, queue,
    connectionId, seed, maxSamples: 6000, counterBase: 0, timestampBase: 0, flags: 0);
var receipt = owner.Session.Observe(snapshot, issuedLocal, relativeElapsedUs,
    nowMs, independentSyntheticAck, cancellationToken);
```

Constructor checks null world/store/queue, world owner affinity and active Ready/AwaitingServerReady state before bootstrap. Actual existing bootstrap creates established transport and immutable synthetic request/response strings. RootSession construction then verifies inherited store/current-owner and configured bounds. Child constructor failure closes created bootstrap transport and disposes any completed root prefix; borrowed scene/FIFO owners are untouched. Invalid maxSamples0/6001 exercises failure after bootstrap construction. Closed world is rejected before bootstrap, without resurrection or queue rollback.

Properties Transport,Session,RequestHex,ResponseHex,IsDisposed and Dispose require construction-thread affinity. They remain readable after disposal for diagnostics/history. Returned child references are borrowed from wrapper: Session.Observe is the intended sole advancement path while owner is open. Do not separately construct/advance another cadence/drain/session over these owners, mutate transport sequence/configuration or dispose borrowed children independently. Hidden queue-world/capacity association remains caller contract because frozen introspection is absent. A retained raw transport reference itself has no owner-thread guard; caller serialization remains necessary, not mechanically enforced by wrapper getters.

Dispose is idempotent and marks IsDisposed, invokes rootSession.Dispose first, then Transport.Close in finally even if child cleanup throws. It never disposes world/store/FIFO or driver. RootSession history and immutable handshake/receipt strings survive; owned transport becomes Closed and pending/retained counts clear. Constructor failure transport Close and cleanup ordering are source-inspected; hidden failed transport is not exposed through reflection/test hooks. Borrower survival and normal transport close effects are dynamically verified.

[Independent replay](../../SWG-Godot-Client/poc/round78-tests/Networking/replay.py) builds exact14/17-byte handshake plus three cadence/frontier receipts and one root/datagram/ACK before native. Connection F00D00DB/seed A1B2C3D4: request `000100000002F00D00DB000001F0`, response `0002F00D00DBA1B2C3D4020104000001F0`. Actual point driver advances three ordinary10ms steps: native Z speed2 twice, then stopped. Immediate-stop sample creates one62-byte frame at30ms, counter1, flags5; frozen synthetic ACK factory supplies sequence0. All independent bytes/receipts match.

After normal advancement, a deliberate teardown-only fixture puts one retained packet and one pending message into owned transport and one historical frame into borrowed FIFO. Wrapper disposal clears owned transport counts, preserves historical session state/FIFO1 and current LOCAL row/active world. Caller can take the surviving FIFO array; driver remains inspectable. This fixture does not authorize independent transport advancement during normal session operation.

Final31 checks, zero build warnings/errors. Additional gates cover owner-thread property/disposal, null constructor args, invalid sample caps/partial constructor, closed world, child null/stale/precanceled observations and disposed child use. Invalid preflight leaves session healthy at zero samples. Fixed wrapper constructor has no cancellation API; child Observe accepts tokens. Private cleanup/rare allocation failures are source-inspected; no redundant ACK-vector/cadence/reliability suites or sibling78 dependency.

```powershell
python poc/round78-tests/Networking/replay.py --output-dir local-private/round78-review-replay
```

Requires fresh guarded ignored output and cached offline .NET8 dependencies. Confidence: Confirmed tested modern ownership join; synthetic handshake/ACK is not server provenance, live legality and original behavior remain Unknown. No original assets/EXE/socket/send/capture. Next: lead wraps its configured offline root session with this owner, uses only its borrowed Session for serialized Observe, and keeps scene/FIFO teardown separate.
