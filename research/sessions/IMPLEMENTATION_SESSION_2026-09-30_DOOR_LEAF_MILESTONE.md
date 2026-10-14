# 2026-09-30 session: original single-leaf door milestone

Completed the [bounded door checkpoint](../checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SINGLE_LEAF_DOOR.md): strict original DTII schema, named-row fallback, pinned single-leaf loading, original displacement curve, contact retention/hysteresis and conservative physical clearance. The [durable contract](../../docs/SINGLE_LEAF_DOOR_CONTRACT.md) separates confirmed original behavior from modern policy and unresolved solver fidelity.

Independent validation matches 124 original rows and 100 motion/effect/contact frames. Core negative/lifecycle gates pass; prototype host builds without warnings and passes headless/GPU controls/pose verification. Original mesh capture was inspected and remains private. Two names bring the registry to 2,599; scores and protocol counts remain unchanged. Existing unrelated dirty work was preserved; nothing committed or staged.

## Exact resume point

Search effective POB references for a named normal single-leaf style. Resolve its authored transform and owning cell, instantiate `SingleLeafDoorRuntime`, and feed its current conservative leaf contact into the existing one-portal walker before owner transfer. Validate denied permission, closed/partial/full opening, retained contacts and owner transforms independently. This standalone adapter has not yet wired those interior instances. Trace original general collision callers before claiming original solver ordering. Paired leaves, force fields and effect playback remain deferred.
