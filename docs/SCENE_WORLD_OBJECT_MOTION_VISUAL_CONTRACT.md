# World object native motion visual

October 2, 2026. Modern presentation policy; no recovered interpolation/timing claim.

[SceneWorldObjectMotionVisual](../poc/Stardust.Godot/SceneWorldObjectMotionVisual.cs)
implements [ISceneWorldObjectPresentation](../poc/Stardust.Core/Session/ISceneWorldObjectPresentation.cs)
over an accepted [recipe visual](SCENE_WORLD_OBJECT_VISUAL_CONTRACT.md) and the frozen
[motion source](../poc/Stardust.Core/Session/ISceneWorldObjectMotionSource.cs).
There is no concrete motion-store dependency, loader, registry, network handler,
locomotion selection, interpolation or animation replacement.

## Ownership and integration

```csharp
public SceneWorldObjectMotionVisual(SceneWorldCoordinator world,
    SceneWorldObjectVisual inner, ISceneWorldObjectMotionSource motionSource);
public SceneWorldObjectVisual Inner { get; }
public ScenePreparedObjectPublication Publication { get; }
public bool IsRetired { get; }
public bool Tick(double seconds);
public bool Apply(SceneWorldObjectSnapshot snapshot);
public void Dispose();
```

The constructor validates creating/main-thread affinity, exact current world
publication/finalized snapshot, live inner, initial motion/Height and freshness
after source callback before acquiring ownership. It invokes no inner setter,
Tick or Dispose during rejection. Successful construction owns inner; world and
source are borrowed. It validates the initial pose but first writes it during
Tick/Apply. Caller supplies a fresh exclusively owned factory result. If wrapper
construction throws, caller still owns and must dispose/reuse that result.

Lead factory delegate should call the accepted factory, cast its result to
`SceneWorldObjectVisual`, wrap it with this constructor, and dispose the inner in
a constructor-failure catch. The consumer owns the returned wrapper. Existing
factory, consumer, source interface and shared entry points remain unchanged.

## Pose policy

Tick obtains the latest current object snapshot; Apply requires its supplied
snapshot to be current. Resource attempt and creation incarnation remain separate
from typed scalar/motion revisions. The source is queried on the creating Godot
main thread for that exact snapshot. Matching motion transforms convert through
the accepted `GodotAppearanceTransform` Z-reflection adapter. Creature current
Height derives once from the unscaled native pose through
`SceneCreatureStatePresentation.DeriveTransform`, never from the previous root
basis. Inner Apply retains typed visibility and lifetime guards. Static objects
receive only the rigid root pose; opaque TANO contents acquire no field semantics.

Native pose validation reuses `SceneCreatureVisual.ValidateEngineTransform` for
finite/origin/basis budgets and normalized pairwise dot tolerance1e-4. Additional
unit-axis squared-length and proper determinant tolerances1e-4 require a rigid
pose; Height owns creature scale. Finite nonnegative Speed is required but not
used to select clips. MovementCounter/Authoritative are retained metadata without
animation, interpolation or authority-policy interpretation.

Null motion, foreign incarnation or a revision lower than the last accepted
revision behaves as missing motion and restores immutable publication create
placement, plus current Height for creatures. The revision guard does not rewind
when motion disappears. A subsequent higher revision restores motion; providers
must preserve monotonic revisions across clear/republish within an incarnation.
Equal revision with changed record value throws. Only one accepted motion snapshot
is retained. There is no second motion store or source mutation.

Each Tick advances the accepted inner idle clock by the supplied finite
nonnegative seconds, then applies latest pose/state. Apply performs no elapsed
animation advance. Current invalid motion/Height, callback faults or reentry
propagates an exception after retiring owned inner and releasing borrowed
references. Wrong-thread entry rejects before mutation. Stale supplied Apply
returns false without replacing a live pose; stale resource/incarnation retires
and returns false. Dispose is owner-thread, reentry guarded and idempotent.

Raw create bytes, publication/resource identity and immutable source closures
never change. Motion performs no source reads, scheduling, resource reload or GPU
allocation. Missing motion reset is a modern policy, not an original client claim.

## Independent evidence

[Standalone original-data proof](../poc/Stardust.Godot/SceneWorldObjectMotionVisualPreview.cs)
uses an authored fake frozen source and accepted human/terminal closures. It proves
rotated/translated/reflected native poses, repeated Height1.4 without accumulating
scale, rigid static pose, missing/stale/foreign reset, newer pose after null reset,
old-incarnation motion after recreation, idle tick, immutable create/resource/GPU
identity, constructor rejection ownership, near-shear rejection, callback reentry
and invalid-current-motion cleanup. Five roots allocate/dispose, including an
ownership probe and staged factory rollback. Confidence: Confirmed modern behavior
by executed headless/GPU and independent numeric/pixel oracle.

Native independent receipts retain human39 reads/4,686,371 bytes and terminal27
reads/1,401,794 bytes, with one static reload on recreation. Real archive cancellation
closes both jobs with zero publication. Five invalid launcher cases produce no
process logs or captures. The
[launcher](../poc/round17-tests/Rendering/Launch.ps1) creates a private standalone
scene; [seven-argument oracle](../poc/round17-tests/Rendering/verify_preview.py) takes
GPU/headless/cancel logs plus initial/updated/recreated/motion PNGs. Original
render fidelity, actual motion-store implementation, protocol updates, live
networking, locomotion switching and interpolation remain outside this proof.
See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R17_MOTION_VISUAL.md).
