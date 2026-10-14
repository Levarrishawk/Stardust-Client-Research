# Local input and locomotion presentation

October 2, 2026. Modern offline policy, not recovered prediction, input timing,
authority resolution or original animation-graph behavior.

## Timestamped input host

SceneWorldLocalInputHost (external historical reference) is a
Node against the frozen
local driver interface (external historical reference).
Construct normally for monotonic `Time.GetTicksUsec`, or inject `Func<long>`.
Add it to a live parent before `Configure(driver)`. Successful Configure takes
exclusive driver ownership; invalid null/thread/parent/second configuration
leaves the candidate owned by the caller. Driver state and clock must have
nonnegative, compatible timestamps. Reentry rejects before transfer.

Public operations are `HandleInput(InputEvent)`,
`Advance(long timestampMicroseconds,bool active=true)`, `SetPaused(bool)`,
`SetFocused(bool)`, `SetOrientation(ActorViewOrientation)`, `Reset(long)`,
`Close()` and owner-thread `Dispose()`/`IDisposable`. `LastState` and `Failure`
provide diagnostics. All clocks/Advance/reset/transition times use the same
nonnegative monotonic microsecond epoch. The host defines no process loop.
Lead calls Advance from presentation-host BeforeStep, then the pump advances.

Actual unhandled input dispatch recognizes physical W/A/S/D and Shift, falling
back to logical keycodes. Press/release transitions call the driver once; echo,
duplicates, unsupported event/key and modified shortcut presses are ignored.
Recognized release still clears a held key when shortcut modifiers are present.
`PlanarActorInputMapping` normalizes diagonals; walking2/run6 and native +Z forward
are explicit modern defaults. `ActorViewOrientation` optionally rotates planar
heading; no mouse capture, camera implementation, wire encoder or global polling.
EntryAllowed remains null.

Focus loss/tree pause release held input and Advance inactive at the notification
timestamp. Resume consumes the suspended interval inactive and leaves keys empty,
requiring new transitions. Explicit inactive Advance releases once; repeated
suspended frames/setters do not enqueue duplicate zero transitions. Reset clears
keys and orientation and invokes driver Reset. Exit/Close attempts final zero and
inactive Advance, then disposes the owned driver once, including release faults.
Input/driver/time faults record Failure, close ownership and propagate through
public calls; native dispatch catches them in Failure. Callback reentry and wrong
thread reject. This adapter delegates actual fixed-step/drop/suspension accounting
to the driver; fake-driver proof does not establish those unfinished internals.

## Creature locomotion wrapper

SceneWorldLocomotionVisual (external historical reference)
implements the frozen presentation interface with constructor
`(SceneWorldCoordinator world, SceneWorldObjectMotionVisual inner,
ISceneWorldObjectMotionSource motionSource, float runThreshold=4)`.
It exposes borrowed `Inner`, `Publication`, `SelectedClip`, `Frame`, `IsRetired`,
Tick/Apply/Dispose, SetPlaying/SetFocused and Reset.

Constructor rejects invalid threshold, foreign/stale/retired/static inner,
missing loaded idle/walk/run or invalid initial speed before acquiring inner.
Caller retains rejected inner and must dispose/reuse it. Success owns inner;
world/source are borrowed. The selected creature's existing CPU clips are reused,
with no loader, resource scheduling, material change or motion-store dependency.

Matching current motion Speed0, missing/foreign/older motion chooses idle;
positive speed below threshold chooses walk; speed at/above4 chooses run. Threshold
is finite in(0,10]. Current nonfinite/negative speed faults and retires ownership.
The source must remain stable during serialized presentation calls. Only a
monotonic accepted motion revision is tracked; there is no duplicate store.

`AnimationPreviewClock` uses selected clip's original frame rate/end/proxy scale.
Switching resets phase0; Tick advances at most0.25 seconds per call, with accepted
finite range0..60. Inner motion Tick receives0, so its idle clock cannot double
advance. Apply advances no clip time. Focus/playing suspend phase; Reset immediately
sets selected phase and displayed pose to0. Clip selection finishes the accepted
0.15-second pose crossfade by passing0.25 presentation seconds to SetMovement;
this intentionally selects the target pose immediately without advancing clip
phase. It is modern diagnostic policy, not original blending timing. Idle uses
SeekIdle directly. Native root pose/Height and resource/lifetime guards stay in
the accepted motion wrapper; original root-motion transfer is not implemented.

Stale supplied state is rejected; stale resource/incarnation retires. Errors and
callback reentry retire owned inner and release borrowed world/source. Disposal
is main-thread and idempotent; clocks/resources are never reloaded by speed, focus,
phase or input changes. Static terminal in the proof remains a rigid motion visual,
not a locomotion actor. Interpolation and actual local-driver/router integration
remain lead work.

## Independent proof

Standalone preview (external historical reference) dispatches
real prototype host key events, native focus notifications and tree pause/unpause against
an injected clock/fake frozen driver. It then uses original human/terminal closures
and a fake frozen motion source for loaded clip selection. Core clock and separate
Python arithmetic agree: walk/run at0.1 seconds both frame3, rate30, proxy scale1;
end frames33/22. Focus/playing freeze, displayed reset, missing idle, invalid-speed
retirement and constructor ownership are tested. Native pose/Height/raw-create/GPU
identity and recreation checks from the accepted proof remain exercised.
Confidence: Confirmed modern host/visual behavior; no actual driver/store/router
or protocol implementation is inferred.

Launcher (external historical reference) creates a private scene with
headless, fresh GPU capture or real archive cancellation modes. The
seven-argument oracle (external historical reference) takes three
logs and initial/updated/recreated/motion PNGs; it verifies input/clip receipts,
independent frame arithmetic, pose coordinates, GPU identity, pixels and cleanup.
Five roots allocate/dispose; cancellation closes both archive jobs and publishes
zero roots. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R18_LOCAL_INPUT.md)
records input identity, exact paths/commands/results and limits.
