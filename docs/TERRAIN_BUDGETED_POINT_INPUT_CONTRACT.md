# Explicit budgeted point input

October6,2026. Modern offline input policy over existing native picking and targeting.
Adapter (external historical reference),
host (external historical reference),
native proof (external historical reference),
independent oracle (external historical reference).

## Source-compatible admission

```csharp
TerrainPointTargetInputAdapter(TerrainCameraPicker picker,
    SceneWorldTerrainPointTargetDriver controller, int maximumAttempts = 16,
    TerrainOfflineSessionBudget? budget = null, bool declineWhenExhausted = false);
```

Omitted options preserve strict1..16 admission and the throwing exhaustion behavior.
An explicit immutable budget allows maximumAttempts in1..budget.MaximumTargets,
which is bounded at128. Supplying a budget does not change the requested/default
maximumAttempts. The constructor validates live exact borrower query identity,
controller admission at its current clock and picker admission before storing any
borrower. No new engine copy, terrain lookup, picking algorithm or target codec is added.

Owner/main-thread guarded getters MaximumAttempts, RemainingAttempts, IsExhausted
and DeclineWhenExhausted inspect primitive policy/capacity without querying borrowers.
They remain readable after disposal, retaining historical counts. Existing disposal
clears LastResult and drops references without disposing a borrower. All prior
[adapter admission and prefix rules](TERRAIN_POINT_TARGET_INPUT_CONTRACT.md) remain.

## Explicit exhausted decline

For a recognized pressed left button, owner/disposed/null-input/token checks precede
capacity. With declineWhenExhausted true, exhausted HandleInput returns false and
null result before borrower admission, clock/range validation, query, marker or target
mutation. LastResult, picker receipt/attempts, target, counters and caller clock are
unchanged. Cancellation still rejects before decline. Ignored input preserves its
existing behavior; the default policy still throws at the cap. No fault latch or
rearm/refund is added to the adapter.

The actual host checks its normal lifecycle/token guard, then only in the explicit
decline branch rejects a disposed adapter before checking exhaustion. A live exhausted
adapter returns false before ClockReadCount or its clock callback, preserving host
LastDiagnostic/accepted/handled counts and leaving IsFaulted false. _UnhandledInput
therefore leaves the33rd event unhandled. Default host dispatch keeps its original
clock and failure prefixes. An exhausted-then-disposed adapter faults the host with
ObjectDisposedException and null diagnostic clock before reading the source.

Borrower caps remain independent. The caller must choose a matching picker attempt
cap and driver target cap. Adapter attempts include misses and later failed attempts;
only successful hits charge driver targets. No adapter budget raises either borrower's
limit. A smaller driver or picker rejects in existing preflight before another adapter
attempt; it is not treated as adapter exhaustion. Unrelated stale borrowers can remain
uninspected once a live adapter is exhausted. Decline is capacity policy, not a general
lifetime or availability probe.

## Confirmed native evidence

Actual prototype host4.7.2 .NET runs pass59 headless/60 GPU checks. Independently authored
pre-native rows compare32 actual viewport presses:31 hits/one miss, exact attempts,
remaining capacity, host clock/accepted/handled counts, target revisions and binary32
coordinates. Frozen Fraction geometry/projection checks compare all32 rays. A supplied
exact camera basis keeps expected vertical targets independent of LookAt roundoff.
The33rd viewport press performs no clock read/query/handled commit/fault and retains
exact last adapter/picker/target/host references. Direct decline returns null despite
invalid unused timestamp/range. Two actual manual move/zero-stop ticks still complete.

Negative cases cover canceled exhausted input, new capacity admission, smaller
borrower caps, stale clock, canceled fresh input, inactive camera, off-thread getters
and dispatch, disposed exhausted adapter and deterministic borrower cleanup. A fresh
strict16 adapter rejects its17th click before querying. Frozen adapter proof passes69
checks/five hits/one miss; frozen default host proof passes73 checks/four hits/one miss.
Their independently checked binary32 targets, cancellation/reentry/lifecycle prefixes
remain unchanged. Build has zero warnings/errors; no native leak diagnostics occur.

Observation: these are executed replacement policies on synthetic terrain and typed
world fixtures. Confidence: Confirmed for the bounded fixtures. Implementation
consequence: callers may explicitly continue gameplay after consuming click capacity,
while keeping all independent query/target/session budgets and ownership limits.
No original key, ray, movement, clock or live-server parity is established. The GPU
run completes an actual rendered frame; visual scene fidelity is not measured.
128-click execution and every mismatched/stale borrower combination remain untested.
The offline certificate-store diagnostic remains with successful exit0.

Next probe: lead selects explicit maximumTargets/maximumAttempts for its user profile,
joins matching picker/driver caps and decline option, then independently checks click
exhaustion UI, ongoing movement/stop, pause/focus and exact final teardown.
