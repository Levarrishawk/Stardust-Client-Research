# Analysis checkpoint — Chat Rooms Who / moderation panel

Question A09 / W10+W04. Target: verified original `C:\Stardust-DEV\Stardust.exe`; current Stardust-3 source is the server-policy reference. This extends [Chat Rooms main](ANALYSIS_CHECKPOINT_2026-09-24_CHAT_ROOMS_MAIN_MEDIATOR.md) and previously mapped chat packet codecs; it does not re-count those codecs as new.

The main browser's Who callback `0x00CCA0A0` dispatches UI action string `chatRoomWho` (its static initializer is `0x00CB6990`) with the selected room path. The dedicated `SwgCuiChatRoomsWho` RTTI/vtables are `0x015F6F58`/`0x015F6ED4`; constructor `0x00FD2350` binds table and room metadata plus nine buttons: Chat Room, DeOp, Invite, Kick, OK, Op, Uninvite, Ban, Unban. It prepares separate present/invited/moderator/banned/invited+banned icons. A room-modified listener `0x00FD39E0` refreshes the table/details only for its current room ID; deactivation `0x00FD2CD0` removes the listener and closes child dialogs. Button enable state `0x00FD2FA0` depends on selected row and cached role/room checks, but does not replace server validation.

Button callback `0x00FD2DB0` reaches these distinct paths:

| UI action | Client path / result |
|---|---|
| Chat Room | dispatches `chatRoomBrowser` action with current room ID via generic action manager; exact target screen outcome is not traced here |
| DeOp / Op | `0x00FD3A80(0/1)` -> manager `0x00A2A1F0` -> existing `ChatRemoveModeratorFromRoom` / add-moderator request paths; selected identity required |
| Invite | `0x00FD3E40` opens/updates the `SwgCuiChatRoomsInvite` child, not an immediate send |
| Kick | `0x00FD3D00` -> `0x00A2A5A0` with selected other-member identity -> `ChatLeaveRoom`; current server distinguishes kick from self-leave by the authenticated player's name |
| Uninvite | `0x00FD3BC0` -> `0x00A29880(...,0)` -> `ChatUninviteFromRoom` constructor `0x00AFC990` -> reliable send |
| Ban | `0x00FD3E60` opens/updates `SwgCuiChatRoomsBan`, with selected member when present; no direct ban packet at this button |
| Unban | `0x00FD3FA0` -> `0x00A29DD0(...,0)` -> `ChatUnbanAvatarFromRoom` constructor `0x00AFD270` -> reliable send |
| OK | ordinary mediator close path |

The same manager functions' nonzero mode branches construct `ChatInviteAvatarToRoom` (`0x00AFCE00`) and `ChatBanAvatarFromRoom` (`0x00AFD6E0`), consistent with child Invite/Ban dialogs, but their submit callbacks require separate inspection. Current server registrations identify opcodes: Invite `0x7273ECD3`, Uninvite `0xFC8D01F1`, AddModerator `0x90BDE76F`, RemoveModerator `0x8A3F8E04`, Ban `0xD9FA0194`, Unban `0x4C8F94A9`, and Leave/Kick `0x493E3FFA`. Existing server chat handlers apply room type, moderator/owner, target and invitation/ban policy authoritatively.

**Outcome:** Who panel's room-state presentation and action dispatch are mapped. The Invite/Ban submit paths were closed in the subsequent [child-dialog checkpoint](ANALYSIS_CHECKPOINT_2026-09-24_CHAT_ROOMS_INVITE_BAN_SUBMIT.md); precise role-icon composition remains UI depth. No new opcode or whole-client percentage change. Confidence: **confirmed by code/source** for button routing, manager-mode branches, packet constructor names and server registrations; **strongly inferred** that `chatRoomWho` action opens this class from its exact action name and available mediator; **unknown** generic action manager's final screen transition and full per-row visual composition.

Evidence: executable `0x00CCA0A0`, `0x00CB6990`, `0x00FD2350`, `0x00FD2CD0`, `0x00FD2DB0`, `0x00FD2FA0`, `0x00FD39E0`, `0x00FD3A80`, `0x00FD3BC0`, `0x00FD3D00`, `0x00FD3E40`, `0x00FD3E60`, `0x00FD3FA0`, `0x00A29880`, `0x00A29DD0` and referenced constructors; `MMOCoreORB/src/server/zone/ZonePacketHandler.cpp`. Decompiled text is reconstructed pseudocode.
