# Immediate full root transform application

Round65 joins the frozen [full codec](OBJECT_CONTROLLER_TRANSFORM_CODEC_CONTRACT.md), [motion store](SCENE_WORLD_OBJECT_MOTION_CONTRACT.md), [pure placement builder](SCENE_CREATE_PLACEMENT_CONTRACT.md) and current world. This is modern immediate correction policy, with no original player/remote interpolation parity claim.

```csharp
using var router = new SceneWorldFullTransformApplicationRouter(world, motions,
    SceneCreateZeroQuaternionPolicy.Identity); // Or explicitly Reject.
SceneWorldFullTransformApplicationResult result = router.Accept(generation, frame,
    ObjectControllerTransformDirection.ServerToClient, cancellationToken);
```

Construction requires owner-thread world access and an open motion store whose `IsBoundTo` proves the exact borrowed world. Foreign/closed stores reject before retaining references. Zero policy is mandatory and validated. Accept and Dispose enforce construction-thread affinity and defensive reentrancy; there are no callbacks, asynchronous work, queued bytes, engine dependencies or mutable registries in this router. One call decodes at most one exact62/70-byte application frame.

## Admission and result

Disposed calls throw before decoding; precancellation throws before decoding. Codec header/type/size/direction/finite errors propagate. Structurally valid outbound messages return WrongDirection and are never published. Stale generation, inactive world, parent type (including parent ID zero), closed store, missing object, unfinalized object and any nonzero current ContainerId decline. These paths add no motion and perform no world mutation. Snapshot selection uses the current world, never a caller-supplied historical snapshot. Unknown flags and the raw inbound scalar are preserved without gameplay meaning.

For an eligible root, signed-negative speed throws InvalidDataException; there is no abs or clamp. Negative zero is nonnegative and retains its bits. Accepted finite position XYZ and quaternion XYZW are copied into a derived create value based on the current object's raw create, solely to reuse SceneCreatePlacementBuilder. That synthetic derived create is not a new network create or world registration. The original canonical placement/raw create/replication/envelopes remain untouched. The builder scales and normalizes finite nonunit quaternions robustly, including extreme/subnormal inputs; its explicit zero policy controls identity fallback or rejection. Result.DerivedPlacement carries this modern conversion metadata. The store receives native XYZ translation, proper rigid rows, unchanged nonnegative finite speed and raw movement counter.

Before publication the router records a world batch mutation stamp, state/generation, exact current snapshot and prior motion identity, then checks the stamp and prior again. Store publication remains authoritative for canonical object/placement/envelope/current typed replication and incarnation checks, unsigned half-range counter admission, revision and capacity preflight. No batch is acknowledged. Because conversion is pure, synchronous and has no callbacks, an adversarial mid-conversion mutation is not reachable through this API; its extra StaleSnapshot defense is source-inspected rather than dynamically injected. Capacity/U64 overflow/reentrant defenses inherit their existing store contract; this milestone does not claim new dynamic overflow or callback tests.

Result.Raw retains decoded scalar values and exact outer word, so clearing caller bytes cannot alter the result. On publication, Result.Motion is the actual issued record returned by store.Find, not an equal reconstructed DTO; Result.PriorMotion is the previous issued reference. On counter rejection these two references remain the prior row, with CounterRejected status. Earlier declines provide the raw packet and available snapshot, but omit motion lookup to avoid unrelated liveness pruning; null Motion in those results does not imply the store was cleared. Applied is derived from Status. Receipts borrow immutable snapshot/placement/motion identities and can become obsolete after later state changes.

Counter rules are the existing modern store policy: first accepts any U32; subsequent forward unsigned distance1..7FFFFFFF accepts, equal/older/exact half-range rejects. Local publication retains private authoritative history. Destroy/recreate and world reset start new incarnation history; repeated resources/typed-state changes do not. Router disposal releases only its borrowed references and does not clear/dispose/close world or store.

## Reproduction and evidence

[Independent replay](../poc/round65-tests/Networking/replay.py) first handpacks full frames and computes quaternion basis rotations through vector cross products with Python math.hypot. Native coefficient matrices are not used to seed the expected values. Frozen round15 independent lifecycle fixtures provide actual world staging, typed baseline/state, containment and reincarnation. All packet fixtures are synthetic. Profile JSON supplies the accepted catalog/options only; zero resource jobs or original assets are loaded.

```powershell
python poc/round65-tests/Networking/replay.py --output-dir local-private/round65-review-replay
```

Output must be a fresh guarded ignored private directory. Offline .NET8/cache inputs follow the accepted codec replay. Native scenarios cover eight quaternion/pose cases with exact raw bytes, asymmetric XYZ, nonunit/zero/extreme/subnormal normalization, signed-zero speed/position and maximum finite speed/position. Basis error must be below2e-7. Six serial cases are checked by independent Python unsigned ordering/revision calculations; native fixtures additionally check authoritative/local interleave and prior preservation.

Native lifecycle/failure gates include wrong direction, header/type/invalid enum, every root truncation, extra bytes, negative speed, each F32 slot NaN/positive/negative infinity, explicit zero rejection, missing/unfinalized/parent/contained targets, stale generation, typed-state refresh, reincarnation/reset, idle/closed world/store, foreign world, owner-thread construction/accept/dispose, precancellation and borrowed disposal. Raw decoded values and issued motion identities are checked in actual results. The immutable world placement and typed revision/currentness remain intact; typed wrappers themselves are fresh on each snapshot and therefore are not compared by wrapper reference.

Confidence: Confirmed offline implementation/fixture behavior. Original movement timing, network reachability, remote interpolation and parent-world resolution remain outside scope. Next action: consume this explicit-direction router at a bounded host receive hook while preserving pump failure ownership and testing actual current motion application.
