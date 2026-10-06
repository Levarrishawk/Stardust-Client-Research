# Serialized bounded offline root generations

[OfflineSoeRootMotionRestartOwner](../poc/Stardust.Core/Session/OfflineSoeRootMotionRestartOwner.cs) owns at most one active frozen [root session owner](OFFLINE_SOE_ROOT_MOTION_SESSION_OWNER_CONTRACT.md), one immutable last [completion report](OFFLINE_SOE_ROOT_MOTION_COMPLETION_CONTRACT.md), and any terminal failed retirement prefix. It stores no factory or queue reference and creates no codec/socket/cadence policy.

```csharp
using var restart = new OfflineSoeRootMotionRestartOwner(maximumRuns: 8);
var borrowed = restart.Start(() => new OfflineSoeRootMotionSessionOwner(
    world, store, queue, connectionId, seed, maxSamples: 3, flags: 5));
// Advance only borrowed.Session under its existing offline contract.
var report = restart.Complete(queue);
var nextBorrowed = restart.Restart(nextFreshFactory);
```

Start(factory) and Restart(factory) return borrowed OfflineSoeRootMotionSessionOwner; Complete(queue) returns OfflineSoeRootMotionCompletion. Owner-readable properties: nullable Active, Generation:int, nullable LastCompletion, IsDisposed and nullable CleanupFaultType:string. History remains readable after disposal. MaximumRuns1..32 bounds successful factory admissions, including an admitted generation that subsequently faults. Generation starts0, increments once after successful admission and never resets. Start is initial-only; Restart requires a previous Complete and no active owner. Callback is never invoked for active/disposed/terminal/cap-invalid calls.

Factory runs synchronously on construction thread only after prior retirement. Returned owner must be undisposed, with actual session samples/queued/drained/next-sequence0, nonfaulted state, elapsed0/now−1/no last receipt, and Established transport with pending/retained0. Null/disposed/advanced/dirty returns reject. Candidate cleanup closes/disposes the returned prefix; ordinary failure leaves generation/previous immutable report unchanged and permits retry. Factory owns any prefix it creates but throws before returning; controller cannot discover an unreturned object.

World/store/FIFO/incarnation association, distinct connection IDs and no independent child/transport advancement remain caller contracts where frozen APIs expose no exact binding or internal sequence-origin/open-child API. Validation checks public fresh history, not an invented private proof. Previously retired owners have IsDisposed=true and cannot be resurrected. Fresh callback must create or transfer the actual intended same-thread owner; async/late callback work is unsupported and no callback is retained by controller.

All public operations/property reads enforce owner thread and defensive nonreentry. Reentry while factory executes poisons admission even if factory catches the nested exception and returns a valid child. Returned child is then disposed; controller history stays unchanged. Wrong-thread entry fails before invoking callback. Failed ordinary callback/admission is not a terminal generation fault if cleanup succeeds.

Complete reads actual session/transport and caller queue before retirement. Healthy session with any queue count/bytes or pending/retained packets rejects without retirement/report change. Caller may drain/release its outstanding prefix and retry. An actual faulted session may complete with nonempty FIFO/reliability: report retains that honest pre-retirement prefix, CleanDrain=false, and controller closes owned session/transport while never clearing borrowed FIFO. This method does not certify full sample-budget completion or server ACK provenance.

After buffer admission, LastCompletion is stored before current Dispose so it survives a retirement exception. Active becomes null regardless of cleanup outcome. Any retirement cleanup exception retains failed owned prefix and latches CleanupFaultType; Start/Restart/Complete then reject terminally. Failed factory cleanup aggregates original admission and cleanup exceptions and preserves prior report/generation. Dispose never silently retries failed retirement. If emergency Dispose of an active owner fails, it retains that prefix and fault type too; it creates no completion report because no caller queue was supplied. Rare same-thread current-owner disposal failures are source-inspected rather than artificially injected.

Normal Dispose is idempotent, closes active owned child and clears Active, preserving generation/report histories and all borrowed scenes/FIFOs. Explicit Dispose may abort an unfinished active generation without claiming completion. Returned references belong to controller; callers must not independently dispose/advance overlapping children outside the intended serialized Session.Observe path.

[Independent replay](../poc/round80-tests/Networking/replay.py) authors all handshake/root/ACK/receipt/completion expectations before native. Three successful admissions use distinct connection IDs/seeds: F00D00DB/A1B2C3D4,11223344/FFFFFFFF,55667788/0. Each actual point-driver trace takes three10ms steps: native Z speed2 twice then stop. Generations1/3 drain independently packed62-byte counter1/flags5 roots and synthetic ACK0. Generation2 supplies wrong ACK1 after cadence queues stop, preserving Samples3/Queued1/Drained0/Next0/fault/FIFO1/62. Complete closes that owner, caller separately clears FIFO, and generation3 starts fresh.

Final92 checks, nine actual point updates, three exact handshakes, three main completion reports plus the independent empty cleanup-case report, two exact protected root/ACK frames. Factory is proven to run only after previous owner's Closed state. Healthy queue/pending/retained Complete rejection preserves active owner; admission failures cover null/throw/old-disposed/disposed-new/advanced/dirty/caught-reentry, thread and cap. Rejected same-thread candidates close transport and preserve scene/store/FIFO. Emergency controller disposal preserves FIFO and records no invented completion.

One executed cleanup-failure case intentionally returns a foreign-thread owner after a separate empty healthy generation retired. Both validation and attempted cleanup throw affinity errors: AggregateException preserves both causes, CleanupFaultType is InvalidOperationException, prior generation1 and exact immutable zero-sample report remain, and subsequent factories/completion cannot run. Failed prefix is retained privately, not retried on Dispose. Test explicitly releases the producing thread to close its own child safely. Private retention is source-inspected; this invalid factory is not accepted normal usage or automatic cross-thread cleanup capability.

```powershell
python poc/round80-tests/Networking/replay.py --output-dir local-private/round80-review-replay
```

Fresh guarded ignored output/cached offline .NET8 required; build zero warnings/errors. No earlier production edits, sibling80 dependency, full codec/reliability vectors, original assets/EXE execution or live contact/send/capture. Confidence: Confirmed tested synthetic serialized lifecycle; hidden binding and rare retirement failures remain explicit limits. Next: lead joins two successive configured owners and a fault recovery, retains old report/display until new admission succeeds, and orders caller scene/FIFO cleanup separately.
