# Persistent offline restart HUD contract

Date: 2026-10-06. Scope: authored replacement preview UI, not recovered Stardust UI or protocol.

## API and ownership

TerrainOfflineRestartHud (external historical reference) is a managed `IDisposable` facade. `Configure(Node owner, TerrainOfflineOutcomeHudState outcome, Func<bool> restart, CancellationToken token=default)` admits one immutable outcome and one caller-owned synchronous restart callback once. `RequestRestart()` returns the callback proposal only after post-callback validation. `Caption`, `RequestCount`, `AcceptedRestartCount`, `RetirementCount`, `IsRetired`, and `LastDiagnostic` retain historical values; access requires the constructing managed thread and prototype host main thread.

The facade composes the frozen [outcome HUD](TERRAIN_OFFLINE_OUTCOME_HUD_CONTRACT.md) and owns one additional CanvasLayer23 and Button. It borrows exact owner, parent, viewport, callback and lifetime token. It does not own a run, session, scheduler, world, clock, transport, or borrowed owner. A successful restart means the callback returned true and guards passed; real run readiness remains the caller's responsibility. The caller explicitly disposes this display after its replacement run is accepted/ready.

## Persistent state and callback boundary

A false callback leaves both displays available for another retry. True disables the button and admits one accepted restart; direct, duplicate and late signal requests then return false without invoking the callback or increasing requests. Requests count actual callback admissions, including a callback that throws. The immutable admitted caption remains historical after retirement and disposal.

Before and after callback execution, guards require exact live, unqueued owner/parent/viewport and owned layer/button, same viewport identities, unchanged button position/size/text, MouseFilter Stop and FocusMode None, uncanceled lifetime, and no caught reentry. Calling frozen outcome `Refresh()` against the captured immutable state validates its actual label/panel subtree. Raw label/panel deletion therefore rejects before callback admission or rejects after one callback with an honest partial prefix. This internal refresh invokes only the captured constant-state closure, never a second external outcome/report callback.

Exceptions retire/disconnect the restart callback once and disable a surviving button. A still-valid passive outcome remains visible until explicit disposal. An invalid passive subtree cannot be promised visible; its cached caption persists. `LastDiagnostic` distinguishes invocation, return, proposed bool, commit and exception type. External callback effects are not rolled back when subsequent cancellation, deletion or reentry prevents acceptance. Engine signal exceptions are contained after recording that failure. Direct calls propagate the exception.

Native layer exit/protected disposal clears its callback before notifying managed retirement. It never calls native Free. Explicit facade Dispose, including interface disposal, frees only surviving owned layers/outcome and is repeatable. A detached live owned layer still needs explicit disposal. Owner destruction, queued deletion and callback-side owner destruction are validated without double native deletion.

## Layout and input

The passive outcome is CanvasLayer22, panel at16,328 size608x148 with five lines, as defined by its frozen contract. Restart is CanvasLayer23; Button at16,484 size240x40, font16, text `Restart offline preview`, default release action, FocusMode None and MouseFilter Stop. Only the button rectangle captures mouse input; the passive panel ignores it. The fixed1280x720 GPU capture shows an8px gap, all caption lines and full button text without overlap or clipping. Smaller viewports, localization and accessibility are unverified.

## Reproduction and evidence

Round80 runner (external historical reference), independent oracle (external historical reference), native verification (external historical reference) and [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R80_RESTART_HUD.md) reproduce the bounded evidence. Exact frozen manifest/archive and original executable SHA admission precede producer/build; the executable is hashed only. Cached replacement prototype host4.7.2/.NET8 packages build offline in fresh private output. No original assets or executable are run.

The synthetic GUI scene uses a standalone1280x720 SubViewport, always-updated GPU target, and a passive TextureRect displaying its texture. Before injected input, call that SubViewport's NotifyMouseEntered once. Send InputEventMouseMotion with Position/GlobalPosition at the target and nonzero Relative; send left down with Left mask then left up at the same position, through PushInput(event,true), then await ProcessFrame. This triggers the actual Button.Pressed signal; no emitted-signal shortcut admits the six positive GUI rows. A separate late EmitSignal checks suppression only.

Headless94 and GPU95 checks compare independently produced expectations before native execution: two successive synthetic display owners, six real GUI rows, two caption histories,22 failure prefixes and five native teardown cases. Each owner receives false, true, then disabled duplicate gestures; callbacks/requests2, accepted1, retirement1 after caller disposal, zero owned children. Outside700,500 press/release reaches an unhandled-input probe; inside30,500 is captured. Label/panel deletion preflight calls0, deletion during callback calls1, and mouse/focus/layout tampering calls0. Native traces and screenshot establish this authored implementation with Confirmed confidence, not compatibility research coverage or real restart-report provenance.

The failed native-window gesture replays were retained: events reached the button but hover remained false and no callback ran. Motion/frame/NotifyMouseEntered alone did not repair that fixture. the separate implementation record is correlation evidence: its native-window hover path uses display-server window state, whereas standalone SubViewport uses injected coordinates. The passing standalone fixture avoids OS mouse warp; a physical OS-window pointer click remains unexecuted. No production button action or assertion was relaxed. Offline certificate-store diagnostics remain in logs despite successful exit0; no network was requested.

Next probe: integrate the callback with the caller's actual two-run lifecycle, prove accepted replacement readiness before disposing the old display, and compare real outcome report provenance independently. This worker's two owners are synthetic display owners, not two executed session owners.
