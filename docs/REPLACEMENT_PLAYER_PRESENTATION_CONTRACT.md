# Selected player presentation adapter

October 6, 2026. Bounded modern replacement-client integration policy.
This adds no recovered executable behavior or live compatibility claim.

[ReplacementPlayerPresentationHost](../poc/Stardust.Godot/ReplacementPlayerPresentationHost.cs)
connects one selected creature to the existing concrete
[presentation pump](SCENE_WORLD_PRESENTATION_PUMP_CONTRACT.md) and
[process host](SCENE_WORLD_PRESENTATION_HOST_CONTRACT.md).
Its factory returns the ordinary `SceneWorldObjectVisual`; callers can borrow
that concrete visual for existing movement wiring without another wrapper.

## Public boundary

```csharp
public partial class ReplacementPlayerPresentationHost : Godot.Node
public void Configure(SceneWorldCoordinator world, ulong selectedObjectId,
    Action<double> updateSession);
public SceneWorldPresentationPump Pump { get; }
public Task Completion { get; }
public bool PlayerPublished { get; }
public ulong CompletedFrames { get; }
public Exception? Failure { get; }
public void RequestClose();
```

Add the adapter to a live parent and configure it on its creating Godot main
thread, after binding the selected identity and before feeding scene messages.
Capture the same current world generation. Configuration accepts once and requires
a nonzero selected object ID, an open world, a non-null update callback, and no
existing scene/snapshot for another object. Pre-admission rejection leaves the
world caller-owned. A failure after concrete pump construction closes and drains
that acquired prefix and faults completion; callers must await it before quitting.

Each successful host frame calls the session update once before the concrete
pump step. Exact parent, viewport, world generation and selected scene identity
are checked at frame/publication boundaries. A callback failure, caught
configuration reentry, generation replacement, closed world, or unrelated
publication fails this adapter. Closing inside the callback suppresses that
frame's pump step. A fresh selection requires a fresh adapter/world owner.

The concrete factory admits only the selected object with a `CreatureDetail0`
recipe, with a one-actor budget. It rejects unrelated publications before visual
allocation. It neither invents an actor nor fabricates a baseline. This bounded
adapter cannot host a general populated zone; an independent world host remains
necessary for other actors.

## Ownership and shutdown

After admission the adapter owns the pump and world close through its child
`SceneWorldPresentationHost`. The caller owns session, transport ports, camera,
installation preparation and external jobs. `Pump` is borrowed for owner-thread
observation and existing visual lookup, not permission to independently step,
close or dispose it. The adapter retains no actor/resource reference for
`PlayerPublished`: it observes the consumer transiently and reports false as soon
as close or native exit starts. Completed frames and failure remain observable
after retirement. `Completion` is one stable task.

Use `RequestClose()`, keep Godot's loop running, await `Completion`, then free the
node and stop the application. Repeated close is harmless. Completion resolves
only after actual pump close, job drain and disposal, or faults after cleanup
attempts. The adapter clears its update callback and managed world/parent/viewport
references on completion. It retains the disposed concrete pump for borrowed
primitive/empty-consumer observations.

The child host opts into `DeferNativeExitClose` before configuration. Removing
the adapter from its parent stops admission and processing immediately, then
posts actual pump/actor cleanup to Godot's captured synchronization context after
the native removal callback unwinds. Await completion before freeing the detached
node. World state need not already be Closed inside the removal callback.

Directly freeing an active adapter is an abort, not supported graceful cleanup:
Godot destroys the native actor before managed visual disposal. The bounded
negative proof observes faulted completion, an absent native root and drained
jobs, but the visual's successful-disposal counter remains incomplete. It does
not claim successful visual retirement for that path. No engine quit, timeout or
process kill can substitute for awaited drain.

## Reproducible component proof

[replay.py](../poc/round84-tests/Rendering/replay.py) stages source, build products
and package copies into a fresh ignored private output. It uses explicit local
dotnet/Godot runtimes and six cached package families with package sources cleared.
Each child has a 120-second external process deadline. No original executable,
server contact, DNS, sockets, captures or sibling implementation is required.

```text
python poc/round84-tests/Rendering/replay.py --dotnet ABS --godot ABS --packages ABS --output FRESH_PRIVATE_ABS
```

The self-contained headless proof exercises 39 assertions with the real adapter,
host, coordinator and pump: rejection/ownership, callback ordering, failure,
reentry, generation mismatch, callback close, delayed job/native removal and fresh
owner. Its independent [peer](../poc/round84-tests/Rendering/peer.py) authors the
applications before native startup. These synthetic StartScene/create/typed
CREO3/end/server-ready fixtures exercise the frozen codec, not transport/login
compatibility or a captured complete legacy baseline.

Optional explicit `--asset-root ABS --config ABS` prepares the installed shared
human-male creature through the frozen resource pipeline and runs 59 headless
assertions. `--gpu` adds a screenshot assertion (60 total); the independent
[verifier](../poc/round84-tests/Rendering/verify.py) checks original root hash
before native startup, object 42, engine position `(1.5, 2, 3)`, publication,
textures, visible pixels, root removal and job drain. `--native-exit` selects
positive native removal; `--native-free` selects the explicitly faulted abort
case. These two lifecycle switches are alternatives. Receipts distinguish an
expected abort from successful visual disposal.

Original preparation uses the configured overlay, inherited `hum_m.sat`, and
explicit frozen animation choices from `appearance/lat/all_m.lat`: idle
`emt_standing_idle_ambient_1_ag`, walk/run `loop_standing` routes `[0,1]`/`[0,2]`.
The two source-job opens/closes count overlay loaders, not individual archives.
The tested installation mounts 48 archives. The creature is a template preview;
customized player appearance, equipment, gameplay camera, session and port
integration are outside this component proof.

Observation: the concrete actor publishes at the authored converted position,
and controlled close/native removal retire its root and drain source jobs.
Confidence: Confirmed for these executed modern component fixtures.
Consequence: the lead can configure this adapter after selection binding and
join it to independently verified session/ports/original preparation, then run
combined acceptance. Next action: execute that joined proof; this component
alone establishes no live-server or total replacement-client coverage.
