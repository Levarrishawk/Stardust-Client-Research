# Offline playground inspection contract

Round82 extends the [persistent shell](PERSISTENT_OFFLINE_PLAYGROUND_CONTRACT.md) with an owned status panel and Close control. This is replacement-client UI behavior, not recovered Stardust protocol or executable semantics.

## Primitive snapshot

`PersistentOfflinePlayground.InspectionStatus` returns an immutable `OfflinePlaygroundInspectionStatushistorical prototype identifier (omitted)InspectionCaption` returns its visible text while the owned panel exists. The record contains strings, integers, booleans and nullable values only; no actor, camera, session, node, resource or completion callback references escape through it.

- Shell state: `State`, `StartedRuns`, `MaximumRuns`, `RemainingRuns`, `ChildPresent`, `CurrentReady`, `Terminal`, `Closing`, `RestartEnabled`, `CloseEnabled`.
- Last completion report: `Reason`, `CompletedUpdates`, `AttemptedUpdates`, `Faulted`, `CleanupSucceeded`, `Actors`, `OutstandingArchiveJobs`, `ArchiveCalls`, `ArchiveClosed`, `RootFrames`, `FailureType`, `CleanupFailureTypes`.
- Close lifecycle: `CloseRequests`, `WindowCloseConnected`, `QuitIssued`.

The report fields are nullable before the first completion. While a successor runs, they describe the previous completed run, explicitly labeled **Last run** and **Last updates**. They are not live counters. `RemainingRuns` counts admissions still available, not ticks. State priority is Closing, Terminal, current child Active/Loading, exhausted RunLimit, otherwise Idle. Restart requires an idle, nonterminal shell with remaining admissions and an enabled outcome HUD.

The panel shows completed and attempted updates separately, fault and cleanup status, actors/jobs, archive close/call totals and root frames. It occupies `(640,328)` at `624x196` in the existing 1280x720 layout, CanvasLayer24, font14. Its controls ignore pointer input and never take keyboard focus. The Close button at `(272,484)` is `240x40`, consumes its own click, and takes no keyboard focus.

## Controls and ownership

`TerrainOfflineRestartHud.DisableRestart()` is a one-way availability latch. It changes neither request/accept/retirement histories nor the frozen outcome. The shell disables restart for cap exhaustion, terminal failure and closing before reporting completion through `RunCompleted`. An exhausted idle shell visibly says **Run limit reached. Restart is disabled.** Successful replacement still retains the previous outcome until the successor reaches readiness; a factory rejection retains the outcome and permits retry.

The shell subscribes exactly once to its actual owning `Window.CloseRequested`. That signal, the existing WM-close notification, the Close button and `RequestClose()` use the same idempotent flag-only request. The first accepted request increments `CloseRequests`; repeated routes cannot start another drain or quit. `_Process` performs the existing child shutdown/drain and quit, guarded by `QuitIssued`. Controlled teardown disconnects both callbacks and frees the owned inspection layer there. Native `_ExitTree` disconnects and forgets references without calling `Free`, and clears child/candidate/HUD references. Abrupt native removal remains abort rather than a successful drain.

## Evidence and confidence

**Observation:** the owner-thread snapshot reads only the current child readiness plus the shell's cached primitive completion report. Native standalone fixtures exercised actual GUI restart/close clicks, cap and terminal disabled buttons, passive panel input, repeated close routes, exact Window signal and late signal after native teardown. An independently authored, strict-type oracle was generated before each native run. Full private prototype host/Core builds passed with warnings treated as errors.

**Interpretation and confidence:** Confirmed for the synthetic standalone prototype host control/lifecycle fixtures; source-reviewed for the unchanged asset-backed child bridge. The fixtures explicitly inject authored completion/readiness and do not prove original-asset counters, physical OS close delivery, gameplay, protocol, or original executable behavior. Signal emission and WM notification calls are synthetic routing tests, not physical OS events.

**Compatibility consequence:** lead integration should consume this primitive API, retain actual completed/attempted distinctions, and verify it against full child reports. No retired preview reference should be retained for inspection. See the [Round82 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R82_PLAYGROUND_INSPECTION.md) for exact scope and reproduction.
