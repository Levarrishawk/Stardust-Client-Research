# World object recipe visuals

October 2, 2026. Modern offline prototype host policy; no new executable or protocol claim.

SceneWorldObjectVisualFactory (external historical reference)
accepts the frozen presentation interface (external historical reference).
Its constructor takes `SceneWorldCoordinator world`, `Node parent`, and optional
`bool terminalFrame0Inspection=false`. `Create(publication,snapshot)` returns
`ISceneWorldObjectPresentation?`; successful instances are publicly named
`SceneWorldObjectVisual`, exposing `Publication`, `CreatureVisual`, and
`StaticVisual`. Exactly one recipe visual is present. IDs come from the supplied
publication and snapshot. The factory has no loader, registry, scheduler, consumer,
acknowledgment machinery, or same-round runtime dependency.

## Identity and ownership

Observation: the accepted coordinator (external historical reference)
owns generic publications and typed replication snapshots separately. Confidence:
Confirmed by baseline source and independent native fixture checks. Consequence:
resource attempts are not invalidated by scalar revision alone.

The factory requires the main thread and a live parent. Before GPU allocation it
checks exact world publication ownership/currentness, finalized object handle,
incarnation, current object metadata, placement, and byte-identical create
encoding. Envelope observations must match the current immutable world-owned
records. Creature selection requires explicit `CreatureView3Scalars`, current
typed state, matching generation/incarnation/object ID, and creature resources.
Static selection requires static resources and absent typed replication.
Mismatches return null without allocation; invalid parent/thread/descriptor policy
throws. Unsupported recipes fail closed.

Both accepted visual APIs check a borrowed lifetime predicate before construction,
after construction, and after parent attachment. The factory rechecks before
returning. During construction the predicate also checks the staged state;
after successful publication it releases that snapshot and checks resource
attempt/incarnation independently. A state change triggered by parent attachment
is tested to roll back and dispose the candidate. This is serialized main-thread
policy, not synchronization for concurrent world mutation.

`Tick(seconds)` retires stale resources/incarnations and delegates creature time
or static lifetime checks. `Apply(snapshot)` rejects stale observations. Creature
Height derives from immutable publication placement through the accepted
`SceneCreatureStatePresentation.DeriveTransform`; visibility follows typed
baseline visibility. State-only updates keep resource attempts and GPU IDs.
Invalid current Height disposes the creature visual and throws; it does not
invent a wire rejection or fail the world. Static application only validates a
current opaque observation. It assigns no meaning to TANO bytes.

`Dispose()` is idempotent and releases owned GPU roots/resources and the world
reference. Required public publication/visual getters remain available for
identity inspection; the publication CPU closure is borrowed, never mutated or
disposed by the adapter. All methods belong to the creating prototype host main thread.
The caller owns actor capacity, acknowledgment/retry, polling and final disposal.

## Static inspection policy

Default policy rejects switch-texture resources before allocation. Explicit
`terminalFrame0Inspection:true` uses the accepted
[static visual contract](SCENE_STATIC_VISUAL_CONTRACT.md): pinned terminal,
four retained LODs, two exact frame-0 screen surfaces, and complete materials.
This diagnostic override does not establish original screen timing, effect
execution, general switch-texture rendering, or original render fidelity.

## Independent proof and integration

Standalone preview (external historical reference) uses one
accepted world and the factory directly. Its loader exists only in the proof.
No shared Main or project file changes are required: the
launcher (external historical reference) creates an ignored private
scene referencing this script. Explicit arguments are installed prototype host path,
read-only archive root, pinned configuration, and generated peer path.
Modes are headless, cancellation, or fresh private PNG capture; capture produces
initial, updated and recreated images. The proof's fixed IDs are synthetic
fixture identities, not factory requirements.

The peer producer (external historical reference),
native runner (external historical reference), and
receipt verifier (external historical reference) preserve the accepted
independent Round14 fixture semantics. Synthetic TANO contents remain opaque.
The the separate implementation record
takes GPU log, headless log, cancellation log, and three PNGs.
Launcher negatives (external historical reference) test five
rejections before process creation. Detailed results and reproduction are in the
[checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R15_OBJECT_VISUAL.md).

Lead integration should instantiate the factory once and pass its `Create`
delegate to presentation bookkeeping through the frozen interface, then forward
current snapshots to `Apply`, elapsed time to `Tick`, and retirement to `Dispose`.
The worker neither assumes nor edits an unfinished consumer. Combined capacity,
acknowledgment retry and integrated Main wiring remain lead acceptance work.
