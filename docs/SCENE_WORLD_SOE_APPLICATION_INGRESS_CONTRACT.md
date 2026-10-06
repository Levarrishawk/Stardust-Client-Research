# Offline SOE application ingress to presentation pump

Round70 borrows actual world, SceneWorldPresentationPump and socket-independent SoeClientTransport, joining actual SOE receive to the existing [combined application router](SCENE_WORLD_COMBINED_MOTION_INGRESS_CONTRACT.md). It creates no sockets, sends no controls, pumps no datagrams and closes no borrower.

```csharp
using var ingress = new SceneWorldSoeApplicationIngress(world, pump, transport,
    maxApplicationsPerDatagram: 8, maxApplicationBytes: 256);
var receipt = ingress.Receive(world.Generation, datagramSpan, cancellationToken);
// Every receipt.ControlDatagrams byte[] now belongs to the caller; no send is implied.
```

Maximum application count is explicit1..128; byte cap explicit1..65536, applied both per application and to the returned application aggregate. Applications must be nonempty. Transport's own framing/reassembly limits remain authoritative before these modern post-receive bounds. No separate per-step pump-byte budget is invented; the frozen pump exposes queued bytes/count/per-message and per-step message count.

Constructor verifies world and pump owner affinity through world.Snapshots and pump.QueuedMessageCount. Frozen pump exposes no exact-world binding/IsClosed API, so correct supplied pump/world relationship is caller responsibility, corroborated by actual construction in fixtures. It does not claim a mechanically proved binding or open-pump state. SoeClientTransport exposes no owner API; callers serialize its use. All ingress operations/property reads/disposal enforce construction-thread affinity and defensive nonreentrancy.

## Preflight and receive result

Receive checks bridge disposed, precanceled token and terminal IsFaulted first. Stale supplied generation, closed/failed world or non-Established transport returns a typed decline before transport.AcceptDatagram. It rechecks world/pump owner access before transport mutation. Open Idle/Selecting worlds are not rejected merely for lacking a finalized scene; actual queued application/world parser handles staging. No automatic world closure or zone-legality inference follows.

Actual AcceptDatagram returns owned application arrays and separately owned controls. The bridge validates the entire returned application count/per-app/aggregate bytes before copying any app to pump. QueueApplication copies each app; ingress increments its queued-progress counter and clears that source app. Finally all returned apps are cleared, including unqueued suffix on failure. No application bytes are retained by ingress.

On success receipt carries Status, ReturnedApplications, QueuedApplications, QueuedBytes, PeerDisconnected and actual ControlDatagrams. Controls transfer exclusively to the caller without byte copying; the bridge retains none and never clears them after transfer, even on disposal. PeerDisconnected is reported to caller, not translated into automatic pump/world close. On any failure after transport admission, untransferred controls are cleared along with remaining app arrays; no receipt/control transfer occurs.

Bad CRC/short framing uses actual RejectedFraming result, queues no apps and is nonfatal. A duplicate SOE sequence can generate an ACK but no apps. A duplicate full application movement counter carried in a distinct SOE sequence is instead queued and rejected later by the actual motion store. These layers and denominators remain separate.

ProcessedDatagramCount counts actual AcceptDatagram returns, including framing rejection/duplicates and returns followed by a bridge failure. An exception inside AcceptDatagram itself does not increment it. QueuedApplicationCount counts successful QueueApplication copies, including completed prefixes of failed receives. Counter overflow is checked before transport admission and is source-inspected, not seeded/exhaustively tested. Property counters and IsFaulted remain historical after disposal.

## Failure prefixes and owners

After transport starts, any transport exception, application bound, cancellation or queue failure latches terminal IsFaulted. Consumed reliability, received sequence/ACK state and earlier pump copies are completed prefixes; nothing is rolled back/requeued/retried. Subsequent receives throw before effects. Actual queue-capacity failure dynamically leaves one prior copied root; actual count/per-app/aggregate bounds consume reliability before queuing zero apps. Duplicate replay directly to the borrowed transport proves consumed sequence state while the bridge remains terminal.

Malformed61-byte full application inside otherwise valid SOE is not a framing/bound failure with the supplied limits. Ingress queues it and transfers controls, remaining healthy. Later pump.Step invokes the full decoder, throws, closes world, clears queued suffix and cancels/observes actual resource jobs. Earlier applied motion survives as immutable historical value and ceases to be current. Ingress sees closed world on its next call and declines before receive. Pump owns this cleanup, not ingress.

Mid-receive cancellation/world changes and a closed-pump failure after preflight are source-inspected possibilities with no injected callback/backdoor. With correctly paired world/pump and serialized owner-thread use, pump closure normally closes world and is caught by preflight. No private source-array exposure or reflection manufactures rare coverage. Explicit Array.Clear of retained/untransferred arrays is source-inspected; copied pump bytes, transferred-control survival and progressed queue/reliability counts are dynamically checked.

Dispose is owner-thread/idempotent and only releases borrowed refs. It never clears transferred controls, queued pump copies, transport reliable state, world/store/driver or jobs. The caller separately disposes these owners and clears caller-owned controls when finished.

## Independent validation

[Own replay](../poc/round70-tests/Networking/replay.py) packs a representative62-byte ServerToClient root/duplicate before native, with distinct reliable sequences0/1 and exact control bytes from the frozen independent round14 peer framing/CRC model. It uses a handpacked synthetic response because frozen SoeSessionCodec has no EncodeResponse, then actual BeginSession/AcceptSessionResponse. Multi-application and malformed61-byte fixtures are independently framed; no expected root/ACK/trajectory is seeded from native output.

```powershell
python poc/round70-tests/Networking/replay.py --output-dir local-private/round70-review-replay
```

Fresh guarded ignored output and cached offline .NET8 packages are required. Final replay passes73 native checks, five exact ACK/control frames, exact recovered root bytes, one ordinary grounded-driver correction-adoption tick and four real post-transport prefix cases. Native Combinedrouter/pump applies one correction and drops one duplicate application; duplicate SOE sequence queues none. Independent binary32 arithmetic predicts the subsequent local row/native basis/speed/revisions. Controls remain intact after ingress disposal, and queued bytes apply afterward, proving borrowed survival.

Additional gates cover Offline/SessionRequested/Closed transport, bad CRC/short framing, precancel/stale generation/foreign thread/constructor/disposal, malformed bounds, closed-world preflight, queue capacity and actual malformed pump terminal cleanup. Two controlled resource jobs are canceled/observed on applicable contexts; no resource completes or actor instantiates. Catalog/bank profile JSON and synthetic terrain/lifecycle bytes only; original asset/network send count is zero. Actual actor/host-controller cleanup remains lead integration work. Confidence: Confirmed tested offline join; original network legality/prediction/interpolation and rare concurrent prefixes remain unproved. Next action: lead host uses this actual receive bridge for synthetic framed corrections before steering, validates caller-owned controls and retains outer/inner cleanup ownership.
