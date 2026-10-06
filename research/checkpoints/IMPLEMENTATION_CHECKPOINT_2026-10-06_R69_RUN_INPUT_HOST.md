# Round69 held Shift run-input host

Date: 2026-10-06. Status: implemented and verified; idle after seal.

## Bounded question

Can an actual Godot Node track independent left/right/unspecified Shift ownership and emit caller-clock run booleans only on aggregate changes, while honestly distinguishing local mask acceptance, consumed events and callback prefixes? Stop after independent pre-native timeline, actual viewport/GUI/focus executions, targeted mask/callback/lifetime negatives and unchanged frozen scope pass.

## Inputs and tools

Trusted sync verified4,226 accepted68 public files before edits. Incoming manifest SHA-256: `069f8419cc82801ee88484f8e20f1d519f8e30044c4427a9cd653c972841bf9d`; archive: `5f3333ab06f047348a3aed85ee1ef824591801ee19c26d6954ba45074cefddee`. Original executable identity matched `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`; it was not executed. Tools: PowerShell, ripgrep, cached Python/.NET8/offline Godot4.7.2 and Git read-only checks. Actual KeyLocation API was inspected in cached GodotSharp documentation and exercised natively. No Runtime69 or other sibling implementation, Core/project edit, original asset or proprietary corpus is consumed.

The [source](../../poc/Stardust.Godot/TerrainRunInputHost.cs) borrows generic `Func<long>` and `Action<bool,long>`. The [independent model](../../poc/round69-tests/Rendering/verify_picker.py) generates masks, booleans, clocks and callback/event counts before native execution. Native verification reads Actions only, not the saved expected rows, and produces no expectation seed.

## Observations and conclusions

Confirmed from source and actual execution: Configure accepts once under live exact same-viewport parent on constructing/Godot main thread and invokes neither callback. Actual InputEventKey through Viewport.PushInput exposes PhysicalKeycode and Location. Physical Shift wins; logical fallback occurs only for physical None. Left/right/unspecified map independently to bits1/2/4. Release removes exactly its corresponding ownership bit, preserving aggregate run when another is held. Unspecified release does not wildcard-clear left/right. This is modern policy, not recovered original behavior.

Confirmed: echo, unrelated physical code with logical Shift, duplicate press, unchanged release, disabled input and non-key motion do not read clock/invoke sink. Changed masks are accepted and consumed even if aggregate running remains true; those mask-only diagnostics have null timestamp and false sink-invocation/return flags. Cancellation and queued-parent rejection on an already-running mask-only transition preserve prior left ownership without additional clock/sink calls.

Confirmed: focused actual Control consumes Shift press/release before the unhandled host. A later actual `_UnhandledInput` observer proves accepted host/GUI events are consumed and ignored/failing events propagate with clear in-callback handled flag. Disable/focus loss sends false once when running, empties held state, ignores repeated disable and resumes empty on explicit re-enable. Native focus notification propagation is injected; actual OS/user focus transitions are unrun.

Confirmed: clock callback caught Dispatch/SetEnabled/Configure/Close reentry prevents sink invocation. Sink callback caught Dispatch/SetEnabled/Close can leave completed external running=true while host mask/accepted commit is rejected. Invoked throwing sinks and failed false-stop suffixes preserve possible external prefixes. A supplied negative clock is passed unchanged to the actual sink, which rejects it; no host timestamp domain is copied. Direct selected errors propagate; key/focus event boundaries retain one bounded failure and disable processing without consuming failed keys.

Confirmed: Close sends no implicit sink value, drops borrowed references and clears local state. Nonzero external run intent survives Close without prior disable; explicit disable stops it before closure. Actual queued-parent ExitTree and removal inside a clock callback close terminally and prevent further callback work. Wrong-thread Dispatch/SetEnabled/Close are executed negatives. Off-thread Configure and unsupported numeric KeyLocation rejection share source guards but are not separately executed. Caller Free drains owned Nodes/Controls; no borrower disposal or simulation mutation is implicit.

## Validation and reproduction

Use the [runner](../../poc/round69-tests/Rendering/Invoke-Picker.ps1) with explicit cached Python/.NET, documented executable identity, cached Godot packages and a fresh ignored private RunRoot. Exact portable command, staged source identities, private receipts and ten final owned public hashes are in the handoff. There is no Runtime69 import; standalone build includes only two own Godot sources.

Final actual build: zero warnings/errors. Headless146 checks and GPU147 checks pass. Both modes independently compare all46 authored action/control rows,23 committed mask transitions,19 handled keys and14 exact aggregate clock/sink calls. The nine successful mask-only key transitions explain the difference between mask and sink counts; four disable/focus stop transitions explain accepted versus handled counts. These denominators stay separate and are not research/implementation/protocol completion measures.

Expected fixture generation precedes native execution; the final audit verifies saved expected content/hash and timestamp ordering. Exact boolean/mask/counter/integer-clock comparisons include left/right/unspecified overlap, release preservation, fallback, GUI consumption, echo, focus and repeated enable/close. Targeted callbacks/token/liveness/ownership assertions pass independently of the main trace. The WASD suite was not repeated.

GPU diagnostic panel readback passes and its private image is visually inspected; it proves replacement UI execution, not original gait/rendering fidelity. Offline Godot emits its known root-certificate-store warning; processes exit0, request no network and show no object/resource-leak diagnostic. Eight runner guards reject wrong/empty executable, public/existing output, live/dangling junctions and non-directory ancestors before model/build. Prior receipt and linked target remain unchanged.

Final scope audit verifies all4,226 frozen public files unchanged, exactly ten own new public files, unchanged HEAD/index, ignored private output, UTF8/noBOM/LF/whitespace, Python/XML/PowerShell syntax, relative links, staged final source hashes and native receipts. Workspace validation, focused diff/check and status are retained privately. An initial private source-generation script used a stale search token and was corrected before producing public source; no failed native run is promoted as verification.

## Compatibility consequence and next action

Independent key ownership prevents one Shift release from cancelling another held key; aggregate run intent avoids redundant caller clock/sink operations. External callback effects and committed host ownership can diverge on failure, and terminal Close is not a walk-reset transaction. Original speed/gait/input policy remains unknown; research/function/protocol metrics are unchanged.

Next probe: join this host as the sole unhandled run-key writer with actual engine-neutral walk/run intent, alongside manual-direction and point-target hosts. Verify explicit GUI/focus stop, independent local key ownership, supplied run speed and authoritative correction without implicit simulation advance or multiple local writers. Retain mask-only counts and completed-sink-prefix diagnostics separately.
