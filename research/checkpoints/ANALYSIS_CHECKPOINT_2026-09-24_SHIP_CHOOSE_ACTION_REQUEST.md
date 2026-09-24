# `shipChoose` action to JTL ship-list request

Date: 2026-09-24. Question W10/W04: which concrete input action causes the existing ObjectController `0x41C` request? Target: verified original `C:\Stardust-DEV\Stardust.exe` SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`; current Stardust-3 source used only for server policy. Read-only disassembly/decompilation is reconstructed pseudocode.

The [HUD action inventory](ANALYSIS_CHECKPOINT_2026-09-24_HUD_ACTION_DISPATCH_INVENTORY.md) establishes `shipChoose` as ground-specific action identifier global `0x0196D0F0`. In ground dispatch `0x00E29EE0`, its exact-name branch finds the local player, gathers nearby objects, filters candidates by a returned type value `0x400B`, and compares squared world-space distances to select the nearest eligible candidate. With a valid player-controller pointer it creates a `MessageQueueGenericValueType<NetworkId>`-like object and calls `0x00B3D250` with inner controller type **`0x41C`**, scalar zero and reliability/control flag `0x83`. On no candidate it emits the embedded `You must be near a terminal...` message and does not take the send branch. The method uses the selected object's network ID, not a client-side ship ID. Candidate type `0x400B` is observed numerically; its historical enum label is not assigned here.

Current Stardust registers `JtlShipListRequestCallback` for `0x41C` in `MMOCoreORB/src/server/zone/ZonePacketHandler.cpp`. `MMOCoreORB/src/server/zone/packets/object/JtlShipListRequestCallback.h` skips the scalar, parses U64 terminal object ID, requires a player and resolved object, checks **9-unit range**, and sends `JtlShipListResponse`. Thus the end-to-end mapping is:

```text
ground HUD action shipChoose
  -> nearest candidate in local object set
  -> ObjectController C→Z inner 0x41C, U64 terminal ID
  -> server JtlShipListRequestCallback
  -> range/object validation -> JtlShipListResponse
  -> previously documented ship-list UI path
```

This closes the missing *user-action trigger* for an already documented controller codec. It does not establish that the client and server use the same terminal-type filter, nor does it prove a successful response in a live deployment. The server must remain authoritative; a replacement may present local proximity feedback but must accept server rejection. No new opcode, no new server-matrix row and no whole-client percentage change. Confidence: **Confirmed by executable** for branch, nearest selection and send type; **Confirmed by current server source** for parse/range/response; exact legacy type label and live encounter **Unknown**.
