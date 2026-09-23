# Craft experimentation mediator to `0x106`

Date: 2026-09-23. Target: `C:/Stardust-DEV/Stardust.exe`; current server source read-only.

## Result

`SwgCuiCraftExperiment` activates only with an active crafting session and
subscribes to manager `Customize`, `Experiment`, and `SessionEnded` events,
plus `ClientObject::ComplexityChange` and message-box closure. The UI's
experiment action (`0x00D2BBE0`, second button) gathers row/point pairs and
calls `CuiCraftManager` sender `0x0098CF20`. That sender requires crafting
state `3`, filters out nonpositive point allocations, sums the positive
allocations, constructs the already mapped experiment record, and enqueues
ObjectController inner `0x106` with the current one-byte counter. The server
requires state `3` and runs `CraftingSession::experiment`.

The other button in `0x00D2BBE0` is a stage-advance path: with zero pending
allocation it calls `0x0098CBE0`, which queues `nextCraftingStage` using a
decimal client counter when manager stage is `3`, or publishes `Customize`
locally when already at stage `4`. A nonzero allocation takes a confirmation
path before continuing. Do not collapse experimentation and stage advancement
into one operation.

## Evidence

| Boundary | Evidence | Confidence |
|---|---|---|
| Mediator lifecycle | RTTI/vtable `0x015F1334/0x015F13B8`, activation `0x00D2B8B0`, deactivation `0x00D2B9B0` | Confirmed by executable code |
| UI row/point collection | `0x00D2BBE0` builds vector from row index and widget allocation fields, calls `0x0098CF20` | Confirmed by executable code; exact widget labels tentative |
| Experiment controller send | `0x0098CF20` checks stage `3`, filters positive values, sums total, calls `0x00B3D250(0x106,...,0x83)` | Confirmed by executable code |
| Stage advance | `0x00D2BBE0 -> 0x0098CBE0`; manager uses `nextCraftingStage` hash `0x01932B08` | Confirmed by executable code |
| Server | `CraftingExperimentCallback`/`CraftingSessionImplementation::experiment` and `NextCraftingStageCommand` | Confirmed by current Stardust source |

## Open

The presentation of attribute weights, success/failure values, and
confirmation text is not exhaustively mapped. The byte-level `0x106` payload
and generic response handling were established in earlier checkpoints.

Scratch: `local-private/session-2026-09-23-marathon/block36_experiment.txt`
and associated decompiles.
