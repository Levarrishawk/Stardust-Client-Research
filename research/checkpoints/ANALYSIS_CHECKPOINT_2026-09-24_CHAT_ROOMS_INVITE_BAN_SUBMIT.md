# Analysis checkpoint — Chat Rooms Invite and Ban submit dialogs

Question A09 / W10+W04. Target: verified original `C:\Stardust-DEV\Stardust.exe`; current Stardust-3 source is used for packet registration/policy. This closes the explicit child-dialog submit gap from the [Who panel](ANALYSIS_CHECKPOINT_2026-09-24_CHAT_ROOMS_WHO_MODERATION_PANEL.md), not the already-mapped wire codecs.

`SwgCuiChatRoomsBan` constructor `0x010A7F40` and `SwgCuiChatRoomsInvite` constructor `0x010A8D40` have parallel controls: OK, Cancel, target `textbox`, and `textRoom`. The Who panel opens/prepopulates them through `0x010A8450` (Ban) and `0x010A9290` (Invite). Missing required widgets use a fatal UI-data configuration path. Both dialogs attach callbacks on activation and detach on deactivation; no request is sent by merely opening one.

| Dialog event | Verified path | Failure/success behavior |
|---|---|---|
| Ban OK | `0x010A8200` reads target textbox, normalizes/constructs ChatAvatarId through `0x009E91C0`, passes retained room ID and mode 1 to `0x00A29DD0`, which constructs `ChatBanAvatarFromRoom` at `0x00AFD6E0` and sends reliably. | Local manager failure shows its diagnostic and keeps dialog; successful send closes dialog. The server's room/moderator/target checks are authoritative. |
| Invite OK | `0x010A9000` performs the parallel target conversion and calls `0x00A29880(...,1)`, which constructs `ChatInviteAvatarToRoom` at `0x00AFCE00` and sends reliably. | Same local-failure/close-on-success pattern; server validates private room, authority and target. |
| Either Cancel | respective button callback calls ordinary mediator close without an outgoing request. | No target mutation. |

The current server registers `ChatBanFromRoomCallback` as `0xD9FA0194` and `ChatInviteToRoomCallback` as `0x7273ECD3`. Both callbacks parse target ChatAvatarId, room path and U32 request ID and then invoke current ChatManager policy. The UI's target text is not a server authorization token, and sending is not a successful ban/invite until a result arrives. The request-correlated result handlers were already covered in prior chat protocol work.

**Outcome:** Ban and Invite child submit paths are now connected to their concrete packet constructors; the Who panel's core moderation action chain is bounded. Exact target-name normalization rules, dialog visuals and asynchronous result presentation remain optional UI depth. No whole-client percentage change.

Evidence: executable RTTI/vtables, `0x010A7F40`, `0x010A8200`, `0x010A8D40`, `0x010A9000`, `0x010A8450`, `0x010A9290`, `0x00A29DD0`, `0x00A29880`, `0x00AFD6E0`, `0x00AFCE00`; server `MMOCoreORB/src/server/zone/ZonePacketHandler.cpp` and corresponding chat callbacks/manager. Decompiled text is reconstructed pseudocode. Confidence: **confirmed by code/source** for UI event→manager→packet send and server registration; **unknown** exact local target-name normalization and final display timing.
