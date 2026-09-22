# Crafting Response and Stage State Machine

Date: 2026-09-22

## Result

`CuiCraftManager` is confirmed as the client-side crafting session authority
between ObjectController codecs, replicated PLAY/MSCO/TANO state and the seven
crafting mediators. Its response handling is counter-correlated, stage-gated and
event-driven. Stale replies are deliberately discarded; successful replies
publish typed events that cause mediators to advance or close.

## Response dispatch

The shared response record is `{S32 operation, S32 result, U8 counter}`.
Different outer controller types select different handlers:

| Outer type | Operation/value | Client handler | Effect |
|---:|---:|---|---|
| `0x10C` | `0x107` fill slot | `cui_craft_receive_generic_response_VA_0098AFE0` | Resolve pending transfer by counter; require slot status `1`; display localized failure for nonzero result; clear busy state and publish `SlotTransferStatusChanged`. |
| `0x10C` | `0x108` empty slot | same | Resolve pending removal; require slot status `-1`; display failure for nonzero result; clear busy state and publish slot-transfer change. |
| `0x10C` | `0x10A` create/practice completion | same | Reject stale completion counter; close associated message box; result zero is failure, nonzero publishes successful completion path with create-item mode. |
| `0x10C` | `0x10B` manufacture-schematic completion | same | Same counter/lifetime gate; nonzero publishes completion with manufacture-schematic mode. |
| `0x10C` | `0x15A` customization acknowledgement | same | Clear pending customization; result zero advances/finishes customization, nonzero shows localized failure. |
| `0x1BE` | `0x109` assembly/stage reply | `cui_craft_receive_assembly_stage_response_VA_0098A750` | Behavior depends on pending mode 2/3/4: assembly, customize-stage finish, or final finish. Requires matching counter and expected replicated stage before publishing `StageChange` or completing. |
| `0x113` | `0x105` experimentation reply | `cui_craft_receive_experiment_response_VA_0098ACF0` | Requires matching experiment counter; accepts result range `-1..8`, applies result and publishes `Experiment`; stale response publishes neutral/zero event and is discarded. |
| `0x102` | draft schematic list | draft-schematic handler plus manager cache | Publishes `DraftSchematicsReceived`; stale list responses are explicitly discarded. |
| draft-slot response | slot list | `cui_craft_receive_draft_slots_VA_0098A3F0` | Requires the outstanding request token, installs slot data, and publishes `DraftSlotsReceived`; stale responses are discarded. |

Additional generic operations `0x10F` and `0x111` are recognized by the client
for empty-hopper/restart-era flows. Current Stardust's principal session path
shown in `CraftingSessionImplementation` emits `0x107..0x10B`, `0x105`, and
`0x15A`; the extra operations remain compatibility branches.

## Stage policy

- Pending mode `2` means assembly. After a correlated `0x109`, the manager
  expects replicated stage 3 when experimentation is available, otherwise stage
  4. It applies the assembly result, handles special result 8 through a separate
  branch, then publishes the observed stage.
- Pending mode `3` means the customization transition. The client requires
  replicated stage 4 and response result 4 before publishing the customization
  stage.
- Pending mode `4` means final finish. The client requires replicated stage 5
  and result 4, then runs the completion/close path.
- Experiment results are a closed numeric range `-1..8` at the client boundary.
  Values outside that range are diagnosed and ignored. The exact localized
  label for every numeric result remains a presentation enum task.
- Every pending family owns a distinct `U8` counter. A numerically valid reply
  with the wrong counter is stale and must not mutate the current session.

## Replication interaction

Controller replies do not carry the full new crafting model. Stardust sends
PLAY:9, MSCO:3/7 and TANO:3 deltas before or around the acknowledgement. The
manager checks the resulting replicated crafting stage before it publishes a UI
transition. This ordering is material: applying the acknowledgement without
the corresponding replicated state can advance the UI into an invalid screen.

The replacement protocol layer should therefore queue decoded network events
in receive order, apply baselines/deltas to the session model, and only then let
the crafting coordinator evaluate the correlated acknowledgement.

## Replacement state model

Use explicit state and pending-operation records rather than embedding behavior
in UI nodes:

`Idle -> DraftList -> Assembly -> ExperimentOrCustomize -> Customize -> Finish -> Completed/Cancelled`

Each pending operation stores `{kind, counter, sessionGeneration, affectedSlot}`.
Responses must match kind and counter; a session-generation mismatch should be
treated exactly like the legacy stale-response discard. UI mediators subscribe
to immutable events (`DraftSchematicsReceived`, `DraftSlotsReceived`,
`SlotTransferStatusChanged`, `StageChange`, `Experiment`, `Customize`,
`Complete`, `SessionEnded`).

## Confidence

- Counter gates, pending modes, expected replicated stages, result ranges and
  typed publications: **Confirmed by code**.
- Current server operation IDs and send ordering: **Confirmed by current
  Stardust source**.
- Exact historical labels for all assembly/experiment numeric results and the
  dormant `0x10F/0x111` branches: **Partially known / non-blocking for codec,
  still required for complete presentation parity**.

## Evidence

- `0x0098A3F0`, `0x0098A750`, `0x0098ACF0`, `0x0098AFE0`, `0x0098CBE0`,
  `0x0098CF20`, `0x0099DF50`
- RTTI transceivers for all `CuiCraftManager::Messages::*` event families
- `CraftingSessionImplementation.cpp` controller/delta send order
