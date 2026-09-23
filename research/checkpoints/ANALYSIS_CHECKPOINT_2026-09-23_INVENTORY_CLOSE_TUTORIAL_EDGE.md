# Inventory close-to-tutorial response edge (overnight block 25)

This resolves the conditional send left open in block 23. In `SwgCuiInventory` deactivation `0x00C87D50`, flag `+0xA8` gates constructor `0x00AD2390`, followed by `game_network_send_VA_00462670(..., reliable=1)`. Decompilation of `0x00AD2390` embeds the exact named message `NewbieTutorialResponse`. The argument is a retained string at `0x01954CC8`; cross-references show `0x00C87260` initializing that string from literal `closeInventory` at `0x018BFDB8`, and `0x00C87D50` using it. The server's registered `NewbieTutorialResponseCallback` at opcode `0xCA88FBAD` parses the ASCII action key and, for `closeInventory`, notifies `NEWBIECLOSEINVENTORY` observers.

The cross-boundary chain is therefore:

```text
inventory mediator deactivation, armed +0xA8
  -> NewbieTutorialResponse("closeInventory") constructor 0x00AD2390
  -> reliable GameNetwork send 0x00462670
  -> server 0xCA88FBAD NewbieTutorialResponseCallback
  -> NEWBIECLOSEINVENTORY observer
```

This supplements the broader tutorial protocol checkpoint from 2026-09-15. It does not prove what arms `+0xA8` or whether it clears after this edge; repeated-close behavior remains open. It also prevents misclassifying the send as a generic inventory-close packet. Confidence: **confirmed** for constructor, literal/payload source, reliable send, opcode and server observer branch; **unknown** for arming/reset policy.
