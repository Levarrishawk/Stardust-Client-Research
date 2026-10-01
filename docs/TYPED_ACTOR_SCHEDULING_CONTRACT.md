# Shared typed actor scheduling

`FixedStepActorHost<TState>` borrows state/tick/reset delegates and preserves the existing [fixed-step contract](FIXED_STEP_ACTOR_CONTRACT.md): integer microseconds, 20 ms default ticks, eight default catch-up steps, whole-tick drop accounting and explicit suspension. `PortalActorInputTimeline<TState>` preserves the [timestamped input contract](ACTOR_INPUT_TIMELINE_CONTRACT.md), including first eligible tick-start application, bounded nonlate queue and skipped-history consumption.

The historical single-door classes are thin compatibility facades over these shared implementations. Their public state remains `PortalDoorActorState`; multi-door callers retain `InteriorDoorActorSceneState` with all physical leaves. No artificial selected leaf is inserted into scene state.

Argument, invalid-data and invalid-operation failures (including disposal) latch the concrete fault type, retain completed ticks and discard uncompleted time. Later updates do not retry until explicit reset. Updates and reset cannot reenter. The scheduler does not own or dispose its simulation. Scene permission callbacks complete before controller mutation, so a callback failure commits only earlier successful ticks.

[Scene scheduling gate](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_TYPED_SCENE_SCHEDULING.md) compares all controllers/ownership across three render partitions and an independently indexed 420-tick direct reference. Scene stall, pause, two completed ticks before callback failure, reset/dispose and reentrancy pass. Existing single-door scheduling/input tests and a pinned original Godot headless regression pass. Render partition invariance requires no catch-up drops and the same timestamped input history.
