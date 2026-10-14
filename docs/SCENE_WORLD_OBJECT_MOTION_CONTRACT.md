# World object motion store

R17-R, October 2, 2026. [Store](../../SWG-Godot-Client/poc/Stardust.Core/Session/SceneWorldObjectMotionStore.cs) implements the lead-frozen [motion snapshot/source](../../SWG-Godot-Client/poc/Stardust.Core/Session/ISceneWorldObjectMotionSource.cs) over the accepted [world](SCENE_MIXED_WORLD_CONTRACT.md). This is modern engine-neutral pose bookkeeping; it introduces no protocol codec, original movement-counter claim, fixed step, physics, interpolation or engine conversion.

## API and borrowed ownership

```csharp
public SceneWorldObjectMotionStore(SceneWorldCoordinator world, int maximumObjects = 64);
public bool PublishLocal(SceneWorldObjectSnapshot snapshot, AppearanceTransform transform, float speed);
public bool PublishAuthoritative(SceneWorldObjectSnapshot snapshot, AppearanceTransform transform,
    float speed, uint movementCounter);
public SceneWorldObjectMotionSnapshot? Find(SceneWorldObjectSnapshot snapshot);
public bool IsCurrent(SceneWorldObjectMotionSnapshot motion);
public void Prune();
public void Clear();
public void Dispose();
```

Capacity is 1..4096. Constructor calls world.Snapshots to verify world owner affinity and reject a busy world before retaining it. The store borrows the world: disposal releases rows/world references and never calls world.Close or drains jobs. All public operations enforce construction-thread affinity and reject store reentrancy. No callbacks are registered by the store; the reentrancy guard is defensive, while nested busy-world construction is verified by native tests.

One bounded dictionary stores immutable motion records and a private last-authoritative U32 counter per current finalized creation incarnation. Rows retain no arbitrary old SceneWorldObjectSnapshot, replication baseline, placement, envelope history, resource publication or asset reference. Motion contains only incarnation, revision, value transform/speed and optional movement-counter metadata. Caller-held obsolete motion records are outside the store's bounded ownership.

## Publication, freshness and revisions

Publish validates transform/speed and a current active finalized world snapshot before row mutation. The snapshot must expose the canonical current registry object and placement references, exact handle, current opaque envelope history and independently current typed replication when present. A copied outer snapshot with these canonical members is acceptable; altered/null members, foreign registry identity, pending phase, obsolete handle or stale typed/opaque state reject. Null top-level arguments throw. Invalid values throw; a structurally stale/foreign snapshot returns false. These paths preserve rows, private authoritative counters and the global revision.

Motion lifetime follows creation incarnation, independently of resource attempts, lifecycle handle revisions and typed-state revisions. Height/state-only and opaque-history updates preserve the stored motion. Queries/publication require a fresh world snapshot, so an old query snapshot can return null while IsCurrent on its issued motion remains true. Containment or repeated finalization can change a handle while preserving the incarnation/pose. Resource restart does not reset motion. Destroy/recreate, generation reset, pending replacement, failure and close retire old rows. Find/IsCurrent/Prune synchronize this liveness; a successful publication can reclaim stale capacity after full preflight.

Revision is **store-wide**, starts at one on the first successful publish, and advances with checked U64 arithmetic only at successful row commit. Clear, Prune, recreation and world generation changes do not reset it. This prevents adapters holding an earlier revision guard from ignoring a newly published pose after Clear. There are no per-incarnation tombstones. Invalid input, stale snapshots, rejected counters and capacity failure consume no revision. Capacity is preflighted before pruning or committing rows. U64 overflow throws before mutation; reaching it was not execution-tested because the frozen API has no seed/test hook.

Find returns the stored immutable motion record. IsCurrent requires the exact issued record reference still retained for a live finalized incarnation. Equal-valued public record clones and other-store records reject, preventing Clear/recreation from reviving obsolete records even when fields could otherwise coincide.

## Authoritative counter policy

The first authoritative publication for a row accepts any U32 counter. Subsequently use the modern serial policy: distance is unsigned `(incoming - last)` modulo 2^32. Accept only distance in 1..0x7FFFFFFF. Distance zero rejects a duplicate; exactly 0x80000000 rejects the ambiguous half-range; greater distances reject older values. Forward wrap, including FFFFFFFF to zero, is accepted. This assumes updates within half the serial range and is explicitly replacement policy, not recovered legacy timing/order.

PublishLocal emits Authoritative false and MovementCounter null, while retaining the row's private last-authoritative counter. Thus a local pose cannot make a duplicate/older subsequent authoritative update look new. Accepted authoritative input emits Authoritative true and its supplied counter. Local and authoritative acceptance both advance the same global revision. Rejected authoritative input changes neither counter nor pose. Clear explicitly removes private counter history; the next authoritative publication is first for that new row, while the store-global revision still advances. Normal resource/state updates do not clear this history.

## Native rigid values and cleanup

AppearanceTransform uses existing native column-vector affine rows and translation. All 12 components and speed must be finite; speed must be nonnegative. There is no arbitrary positional or speed cap, conversion, quaternion reconstruction or normalization. Float signed zero is preserved, including speed. Validation evaluates row squared lengths, pairwise dot products and determinant in double precision: each squared length differs from one by at most 1e-4, each dot magnitude is at most 1e-4, determinant differs from positive one by at most 1e-4. This numerical tolerance admits small float drift; visible scale, shear, collapse and reflection reject. It is not an exact mathematical rigidity or original numerical behavior claim.

Height remains independent replication and must not be baked into native motion orientation. A later engine adapter may combine pose with its separately validated Height policy; this store neither edits raw create placement nor synthesizes that adapter behavior.

Clear removes rows/counter history but retains the global revision. Prune removes only nonlive finalized incarnations. Dispose clears rows and the borrowed world reference; repeated disposal, Clear and Prune are harmless. Disposed Find returns null/IsCurrent false; publication throws ObjectDisposedException. Foreign-thread operations reject even after disposal.

[Native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/SceneWorldObjectMotionScenarios.cs) use accepted synthetic lifecycle/typed/opaque bytes, no asset reads, and an independent U64 serial-boundary model for 64 pairs. They cover counter-local interaction, rigid/invalid values, atomicity/capacity, state/resource independence, all lifecycle boundaries, clone/foreign identity, Clear revision continuity and owner/busy-world construction. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R17_OBJECT_MOTION.md) records identity/reproduction and the next adapter join.
