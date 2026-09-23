# Craft customization-to-completion state timeline

Date: 2026-09-23. Target: `C:/Stardust-DEV/Stardust.exe`; current server source read-only.

## Result

The previously noted client `5` versus server `6` completion guard is a
state-publication timing difference, not an established incompatibility.

1. Client sends customization `0x15A` while its replicated PLAY crafting state
   is `4` (`0x0098C570`).
2. Stardust `CraftingSessionImplementation::customization` sends prototype and
   schematic deltas plus `0x10C` operation `0x15A`, then sets its **private
   session state** to `5`. It does not publish a PLAY crafting-state delta in
   this function.
3. Client still sees replicated PLAY state `4`, so `0x0098C570`'s non-`5`
   branch reaches `0x0098CA40`, which queues `nextCraftingStage` with a
   client counter. `0x0098DB70` reads the creature's replicated crafting-state
   accessor, not the server's private session variable.
4. Server `nextCraftingStage` in private state `5` calls `finishStage1`. That
   publishes a PLAY state-`5` delta and `0x1BE` operation `0x109`, result `4`,
   then changes private session state to `6`.
5. Client's correlated stage response accepts replicated state `5` (as mapped
   earlier); final UI action now reaches `0x0098C570` state-`5` branch and
   queues `createPrototype` or `createManfSchematic`.
6. Server `CreatePrototypeCommand` requires private state `6`; the sequence
   above satisfies that guard. The manufacture-schematic callback similarly
   acts on the active session.

## Confidence

The sender branches, server state assignments, PLAY delta construction, and
response operation/result are **confirmed by code**. This is a static
ordering model; actual network ordering under reconnection/packet loss still
requires runtime testing. The key architecture lesson is to distinguish
replicated client-visible crafting state from server-private session state.

## Replacement contract

Do not change local UI stage optimistically on `0x15A` acknowledgement alone.
Process deltas and correlated responses in order, then allow completion only
when the replicated state and response agree. Keep the `nextCraftingStage`
transition between customization and final-create command.

Evidence: client `0x0098C570`, `0x0098CA40`, `0x0098DB70`, response handler
`0x0098A750`; server `CraftingSessionImplementation.cpp:customization`,
`nextCraftingStage`, `finishStage1`, `CreatePrototypeCommand.h`.
