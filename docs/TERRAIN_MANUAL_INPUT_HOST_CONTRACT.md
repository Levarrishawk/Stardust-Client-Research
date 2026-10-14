# Manual native-direction input host

[TerrainManualInputHost](../../SWG-Godot-Client/poc/Stardust.Godot/TerrainManualInputHost.cs) is a Godot Node borrowing a caller clock reader and native-direction sink. It owns four held-key bits, configuration references, checked counters and one latest diagnostic. It does not normalize diagonals, apply speed, advance clocks, tick simulation, send messages or dispose a caller-owned object. This is modern replacement policy; original movement/input units, cadence and gameplay parity remain unknown.

## API and key policy

Attach the Node to a live exact parent in its intended viewport, then configure once on its constructing managed thread and Godot main thread:

`Configure(Func<long> clock, Action<System.Numerics.Vector2, long> sink)`

Configuration validates live host/parent, same viewport and non-null delegates without invoking either delegate. `Dispatch(InputEvent input, CancellationToken cancellation = default)` returns true only for an enabled, relevant changed held-key transition whose sink and host commit succeed. Echo, unrelated input and unchanged key state return false without clock/sink/counter/diagnostic mutation. Null/unconfigured/closed/faulted direct calls reject before routing. Actual `_UnhandledKeyInput` calls Dispatch and marks the event handled only after true; GUI-consumed key events never reach it.

PhysicalKeycode wins whenever it is not `Key.None`; only absence permits fallback to Keycode. An unrelated nonzero physical code with logical W is ignored. Physical W/S/A/D map to four bits1/2/4/8. Native direction is `(D - A, S - W)`: W is nativeZ=-1, S+1, A nativeX=-1, D+1. Components are exact binary32 -1,0,+1; opposite keys cancel. Diagonal `(1,-1)` is intentionally unnormalized; the caller owns normalization and speed.

`CurrentInput` and `HeldKeyMask` expose last committed host state. `AcceptedCount` counts successful key or nonzero-stop sink transitions; `HandledCount` counts successful actual key-event consumption only. `ClockReadCount` counts reader invocations; `SinkCallCount` counts sink invocations, including later throws. `IsConfigured`, `IsEnabled`, `IsClosed`, `IsFaulted`, `LastDiagnostic` and `LastFailure` are owner/main-thread inspections. Faulted or closed hosts report IsEnabled=false.

## Callback and failure ordering

A relevant transition checks liveness/token and next accepted counter, reads the caller clock, checks caught host reentry/liveness/token, invokes the actual sink with proposed direction and exact returned clock, then checks reentry/liveness/token again. Only then does held state/AcceptedCount commit. The host copies no controller checks and does not impose a timestamp domain; the sink may reject an invalid caller clock.

One latest `TerrainManualInputDiagnostic` records requested direction, proposed bits, nullable returned clock, operation kind, SinkInvoked, SinkReturned, StateCommitted, nullable failure type and engine-boundary origin. It retains no exception object, message or stack. Ignored input and enable transitions needing no sink preserve the last submitted diagnostic. Configuration and pre-routing direct argument/state rejection do not invent accepted diagnostics.

Nested Dispatch, SetEnabled, Configure or Close sets a poison flag before throwing. A clock callback catching that exception still prevents the outer sink call. If the sink catches nested host mutation and returns, its external effect may be completed while the host rejects its own held-state/accepted commit. SinkInvoked=true/SinkReturned=true/StateCommitted=false records that prefix. A throwing sink may also have changed external state; SinkReturned=false is not evidence of rollback. Token cancellation or tree exit inside a callback follows the same prefix policy.

Selected routing failures propagate from direct Dispatch/SetEnabled, record one bounded failure and disable key processing. The actual Godot key/focus event boundary records failure and suppresses its escape into engine callback processing. Failed key events remain unhandled. Fault is terminal for further dispatch/configuration; Close remains available. There is no retry or rollback of delegates' external effects.

## Disable, focus and close

`SetEnabled(bool enabled, CancellationToken cancellation = default)` returns false for an unchanged enabled setting without clock/sink work. Disabling submits zero once if CurrentInput is nonzero, then clears held bits and disables key processing. Repeated disable sends no second zero. Opposing held keys with zero net direction are cleared without a zero sink call. Re-enable resumes an empty held set without reading the clock or replaying old keys. If a zero sink throws after an external stop, the previous held state remains committed internally while the host faults; external and host states can differ.

Actual window-focus-loss notification performs that disable/stop policy. Focus gain does not auto-enable. The UI caller explicitly disables before GUI focus/capture when an active direction must stop, then explicitly re-enables. The host cannot infer GUI capture from already-consumed events.

Close is terminal, owner-thread guarded and idempotent. It drops clock/sink/parent references, clears local held bits and disables processing. It makes no sink call. The caller must explicitly disable before Close if a stop is required; a previously published external nonzero intent can survive Close. Scalar/latest diagnostic records contain no borrower reference and remain inspectable. Actual `_ExitTree` closes immediately, poisoning a busy outer callback; caller owns final Node Free. No simulation or borrower disposal is implicit.

## Reproduction

The [offline runner](../../SWG-Godot-Client/poc/round68-tests/Rendering/Invoke-Picker.ps1) stages only this host and its [verification node](../../SWG-Godot-Client/poc/Stardust.Godot/TerrainManualInputHostVerification.cs) with cached Godot packages. There is no Runtime/Core/project dependency. The [independent Python model](../../SWG-Godot-Client/poc/round68-tests/Rendering/verify_picker.py) generates42 authored actions and20 expected sink calls before native execution and compares exact held masks, binary32 axis bits, clocks, counters, enabled state, GUI consumption and event propagation.

Actual headless/GPU Viewport.PushInput, focused Control.AcceptEvent, native propagated window-focus notifications, callback-prefix, liveness/thread/token and ownership results are recorded in the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R68_MANUAL_INPUT_HOST.md). Focus notifications are injected through the native Godot notification path; no live user keyboard or operating-system focus transition is captured. The GPU panel proves bounded replacement UI execution, not original visual or movement fidelity.
