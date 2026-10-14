# 2026-09-30 original multi-door prototype host host

## Question and inputs

Does the original three-cell/two-leaf scene render and retain shared timing/input/lifecycle semantics? Close after controls, owner placement, timestamped route, physical keys and headless/GPU gates. POB/specification/table/leaf pins are unchanged from the [original scene](IMPLEMENTATION_CHECKPOINT_2026-09-30_ORIGINAL_INTERIOR_SCENE.md) and [bundle](IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_SCENE_BUNDLE.md) checkpoints. Tool: prototype host 4.7.2 .NET x64 and bounded offline preview launcher.

## Confirmed results

Headless and GPU runs pass **16 manual transfers over two owner poses**, second-door denial/retreat, complete reset, actual physical timestamped key submission/cancellation/release and normalized diagonals, two-pose diagonal owner round trips, a typed timestamped route with four transfers, bounded stall/pause/focus and all-leaf/actor/floor reflected placement. Both primary leaf transforms update from independent owned controller snapshots. Exit clears scene ownership and releases resources.

Private capture inspected: both leaves, route floors, actor and current owner are visible and consistent with controller states. The transformed-owner checkbox now matches the final fixture owner; wrapped route title leaves more geometry visible. Known nonfatal certificate-store diagnostic persists; build zero warnings/errors.

Confidence: Confirmed bounded host composition. Full obstacle meshes and moving closing-leaf collision response remain Unknown. No original executable/server execution, new VA labels, protocol rows or research scores.

## Next probe

Define and test an explicit modern policy for a closing leaf sweeping into a stationary actor. Preserve denied entry while safely holding an obstructed close; verify controller events and teardown without changing the historical default.
