# Item-transfer command selection (overnight block 26)

This block traces the normal online transfer helper in `Stardust.exe` to the existing Stardust server queue-command implementations. It does not claim that every drag/drop UI gesture is closed.

The client has two containment helpers: `0x00654850` requires a volume destination, while `0x00654720` requires a slotted destination and nonnegative arrangement index. In the networked branch, both call `0x00654C00`; the alternative branch performs local containment checks/mutation and is not evidence for server-visible behavior. Other callers include object/container and UI functions, while `0x00C887F0` is an explicit offline/debug inventory-population path and must not be mistaken for normal player drag/drop.

`0x00654C00` lazily hashes three command literals with `0x00AA4810`: `transferItemWeapon`, `transferItemArmor`, `transferItemMisc`. The default is Misc. If destination is the local player and has a slotted container, the helper obtains the source object's game-object type. `0x011B5A80(type, 0x100)` classifies the ARMOR family (corroborated by server `SceneObjectType::ARMOR = 0x100`) and selects Armor. Otherwise it enumerates candidate slots for the selected arrangement; presence of `hold_r` or `hold_l` selects Weapon. No qualifying slot retains Misc. This is a client command-selection policy, not a wire opcode switch.

The helper formats a Unicode argument string from **destination NetworkId, arrangement/transfer index, and three float position values** using a `%s %d %f %f %f`-shaped format. It then calls `0x0046E5F0`, which creates a command-queue entry and invokes `client_command_queue_enqueue_VA_0046D7A0`. The source item's NetworkId is the queue command target; the two trailing enqueue arguments at this call site are zero. The exact coordinate frame of the three floats remains unproved and the server currently tokenizes but does not use those values in its inspected command prefix.

The current server registers all three names in `CommandConfigManager4.cpp`. `TransferItemMiscCommand`, `TransferItemArmorCommand`, and `TransferItemWeaponCommand` parse destination U64, transfer type/index, then three floats; the target U64 identifies the item. All three abort an active trade before transfer; subsequent permission and containment checks differ by class. `TransferItemCommand` (without suffix) is registered too but its current implementation returns success without transfer, so the three selected suffix commands are materially important.

```text
container/slotted transfer helper
  -> 0x00654C00 choose Misc/Armor/Weapon and format args
  -> 0x0046E5F0 command queue -> ObjController 0x116
  -> server command-name registry -> matching TransferItem*Command
  -> validation, trade abort, authoritative containment/equipment mutation
```

Confidence: **confirmed** for literal/hash mapping (checked against x86 instructions), armor constant and `hold_r`/`hold_l` tests, queue bridge, and server parsers; **strongly inferred** for the normal online/offline distinction from branch behavior; **unknown** for the exact coordinate frame and complete user-gesture provenance. Provisional name `0x00654C00 choose_and_enqueue_transfer_item_command`.
