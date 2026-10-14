# Selected player presentation adapter

October 6, 2026. Bounded modern replacement-client integration policy.
This adds no recovered executable behavior or live compatibility claim.

ReplacementPlayerPresentationHost (external historical reference)
connects one selected creature to the existing concrete
[presentation pump](SCENE_WORLD_PRESENTATION_PUMP_CONTRACT.md) and
[process host](SCENE_WORLD_PRESENTATION_HOST_CONTRACT.md).
Its default factory returns the ordinary `SceneWorldObjectVisual`. An optional
borrowed motion source wraps that fresh result with the existing
`SceneWorldObjectMotionVisual`; `SelectedVisual` exposes the current inner visual
for caller observation in either mode.

## Public boundary

> Historical prototype-specific command or code example omitted from the research-only documentation.


Add the adapter to a live parent and configure it on its creating prototype host main
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

Use `RequestClose()historical prototype identifier (omitted)Completion`, then free the
node and stop the application. Repeated close is harmless. Completion resolves
only after actual pump close, job drain and disposal, or faults after cleanup
attempts. The adapter clears its update callback and managed world/parent/viewport
references on completion. It retains the disposed concrete pump for borrowed
primitive/empty-consumer observations.

October 6, 2026 Round86 extension: the optional source preserves three-argument
behavior. The consumer owns the motion wrapper, which owns its accepted inner;
the caller owns the source/store. If wrapper construction fails, the adapter
disposes the fresh factory result before propagating failure. It never disposes
the borrowed source. Existing wrapper rules supply native-to-engine conversion,
Height application, immutable revision checks and missing/foreign/stale fallback;
this adapter introduces no motion math, interpolation, animation choice or cadence.

`SelectedVisual` is a creating/main-thread borrowed observation. It returns only
a current selected live creature visual, unwrapping `wrapper.Inner` when needed,
and returns null before publication, after generation replacement, during close
or after retirement. `PlayerPublished` uses this same boundary. Neither property
caches an actor/resource. Do not retain or mutate a retired borrowed visual. The
adapter clears its own callback/source references on close and completion;
the wrapper retains its borrowed source only until actual pump cleanup. Await
host completion before disposing the caller's motion store.

The child host opts into `DeferNativeExitClose` before configuration. Removing
the adapter from its parent stops admission and processing immediately, then
posts actual pump/actor cleanup to prototype host's captured synchronization context after
the native removal callback unwinds. Await completion before freeing the detached
node. World state need not already be Closed inside the removal callback.

Directly freeing an active adapter is an abort, not supported graceful cleanup:
Prototype host destroys the native actor before managed visual disposal. The bounded
negative proof observes faulted completion, an absent native root and drained
jobs, but the visual's successful-disposal counter remains incomplete. It does
not claim successful visual retirement for that path. No engine quit, timeout or
process kill can substitute for awaited drain.

## Reproducible component proof

replay.py (external historical reference) stages source, build products
and package copies into a fresh ignored private output. It uses explicit local
historical implementation path (omitted) runtimes and six cached package families with package sources cleared.
Each child has a 120-second external process deadline. No original executable,
server contact, DNS, sockets, captures or sibling implementation is required.

> Historical prototype-specific command or code example omitted from the research-only documentation.


The self-contained headless proof exercises 39 assertions with the real adapter,
host, coordinator and pump: rejection/ownership, callback ordering, failure,
reentry, generation mismatch, callback close, delayed job/native removal and fresh
owner. Its independent peer (external historical reference) authors the
applications before native startup. These synthetic StartScene/create/typed
CREO3/end/server-ready fixtures exercise the frozen codec, not transport/login
compatibility or a captured complete legacy baseline.

Optional explicit `--asset-root ABS --config ABS` prepares the installed shared
human-male creature through the frozen resource pipeline and runs 59 headless
assertions. `--gpu` adds a screenshot assertion (60 total); the independent
verifier (external historical reference) checks original root hash
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

Round86 headless runner (external historical reference) uses the same
explicit local runtime/cache/private-output arguments and requires
`--asset-root ABS --config ABS`. GPU is reserved for lead acceptance. Its 104
checks retain the 39 structural and 20 original-resource checks, then add actual
`SceneWorldObjectMotionStore` local/authoritative publication, duplicate/older
counter rejection, copied immutable rows, foreign incarnation and stale revision
fallback, disposed-source fallback, borrowed thread guards, and four concrete
cleanup cases: native removal, wrapper constructor failure, callback failure and
generation replacement. The original source is decoded once for the selected
object and reused as immutable CPU descriptors across fresh visuals; the two
overlay-job counts include the separately prepared unrelated object, not archives.
The independent verifier compares authored native positions against actual engine
positions `(4,5,6)`, `(7,8,9)` and fallback `(1.5,2,3)` before accepting the receipt.
Disposed-store fallback is a deliberate negative probe, not recommended shutdown
ordering. Source/visual lifecycle claims are confirmed for these executed modern
fixtures; actual session movement submission and lead GPU join remain separate.
