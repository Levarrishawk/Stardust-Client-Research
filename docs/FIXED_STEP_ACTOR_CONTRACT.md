# Fixed-step doorway actor host

## Owned timing boundary

FixedStepPortalActorHost (external historical reference) borrows the [persistent actor](PERSISTENT_DOOR_ACTOR_CONTRACT.md). It accepts monotonic nonnegative integer microsecond timestamps, finite world velocity bounded to 10 metres/second per component, nullable permission and an active flag. Default simulation frequency is 50 Hz: 20,000 microseconds per tick. Each update runs at most eight ticks, drops remaining whole ticks and retains the fractional remainder. Dropped and suspended time are reported separately.

The first update anchors time without movement. Inactive updates and the first resumed update discard pending/elapsed time; this prevents focus or pause gaps from becoming catch-up movement. Reset restores actor/controller state and starts an explicit new timestamp epoch. Expected actor failures latch their type, preserve completed work and stop implicit retries until reset. The host does not dispose its borrowed actor. Caller serialization is required.

These are Confirmed replacement implementation policies, not recovered original timer behavior. Integer accounting avoids accumulating render-delta rounding; the underlying actor and curve retain their documented float arithmetic. Configuration allows 1–100 ms steps and 1–32 steps per update. Invalid timestamp/input is rejected before state mutation.

## Input sampling and determinism

The supplied input snapshot applies to every tick produced by that update. Equal outcomes across render partitions require the same input at each simulation tick; the host does not reconstruct when a key changed between renders. Current tests align phase changes with fixed tick boundaries. The [timestamped timeline](ACTOR_INPUT_TIMELINE_CONTRACT.md) now supplies that seam.

The prototype host `-DoorWalk` adapter uses `Time.GetTicksUsec()`, W/S physical keys, 1.6 metres/second doorway-axis velocity, explicit continuous/manual mode and pause. Focus notifications and `Window.HasFocus()` suspend movement. Manual buttons are disabled in continuous mode. Reset and owner replacement re-anchor the clock; teardown disconnects updates and releases actor resources. A latched scheduler fault disables continuous mode and exposes reset-required state.

## Verification

Synthetic scenarios (external historical reference) compare 100 Hz, 20 Hz and jittered render partitions, validate pause/resume, monotonic and finite input rejection, configuration bounds, borrowed disposal, fault/reset and stall accounting. A 2,000,007 microsecond gap produces eight ticks, 1,840,000 dropped microseconds and seven pending microseconds.

Independent original verifier (external historical reference), using the separate implementation record, checks 1,428 updates, 1,260 actor ticks and 30 phase-end states over three render partitions and two original owner poses. All six effective asset pins match the [original instance checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_DOOR_MOVEMENT.md). The persistent 948-tick trace still passes after oracle reuse.

Headless/GPU prototype host checks exercise bounded catch-up, pause/focus notifications, mode ownership and injected W/S press, opposing-key cancellation and release. Injected events include logical and physical key codes and explicitly flush the engine buffer before immediate test polling. The production adapter polls once per render. GPU capture was inspected. Human keyboard interaction is not claimed from injected-event verification.

## Remaining work

Timestamped input transitions now pass their [bounded gate](ACTOR_INPUT_TIMELINE_CONTRACT.md). General planar movement, slopes, world obstacles, moving-leaf overlap and original narrow-phase/global actor ordering remain separate. Research scores and canonical function/protocol counts are unchanged.
