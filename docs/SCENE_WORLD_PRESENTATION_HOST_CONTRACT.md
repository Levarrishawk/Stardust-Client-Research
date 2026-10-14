# Godot world presentation host

October 2, 2026. Modern process/ownership policy, not recovered legacy timing.

[SceneWorldPresentationHost](../../SWG-Godot-Client/poc/Stardust.Godot/SceneWorldPresentationHost.cs)
is a reusable `Node` against the frozen
[ISceneWorldPresentationPump](../../SWG-Godot-Client/poc/Stardust.Core/Session/ISceneWorldPresentationPump.cs).
It references no concrete Round16 pump and owns no network, ingress protocol,
resource loader, world registry, visual factory, camera or application quit.
The accepted [consumer contract](SCENE_WORLD_OBJECT_PRESENTATION_CONTRACT.md)
continues to define actor ownership and acknowledgment behavior.

## Integration API

```csharp
public void Configure(ISceneWorldPresentationPump pump, Action<double>? beforeStep = null);
public event Action<SceneWorldPresentationPumpStep>? FrameCompleted;
public Task Completion { get; }
public Task CloseAsync();
public Action<double>? BeforeStep { get; set; }
public Exception? Failure { get; }
public bool IsClosing { get; }
public ulong CompletedFrames { get; }
public SceneWorldPresentationHostFrame? LastFrame { get; }
public bool DeferNativeExitClose { get; set; }
```

`SceneWorldPresentationHostFrame` contains `Index`, `Seconds` and `Step`. Only the
latest receipt is retained. A receipt counts a successful Step before observers,
so an observer fault may leave a receipt while Completion fails. The public event
receives the pump step directly for lead wiring. `Active=false` is reported as-is;
it does not implicitly close a host that may be waiting for scene selection.

Add the node to a live parent first. Successful Configure transfers exclusive pump
ownership and enables processing; rejection leaves the passed pump with the
caller. Configure accepts once, validates creating/main-thread affinity, live
parent, and non-null drain task, and rejects configuration reentry. Null, failed,
second, detached or wrong-thread configuration transfers nothing. Rejected
configuration can be retried if the host remains open. Configure replaces the
BeforeStep property with its supplied argument, including null.

## Frame and callback policy

Each `_Process(delta)` accepts finite nonnegative seconds, invokes optional
BeforeStep callbacks for owner-thread offline ingress, calls `pump.Step(delta)`
once, records the numbered receipt, and invokes FrameCompleted observers.
Zero seconds is valid. The host does not invent clamping, catch-up, message or
resource budgets: the pump owns its bounded scheduling policy.

Callback execution is serialized. Mutation of callback subscriptions/BeforeStep
during configuration, a frame or after close is rejected. Reentering a frame is
rejected; a propagated callback error triggers failure close. A callback may call
CloseAsync; subsequent multicast callbacks and Step/observers are suppressed once
close begins. Callback exceptions stop later callbacks. A callback that closes
then throws still faults Completion, even when drain/disposal finish immediately.
Wrong-thread public mutations reject before touching the pump.

## Close and drain

Close disables process, clears callbacks, detaches owned pump reference, invokes
Close once, awaits its DrainCompletion, then invokes Dispose once on the owner
thread. CloseAsync always returns the same Completion task. Callback/Step/close/
drain/disposal errors are retained in Failure and fault Completion after cleanup
attempts; multiple errors are aggregated. No retries or additional frames occur
after close. A missing/throwing drain getter after ownership is a pump contract
failure; the host reports it and still attempts disposal.

`_ExitTree` synchronously requests close and starts the same drain handoff. Removing
the node does not imply drain has finished. The application must keep Godot alive
and await Completion/CloseAsync before quitting; the library never calls
`GetTree().Quit`. Process termination cannot force an awaited drain. The host does
not prematurely dispose a pump while its valid drain task is pending, nor impose
a library timeout that would claim outstanding work had ended. The standalone
launcher and proof have external deadlines.

October 6, 2026 extension: `DeferNativeExitClose` defaults to false, preserving
the synchronous exit behavior above. Set it on the owner thread before Configure
to opt into cleanup after native removal unwinds. Configuration captures the
current Godot synchronization context and rejects a missing context before
ownership transfer. The policy cannot change after admission. On `_ExitTree`,
the opted-in host immediately marks closing, disables processing and clears
callbacks/parent/viewport references, then posts actual owned-pump close/drain/
disposal to that context. World close and actor destruction occur in that posted
callback; they are not promised inside the native removal callback. Completion
still means actual cleanup, and the application must keep Godot running until it
finishes. Ordinary CloseAsync retains its immediate close path.

The [selected player adapter](REPLACEMENT_PLAYER_PRESENTATION_CONTRACT.md) opts
in because freeing its concrete creature while Godot is removing the parent
produced a native parent-busy failure. Its real-original removal proof checks
eventual root absence, empty consumer and drained jobs. Direct native Free is an
explicit faulted abort rather than successful managed visual disposal; callers
should close, await completion, then free. This extension does not change the
historical fake-pump proof or its counts below.

## Evidence and next integration

Observation: [standalone preview](../../SWG-Godot-Client/poc/Stardust.Godot/SceneWorldPresentationHostPreview.cs)
uses a fake frozen-interface pump and genuine baseline Core world/consumer without
selection, resource loading or visual creation. Confidence: Confirmed for modern
host mechanics by executed headless proof and independent
[receipt oracle](../../SWG-Godot-Client/poc/round16-tests/Rendering/verify.py). Consequence: this scope
is reviewable independently of the concrete pump under development.

The proof passes 102 assertions (including repeated completion/cleanup checks),
18 pumps with one close/disposal each, seven Step calls including one injected
Step fault, and three genuine Godot process frames. It covers success-only
ownership, invalid configuration/thread/delta, ingress/event/Step faults, callback
and frame reentry, close suppression, close-then-throw, delayed drain, exit-tree
handoff, aggregate failure and complete release. The
[launcher](../../SWG-Godot-Client/poc/round16-tests/Rendering/Launch.ps1) creates a private scene and
runs headless with a 45-second deadline. No shared Main/project edits are needed.

Lead next wires the concrete pump and original-resource ingress state machine
through Configure/FrameCompleted, awaits host completion before application quit,
and runs combined scheduling/visual acceptance. GPU/original assets, real pending
resource cancellation, concrete budget behavior, movement/interior wiring and
manual process termination are not separately tested by this host-only proof.
See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R16_PRESENTATION_HOST.md).
