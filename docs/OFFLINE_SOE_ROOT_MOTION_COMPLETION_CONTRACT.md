# Primitive offline root completion observation

[OfflineSoeRootMotionCompletion.Capture(session, transport, queue)](../../SWG-Godot-Client/poc/Stardust.Core/Session/OfflineSoeRootMotionCompletion.cs) returns an immutable primitive snapshot of actual frozen child history and queue/transport counts. It owns no state, references, buffers, codec or lifetime and performs no drain/receive/send/cadence operation.

Fields: Samples:int, Queued:int, Drained:ulong, NextSequence:ushort, SessionFaulted:bool, TransportState:string, QueueCount:int, QueueBytes:int, PendingMessages:int, RetainedPackets:int, CleanDrain:bool. TransportState is the actual enum name. Every field comes from session.State or public actual queue/transport getters; no count is reconstructed from status or earlier receipts.

CleanDrain is exactly `!SessionFaulted && QueueCount == 0 && QueueBytes == 0 && PendingMessages == 0 && RetainedPackets == 0`. An empty healthy Established session at Samples0 therefore has CleanDrain=true. This is not a claim that sample budget/session/world teardown completed, that transport is Closed, that a server received an ACK or that supplied owners are associated. Queued/Drained equality and sample limits are not added conditions.

Capture checks all three null inputs before reads, then reads cached session.State, queue Count/QueuedBytes and transport enum/pending/retained counts. Session and queue owner-thread guards are inherited; actual foreign-session and foreign-queue reads throw. Transport itself has no owner API. Caller must supply exact associated owners and serialize all access. Snapshot is sequential, not an atomic cross-thread transaction; association/configuration matching remains an explicit caller contract. No reflection/private access adds a false binding proof.

After [root owner](OFFLINE_SOE_ROOT_MOTION_SESSION_OWNER_CONTRACT.md) disposal, actual Session history and Closed transport remain inspectable. Queue history counts remain until caller drains/disposes queue. Captured primitive values contain no borrower references and remain unchanged across subsequent cleanup.

[Independent replay](../../SWG-Godot-Client/poc/round79-tests/Networking/replay.py) computes exact handshake, one expected root/protected datagram/ACK, three complete session receipts and eight reports before native. Each of two actual frozen point-driver traces has three ordinary10ms updates: native Z speed2 twice, then stopped. Positive trace drains one counter1/flags5 root at30ms via frozen owner/session/ACK helper. Fault trace supplies sequence1 ACK when sequence0 is expected: cadence consumes sample3/queues1, no drain succeeds, session faults and FIFO1/62 remains. No rollback or retry.

| Observation | Samples/Queued/Drained/Next | State; queue packets/bytes; pending/retained | CleanDrain |
| --- | --- | --- | --- |
| Initial | 0/0/0/0 | Established;0/0;0/0 | true |
| Positive prefix | 3/1/1/1 | Established;0/0;0/0 | true |
| Deliberate teardown fixture | 3/1/1/1 | Established;1/62;1/1 | false |
| Owner disposed | 3/1/1/1 | Closed;1/62;0/0 | false |
| Caller queue disposed | 3/1/1/1 | Closed;0/0;0/0 | true |
| Fault prefix | 3/1/0/0, faulted | Established;1/62;0/0 | false |
| Fault owner disposed | same faulted history | Closed;1/62;0/0 | false |
| Fault queue disposed | same faulted history | Closed;0/0;0/0 | false |

Capture is repeatedly equal while actual state references/counts stay unchanged. Historical normal report remains unchanged even when later fixture/owner cleanup differs. Pending/retained fixture occurs after normal session advancement ends and only establishes report/cleanup behavior. It does not authorize external transport advancement during owned session use. A foreign thread constructs/disposes its own queue/scene; main Capture rejects that queue without mutating either owner.

Final24 checks, eight exact reports, six total point updates across positive/fault traces, three positive complete receipts and one exact root/datagram/ACK. Null/inherited foreign guards and clean read-only borrower preservation pass. Zero build warnings/errors, no redundant cadence/ACK-vector/reliability suite, sibling79 input or earlier production change.

```powershell
python poc/round79-tests/Networking/replay.py --output-dir local-private/round79-review-replay
```

Fresh guarded ignored output/cached offline .NET8 required. Confidence: Confirmed tested primitive offline observation; caller association/serialization contract remains. Original/live phase/ACK provenance and full completion remain unproved. No original assets/EXE/socket/send/capture. Next: lead serializes this report before and after its configured owner cleanup and distinguishes CleanDrain from full session outcome.
