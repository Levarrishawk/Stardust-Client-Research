# 2026-09-30 shared typed scene scheduling

## Question and evidence

Can multi-door state use the established scheduling/input rules without a second timing implementation or fictional single-leaf state? Close after direct-tick/render partition comparison, failure/time gates and single-door regression.

Inputs are the synthetic [three-cell/two-leaf fixture](IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_DOOR_SCENE.md). New shared typed scheduler/timeline contain the prior algorithms; the existing single-door API delegates through thin facades. Scene callers keep complete typed snapshots. Concrete invalid-operation failures now latch alongside prior data/argument/disposal faults; reentrant updates/reset reject.

Native comparison passes **420 ticks** for each of three render partitions (10 ms, 50 ms and mixed 7/11/23/41 ms) and an independently indexed direct scene run. All controller snapshots, ownership and last movement match exactly; no dropped ticks. Bounded stall accounting, pause/resume, partial callback failure after two completed ticks with 60 ms discarded, no implicit retry, explicit reset, borrowed disposal and reentrant reset pass.

Existing single-door render partition, stall/pause/fault and timestamped history/order/capacity/reset gates pass unchanged. Godot build passes; pinned black-sun lower-floor headless controls, reflected placement and scheduling/input regression pass. Input SHA-256 pins remain in the [connected host checkpoint](IMPLEMENTATION_CHECKPOINT_2026-09-30_CONNECTED_DOOR_GODOT.md). No original execution or live networking.

Confidence: Confirmed modern shared scheduling and bounded regressions. Original thread/update ordering remains Unknown. See the [durable contract](../../docs/TYPED_ACTOR_SCHEDULING_CONTRACT.md).

## Next action

Load the existing pinned original three-cell/two-door fixture through a reusable asset bundle and expose a bounded Godot scene preview with both leaves, ownership, controls and typed input timeline.
