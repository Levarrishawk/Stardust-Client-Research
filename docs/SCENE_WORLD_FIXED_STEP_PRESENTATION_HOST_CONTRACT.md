# Opt-in fixed-step presentation schedule

October6,2026. Modern caller-supplied scheduling policy extending the
[presentation host](SCENE_WORLD_PRESENTATION_HOST_CONTRACT.md).
SceneWorldPresentationHost (external historical reference)
retains its exact existing overload and adds a schedule overload:

```csharp
void Configure(ISceneWorldPresentationPump pump, Action<double>? beforeStep = null);
void Configure(ISceneWorldPresentationPump pump, Action<double>? beforeStep,
    Func<double,int>? fixedStepSchedule, double fixedStepSeconds = .01,
    Func<bool>? continueFixedSteps = null);
```

The first three schedule-overload arguments are explicit. This avoids C# ambiguity
with the existing overload while preserving its source, CLR and method-group/delegate
signature. Pass null for beforeStep when unused. Without a schedule, fixed seconds must
equal the default0.01 and the continue callback must be null; extra options reject before
pump ownership transfer. Fixed seconds must be finite in0.001..0.1, inclusive.

The existing overload forwards exact defaults. Its process path still calls BeforeStep,
Step(delta), and FrameCompleted once for finite nonnegative delta, including zero. Its
existing callback/receipt/close/drain behavior remains; no schedule guard is applied to
that path. This is verified by the unchanged frozen default proof and a two-parameter
Configure delegate invoked with variable delta0.037.

## Scheduled execution and prefixes

For each finite nonnegative process delta, the host invokes the borrowed schedule once
and requires0..8 slots while still open. Zero calls no before-step, pump or completion
callback. No new delta clamp, accumulator or planner exists in the host; caller owns
reservation and elapsed-time policy. Before each reserved slot, optional continueFixedSteps
is read once. False stops the remaining reserved slots without closing the host or refund.
There is no refund API or retry within that process call.

Each slot separately invokes every actual BeforeStep callback with fixed seconds, calls
the actual owned pump.Step(fixed seconds), records its numbered receipt, then invokes
FrameCompleted callbacks. CompletedFrames/LastFrame count successful Step receipts before
completion observers, matching the existing prefix rule. An observer fault can retain
that frame while Completion fails; a before-step fault records no frame for that slot.
There is no batching of several simulation steps into one presentation receipt.

Scheduled callbacks and pump returns are followed by exact live node/parent/viewport and
reentry guards. Captured parent and viewport must stay current. Caught nested _Process,
Configure (including null arguments), callback mutation/subscription attempts poison the
outer scheduled call. CloseAsync is an intended stop operation, not rejected reentry.
Close, node exit, failure or cancellation exception stops immediately; no later slots or
multicast callbacks run. Normal prototype host reparenting triggers ExitTree and normal close.
Failure preserves completed prefixes without rollback and faults Completion after the
existing close/drain/dispose handoff. A callback's earlier effects cannot be undone.

## Ownership and lifetime

Schedule and continue callbacks are borrowed and cleared on Close alongside existing
callbacks and captured parent/viewport references. The host owns the accepted pump's
Close, drain wait and Dispose exactly once. All configuration rejection precedes transfer;
caught drain-getter reentry/current-parent loss in scheduled configuration also rejects
without touching candidate ownership. Rejected configuration can be retried while open.
Methods remain historical implementation path (omitted) bound; off-thread rejection occurs before
schedule or pump effects. The host still owns no world input, simulation planner or quit.

The caller can translate cancellation into CloseAsync or throw a cancellation exception
from a callback. The latter is a retained failure, not a silent successful stop. No new
CancellationToken parameter or Runtime74 dependency is introduced. Checked frame-counter
overflow and arbitrary pump behavior outside its interface contract remain unexecuted.

## Reproducible bounded evidence

Invoke-Picker.ps1 (external historical reference) builds a private
standalone scene with the modified host, frozen Core and frozen default proof/profile
inputs from the verified4404-file archive. The
the separate implementation record writes
exact slot traces, counts and25 failure prefixes before native execution. No original
assets, original executable execution or unfinished sibling source is involved.

New verification (external historical reference) uses an
instrumented wrapper over the actual frozen SceneWorldPresentationPump and a cold generic
world with forbidden loader/factory. Four schedules0/1/2/8 produce11 separate actual
pump calls. Endpoint gate permits2 of8, interval endpoints0.001/0.1 pass, and25 exact
prefixes cover count/delta/source/before/pump/observer faults, caught reentry, normal close,
parent change, exit/free, off-thread, second-slot failure and cancellation. Six configuration
rejections prove caller ownership. Final80 headless/81 GPU checks pass; private GPU capture
is a small replacement-only diagnostic. The frozen default proof passes102 checks over18
pump owners,7 attempted steps and exactly18 closes/disposals.

The cold world does not prove scene publication, rendering-actor cadence, gameplay state,
network throughput or original timing parity. See the
[checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R74_FIXED_STEP_PRESENTATION_HOST.md)
for retained failures, validation and the precise main-scene join.
