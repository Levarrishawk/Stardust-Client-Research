# Outer multipacket disconnect ordering

Round72 closes bounded offline ordering through actual transport, ingress and scene pump. It adds [scenarios](../poc/Stardust.Core.SmokeTests/SceneWorldMultipacketDisconnectScenarios.cs), an [independent replay](../poc/round72-tests/Networking/replay.py), and one lead-approved exceptional cleanup line in SoeClientTransport.AcceptOuterMulti.

Outer decoded bytes use `00 03`, followed by one-byte length plus each decoded leaf. This is distinct from application `00 19` nesting. Frozen decoder flattens up to64 leaves/depth4. AcceptOuterMulti validates every leaf shape before dispatch, then processes leaves in order until PeerDisconnected. Validated suffix is ignored after matching disconnect. This prevalidation checks packet shape, not connection-ID semantics or full application codecs.

Independent Python packs an exact62-byte synthetic S2C root correction: operand0x71, object F122334455667788, counter33, quaternion(.5,.5,.5,.5), wire position(1.25,-3.75,2), speed4. Reliable prefix is `00 09 00 00` plus that root; suffix is sequence1 with the same root. Disconnect is `00 05 12 34 56 78 00 06`. Outer packets are independently protected using selector4, seed A1B2C3D4 and CRC2 before native. Replay records exact four protected packets and verifies exact root bytes. Expected prefix ACK is `00 15 D4 D4 D4 F4 1A`.

| Leaves | Observed completed prefix | Scene policy |
| --- | --- | --- |
| reliable0, matching disconnect, reliable1 | Closed transport; one returned/copied root, one ACK, PeerDisconnected | Caller may Step once to apply counter33, then close |
| same leaves | Same receipt, queued root retained | Caller may Close immediately and drop root |
| matching disconnect, reliable0 | Closed; zero returned/copied apps, zero controls | Caller closes owners; no root delivery |
| reliable0, matching disconnect, malformed two-byte disconnect suffix | Shape prevalidation throws before any reliable dispatch; Established | Ingress terminal fault; direct reliable0 retry returns one app |
| reliable0, wrong-ID disconnect, reliable1 | Semantic exception after reliable0 consumed; Established | Ingress terminal fault, no receipt/pump copies; direct retry returns no app |

Exceptions inside transport leave ingress ProcessedDatagramCount/QueuedApplicationCount zero and no transferred receipt. Ingress refuses another receive after fault. Direct borrowed transport retries produce the same independently expected ACK; application count distinguishes unconsumed prevalidation from consumed sequence prefix. No rollback or semantic reset is claimed. Exceptional collected application arrays are cleared by existing code. The approved fix also zeroes collected controls before throw. Those untransferred private arrays are not exposed: zeroing is source-inspected, not dynamically measured.

Successful controls transfer to caller unchanged and survive ingress disposal, queue disposal, pump.Close and transport.Close. Pure transport receipt also retains exact application bytes after repeated Close until caller clears them. Ingress copies its returned root to pump and clears source arrays; successful copy applies through actual typed full-motion router even while transport is Closed, when caller explicitly chooses that policy. This is modern owner orchestration, not a claim of original client policy or live legal packet order.

Actual pending outbound root queue retains one62-byte frame when bridge declines Closed transport before take. Bridge AcceptedCount remains zero. Caller disposes queue separately. Pump owner closes world, drops remaining copies, cancels/drains controlled resource completions; no late application route occurs. Only apply-policy context starts two resource jobs; drop/disconnect-first contexts start none. Current scene/store borrowers survive ingress disposal until pump owner closes world.

Replay passes52 native checks, three success/policy cases, two failure-prefix cases and one pure transport ownership case. Build zero warnings/errors; workspace links and focused diff checks pass. Run with cached offline .NET8 packages and a fresh ignored destination:

```powershell
python poc/round72-tests/Networking/replay.py --output-dir local-private/round72-review-replay
```

Confidence: Confirmed bounded offline execution; private exceptional zeroing source-inspected. No original assets/EXE execution, sockets, sends, capture or full retransmission suite. Live phase legality and trailing reason semantics remain Unknown. Next: host joins a zero-speed correction before final disconnect and chooses explicit apply-before-close policy.
