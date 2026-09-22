# Combat Queue UI Checkpoint

Date: 2026-09-22  
Target: `C:\Stardust-DEV\Stardust.exe`

## Finding

`SwgCuiCombatQueue` is a live view and cancellation surface over
`ClientCommandQueue`; it does not maintain a second authoritative combat queue.

## Evidence

RTTI confirms subscriptions to:

- `ClientCommandQueue::Messages::Added`, carrying sequence plus queue entry;
- the queue's typed removing payload;
- `CreatureObject::Messages::StatesChanged`.

Constructor `0x00F503A0` binds the queue volume, sample item, clear button,
peace/attack toggle and target-name text. It loads localized attack strings and
initializes the list from the client command queue.

The refresh path resolves queued sequence IDs back to `ClientCommandQueueEntry`
records, builds visible rows, updates progress and target presentation, and
warns on stale/bad sequence IDs. State changes govern whether the peace/attack
control and mediator are visible.

## Cancellation

The clear path `0x00F517B0` enumerates the visible/eligible sequence IDs and
calls `0x0046E650` for each. That helper is now confirmed to:

1. call `command_queue_cancel_send_VA_0062CBD0(sequenceId)`;
2. immediately call
   `client_command_queue_remove_apply_VA_0046E720(sequenceId,0,0,0)`.

Thus cancellation is optimistic locally and also sent to the server. The
existing command-queue result path later reconciles authoritative outcomes.
Individual row progress can be forced to complete during teardown/removal so
the visual transition finishes without retaining a stale queue entry.

## Replacement requirements

- Keep the sequence ID as a fixed-width protocol identifier, not a pointer.
- Expose one observable command-queue model to both combat UI and other command
  consumers.
- On cancel, send the established cancellation message and transition the local
  view optimistically, but tolerate server results arriving concurrently.
- Key UI rows by sequence ID and make add/remove/result handling idempotent.
- Derive posture/combat-state presentation from replicated creature state.

The original queue icons, strings and widget assets are presentation inputs;
the legacy linked lists, tree containers and mediator pointers should be
redesigned.

## Confidence

Construction, event sources, sequence-keyed projection and the exact
send-plus-local-remove cancellation boundary are **confirmed**. Some animation
timings and visual row fields remain fidelity details.

