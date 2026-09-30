# Original doorway: physical leaf contact before owner transfer

## Bounded behavior

An explicit original POB reference now connects the [single-leaf runtime](SINGLE_LEAF_DOOR_CONTRACT.md) to the [actor-box barrier walker](PORTAL_BARRIER_COLLISION_CONTRACT.md). The replacement loads the named leaf and both paired floors, composes the authored door transform with the supplied cell owner transform, and returns either a source-owned contact or one permitted floor transfer with remaining motion.

This is a clean offline inspection slice. It uses a conservative primary appearance box for the leaf; exact legacy narrow-phase shape and global solver ordering remain **Unknown**. Only rigid yaw owner frames and level movement are supported. Paired-controller coordination, force fields, sliding/depenetration, animated leaf collision over a timestep, general actor/world collision and free continuous gameplay remain separate.

## Original evidence and selected instance

Effective `appearance/thm_tato_cantina.pob` contains a named `cantina_door` at cell **3 / cantina**, reference **10**, targeting cell **12 / private_room**, reference **0**. Both references name geometry **13**, carry an explicit transform and permit runtime door creation. Their authored transforms match; both floor boundary edges pair within the existing 1e-4 tolerance.

| Source cell/reference | Boundary triangle/edge | Target cell/reference | Boundary triangle/edge |
|---|---|---|---|
| 3 / 10 | 97 / 1 | 12 / 0 | 20 / 1 |
| 12 / 0 | 20 / 1 | 3 / 10 | 97 / 1 |

The door translation is approximately `(-15.883702,-0.899990,14.173881)`. Its basis is approximately a 90-degree yaw: local X points toward world +Z and local Z toward world -X. Floor vertices on both sides agree in the selected POB inspection frame. Each boundary triangle is narrow; a modern inspection approach chooses projected floor points 0.8 units on either side of the authored door origin rather than using triangle centroids that could initially overlap the actor envelope. These approach points are synthetic inspection inputs, not recovered navigation behavior.

**Observation:** original POB bytes establish names, reference ordinals, flags, transforms, geometry key and paired floor locations. **Interpretation:** the selected reference can instantiate the supported original normal leaf. **Confidence:** Confirmed for bytes/pairing; original full actor ordering remains Unknown. **Implementation consequence:** keep source/target reference identity separate from global geometry keys, and compose `cellToWorld * doorToCell * displacement` rather than treating the leaf mesh as already placed in the building.

The prior executable evidence for creation eligibility, controller timing, motion curve, contact retention and permission callbacks is unchanged. This checkpoint introduces no new executable semantic names. The original executable was rehashed before work; no dynamic original-client execution occurred.

## Asset and ownership boundary

[PortalSingleLeafDoorBundleReader](../poc/Stardust.Core/Assets/PortalSingleLeafDoorBundle.cs) requires pinned POB, style-table and primary appearance identities, an eligible nonempty named reference, explicit transform flag 1, no style fallback, supported single normal leaf, both effective floors and at least one uniquely paired floor link. Missing/masked resources, identity mismatch and invalid references fail explicitly. All six effective source identities are retained by the existing bounded owned cache. No unrelated cell appearance graph is loaded.

`CreateRuntime(cellToWorld)` binds the runtime to the source cell and composes owner placement. Cell owner poses are host inputs; they are not newly inferred from the POB. Both selected floors use the same building pose in this proof. The reverse direction is validated by separately binding the opposite source reference; this does not establish legacy paired-controller synchronization.

[PortalBarrierFloorWalker](../poc/Stardust.Core/Assets/PortalBarrierFloorWalker.cs) accepts optional owner poses and source `(cell,portal)` leaf bindings. Missing owner pose entries default to identity consistently with the transfer query. Pose inputs are copied; prepared floor/barrier geometry is retained independently. Leaf runtimes are borrowed, not advanced, reset or disposed by a movement query. The host disposes them.

## Query order and outcomes

1. Capture each bound source-cell leaf's current box and pose before calling external policy callbacks.
2. Compute the bounded floor/transfer candidate using current permission decisions, cached once per directed link for this query.
3. Sweep the actor's world-axis box against permission barriers and captured physical leaf boxes. Leaf clearance is queried regardless of permission or controller fraction thresholds.
4. Select the nearest attainable source-floor contact. Equal parameters preserve permission-barrier priority. The floor candidate is a value calculation; no owner changes until the returned result is adopted by the host.
5. A physical contact returns `PortalBlocked`, zero transfers and the source floor locator, with `ContactKind=DoorLeaf`. Permission contacts retain the existing EntryDenied, PortalBlocked or PermissionUnknown outcome and `ContactKind=PermissionBarrier`.
6. If all relevant source contacts clear and policy permits, return the target floor locator and remaining movement with one transfer.

The physical collider remains unpadded; portal barriers retain recovered 0.05 padding. Actor centers are feet plus the half-height offset. Contact positions are transformed back into the source floor frame before requesting a floor locator; the result reports both local locator and world position.

Initial overlap is explicit and returns zero progress without depenetration. Nonlevel requests return `FloorStopped` with `UnsupportedNonlevelBarrierSweep`. A captured leaf contact after the source boundary, or a source landing requiring unsupported height change, conservatively retains the start locator and reports `UnsupportedSourceContactLanding`; its contact metadata remains available. No source-floor locator is invented across the portal. Generic destination-cell leaf contacts and all unbound doors remain outside this source-cell slice.

The query pose snapshot is stable even if a permission callback resets a borrowed runtime. The next query sees its new state. Callback failure commits no movement or controller change. This is owned modern evaluation policy, not proof of original solver order.

## Verified behavior and adapter

[Independent verification](../tools/python/verify_interior_door.py) reads the POB, both floors, style table, APT and mesh directly from the configured overlay; checks all six identities; recovers paired boundary edges; derives approach points and the original curve; and calculates expanded-box contacts independently. It matches **24 cases: two source references × two owner poses × six states** (closed, partial, open, denied, unknown, reset).

For the cantina-to-private-room approach, closed and 0.25 controller fraction both stop at approximately 0.297549 of the movement; partial displacement is approximately 0.264998. Fully clear motion reaches the target at parameter 1 with one transfer. Denied or unknown entry while fully open stops against the permission barrier at approximately 0.304406. These values describe this actor and approach, not a universal portal clearance threshold.

Synthetic fixtures verify initial overlap, current policy/pose snapshots, rotated/translated owners, permission callback failure, wrong owner binding, partial owner maps, nonlevel stops and contact beyond the source floor boundary. Original-input negative gates cover wrong layout/leaf pins, invalid cell, missing target floor and nonfinite approach distance. Full core smoke passes. Godot build has zero warnings/errors; headless/GPU adapter tests pass **12 control checks over two owner poses**, with reflected leaf/floor/actor placement and source/target ownership. The GPU capture was inspected. Existing YT-1300 floor/transfer/barrier controls also pass.

The [Godot adapter](../poc/Stardust.Godot/Main.InteriorDoor.cs) shows original neutral leaf geometry, both original floors, an actor envelope and explicit contact/permission/time/reset/owner controls. “Test crossing from source cell” replays the fixed inspection approach; it is not continuous player locomotion. Floor lines are transformed back through their rendered parent frame to avoid applying building placement twice. Effect playback and optional frame/textured scene presentation remain deferred. Captures stay private.

Inputs and runnable commands are in the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_DOOR_MOVEMENT.md).

## Next milestone

Replace fixed crossing replays with a persistent actor locator in this two-cell scene: advance the door from current actor contact, consume movement results into actor ownership, reverse direction through the opposite reference, and verify contact retention across repeated steps and cell changes. Establish how one visible leaf and two directional references share a modern runtime without claiming recovered legacy paired-door synchronization. Continue tracing original general collision order when it becomes a fidelity gate.


The [persistent actor follow-up](PERSISTENT_DOOR_ACTOR_CONTRACT.md) now closes repeated stepped ownership and shared modern leaf coordination. Continuous render/input scheduling remains next; original paired-controller behavior remains unresolved.
