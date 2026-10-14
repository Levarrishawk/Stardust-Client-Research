# Round68 manual unhandled-key host

Date: 2026-10-06. Status: implemented and verified; idle after final seal.

## Bounded question

Can an actual prototype host Node translate unhandled physical WASD press/release into caller-clock native direction intent, preserve GUI/focus/enable policy and report callback prefixes without implicit simulation work or borrowed dependency disposal? Stop after independent pre-native event/axis expectations, actual viewport/GUI/notification executions, targeted negatives/cleanup and unchanged frozen scope pass.

## Inputs and tools

Trusted sync verified4,191 accepted67 public files before edits. Incoming manifest SHA-256: `167817431f266e5f841c6d77ed288f8dc4aab79a234bde82d3f0db5ec9709fe4`; archive: `6fe55e2d17e2f280ad9b5a3370a4e6d1756f785a763673ac192f50dab153bc0c`. Original executable identity was checked against `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`; it was not executed. Tools: PowerShell, ripgrep, cached Python/.NET8/offline prototype host4.7.2 and Git read-only checks. No Runtime68, Networking68, existing host amendment, Core/project edit, original asset or private proprietary corpus is consumed.

The host (external historical reference) borrows exactly `Func<long>` and `Action<System.Numerics.Vector2,long>`. The model (external historical reference) independently authors actions, held masks, component axes, expected clocks and counters before native execution. Native code reads only Actions, not the saved expected rows. There is no native-output expectation seed.

## Findings and evidence

Confirmed by source and actual execution: Configure is accepted once under live exact same-viewport parent and historical implementation path (omitted) main thread, without invoking clock or sink. Actual `_UnhandledKeyInput` receives keys through Viewport.PushInput. PhysicalKeycode has priority; Keycode fallback occurs only for PhysicalKeycode.None. W=-nativeZ, S=+nativeZ, A=-nativeX, D=+nativeX; components stay in[-1,1]. Diagonal speed/normalization belongs to caller. Echo, unrelated physical/logical cases, duplicate press, unchanged release, disabled keys and non-key motion do not invoke clock/sink.

Confirmed: a focused actual Control accepts GUI key press/release, which never reaches the unhandled host. A later actual `_UnhandledInput` observer receives ignored/failed keys with clear handled flag and is suppressed for accepted host or GUI events. Actual caller-driven disable/re-enable uses zero once for nonzero direction, ignores repeated disable, clears opposing held keys without redundant zero, and resumes empty. Native window-focus-loss notification propagation stops held input; focus gain remains disabled until explicit enable. This is injected prototype host notification behavior, not a captured operating-system/user focus transition.

Confirmed: sink is called with the exact returned caller clock, which the host neither advances nor validates as a controller timestamp. A supplied negative clock is rejected by the actual test sink and recorded as a sink invocation with no host held commit. Throwing clock/sink and pre-/callback cancellation follow their observed invocation/return prefixes.

Confirmed: clock callbacks catching nested Dispatch, SetEnabled, Configure or Close poison the outer operation before sink invocation. Sink callbacks catching nested Dispatch/SetEnabled/Close can complete an external intent effect and return before the host observes poison; SinkInvoked=true, SinkReturned=true and StateCommitted=false preserve that exact phase. A failed zero stop can already set external zero while host retains its previously committed held direction and faults. No callback-effect rollback is claimed.

Confirmed: direct routing failures propagate; actual key/focus callback boundaries record bounded LastFailure, disable processing and leave failed keys unconsumed. Checked counters distinguish accepted host state, handled key events, clock invocations and sink invocations. One latest primitive diagnostic retains no exception object/message/stack and no borrower reference.

Confirmed: Close clears local held/configuration references and processing but invokes no sink. Closing after an explicitly successful disable leaves external zero; closing a nonzero host without disable leaves the external direction intact. Actual queued-parent ExitTree and removal inside a clock callback close terminally; no subsequent sink runs. Wrong-thread Dispatch/SetEnabled/Close reject before effects. Off-thread Configure shares that owner gate by source inspection; it was not independently executed. Caller Free drains owned Nodes/Controls; caller delegates remain externally usable.

## Validation and reproduction

Use the runner (external historical reference) with explicit cached Python/.NET, documented executable identity, cached prototype host packages and a fresh ignored private RunRoot. Exact portable command and all ten final owned public hashes are in the final handoff. Build stages only the new host/verification; no sibling implementation source is linked.

Actual build: zero warnings/errors. Headless114 checks and GPU115 checks passed. Both modes compare all42 independent authored event/control actions and20 exact native sink calls, including exact binary32 axis bits and integer clocks, held masks, enabled state, accepted/handled/read/invocation counts and GUI capture count. Model generation precedes native execution; final audit records the saved expected artifact hash and verifies its content independently.

GPU diagnostic panel readback passed and the private capture was visually inspected. This proves bounded replacement UI execution, not original movement/rendering fidelity. Offline prototype host reports its existing root-certificate-store warning; processes exit0, request no network and show no object/resource-leak diagnostic. No repeated full picker, terrain or parser corpus was executed because this milestone has no geometry dependency.

Eight runner guards reject wrong/empty executable, public/existing output, live/dangling junction paths and non-directory ancestors before model/build execution. Existing receipt and linked target remain unchanged. Final audit verifies all4,191 frozen public files unchanged, exactly ten own new files, unchanged HEAD/index, ignored private outputs, UTF8/noBOM/LF/whitespace, Python/XML/PowerShell syntax, relative links, staged source hashes and receipts. Organized workspace validation, focused diff/check and final status are retained privately. No failed native build/test was promoted; the first full native replay passed.

## Compatibility impact and next action

The generic direction/clock boundary allows a separate engine-neutral controller to apply its own speed, normalization and intent-switch rules. It does not establish original movement units/cadence, gameplay, GUI policy or input parity and changes no research/function/protocol coverage denominator. External sink effects and committed host state can diverge on callback failure; stopping before terminal Close is an explicit caller responsibility.

Next probe: integrate this host as the sole unhandled-key route, supply the actual engine-neutral current clock and intent sink, disable on GUI capture/focus, and verify manual/point intent switching and authoritative corrections without multiple local writers. Keep clock-phase prevention and completed-sink-prefix diagnostics separate.
