# Sourced native input host

October 4, 2026. This is explicit modern input policy over the frozen48 movement
admission. Original arbitration, effective speed, modifiers, network prediction,
camera behavior and controller mapping are not recovered by this slice.

## Explicit stop in the admission

The [admission](../../SWG-Godot-Client/poc/Stardust.Core/Session/SceneWorldExplicitMovementAdmission.cs)
now exposes its fixed `SpeedSource` and `bool SubmitStop(long timestampMicroseconds)`.
Stop checks owner/reentry/open state and the current active finalized typed CREO3
creation incarnation, then submits exactly `(0,0,0)` with null entry permission
through its owned actual driver. Stale incarnation returns false without driver
mutation. Existing time/order/60-second horizon/1,024 pending-input exceptions
remain atomic. Stop does not consult either speed reader, select a default or
return a source-provenance receipt. A disposed received reader does not prevent
an otherwise current explicit stop. Fixed source remains readable after disposal.
All frozen48 source-admission behavior remains unchanged.

## Node ownership and configuration

[SceneWorldSourcedInputHost](../../SWG-Godot-Client/poc/Stardust.Godot/SceneWorldSourcedInputHost.cs) is
an owner/main-thread Node. Inject `Func<long>` or use monotonic Godot microseconds.
Add it to a live parent, then call:

```csharp
host.Configure(admission, selectedWorldGeneration, receivedReader, orientation);
```

For an authored choice omit the received reader and generation. For a received
choice supply the positive selected generation and the exact borrowed reader
used by the admission. A foreign reader's receipt cannot pass admission; it
results in the unavailable-source stop policy. The immutable optional
`ActorViewOrientation` supplies native planar heading. Configure validates the
live parent and compatible clock/admission state, submits an explicit initial
stop, then takes exclusive ownership of the admission. Successful configuration
is once per lifetime. Failed null/parent/second/configuration/clock checks leave
the candidate with its caller. Construct a fresh admission: future queued inputs
can make the initial stop fail existing timestamp ordering. No source is sampled
or movement receipt produced by Configure.

The source choice remains fixed; there is no Shift source switch. The node owns
only admission. World, motion store, authored evidence and received reader remain
borrowed through the existing admission/reader APIs. It performs no resource load
or private catalog inspection. The [root/pin caller trust](SCENE_WORLD_EXPLICIT_MOVEMENT_ADMISSION_CONTRACT.md)
from frozen48 remains: path CRC is not independent proof of the world's catalog
SHA or recipe identity.

## Input, time and unavailable sources

HandleInput recognizes physical W/A/S/D, falling back to logical keycodes. Echo,
duplicate transitions, unsupported keys including Shift and modified shortcut
presses are ignored. Recognized releases clear keys even with modifiers. Discrete
axes use the frozen planar mapping at unit scale solely to create a normalized
native XZ direction; it does not seed movement speed. Diagonals are normalized,
heading rotates the native direction, and pitch does not enter planar movement.
No mouse capture, global polling, speed2/6 constants or engine coordinate conversion
is introduced. Supplied speeds remain the selected authored/received data.

With directional intent, each accepted transition, orientation change or enabled
Advance obtains a current exact received receipt when required and asks the
admission for a selected movement. With no intent, opposing keys or key release,
it uses explicit stop without sampling speed. A missing/ineligible source returns
no selected receipt and issues an explicit stop. Diagnostics are committed after
successful admission; key candidates are committed after clock and input checks.
`LastAdmission` is null on stop and is never presented as zero-speed provenance.

**Unavailable-source policy:** immediately suspend the driver at the observation
timestamp after stopping. This discards elapsed catch-up rather than advancing
old input while the observed source is ineligible. Held keys remain tracked so a
later current eligible source can be explicitly recaptured; restoration consumes
a resume boundary without catch-up before subsequent ticks move. Focus loss,
pause, inactive Advance and reset clear held keys, requiring fresh transitions.
An earlier queued sourced velocity is a captured historical command, not a live
pointer to received state. A later source update never rewrites it.

Advance is explicit; the Node has no process loop. All calls use one monotonic
nonnegative microsecond epoch. Focus and actual tree pause/unpause stop and
Advance inactive at their transition timestamps, consuming the suspended interval.
Reset requires a nondecreasing timestamp, resets the existing driver, clears keys
and receipt diagnostics and resets orientation; source choice remains fixed.

Suspension preserves the driver's last published pose and measured speed until
an actual tick publishes another result. `SourceUnavailable` and inactive state
mean movement is suspended; a historical nonzero motion-store Speed does not
mean input is currently eligible. Presentation must honor this suspension.

## Failure, callbacks and teardown

Clock callbacks must be pure. The host guards reentry and validates unchanged
live parent and admission-state reference after each sampled clock. Callback
world generation/recreation and source changes are checked by the existing source
and stop admissions. Detaching/freeing the Node during the clock requests teardown;
cleanup checks native instance validity before calling a Node method. Callback
Advance/reset/dispose is rejected after its state change. Already performed
external callback side effects cannot be rolled back. An aliased future timeline
submission is rejected by existing ordering/capacity; owned admission is retired
before any further host ticks. Exclusive ownership forbids callers from mutating
the admission after transfer; no inaccessible pending-history API is invented.

Public failures record `Failure`, stop/suspend best-effort at the last accepted
timestamp and dispose owned admission in a finally block. Failure cleanup never
samples the clock again, preventing recursion. Native input/notifications retain
failures instead of throwing into Godot dispatch. Wrong-thread/reentry guards
reject before transfer or mutation. Close is owner-thread/idempotent and attempts
an explicit stop/inactive Advance before disposal; it never closes borrowed world,
reader or motion store. Standard Godot tree ownership still applies: remove/free
Nodes when their parent no longer owns them. Close/Dispose release admission;
the standalone proof explicitly frees its detached test Nodes.

## Lead integration hooks

This slice does not edit the shared pumped preview, project, presentation host or
hardcoded local host. After reviewing resource publication, the lead should:

1. Construct the admission from the actual current finalized typed creature,
   motion store, fixed chosen source and frozen48 authored snapshot/root pin.
2. Create a received reader for opt-in partial worlds when selecting field7/11;
   use the same reader in admission and Configure with the selected generation.
3. Add this Node to the existing live parent and inject the same timestamp epoch
   used by admission and the caller's presentation BeforeStep.
4. Call sourced Advance before presentation, provide explicit orientation, and
   suspend locomotion presentation on unavailable/inactive input rather than
   interpreting a historical motion Speed as current eligibility.
5. Route focus/pause/reset and close the input owner before retiring actor/world;
   retain ownership of borrowed reader/world/store and free detached Nodes.

No round49 Assets API or pending sibling result is required. Actual original
resource publication may supply the already specified authored snapshot later.

## Evidence and validation

Confirmed modern behavior: the [native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/SceneWorldExplicitStopScenarios.cs)
execute real zero-input ticks and reject stale/order/time/capacity/thread/reentry
cases. [Independent schedules](../../SWG-Godot-Client/poc/round49-tests/Runtime/peer.py) cover 288 cases,
3,955 native checks and 1,152 comparisons. The
[standalone Godot proof](../../SWG-Godot-Client/poc/round49-tests/Runtime/Godot/SourcedHostProof.cs)
dispatches actual InputEventKey events, focus notifications and tree pause; 16
cases/144 checks and [43 independent comparisons](../../SWG-Godot-Client/poc/round49-tests/Runtime/verify_godot.py)
pass over synthetic pinned source inputs and actual sealed admission/drivers.
The launcher checks every existing lexical output ancestor for ReparsePoint
before directory creation and requires a fresh output directory. A private
junction ancestor and an existing directory reject without creating output;
a normal fresh actual-Godot control passes. No native Node leak warning remains. Installed Godot logs an environment
certificate-store read error; this proof uses no connection or TLS operation.

Frozen48 admission,47 received,18 local,39 terrain and default core smoke pass.
Both the standalone and shared Godot projects build with zero warnings/errors.
The [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R49_SOURCED_INPUT_HOST.md)
records input identity, corrected failures, reproduction and precise next work.
