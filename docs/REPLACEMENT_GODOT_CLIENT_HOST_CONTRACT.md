# Explicit Godot client entry

October 6, 2026 Eastern. Bounded modern adapter; no new recovered executable claim.

[ReplacementClientHost](../poc/Stardust.Godot/ReplacementClientHost.cs) and its
[scene](../poc/Stardust.Godot/ReplacementClientHost.tscn) provide actual login,
character selection, controlled movement and close controls over the accepted
[application owner](REPLACEMENT_CLIENT_APPLICATION_CONTRACT.md). The scene starts
unconfigured with disabled controls. It performs no automatic login, DNS, socket
construction, resource discovery, credential storage or application quit.

## Configuration and ownership

```csharp
public sealed record ReplacementClientHostOptions(uint LoginConnectionId,
    uint ZoneConnectionId, bool SyntheticOffline,
    ReplacementControlledMovementScenario? MovementScenario = null,
    long DisconnectTimeoutMilliseconds = 3000);
public void Configure(ReplacementClientApplication application,
    ReplacementClientHostOptions configuration, Func<long> nowMilliseconds);
```

The composition owner first constructs the application with an explicit launch
profile, LoginClientOptions, pinned injected login/zone port factory, minimum
provider and root policy. Application `StartupFailure.RollbackCompletion` belongs
to that caller; a failed constructor never supplies an application to this host.
No credentials belong in the launch profile or host options.

Add the host to a live tree and wait for its controls to be ready before Configure.
Admission requires its creating Godot main thread, synchronization context,
distinct connection IDs, disconnect timeout1..60000ms, fresh Offline session,
Idle world and one prepared human catalog entry. Preflight reads owner metadata
but invokes neither clock nor application operation. Rejected admission retains
the caller's application, including all retirement obligations. After admission,
`IsConfigured` is true and the host exclusively owns application retirement and
owner-thread finalization. A failure after transfer starts retirement; it does
not return that ownership. Second or reentrant configuration rejects. Native
controls remain Godot main-thread objects, not thread-neutral data.

`Application` is a borrowed owner until finalization and throws afterwards.
`Presentation` returns a transient borrowed adapter only while active. Callers
must not independently tick, dispose or close those owners after transfer.
Completion, configuration/finalization flags, counts, bounded failure names and
cleanup observations remain readable after finalization on the owner thread.
`PresentationCompletion` exposes the exact actual task bound to application
close. The harness retains pre-admission Session/Run references solely for their
historical closed-state/count observations; this grants no post-close mutation.

## Actual controls and readiness

Start-login queues the explicit login ID. Authenticate reads the two LineEdit
fields once and calls the accepted application operation; password is cleared
in a finally block. Selection and close also clear both fields. Credential
values are never printed, serialized, added to a receipt or retained by this
host outside the controls and synchronous authentication call. Immutable managed
strings and Godot's internal field storage are not claimed securely erasable.

The OptionButton is populated from the actual session Characters, with bounded
64-character display names and control characters removed. Selection accepts
only nonzero identity and a race/template CRC in the prepared human catalog.
Unsupported rows stay visible and their select action reports a bounded reason
without acquiring a zone port or changing the selection generation. This is a
modern one-human catalog admission, not general avatar support.

The accepted selected-player presentation adapter supplies the original visual
and existing motion wrapper. `World.State == AwaitingServerReady` means local
ready was submitted; `World.State == Ready` means server-ready was accepted.
The status text distinguishes them. Publication can occur while awaiting the
server, but Move and W require actual Ready, one local ready submission and a
currently published original selected human. Neither publication alone nor
the local ready counter arms movement.

Movement delegates entirely to Runtime90's reviewed controlled pacer. The
explicit scenario supplies selected identity and fresh-stamp-zero, matching
validated-position and movement-prerequisite assertions. These are caller
assertions; this host does not infer them from a synthetic ready packet or PTAT
decode. No scenario means no movement. The pacer uses existing local-driver and
wire primitives, at most three roots,400ms apart, speed0.5, counters1/2/3 and
stamps400/800/1200. Application Tick precedes pacer Tick in the actual presentation
update. Stop retires the same pacer permanently; W cannot restart the cap.
W is ignored while a credential field owns GUI focus. Escape requests close.
No terrain/floor simulation or new pacing formula is implemented here.

## Close and real completion

Close-button and window-close requests clear controls, stop movement and attempt
protected protocol disconnect when the application is open, Selected and bound
to a current generation. The owner keeps ticking the actual send/timeout path;
it does not block the Godot thread or claim server release from local completion.
Pre-selection close retires directly. A failed application cannot submit another
disconnect and instead retires its actual acquired prefix.

Retirement requests presentation close and binds its actual Completion to
application BeginClose. A main-context asynchronous loop polls TryFinalizeClose;
it releases no installation/motion owner until all actual borrowers and that
presentation task complete. Genuine task faults remain cleanup observations.
`IsFinalized` means finalization was attempted; Completion faults on operational
or cleanup failure. Provider failure therefore safely finalizes while retaining
the failed provider task, not a successful-drain claim.

The host disables SceneTree AutoAcceptQuit while it owns the application and
restores its prior value after finalization if still false. It never calls Quit.
The composition owner must observe Completion, free the host afterwards and
decide whether to exit the window/application. A close request is not itself a
promise that this reusable node terminates the process.

Native removal posts retirement to the captured Godot context after removal
unwinds. Supported proof removes the host while retaining its native nodes and
keeps Godot's loop running through completion, then frees it. Immediate forced
QueueFree of a live hierarchy produced actual presentation cleanup faults in an
earlier probe; those faults were observed and ownership finalization attempted.
That is not a successful clean-removal proof. External process termination or
uncoordinated native destruction cannot promise drained ownership. Await normal
close Completion before freeing a live hierarchy.

## Reproducible evidence and remaining limits

The [explicit native harness](../poc/round90-tests/Rendering/README.md) authors
accepted login/zone/world peers independently before native startup. Actual
buttons/fields and Godot injected key events traverse this production host.
Its two-character list proves unsupported selection retention; a withheld
server-ready proves no pre-ready movement. Exact protected auth/select/ready,
controlled roots and legal disconnect are independently compared, as are original
preparation, closure-source and terrain hashes and float32 actor positions.
The selected actor's native identity remains the same throughout movement.

Eight original-resource headless cases exercise success, pre-ready cancellation,
minimum-provider failure, disconnect send error/timeout, movement stop,
window-close and retained-node native removal. Detached/wrong-thread admission,
password clearing and rejected post-finalization borrowing are checked inside
the native harness. Confidence is Confirmed for these exercised modern inputs
and lifetimes. Optional GPU execution and its screenshot require an explicit
launcher flag and are separately reported.

Synthetic controls are not physical OS input evidence. Synthetic peers are not
live interoperability or current-server complete-baseline evidence. No original
executable is run. Full procedural terrain, general avatars/gameplay, validated
floor/state provenance, server-observed release and live authority remain open.
Round92 adds optional `ControlledAttempt` and `ApprovedCharacterId` host
options while preserving earlier callers. Actual StartLogin starts the guard;
observations before and after ticks use real readiness/publication state.
Deadline stops request normal legal disconnect and await actual cleanup.
The approved ID is checked before template acceptance or application selection,
so a second supported human cannot acquire the zone port or mutate generation.
The [separate bootstrap](REPLACEMENT_CLIENT_BOOTSTRAP_CONTRACT.md) supplies these
options from strict explicit launch inputs. Default direct scene startup refuses
contact. Round93's explicit standalone opt-in supplies the same host and observes
actual Completion before redacted diagnostics and quit; injected92 callers retain
their quit decision. The real transport constructor branch is source-reviewed
only; native verification substitutes labeled offline spy ports.

Next: independently replay the standalone admission/receipt/cleanup proof, then
obtain operator scenario inputs and specific live authorization. Current-server
baseline, full terrain and validated-state gates remain open.
