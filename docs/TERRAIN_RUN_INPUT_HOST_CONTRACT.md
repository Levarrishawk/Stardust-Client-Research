# Held Shift run-input host

TerrainRunInputHost (external historical reference) is an actual prototype host Node over caller-owned `Func<long>` clock and `Action<bool,long>` run-intent sink. It owns a three-bit held mask, callback/configuration references, checked counters and one latest primitive diagnostic. It performs no speed, direction normalization, simulation tick, clock advance, network send or borrower disposal. Original Shift/gait/speed/input cadence and parity remain unknown.

## Configuration and key ownership

Attach under a live exact parent in the intended viewport and call `Configure(Func<long> clock, Action<bool,long> sink)` once on the constructing managed thread and prototype host main thread. It validates live host/parent/same viewport and non-null delegates, starts enabled with empty mask/false running and invokes neither callback.

`Dispatch(InputEvent input, CancellationToken cancellation = default)` recognizes enabled, non-echo Shift keys. PhysicalKeycode is preferred whenever non-None; logical Keycode is consulted only when PhysicalKeycode is None. A non-Shift physical code with logical Shift is ignored. It uses actual `InputEventKey.Location`: Left=held bit1, Right=bit2, Unspecified=bit4. These are independent ownership slots. Unspecified is not treated as a wildcard; release clears only its matching slot. A mismatched-location release does not clear another slot. This is explicit modern policy and does not use prototype host IsMatch wildcard semantics.

Running is true iff any held bit exists. Echo, unrelated input, duplicate press and unchanged release return false without clock/sink/counter/diagnostic changes. A relevant changed mask returns true after successful local commit, even when aggregate running remains true. Actual `_UnhandledKeyInput` marks that event handled only after success. Already consumed GUI events never reach this callback.

## Different counters and diagnostics

`HeldKeyMask` and `IsRunning` inspect last committed local ownership. `AcceptedCount` counts committed mask transitions, including nonempty disable/focus stops. `HandledCount` counts successfully consumed actual key events only. `ClockReadCount` and `SinkCallCount` count their respective callback invocations and can differ from accepted/handled counts on failure. All inspections are owner/main-thread guarded. `IsEnabled` is false when faulted/closed; `IsConfigured`, `IsFaulted`, `IsClosed`, `LastDiagnostic` and `LastFailure` expose configuration/lifetime diagnostics.

When the proposed aggregate boolean changes, read the caller clock, guard reentry/liveness/token, invoke the sink with that exact clock and proposed boolean, guard again and commit the held mask. When the mask changes but boolean does not, invoke neither callback; apply owner/liveness/token/reentry and checked accepted-counter guards, then commit the local mask. No old clock is invented for a mask-only result.

One `TerrainRunInputDiagnostic` records proposed Running/mask, nullable timestamp, operation kind, SinkInvoked, SinkReturned, StateCommitted, failure type and engine-event origin. Successful mask-only diagnostics have null timestamp, false invocation/return flags and true StateCommitted. Successful aggregate transitions carry the actual returned clock and true callback/commit flags. Ignored input and enable toggles needing no submitted transition leave the prior diagnostic. It stores no exception object/message/stack or borrower reference.

## Failures and external prefixes

Caught nested Dispatch, SetEnabled, Configure or Close during a clock callback poisons outer submission before the sink. A sink can catch nested host mutation, complete an external effect and return; the host then faults before committing its mask. SinkInvoked=true/SinkReturned=true/StateCommitted=false explicitly records that completed external prefix. A throwing sink may also have external effects; a false return flag does not prove rollback.

Selected transition failures propagate from direct calls, record one failure, preserve prior committed mask/accepted state and disable key processing. Actual key/focus callback boundaries record failure and suppress escape into engine callback processing. Failed key events remain unhandled. Pre-routing direct state/null/unsupported-location rejection does not create an accepted diagnostic; the engine event boundary still faults on such an event error. Fault is terminal for further dispatch/enable/configuration; Close is available. The host does not validate or advance caller timestamps; the sink owns that domain and may reject an invalid clock.

## Enable, focus and terminal close

`SetEnabled(bool enabled, CancellationToken cancellation = default)` returns false for an unchanged setting. Disable submits false exactly once if currently running, then clears all held bits and disables processing. Repeated disable sends no second false. Re-enable starts empty/false without callback invocation or replay of old held state. If a false sink throws after stopping external run intent, local committed held ownership can remain nonempty while the host faults.

Actual window-focus-loss notification applies disable/stop. Focus gain does not automatically enable. UI callers explicitly disable before GUI capture when external walk reset is required, then re-enable explicitly. Consumed GUI Shift events cannot themselves tell this unhandled host that capture began.

Close is owner-thread guarded, terminal and idempotent. It drops callback/parent references, clears local bits and disables processing without invoking the sink. A previously published external running=true can survive Close; disable first if reset is needed. Actual `_ExitTree` closes immediately and poisons busy callback work. Caller owns Node Free and external callback effects. The diagnostic contains only primitives and can remain inspectable after Close.

## Reproduction

The offline standalone runner (external historical reference) stages only this host and its verification node (external historical reference), using cached prototype host packages. It has no Core/Runtime/sibling dependency. The independent model (external historical reference) authors46 action/control events before native execution. Its successful timeline has23 committed masks,19 handled keys and14 exact clock/sink calls; these are separate denominators, not coverage scores.

Actual Viewport.PushInput, focused Control.AcceptEvent and native propagated window-focus notification tests compare masks, booleans, clocks, counters, callback phases, ignored/consumed events, mask-only token/liveness guards and cleanup. Results and input identities are in the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R69_RUN_INPUT_HOST.md). Focus notifications are injected through prototype host's native notification path; no live user keyboard or operating-system focus transition is captured. GPU diagnostic panel proof does not establish original input, movement or visual parity.
