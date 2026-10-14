# Explicit following pivot for the orbit host

October6,2026. Modern supplied engine-world pivot policy. This extends the
[orbit host contract](TERRAIN_CAMERA_ORBIT_HOST_CONTRACT.md) with an opt-in follow
operation; it does not implement original camera behavior or an actor-follow adapter.

```csharp
public Vector3 Pivot { get; }
public bool SetPivot(Vector3 pivot, CancellationToken cancellation = default);
```

TerrainCameraOrbitHost (external historical reference) accepts explicit
pivot updates while input is disabled. The caller supplies an engine-world point; no
native coordinate conversion, actor ownership or Runtime72 source is implied. Components
must be finite and within+/-1e6. Creating managed thread and prototype host main thread, configured
open healthy host, exact live parents, same viewport and active perspective camera are
required. Lifetime and cancellation are checked before an equal-pivot no-op returns false.
Equal updates preserve pose, diagnostic and every counter.

Changed updates retain the existing yaw, pitch and distance. Position is calculated
against the candidate pivot using the existing binary64 orbit arithmetic and one final
binary32 cast. Existing bounded/noncollapsed/nonpole admission still applies. SetPivot
additionally rejects candidate position approximately equal to its target under prototype host
`Vector3.IsEqualApprox`: at large coordinates a nonzero offset can still be rejected by
LookAt. This extra check applies only to the new operation; existing Configure/Dispatch
admission remains unchanged. Numeric/canceled preflight rejection leaves the healthy
camera, stable state, diagnostic and counters unchanged.

After admission, the actual camera GlobalPosition setter and LookAt(candidate,Up) execute
with lifetime/reentry/cancellation checks after each. Only after both return and guards
pass does Pivot commit. PoseAttemptCount increments for admitted mutations; Revision
increments for successful pivot commits. AcceptedCount and HandledCount retain their
input-event denominators. LastDiagnostic.Kind is Pivot and Fromprototype hostEvent is false.
Yaw/Pitch/Distance remain unchanged. No new counter is needed.

Mutation failure faults the host and records the existing diagnostic prefix. A camera
position or basis may already have changed while Pivot and Revision remain at their
last successful values. There is no rollback. Caught nested SetPivot attempts poison the
outer mutation. Faulted/closed hosts reject further updates. Preflight lifetime rejection
does not itself set the fault flag, matching the existing direct-call policy. Close drops
the borrowed camera reference without freeing or restoring the camera. Pivot remains
the last committed diagnostic value after Close; getters still require owner thread.

## Bounded evidence

The standalone verification (external historical reference)
executes12 follow updates across three yaw/pitch/distance configurations and four pivots.
An independent Python oracle (external historical reference) generates
position/look-basis/perspective-ray expectations before native execution, reusing the
accepted orbit arithmetic oracle. Each update follows an accepted input and disabled
input state, proving orbit/input counters are retained. Position tolerance2e-5;
basis/ray tolerance3e-5.

Final headless98 checks and GPU100 checks pass, with one private GPU target capture.
Seven lifetime/effect fixtures cover caught reentry, cancellation after position,
projection/reparenting after position, freed/queued camera and unconfigured host.
Additional fixtures cover malformed vectors, initial cancellation, exact and approximate
binary32 collapse, equal pivot, off-thread calls, closed/faulted calls and borrowed
survival. The unchanged frozen orbit verification also passes118 headless checks,
47 independently compared input poses/rays and24 accepted inputs with the modified host.

Invoke-Picker.ps1 (external historical reference) uses offline cached
historical implementation path (omitted) verifies executable identity, verifies the frozen4335-file archive and
manifest in its producer, stages the accepted regression source, produces expectations,
builds and runs the new scene headless/GPU, then the old regression headless. Output must
be fresh and ignored; reparse/nondirectory/public output paths are rejected. No original
executable execution, original assets, live input capture, network, main-scene mutation
or unfinished sibling dependency is involved.

The bounded tests do not prove arbitrary transformed/scaled parent accuracy, checked
counter overflow, all large-coordinate poses or a gameplay following policy. LookAt
returns void; native engine error logging is not an exception contract. See the
[checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R72_CAMERA_PIVOT_HOST.md)
for the rejected precision experiment and resume point.
