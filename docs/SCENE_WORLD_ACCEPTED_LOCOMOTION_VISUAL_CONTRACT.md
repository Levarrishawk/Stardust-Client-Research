# Supplied accepted locomotion presentation

October6,2026. Explicit modern presentation policy, confirmed by bounded replacement execution.

SceneWorldAcceptedLocomotionVisual (external historical reference)
owns a caller-associated presentation chain containing the supplied frozen
[motion visual](SCENE_WORLD_OBJECT_MOTION_VISUAL_CONTRACT.md). It borrows the world,
a `Func<LocomotionPreviewState>` and an optional `Func<bool>` enable callback.
It implements ISceneWorldObjectPresentation (external historical reference).

```csharp
new SceneWorldAcceptedLocomotionVisual(world, inner, source,
    ownedPresentation: null, enabled: null);
bool Tick(double seconds);
bool Tick(double seconds, CancellationToken token);
bool Apply(SceneWorldObjectSnapshot snapshot);
bool Apply(SceneWorldObjectSnapshot snapshot, CancellationToken token);
void Dispose();
```

The default chain is exactly `inner`; the default enable value is true. The constructor
requires the current live creature publication and prepared idle/walk/run resources.
A supplied chain must expose the exact same publication reference. The constructor
rechecks current publication and live root after that interface getter, before adopting
ownership. Failed construction leaves disposal with the caller and never reads either
state/enable callback. The public diagnostics are `Inner`, `Publication`, `IsRetired`,
`SelectedClip`, `Frame`, `LastState`, `SourceReads`, `EnableReads` and `Presentations`.
`LastState` retains the exact successful immutable source record until retirement.

## Caller association and ownership

The caller must guarantee that the chain contains the supplied motion visual exactly
once, and that the source belongs to the same actor, incarnation and publication with
matching prepared clip metadata. Publication reference equality catches an obvious
unrelated chain; it cannot inspect arbitrary private nesting. The public
LocomotionPreviewState (external historical reference) has no
actor, generation, incarnation, publication or timestamp provenance. Consequently this
wrapper cannot prove association, detect a stale but structurally valid source record,
or infer it from fabricated fields. No monotonic-state constraint is imposed on caller
resets. These responsibilities are an explicit integration contract.

Ownership transfers only on successful construction. Retirement calls the owned
chain's `Dispose` exactly once, without additionally disposing `Inner`; references to
the world, chain and callbacks are then dropped. Borrowed world/source lifetime remains
with the caller. Calls require the creating managed thread and prototype host main thread.
Off-thread rejection occurs before callbacks or disposal; the owner can still use or
dispose the wrapper afterward. Main-thread disposal is idempotent.

## Exact presentation and failure policy

`Tick` validates finite seconds in0..60. While enabled it forwards one chain `Tick(0)`
then reads the state exactly once. While disabled it forwards one chain `Tick(seconds)`
and never reads the state. Thus the existing chain can preserve its earlier elapsed
presentation gates until the caller enables accepted-state presentation. This wrapper
owns no clock, does not call `Advance`, does not choose gait from speed, and does not
move an actor. `Apply` rejects an obsolete/foreign snapshot before callbacks, forwards
the current snapshot once, and presents the supplied state only while enabled.

Exact mode mapping is Idle/Walk/Run to idle/walk/run. Frame is finite and lies in
0..the selected prepared clip's EndFrame, inclusive. NormalizedPhase is finite inthe separate implementation record requires cached Python,
historical implementation path (omitted) packages and the pinned original executable/assets. It rejects wrong executable
identity and unsafe/nonfresh output roots, verifies the Round71 incoming4297-file manifest
and archive, and stages frozen historical implementation path (omitted) inputs into an ignored output tree. It runs
the the separate implementation record before
building and executing only the replacement standalone scene. No original executable
execution or full texture/animation regression is involved.

The producer reuses the accepted Round13 synthetic lifecycle/baseline producer and
authors a scalar-only Height delta. It reads pinned idle/walk/run, body skeleton and
body mesh through the effective original overlay, reuses established quaternion table
and bind-pose tooling, and independently computes six clock states and eight first-shader
body vertices per state before native execution. All source-derived output stays ignored.
Native execution reads39 original resources/4,686,371 bytes once per run and reuses that
prepared bundle across failure fixtures. Exact sample coverage is six states/48 vertices;
this does not establish whole mesh or locomotion-system coverage.

See the [Round71 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R71_ACCEPTED_LOCOMOTION_VISUAL.md)
for results and the precise next integration probe.
