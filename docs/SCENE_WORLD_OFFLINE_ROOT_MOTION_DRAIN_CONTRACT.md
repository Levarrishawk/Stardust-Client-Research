# Single-frame offline root drain

SceneWorldOfflineRootMotionDrain (external historical reference) replaces repeated host orchestration of the frozen [root bridge](SCENE_WORLD_ROOT_MOTION_TRANSPORT_BRIDGE_CONTRACT.md). It borrows world, queue and transport, owns one bridge, and retains only immutable receipt strings/history. No handshake, socket, send, retry or codec change.

```csharp
using var drain = new SceneWorldOfflineRootMotionDrain(world, queue, transport,
    seed: 0xA1B2C3D4, maximumDatagram: 496);
var receipt = drain.TryDrainNext(world.Generation, nowMilliseconds, independentProtectedAck);
// null means no transfer; otherwise receipt has Sequence/ApplicationHex/DatagramHex/AckHex/AcceptedCount.
```

Constructor proves world/queue owner affinity and accepts maximumDatagram69..65507. Binding to the exact world, negotiated seed/CRC2/selectors1+4/maximum size and dedicated fresh channel-zero sequence origin0 are explicit caller contracts: frozen APIs cannot inspect these associations/configuration. Dedicated means no other channel-zero sender uses this transport while the drain is active. Other-channel work causes a conservative busy decline until caller clears it. A same-generation foreign queue with identical object IDs cannot be mechanically distinguished; no binding proof is claimed.

All operations enforce owner-thread/nonreentrancy. TryDrainNext first checks disposed/token/terminal fault and nonnegative monotonic nowMilliseconds relative to last admitted transfer. Equal clocks are allowed. Stale generation, inactive/closed world, non-Established transport, any pending or retained outbound packet, or empty FIFO returns null before ACK parsing/queue take. These declines do not advance clock/sequence/count. Borrowed queue may prune stale incarnation rows during its own subsequent TryTake and return false; this is its existing completed prefix, not rollback.

Eligible ACK must decode under supplied seed/CRC2/maximum, be exactly four decoded bytes `00 15` plus big-endian expected U16 sequence. Bad CRC, short wire, wrong sequence/channel/opcode throws before transfer, leaving drain healthy and FIFO/transport unchanged. Decoded ACK scratch is zeroed. Caller ACK bytes are borrowed read-only and survive success/failure; receipt AckHex is an immutable copy.

Actual bridge admission transfers/copies one FIFO root to transport. LastNowMilliseconds then commits. Actual PumpQueuedDatagrams must return exactly one packet; protected decode must yield `00 09`, expected sequence and exactly62 application bytes. Existing ObjectController codec decodes C2S root and re-encodes byte-identically. Provided independent ACK is then processed by actual AcceptDatagram and must release exactly1, return no apps/controls/disconnect/framing error and leave pending/retained counts0. Success creates receipt, increments AcceptedCount, advances sequence moduloU16 and stores LastReceipt. No synthesized ACK expectation is taken from native output.

Failure after admission latches terminal own fault with honest borrowed prefix. Sequence/counter/LastReceipt commit only on complete success; clock and BridgeAcceptedCount retain admission progress. Tested pre-used channel0 leaves unexpected sequence1 retained; supplied wrong seed leaves one retained frame; negotiated32 versus caller496 yields three retained fragments and fails the single-packet check. No rollback or borrower close. A bridge exception after ownership transfer also terminally faults this drain according to bridge.IsFaulted; pre-transfer bridge/queue exceptions propagate without inventing admission. Rare concurrent cancellation/allocation failures are source-inspected, not manufactured.

Returned pump datagrams, decoded packet, canonical root and any returned ACK-result app/control arrays are zeroed in finally after strings are created. Private byte zeroing is source-inspected; exact historical strings/caller ACK survival and public queue/transport counts are dynamically verified. Buffers not returned by a throwing borrowed API remain governed by that API's own cleanup.

Dispose clears/disposes owned bridge and releases references only. It preserves world/store/FIFO/transport and handed-out data. Owner-readable AcceptedCount,BridgeAcceptedCount,NextSequence,LastNowMilliseconds,IsFaulted,LastReceipt remain historical afterward. Caller separately disposes queue and closes transport/pump. Receipts remain usable after that owner cleanup.

Independent replay (external historical reference) handpacks eight identity-pose C2S roots before native: fractional positions, negative zero, native-to-wire(X,Z,Y), flags0/all-one/5, U32 counter/timestamp wrap. It independently protects sequences0..7 and exact ACKs with the frozen peer framing model, then uses actual issued LOCAL rows, FIFO, bridge, pump and ACK release. Eight historical FIFO frames survive later row publications and drain in order. Native output includes immutable receipts after context cleanup; Python compares every byte/string/count.

Final replay67 checks, eight exact roots/protected frames/ACKs and three real post-transfer failures; offline build zero warnings/errors. Additional gates cover invalid ACK/preCanceled/owner/disposal/stale/negative/backward clocks, pending/retained other channel, empty/offline/closed/inactive owners, mismatched foreign-queue generation and invalid constructor size. U16 wrap expression is source-inspected, not exercised through65536 transfers. All fixtures synthetic; no original assets/EXE execution or live activity.

```powershell
python poc/round74-tests/Networking/replay.py --output-dir local-private/round74-review-replay
```

Fresh guarded ignored output/cached offline .NET8 packages required. Confidence: Confirmed tested offline modern orchestration. Live server legality and original sender/cadence remain Unknown. Next: lead replaces its ad-hoc helper with this dedicated owner and supplies independently precomputed ACKs through the existing long-session profile.
