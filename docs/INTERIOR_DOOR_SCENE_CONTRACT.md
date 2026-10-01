# Bounded persistent multi-door actor scene

`InteriorDoorActorScene` owns one actor and one `SingleLeafDoorRuntime` per explicitly bound physical portal geometry. It composes the existing [precise door walker](PRECISE_DOOR_ACTOR_CONTRACT.md) over caller-supplied loaded floors and one rigid building owner pose. This is modern scheduling, not a recovered original scene update order.

## Construction and ownership

Each `InteriorDoorBinding` selects a cell/portal and resolved primary leaf. Both paired references must be eligible, share style and authored pose and have an unambiguous target. Duplicate physical geometry and fallback styles reject. Every eligible paired doorway among the loaded floors requires an explicit leaf; unknown/unloaded neighbors remain query failures. Controllers are owned exclusively by the scene and shared across both incident references. Floor data is copied by the movement queries.

Limits: 64 loaded floors, 16 physical leaves, 128 paired links and 32 eligible directional link incidences. Directional barriers plus physical incidences fit the existing 64-box query budget. Positive actor half extents are at most 100 m each. Tick duration is finite in [0,1] seconds; finite motion components are at most 100 m. Existing one-transfer and optional one-step request limits remain in force.

## Tick and failures

1. Validate finite bounded inputs and reject reentrancy.
2. Sample every paired-link permission once before changing any controller. A thrown callback leaves the actor and all controllers untouched.
   Tessellated links on one physical doorway incidence must return the same permission; inconsistent values reject before mutation.
3. In geometry-index order, notify incident leaves of current proximity and advance each controller once. Both permission fields must be known and true to request opening. Actor retention is removed on the next tick after the actor leaves both incident cells, or by the existing distance rule.
4. Capture current leaf poses and apply precise movement using the sampled directional permissions. Adopt the validated locator and returned transfer count.

Reset and dispose clear every controller and retained contact. They reject calls from within permission callbacks. Public state contains immutable dictionary snapshots; an old snapshot does not change on later ticks. The scene does not own source assets or resolve network policy. Permission-callback atomicity does not imply arbitrary rollback of internal programming errors.

## Verification

[Synthetic scene gate](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_DOOR_SCENE.md) covers three cells/two leaves, repeated forward/reverse ownership, transformed owner, closed-leaf denial, open-leaf directional denial/unknown permission, retreat, stale retention, callback failure/reentrancy and reset/dispose. An [independent Python reference](../tools/python/verify_interior_door_scene.py) matches 1,440 ticks / 24 transfers / 128 blocked ticks. No original corpus or general obstacle coverage is claimed by this synthetic gate.

[Original Mustafar route](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_ORIGINAL_INTERIOR_SCENE.md) composes 5→6→7 using two pinned leaves. Independent comparison matches 2,880 ticks / 24 transfers / 172 blocked ticks / zero steps over two owner poses, including denial/unknown/retreat/recovery and controller retention. The other physical leaf is independently clear on every offered short path. This establishes one representative original composition, not arbitrary world navigation.

## 2026-09-30 shared typed scheduling

[Shared scheduler/timeline](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_TYPED_SCENE_SCHEDULING.md) preserves single-door API and complete multi-door snapshots. Three render partitions and direct 420-tick scene reference match; stall/pause/partial fault/reset/dispose/reentrancy and existing single-door native/original headless gates pass.

## 2026-09-30 original scene host

[Godot multi-door scene](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_SCENE_GODOT.md) passes headless/GPU: 16 manual transfers / two poses, second denial/retreat/reset, physical timestamped keys and diagonal round trips, typed four-transfer route, stall/pause/focus and both-leaf reflected placement. Private capture inspected.

## 2026-09-30 explicit closing safety

[Optional closing sweep](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_DOOR_CLOSING_SAFETY.md) holds an obstructed close without granting entry. Native lifecycle/event/failure/scene-retreat gates and independent 240 ticks / 52 holds pass; original unguarded 2,880-tick scene regression unchanged. Original obstruction policy remains unknown.
